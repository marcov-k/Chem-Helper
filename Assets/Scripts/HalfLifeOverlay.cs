using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using TMPro;
using NUnit.Framework.Constraints;
using UnityEngine.Windows;

public class HalfLifeOverlay : MonoBehaviour
{
    [SerializeField] List<TMP_InputField> inputs = new List<TMP_InputField>(); // in the order: initial mass, final mass, elapsed time, half-life
    [SerializeField] ResultLocker resultText;
    string finalRegexString = @"^(?:(?:(?:[0-9]+)|(?:[0-9]+\.|(?:\.[0-9]+))[0-9]*))(?:x10\^\-?[1-9][0-9]*)?$";
    string zeroRegexString = @"^\-?0+\.?0*(?:x10\^\-?[0-9]*)?$";
    Regex finalRegex;
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
        finalRegex = new Regex(finalRegexString);
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

    public string CalculateHalfLife(HalfLifeData data, int solveIndex)
    {
        string result = "";
        switch (solveIndex)
        {
            case 0:
                result = SolveStartMass(data);
                break;
            case 1:
                result = SolveEndMass(data);
                break;
            case 2:
                result = SolveTime(data);
                break;
            case 3:
                result = SolveHalfLife(data);
                break;
        }
        string output = result;
        return output;
    }

    string SolveStartMass(HalfLifeData input)
    {
        string equation = $"({input.time})/({input.halflife})";
        string time = EquationHandler.SolveEquation(equation, false);
        equation = $"({input.endMass})x(2^({time}))";
        string result = EquationHandler.SolveEquation(equation, true, EquationHandler.SigFigCount(input.endMass));
        string output = result;
        return output;
    }

    string SolveEndMass(HalfLifeData input)
    {
        string equation = $"({input.time})/({input.halflife})";
        string time = EquationHandler.SolveEquation(equation, false);
        equation = $"({input.startMass})x(0.5^({time}))";
        string result = EquationHandler.SolveEquation(equation, true, EquationHandler.SigFigCount(input.startMass));
        string output = result;
        return output;
    }

    string SolveTime(HalfLifeData input)
    {
        string equation = $"({input.endMass})/({input.startMass})";
        string logNum = EquationHandler.SolveEquation(equation, false);
        string logBase = "0.5";
        string log = EquationHandler.SolveLog(logBase, logNum);
        equation = $"({input.halflife})x({log})";
        string result = EquationHandler.SolveEquation(equation, true, EquationHandler.SigFigCount(input.halflife));
        string output = result;
        return output;
    }

    string SolveHalfLife(HalfLifeData input)
    {
        string equation = $"({input.endMass})/({input.startMass})";
        string logNum = EquationHandler.SolveEquation(equation, false);
        string logBase = "0.5";
        string log = EquationHandler.SolveLog(logBase, logNum);
        equation = $"({input.time})/({log})";
        string result = EquationHandler.SolveEquation(equation, true, EquationHandler.SigFigCount(input.time));
        string output = result;
        return output;
    }

    void HandleHalfLife()
    {
        string warningText = "Invalid: ";
        bool showWarning = false;
        if (setInputs.indexes.Count < inputs.Count - 1)
        {
            warningText += "Not Enough Inputs";
            showWarning = true;
        }
        else
        {
            for (int i = 0; i < inputs.Count; i++)
            {
                if (inputs[i].text != "?")
                {
                    if (zeroRegex.IsMatch(inputs[i].text) || !finalRegex.IsMatch(inputs[i].text))
                    {
                        warningText += DetermineLabel(i) + " Input Number";
                        showWarning = true;
                        break;
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
            int solveIndex = setInputs.FindMissingIndex(inputIndexes);
            string result = $"Result: {DetermineLabel(solveIndex)}: ";
            HalfLifeData data = new HalfLifeData(inputs[0].text, inputs[1].text, inputs[2].text, inputs[3].text);
            result += CalculateHalfLife(data, solveIndex);
            resultText.SetText(result);
        }
    }

    string DetermineLabel(int index)
    {
        string output = "";
        switch (index)
        {
            case 0:
                output = "Initial Mass";
                break;
            case 1:
                output = "Final Mass";
                break;
            case 2:
                output = "Elapsed Time";
                break;
            case 3:
                output = "Half-Life";
                break;
        }
        return output;
    }

    public void Calculate()
    {
        if (!warning.GetWarningVisible())
        {
            HandleHalfLife();
        }
    }
}

public class HalfLifeData
{
    public string startMass;
    public string endMass;
    public string time;
    public string halflife;

    public HalfLifeData(string startMass, string endMass, string time, string halflife)
    {
        this.startMass = startMass;
        this.endMass = endMass;
        this.time = time;
        this.halflife = halflife;
    }
}
