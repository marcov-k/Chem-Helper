using UnityEngine;
using System;
using System.Text.RegularExpressions;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class LightOverlay : MonoBehaviour
{
    [SerializeField] List<TMP_InputField> inputs = new List<TMP_InputField>(); // in the order: wave, freq1, energy, freq2
    [SerializeField] List<TMP_InputField> freqInputs = new List<TMP_InputField>();
    [SerializeField] ResultLocker waveResultText;
    [SerializeField] ResultLocker freqResultText;
    [SerializeField] ResultLocker energyResultText;
    const string finalRegexString = @"^(?:(?:(?:[0-9]+)|(?:[0-9]+\.|(?:\.[0-9]+))[0-9]*))(?:x10\^\-?[1-9][0-9]*)?$";
    const string zeroRegexString = @"^\-?0+\.?0*(?:x10\^\-?[0-9]*)?$";
    Regex finalRegex;
    Regex zeroRegex;
    readonly List<int> inputIndexes = new List<int>();
    readonly List<int> freqIndexes = new List<int>();
    IndexFIFO setInputs;
    IndexFIFO setFreqs;
    Warning warning;
    const string speed = "3.0x10^8";
    const string planck = "6.626x10^-34";

    void Awake()
    {
        warning = FindFirstObjectByType<Warning>();
    }

    void Start()
    {
        finalRegex = new Regex(finalRegexString);
        zeroRegex = new Regex(zeroRegexString);
        for (int i = 0; i < freqInputs.Count; i++)
        {
            int index = i;
            freqInputs[i].onValueChanged.AddListener(delegate { SyncFreq(index); });
            freqIndexes.Add(i);
        }
        for (int i = 0; i < inputs.Count; i++)
        {
            int index = i;
            inputs[i].onValueChanged.AddListener(delegate { InputChange(index); });
            inputIndexes.Add(i);
        }
        setInputs = new IndexFIFO(1);
        setFreqs = new IndexFIFO(1);
    }

    void InputChange(int changedIndex)
    {
        if (inputs[changedIndex].text != "?" && inputs[changedIndex].text != "")
        {
            setInputs.AddIndex(changedIndex);
            List<int> changedIndexes = new List<int>();
            bool freqChanged = false;
            if (changedIndex == 1 || changedIndex == 3)
            {
                freqChanged = true;
            }
            if (setInputs.indexes.Count == 1)
            {
                changedIndexes = setInputs.FindAllMissingIndexes(inputIndexes);
                foreach (int index in changedIndexes)
                {
                    if (freqChanged)
                    {
                        if (index != 1 && index != 3)
                        {
                            inputs[index].text = "?";
                        }
                    }
                    else
                    {
                        inputs[index].text = "?";
                    }
                }
            }
        }
        else if (inputs[changedIndex].text == "")
        {
            List<int> blankIndexes = setInputs.FindAllMissingIndexes(inputIndexes);
            foreach (int index in blankIndexes)
            {
                if (index != changedIndex)
                {
                    inputs[index].text = "";
                }
            }
            setInputs.RemoveIndex(changedIndex);
        }
    }

    void SyncFreq(int index)
    {
        setFreqs.AddIndex(index);
        int syncIndex = setFreqs.FindMissingIndex(freqIndexes);
        if (freqInputs[syncIndex].text != freqInputs[index].text)
        {
            freqInputs[syncIndex].text = freqInputs[index].text;
        }
    }

    public LightData CalculateLight(LightData data)
    {
        string wave = EquationHandler.RemoveCharacter(data.wavelength, "?");
        string freq = EquationHandler.RemoveCharacter(data.frequency, "?");
        string energy = EquationHandler.RemoveCharacter(data.energy, "?");
        if (wave != "")
        {
            freq = SolveFreqFromWave(wave);
            energy = SolveEnergy(freq);
        }
        else if (freq != "")
        {
            wave = SolveWave(freq);
            energy = SolveEnergy(freq);
        }
        else if (energy != "")
        {
            freq = SolveFreqFromEnergy(energy);
            wave = SolveWave(freq);
        }
        if (EquationHandler.OperationCount(wave) < 1)
        {
            wave = EquationHandler.SigFigSciNota(wave, EquationHandler.SigFigCount(wave));
        }
        if (EquationHandler.OperationCount(freq) < 1)
        {
            freq = EquationHandler.SigFigSciNota(freq, EquationHandler.SigFigCount(freq));
        }
        if (EquationHandler.OperationCount(energy) < 1)
        {
            energy = EquationHandler.SigFigSciNota(energy, EquationHandler.SigFigCount(energy));
        }
        LightData output = new LightData(wave, freq, energy);
        return output;
    }

    string SolveWave(string freq)
    {
        string equation = $"({speed})/({freq})";
        string output = EquationHandler.SolveEquation(equation, true, EquationHandler.SigFigCount(freq));
        return output;
    }

    string SolveFreqFromWave(string wave)
    {
        string equation = $"({speed})/({wave})";
        string output = EquationHandler.SolveEquation(equation, true, EquationHandler.SigFigCount(wave));
        return output;
    }

    string SolveFreqFromEnergy(string energy)
    {
        string equation = $"({energy})/({planck})";
        string output = EquationHandler.SolveEquation(equation, true, EquationHandler.SigFigCount(energy));
        return output;
    }

    string SolveEnergy(string freq)
    {
        string equation = $"({planck})x({freq})";
        string output = EquationHandler.SolveEquation(equation, true, EquationHandler.SigFigCount(freq));
        return output;
    }

    void HandleLight()
    {
        string warningText = "Invalid: ";
        bool showWarning = false;
        int setIndex = -1;
        if (setInputs.indexes.Count < 1)
        {
            warningText += "Missing Input";
            showWarning = true;
        }
        else
        {
            setIndex = setInputs.indexes[0];
            if (zeroRegex.IsMatch(inputs[setIndex].text) || !finalRegex.IsMatch(inputs[setIndex].text))
            {
                switch (setIndex)
                {
                    case 0:
                        warningText += "Wavelength";
                        break;
                    case 1:
                    case 3:
                        warningText += "Frequency";
                        break;
                    case 2:
                        warningText += "Energy";
                        break;
                }
                warningText += " Input Number";
                showWarning = true;
            }
        }
        if (showWarning)
        {
            warning.ShowWarning(warningText);
        }
        else
        {
            LightData data = new LightData(inputs[0].text, inputs[1].text, inputs[2].text);
            LightData result = CalculateLight(data);
            waveResultText.SetText("Wavelength (λ): " + result.wavelength + " m");
            freqResultText.SetText("Frequency (v): " + result.frequency + " Hz");
            energyResultText.SetText("Energy (E): " + result.energy + " J");
        }
    }

    public void Calculate()
    {
        if (!warning.GetWarningVisible())
        {
            HandleLight();
        }
    }

    public class LightData
    {
        public string wavelength;
        public string frequency;
        public string energy;

        public LightData(string wavelength, string frequency, string energy)
        {
            this.wavelength = wavelength;
            this.frequency = frequency;
            this.energy = energy;
        }
    }
}
