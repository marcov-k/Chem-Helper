using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using System.Text.RegularExpressions;

public class MolMassOverlay : MonoBehaviour
{
    [SerializeField] TMP_InputField compInput;
    [SerializeField] List<TMP_InputField> inputs = new List<TMP_InputField>(); // in the order: mass, moles
    [SerializeField] ResultLocker resultText;
    const string zeroRegexString = @"^\-?0+\.?0*(?:x10\^\-?[0-9]*)?$";
    const string finalNumRegexString = @"^(?:(?:(?:[0-9]+)|(?:[0-9]+\.|(?:\.[0-9]+))[0-9]*))(?:x10\^\-?[1-9][0-9]*)?$";
    const string finalCompRegexString = @"^(?:(?:\((?:[A-Z][a-z]?[0-9]*)+\)[0-9]*)|(?:[A-Z][a-z]?[0-9]*))+$";
    Regex zeroRegex;
    Regex finalNumRegex;
    Regex finalCompRegex;
    readonly List<int> inputIndexes = new List<int>();
    IndexFIFO setInputs;
    Warning warning;

    void Awake()
    {
        warning = FindFirstObjectByType<Warning>();
    }

    void Start()
    {
        zeroRegex = new Regex(zeroRegexString);
        finalNumRegex = new Regex(finalNumRegexString);
        finalCompRegex = new Regex(finalCompRegexString);
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

    public DoubleString CalculateMols(MolMassData data, int solveIndex)
    {
        string atomMass = EquationHandler.CalculateCompoundMass(data.compound);
        DoubleString result = null;
        switch (solveIndex)
        {
            case 0:
                result = SolveMass(atomMass, data.moles);
                break;
            case 1:
                result = SolveMols(atomMass, data.mass);
                break;
        }
        DoubleString output = result;
        return output;
    }

    DoubleString SolveMass(string atomMass, string mols)
    {
        string equation = $"({mols})x({atomMass})";
        int sigfigs = EquationHandler.SigFigCount(mols);
        string resultSigfig = EquationHandler.SolveEquation(equation, true, sigfigs);
        string resultFull = EquationHandler.SolveEquation(equation, false);
        resultFull = EquationHandler.RoundToDecimalPoint(resultFull, 3);
        DoubleString output = new DoubleString(resultSigfig, resultFull);
        return output;
    }

    DoubleString SolveMols(string atomMass, string mass)
    {
        string equation = $"({mass})/({atomMass})";
        int sigfigs = EquationHandler.SigFigCount(mass);
        string resultSigfig = EquationHandler.SolveEquation(equation, true, sigfigs);
        string resultFull = EquationHandler.SolveEquation(equation, false);
        resultFull = EquationHandler.RoundToDecimalPoint(resultFull, 3);
        DoubleString output = new DoubleString(resultSigfig, resultFull);
        return output;
    }

    void HandleMols()
    {
        string warningText = "Invalid: ";
        bool showWarning = false;
        string comp = compInput.text;
        if (comp == "" || setInputs.indexes.Count < inputs.Count - 1)
        {
            warningText += "Missing Input(s)";
            showWarning = true;
        }
        else if (!finalCompRegex.IsMatch(comp) || !EquationHandler.ValidateCompound(comp))
        {
            warningText += "Invalid Compound";
            showWarning = true;
        }
        else
        {
            for (int i = 0; i < inputs.Count; i++)
            {
                if (inputs[i].text != "?")
                {
                    if (!finalNumRegex.IsMatch(inputs[i].text) || zeroRegex.IsMatch(inputs[i].text))
                    {
                        switch (i)
                        {
                            case 0:
                                warningText += "Mass";
                                break;
                            case 1:
                                warningText += "Mole";
                                break;
                        }
                        warningText += " Input Number";
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
            string result = $"Result: {DetermineLabel(solveIndex)}";
            MolMassData data = new MolMassData(comp, inputs[0].text, inputs[1].text);
            DoubleString output = CalculateMols(data, solveIndex);
            switch (solveIndex)
            {
                case 0:
                    output.string1 += " g";
                    output.string2 += " g";
                    break;
                case 1:
                    output.string1 += " mol";
                    output.string2 += " mol";
                    break;
            }
            result += $"{output.string1} (Without SigFigs: ~{output.string2})";
            resultText.SetText(result);
        }
    }

    string DetermineLabel(int index)
    {
        string output = "";
        switch (index)
        {
            case 0:
                output = "Mass: ";
                break;
            case 1:
                output = "Moles: ";
                break;
        }
        return output;
    }

    public void Calculate()
    {
        if (!warning.GetWarningVisible())
        {
            HandleMols();
        }
    }
}

public class MolMassData
{
    public string compound;
    public string mass;
    public string moles;

    public MolMassData(string compound, string mass, string moles)
    {
        this.compound = compound;
        this.mass = mass;
        this.moles = moles;
    }
}

public class DoubleString
{
    public string string1;
    public string string2;

    public DoubleString(string string1, string string2)
    {
        this.string1 = string1;
        this.string2 = string2;
    }
}
