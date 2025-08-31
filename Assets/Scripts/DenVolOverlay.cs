using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Text.RegularExpressions;
using System.Linq;

public class DenVolOverlay : MonoBehaviour
{
    [SerializeField] List<TMP_InputField> inputs = new List<TMP_InputField>(); // in the order: mass, density, volume
    [SerializeField] ResultLocker resultText;
    string finalRegexString = @"^(?:\-?(?:(?:[0-9]+)|(?:[0-9]+\.|(?:\.[0-9]+))[0-9]*))(?:x10\^\-?[1-9][0-9]*)?$";
    string zeroRegexString = @"^\-?0+\.?0*(?:x10\^\-?[0-9]*)?$";
    Regex finalRegex;
    Regex zeroRegex;
    Warning warning;
    List<int> inputIndexes = new List<int>();
    IndexFIFO setInputs;

    void Awake()
    {
        warning = FindFirstObjectByType<Warning>();
    }

    void Start()
    {
        for (int i = 0; i < inputs.Count; i++)
        {
            int index = i;
            inputs[i].onValueChanged.AddListener(delegate { InputChange(index); });
            inputs[i].onEndEdit.AddListener(delegate { EditEnded(index); });
            inputIndexes.Add(i);
        }
        zeroRegex = new Regex(zeroRegexString);
        finalRegex = new Regex(finalRegexString);
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

    public string ConvertDensity(string mass, string density, string volume, int solveIndex)
    {
        string result = "";
        switch (solveIndex)
        {
            case 0:
                result = SolveMass(density, volume);
                break;
            case 1:
                result = SolveDensity(mass, volume);
                break;
            case 2:
                result = SolveVolume(mass, density);
                break;
        }
        string output = result;
        return output;
    }

    string SolveMass(string density, string volume)
    {
        string equation = density + "x" + volume;
        string result = EquationHandler.SolveEquation(equation, true);
        string output = result + " g";
        return output;
    }

    string SolveDensity(string mass, string volume)
    {
        string equation = mass + "/" + volume;
        string result = EquationHandler.SolveEquation(equation, true);
        string output = result + " g/cm3";
        return output;
    }

    string SolveVolume(string mass, string density)
    {
        string equation = mass + "/" + density;
        string result = EquationHandler.SolveEquation(equation, true);
        string output = result + " cm3";
        return output;
    }

    void HandleDensity()
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
                        switch (i)
                        {
                            case 0:
                                warningText += "Mass";
                                break;
                            case 1:
                                warningText += "Density";
                                break;
                            case 2:
                                warningText += "Volume";
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
            string result = "Result: ";
            switch (solveIndex)
            {
                case 0:
                    result += "Mass: ";
                    break;
                case 1:
                    result += "Density: ";
                    break;
                case 2:
                    result += "Volume: ";
                    break;
            }
            result += ConvertDensity(inputs[0].text, inputs[1].text, inputs[2].text, solveIndex);
            resultText.SetText(result);
        }
    }

    public void Convert()
    {
        if (!warning.GetWarningVisible())
        {
            HandleDensity();
        }
    }
}

public class IndexFIFO
{
    public List<int> indexes = new List<int>();
    int maxCount;

    public void AddIndex(int newIndex)
    {
        if (indexes.Count < maxCount && !indexes.Contains(newIndex))
        {
            indexes.Add(newIndex);
        }
        else
        {
            if (indexes.Contains(newIndex))
            {
                indexes.Remove(newIndex);
                indexes.Add(newIndex);
            }
            else
            {
                indexes.RemoveAt(0);
                indexes.Add(newIndex);
            }
        }
    }

    public void RemoveIndex(int index)
    {
        if (indexes.Contains(index))
        {
            indexes.Remove(index);
        }
    }

    public int FindMissingIndex(List<int> indexesToCheck)
    {
        int output = -1;
        foreach (int index in indexesToCheck)
        {
            if (!indexes.Contains(index))
            {
                output = index;
                break;
            }
        }
        return output;
    }

    public List<int> FindAllMissingIndexes(List<int> indexesToCheck)
    {
        List<int> output = new List<int>();
        foreach (int index in indexesToCheck)
        {
            if (!indexes.Contains(index))
            {
                output.Add(index);
            }
        }
        return output.ToList();
    }

    public IndexFIFO(int maxCount)
    {
        this.maxCount = maxCount;
    }
}
