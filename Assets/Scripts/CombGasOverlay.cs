using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using TMPro;

public class CombGasOverlay : MonoBehaviour
{
    [SerializeField] List<TMP_InputField> inputs1 = new List<TMP_InputField>(); // in the order: P, V, T
    [SerializeField] List<TMP_InputField> inputs2 = new List<TMP_InputField>(); // in the order: P, V, T
    readonly List<int> inputIndexes1 = new List<int>();
    readonly List<int> inputIndexes2 = new List<int>();
    IndexFIFO setInputs1;
    List<int> setIndexes = new List<int>();
    IndexFIFO setInputs2;
    const string finalRegexString = @"^(?:(?:[0-9]+(?:\.(?!.*\.))?)|(?:(?:\.(?!.*\.))?[0-9]+))+$";
    Regex finalRegex;
    [SerializeField] ResultLocker resultText;
    Warning warning;

    void Awake()
    {
        warning = FindFirstObjectByType<Warning>();
    }

    void Start()
    {
        finalRegex = new Regex(finalRegexString);
        for (int i = 0; i < inputs1.Count; i++)
        {
            int index = i;
            inputs1[i].onValueChanged.AddListener(delegate { InputChange(index); });
            inputs1[i].onEndEdit.AddListener(delegate { EditEnded(index); });
            inputIndexes1.Add(i);
        }
        for (int i = 0; i < inputs2.Count; i++)
        {
            int index = i;
            inputs2[i].onValueChanged.AddListener(delegate { InputChange(index, 2); });
            inputIndexes2.Add(i);
        }
        setInputs1 = new IndexFIFO(inputs1.Count - 1);
        setInputs2 = new IndexFIFO(inputs2.Count);
    }

    void InputChange(int changedIndex, int inputNum = 1)
    {
        if (inputNum != 1)
        {
            if (inputs2[changedIndex].text != "")
            {
                setInputs2.AddIndex(changedIndex);
            }
            else
            {
                setInputs2.RemoveIndex(changedIndex);
            }
        }
        else
        {
            setIndexes.Remove(changedIndex);
            if (inputs1[changedIndex].text != " " && inputs1[changedIndex].text != "")
            {
                if (setInputs1.indexes.Count < inputIndexes1.Count - 1)
                {
                    setInputs1.AddIndex(changedIndex);
                    int changeIndex;
                    if (setInputs1.indexes.Count == inputIndexes1.Count - 1)
                    {
                        changeIndex = setInputs1.FindMissingIndex(inputIndexes1);
                        inputs1[changeIndex].text = " ";
                    }
                }
                setIndexes.Add(changedIndex);
            }
            else if (inputs1[changedIndex].text == "" && setInputs1.indexes.Count == inputIndexes1.Count - 1)
            {
                int blankIndex = setInputs1.FindMissingIndex(inputIndexes1);
                if (blankIndex != changedIndex)
                {
                    inputs1[blankIndex].text = "";
                    setInputs1.RemoveIndex(changedIndex);
                }
            }
        }
    }

    void EditEnded(int endedIndex)
    {
        if (setInputs1.indexes.Count == inputs1.Count - 1)
        {
            int blankIndex = setInputs1.FindMissingIndex(inputIndexes1);
            if (inputs1[endedIndex].text == "" && endedIndex == blankIndex)
            {
                inputs1[endedIndex].text = " ";
            }
        }
    }

    public CombGasData CalculateCombGas(List<CombGasData> input)
    {
        int solveTarget = FindSolveTarget(input.ToList());
        CombGasData output = input[1];
        int sigfigs = FindLowestSigFigs(input.ToList());
        string result;
        switch (solveTarget)
        {
            case 0:
                result = SolvePres(input, sigfigs);
                output.pres = result;
                break;
            case 1:
                result = SolveVol(input, sigfigs);
                output.vol = result;
                break;
            case 2:
                result = SolveTemp(input, sigfigs);
                output.temp = result;
                break;
        }
        return output;
    }

    int FindSolveTarget(List<CombGasData> input)
    {
        int output = -1;
        if (input[0].blanks.Count > 0)
        {
            List<int> filledIndexes = new List<int>() { 0, 1, 2 };
            filledIndexes.RemoveAll(item => input[0].blanks.Contains(item));
            foreach (int blank in input[1].blanks)
            {
                if (filledIndexes.Contains(blank))
                {
                    output = blank;
                    break;
                }
            }
        }
        else
        {
            if (input[1].blanks.Count > 0)
            {
                output = input[1].blanks[0];
            }
        }
        return output;
    }

