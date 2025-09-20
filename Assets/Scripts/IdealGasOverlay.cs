using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using TMPro;
using NUnit.Framework.Constraints;

public class IdealGasOverlay : MonoBehaviour
{
    [SerializeField] List<TMP_InputField> inputs = new List<TMP_InputField>(); // in the order: P, V, n, T
    [SerializeField] TMP_Dropdown unitDrop; // same order as R constant values
    readonly List<string> rConstVals = new List<string>() { "0.821", "8.314", "62.36" }; // same order as unit dropdown
    [SerializeField] ResultLocker rConstText;
    [SerializeField] ResultLocker resultText;
    const string finalRegexString = @"^(?:(?:(?:[0-9]+)|(?:[0-9]+\.|(?:\.[0-9]+))[0-9]*))(?:x10\^\-?[1-9][0-9]*)?$";
    Regex finalRegex;
    readonly List<int> inputIndexes = new List<int>();
    IndexFIFO setInputs;
    Warning warning;

    void Awake()
    {
        warning = FindFirstObjectByType<Warning>();
    }

    void Start()
    {
        finalRegex = new Regex(finalRegexString);
        for (int i = 0; i < inputs.Count; i++)
        {
            int index = i;
            inputs[i].onValueChanged.AddListener(delegate { InputChange(index); });
            inputs[i].onEndEdit.AddListener(delegate { EditEnded(index); });
            inputIndexes.Add(i);
        }
        unitDrop.onValueChanged.AddListener(delegate { UnitChanged(); });
        setInputs = new IndexFIFO(inputs.Count - 1);
    }

    void UnitChanged()
    {
        rConstText.SetText(rConstVals[unitDrop.value]);
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

    public IdealGasData CalculateIdealGas(IdealGasData input, int solveIndex)
    {
        IdealGasData output = input;
        switch (solveIndex)
        {
            case 0:
                output = SolvePres(input);
                break;
            case 1:
                output = SolveVol(input);
                break;
            case 2:
                output = SolveMol(input);
                break;
            case 3:
                output = SolveTemp(input);
                break;
        }
        return output;
    }

    IdealGasData SolvePres(IdealGasData input)
    {
        string equation = $"(({input.mol})x({input.rVal})x({input.temp}))/({input.vol})";
        string result = EquationHandler.SolveEquation(equation, true);
        input.pres = result;
        return input;
    }

    IdealGasData SolveVol(IdealGasData input)
    {
        string equation = $"(({input.mol})x({input.rVal})x({input.temp}))/({input.pres})";
        string result = EquationHandler.SolveEquation(equation, true);
        input.vol = result;
        return input;
    }

    IdealGasData SolveMol(IdealGasData input)
    {
        string equation = $"(({input.pres})x({input.vol}))/(({input.rVal})x({input.temp}))";
        string result = EquationHandler.SolveEquation(equation, true);
        input.mol = result;
        return input;
    }

    IdealGasData SolveTemp(IdealGasData input)
    {
        string equation = $"(({input.pres})x({input.vol}))/(({input.mol})x({input.rVal}))";
        string result = EquationHandler.SolveEquation(equation, true);
        input.temp = result;
        return input;
    }

    void HandleIdealGas()
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
                if (!(inputs[i].text == "?" || finalRegex.IsMatch(inputs[i].text)))
                {
                    showWarning = true;
                    warningText += GetLabel(i);
                    break;
                }
            }
        }
        if (showWarning)
        {
            warning.ShowWarning(warningText);
        }
        else
        {
            int solveIndex = setInputs.FindMissingIndex(inputIndexes);
            IdealGasData input = new IdealGasData(inputs[0].text, inputs[1].text, inputs[2].text, rConstText.GetText(), inputs[3].text);
            IdealGasData result = CalculateIdealGas(input, solveIndex);
            string output = $"Result: {GetLabel(solveIndex)}: ";
            switch (solveIndex)
            {
                case 0:
                    output += $"{result.pres} {unitDrop.options[unitDrop.value].text}";
                    break;
                case 1:
                    output += $"{result.vol} L";
                    break;
                case 2:
                    output += $"{result.mol} moles";
                    break;
                case 3:
                    output += $"{result.temp} K";
                    break;
            }
            resultText.SetText(output);
        }
    }

    string GetLabel(int index)
    {
        string output = "";
        switch(index)
        {
            case 0:
                output = "Pressure";
                break;
            case 1:
                output = "Volume";
                break;
            case 2:
                output = "Moles";
                break;
            case 3:
                output = "Temp";
                break;
        }
        return output;
    }

    public void Calculate()
    {
        if (!warning.GetWarningVisible())
        {
            HandleIdealGas();
        }
    }
}

public class IdealGasData
{
    public string pres;
    public string vol;
    public string mol;
    public string rVal;
    public string temp;

    public IdealGasData(string pres, string vol, string mol, string rVal, string temp)
    {
        this.pres = pres;
        this.vol = vol;
        this.mol = mol;
        this.rVal = rVal;
        this.temp = temp;
    }
}
