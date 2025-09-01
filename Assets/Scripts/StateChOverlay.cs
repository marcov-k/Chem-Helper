using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using TMPro;

public class StateChOverlay : MonoBehaviour
{
    [SerializeField] List<TMP_InputField> inputs = new List<TMP_InputField>(); // in the order: energy, moles, HoF/V
    [SerializeField] ResultLocker resultText;
    const string posFinalRegexString = @"^(?:(?:(?:[0-9]+)|(?:[0-9]+\.|(?:\.[0-9]+))[0-9]*))(?:x10\^\-?[1-9][0-9]*)?$";
    const string negFinalRegexString = @"^(?:\-?(?:(?:[0-9]+)|(?:[0-9]+\.|(?:\.[0-9]+))[0-9]*))(?:x10\^\-?[1-9][0-9]*)?$";
    const string zeroRegexString = @"^\-?0+\.?0*(?:x10\^\-?[0-9]*)?$";
    Regex posFinalRegex;
    Regex negFinalRegex;
    Regex zeroRegex;
    readonly List<int> inputIndexes = new List<int>();
    IndexFIFO setInputs;
    Warning warning;

    void Awake()
    {
        warning = FindFirstObjectByType<Warning>();
    }

    void Start()
    {
        posFinalRegex = new Regex(posFinalRegexString);
        negFinalRegex = new Regex(negFinalRegexString);
        zeroRegex = new Regex(zeroRegexString);
        for (int i = 0; i < inputs.Count; i++)
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

    public string CalculateStateCh(StateChData data, int solveIndex)
    {
        string result = "";
        switch (solveIndex)
        {
            case 0:
                result = SolveEnergy(data);
                break;
            case 1:
                result = SolveMoles(data);
                break;
            case 2:
                result = SolveHeat(data);
                break;
        }
        string output = result;
        return output;
    }

    string SolveEnergy(StateChData input)
    {
        string heat = EquationHandler.SolveEquation($"{input.heat}x1000", false);
        string equation = $"({input.moles})x({heat})";
        string result = EquationHandler.SolveEquation(equation, true);
        string output = result;
        return output;
    }

    string SolveMoles(StateChData input)
    {
        string heat = EquationHandler.SolveEquation($"{input.heat}x1000", false);
        string equation = $"({input.energy})/({heat})";
        string result = EquationHandler.SolveEquation(equation, true);
        string output = result;
        return output;
    }

    string SolveHeat(StateChData input)
    {
        string equation = $"({input.energy})/({input.moles})";
        string result = EquationHandler.SolveEquation(equation, true);
        result = EquationHandler.SolveEquation($"{result}/1000", false);
        string output = result;
        return output;
    }

    void HandleStateCh()
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
                        warningText += DetermineLabel(i) + " Input Number";
                        showWarning = true;
                        break;
                    }
                    else if (i == 1)
                    {
                        if (!posFinalRegex.IsMatch(inputs[i].text))
                        {
                            warningText += DetermineLabel(i) + " Input Number";
                            showWarning = true;
                            break;
                        }
                    }
                    else
                    {
                        if (!negFinalRegex.IsMatch(inputs[i].text))
                        {
                            warningText += DetermineLabel(i) + " Input Number";
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
            StateChData data = new StateChData(inputs[0].text, inputs[1].text, inputs[2].text);
            string result = "Result: ";
            int index = setInputs.FindMissingIndex(inputIndexes);
            string output = CalculateStateCh(data, index);
            if (index == 1)
            {
                output = EquationHandler.RemoveCharacter(output, "-");
            }
            switch (index)
            {
                case 0:
                    result += $"Energy: {output} J";
                    break;
                case 1:
                    result += $"Moles: {output} mol";
                    break;
                case 2:
                    result += $"HoFus/Vapor: {output} kJ/mol";
                    break;
            }
            resultText.SetText(result);
        }
    }

    string DetermineLabel(int index)
    {
        string result = "";
        switch (index)
        {
            case 0:
                result = "Energy ";
                break;
            case 1:
                result = "Moles ";
                break;
            case 2:
                result = "HoFus/Vapor ";
                break;

        }
        string output = result;
        return output;
    }

    public void Calculate()
    {
        if (!warning.GetWarningVisible())
        {
            HandleStateCh();
        }
    }
}

public class StateChData
{
    public string energy;
    public string moles;
    public string heat;

    public StateChData(string energy, string moles, string heat)
    {
        this.energy = energy;
        this.moles = moles;
        this.heat = heat;
    }
}