    int FindLowestSigFigs(List<CombGasData> input)
    {
        List<int> sigfigCounts = new List<int>();
        List<string> inputStrings = new List<string>() { input[0].pres, input[0].vol, input[0].temp };
        for (int i = 0; i < inputs1.Count; i++)
        {
            if (!input[0].blanks.Contains(i))
            {
                sigfigCounts.Add(EquationHandler.SigFigCount(inputStrings[i]));
            }
        }
        inputStrings = new List<string>() { input[1].pres, input[1].vol, input[1].temp };
        for (int i = 0; i < inputs2.Count; i++)
        {
            if (!input[1].blanks.Contains(i))
            {
                sigfigCounts.Add(EquationHandler.SigFigCount(inputStrings[i]));
            }
        }
        int output = sigfigCounts[0];
        foreach (int count in sigfigCounts)
        {
            if (count < output)
            {
                output = count;
            }
        }
        return output;
    }

    string SolvePres(List<CombGasData> input, int sigfigs)
    {
        string initVal = CalculateInitVal(input[0]);
        string equation = $"(({initVal})x({input[1].temp}))/({input[1].vol})";
        string output = EquationHandler.SolveEquation(equation, true, sigfigs);
        return output;
    }

    string SolveVol(List<CombGasData> input, int sigfigs)
    {
        string initVal = CalculateInitVal(input[0]);
        string equation = $"(({initVal})x({input[1].temp}))/({input[1].pres})";
        string output = EquationHandler.SolveEquation(equation, true, sigfigs);
        return output;
    }

    string SolveTemp(List<CombGasData> input, int sigfigs)
    {
        string initVal = CalculateInitVal(input[0]);
        string equation = $"(({input[1].pres})x({input[1].vol}))/({initVal})";
        string output = EquationHandler.SolveEquation(equation, true, sigfigs);
        return output;
    }

    string CalculateInitVal(CombGasData input)
    {
        string equation = $"(({input.pres})x({input.vol}))/({input.temp})";
        string output = EquationHandler.SolveEquation(equation, false);
        return output;
    }

    void HandleCombGas()
    {
        string warningText = "Invalid: ";
        bool showWarning = false;
        if (setInputs1.indexes.Count < inputs1.Count - 1 || setInputs2.indexes.Count < 1 || setInputs2.indexes.Count < setIndexes.Count - 1)
        {
            warningText += "Missing Input";
            showWarning = true;
        }
        else if (setInputs2.indexes.Count >= setIndexes.Count)
        {
            warningText += "Input Mismatch/Nothing To Solve For";
            showWarning = true;
        }
        else
        {
            foreach (TMP_InputField input in inputs1)
            {
                if (!(input.text == " " || finalRegex.IsMatch(input.text)))
                {
                    warningText += "Invalid Input";
                    showWarning = true;
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
            List<CombGasData> input = new List<CombGasData>();
            input.Add(new CombGasData(inputs1[0].text, inputs1[1].text, inputs1[2].text));
            input.Add(new CombGasData(inputs2[0].text, inputs2[1].text, inputs2[2].text));
            int solveTarget = FindSolveTarget(input.ToList());
            CombGasData result = CalculateCombGas(input.ToList());
            string output = "Result: ";
            switch (solveTarget)
            {
                case 0:
                    output += $"Pressure: {result.pres} atm";
                    break;
                case 1:
                    output += $"Volume: {result.vol} L";
                    break;
                case 2:
                    output += $"Temp: {result.temp} K";
                    break;
            }
            resultText.SetText(output);
        }
    }

    public void Calculate()
    {
        if (!warning.GetWarningVisible())
        {
            HandleCombGas();
        }
    }
}

public class CombGasData
{
    public string pres;
    public string vol;
    public string temp;
    public List<int> blanks = new List<int>();

    public CombGasData(string pres = "1", string vol = "1", string temp = "273")
    {
        if (pres == " " || pres == "") { pres = "1"; blanks.Add(0); }
        if (vol == " " || vol == "") { vol = "1"; blanks.Add(1); }
        if (temp == " " || temp == "") { temp = "273"; blanks.Add(2); }
        this.pres = pres;
        this.vol = vol;
        this.temp = temp;
    }
}
