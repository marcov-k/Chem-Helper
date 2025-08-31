using UnityEngine;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using TMPro;

public class TempChOverlay : MonoBehaviour
{
    [SerializeField] List<TMP_InputField> inputs = new List<TMP_InputField>(); // in the order: energy, mass, Cp, temp. change
    [SerializeField] ResultLocker resultText;
    string posRegexString = @"^(?:(?:(?:[0-9]+)|(?:[0-9]+\.|(?:\.[0-9]+))[0-9]*))(?:x10\^\-?[1-9][0-9]*)?$";
    string negRegexString = @"^(?:\-?(?:(?:[0-9]+)|(?:[0-9]+\.|(?:\.[0-9]+))[0-9]*))(?:x10\^\-?[1-9][0-9]*)?$";
    string zeroRegexString = @"^\-?0+\.?0*(?:x10\^\-?[0-9]*)?$";
    Regex posFinalRegex;
    Regex negFinalRegex;
    Regex zeroRegex;
    List<int> inputIndexes = new List<int>();
    IndexFIFO setInputs;
    Warning warning;

    void Awake()
    {
        warning = FindFirstObjectByType<Warning>();
    }

    void Start()
    {
        posFinalRegex = new Regex(posRegexString);
        negFinalRegex = new Regex(negRegexString);
        zeroRegex = new Regex(zeroRegexString);
        for (int i = 0; i < inputs.Count; i ++)
        {
            int index = i;
            inputs[i].onValueChanged.AddListener(delegate { InputChange(index); });
            inputs[i].onEndEdit.AddListener(delegate { EditEnded(index); });
            inputIndexes.Add(i);
        }
        setInputs = new IndexFIFO(inputs.Count - 1);
    }

    void InputChange(int changedIndex)
    {
        if (inputs[changedIndex].text != "?" && inputs[changedIndex].text != "")
        {
            setInputs.AddIndex(changedIndex);
            int changeIndex;
            if (setInputs.indexes.Count == inputIndexes.Count - 1)
            {
                changeIndex = setInputs.FindMissingIndex(inputIndexes);
                inputs[changeIndex].text = "?";
            }
        }
        else if (inputs[changedIndex].text == "")
        {
            int blankIndex = setInputs.FindMissingIndex(inputIndexes);
            if (blankIndex != changedIndex)
            {
                inputs[blankIndex].text = "";
                setInputs.RemoveIndex(changedIndex);
            }
        }
    }

    void EditEnded(int endedIndex)
    {
        if (setInputs.indexes.Count == inputs.Count - 1)
        {
            int blankIndex = setInputs.FindMissingIndex(inputIndexes);
            if (inputs[endedIndex].text == "" && endedIndex == blankIndex)
            {
                inputs[endedIndex].text = "?";
            }
        }
    }

    public string CalculateTempCh(TempChData data, int solveIndex)
    {
        string result = "";
        switch (solveIndex)
        {
            case 0:
                result = SolveEnergy(data);
                break;
            case 1:
                result = SolveMass(data);
                break;
            case 2:
                result = SolveCp(data);
                break;
            case 3:
                result = SolveTempCh(data);
                break;

        }
        string output = result;
        return output;
    }

    string SolveEnergy(TempChData input)
    {
        string equation = $"({input.mass})x({input.cp})x({input.tempCh})";
        string result = EquationHandler.SolveEquation(equation, true);
        string output = result;
        return output;
    }

    string SolveMass(TempChData input)
    {
        string equation = $"({input.energy})/(({input.cp})x({input.tempCh}))";
        string result = EquationHandler.SolveEquation(equation, true);
        string output = result;
        return output;
    }

    string SolveCp(TempChData input)
    {
        string equation = $"({input.energy})/(({input.mass})x({input.tempCh}))";
        string result = EquationHandler.SolveEquation(equation, true);
        string output = result;
        return output;
    }

    string SolveTempCh(TempChData input)
    {
        string equation = $"({input.energy})/(({input.mass})x({input.cp}))";
        string result = EquationHandler.SolveEquation(equation, true);
        string output = result;
        return output;
    }

    void HandleTempCh()
    {
        string warningText = "Invalid: ";
        bool showWarning = false;
        if (setInputs.indexes.Count < inputs.Count - 1)
        {
            warningText += "Missing Input";
            showWarning = true;
        }
        else
        {
            for (int i = 0; i < inputs.Count; i++)
            {
                if (inputs[i].text != "?")
                {
                    if (zeroRegex.IsMatch(inputs[i].text))
                    {
                        warningText += DetermineLabel(i) + "Input Number";
                        showWarning = true;
                        break;
                    }
                    else if (i == 1 || i == 2)
                    {
                        if (!posFinalRegex.IsMatch(inputs[i].text))
                        {
                            warningText += DetermineLabel(i) + "Input Number";
                            showWarning = true;
                            break;
                        }
                    }
                    else
                    {
                        if (!negFinalRegex.IsMatch(inputs[i].text))
                        {
                            warningText += DetermineLabel(i) + "Input Number";
                            showWarning = true;
                            break;
                        }
                    }
                }
            }
        }
        if (showWarning)
        {
            warning.ShowWarning(warningText);
        }
        else
        {
            TempChData data = new TempChData(inputs[0].text, inputs[1].text, inputs[2].text, inputs[3].text);
            string result = "Result: ";
            int index = setInputs.FindMissingIndex(inputIndexes);
            string output = CalculateTempCh(data, index);
            if ((index == 0 || index == 3) && !output.Contains('-'))
            {
                output = output.Insert(0, "+");
            }
            switch (index)
            {
                case 0:
                    result += $"Energy: {output} J";
                    break;
                case 1:
                    result += $"Mass: {output} g";
                    break;
                case 2:
                    result += $"Specific Heat: {output} J/g°C";
                    break;
                case 3:
                    result += $"Temp. Change: {output}°C";
                    break;
            }
            resultText.SetText(result);
        }
    }

    string DetermineLabel(int index)
    {
        string output = "";
        switch (index)
        {
            case 0:
                output = "Energy ";
                break;
            case 1:
                output = "Mass ";
                break;
            case 2:
                output = "Specific Heat ";
                break;
            case 3:
                output = "Temp. Change ";
                break;
        }
        return output;
    }    

    public void Calculate()
    {
        if (!warning.GetWarningVisible())
        {
            HandleTempCh();
        }
    }
}

public class TempChData
{
    public string energy;
    public string mass;
    public string cp;
    public string tempCh;

    public TempChData(string energy, string mass, string cp, string tempCh)
    {
        this.energy = energy;
        this.mass = mass;
        this.cp = cp;
        this.tempCh = tempCh;
    }
}
