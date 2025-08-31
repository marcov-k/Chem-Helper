using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class UnitConvOverlay : MonoBehaviour
{
    [SerializeField] TMP_InputField numInputField;
    [SerializeField] TMP_Dropdown startPrefInput;
    [SerializeField] TMP_Dropdown startUnitInput;
    [SerializeField] TMP_Dropdown endPrefInput;
    [SerializeField] TMP_Dropdown endUnitInput;
    [SerializeField] ResultLocker resultText;
    string finalRegexString = @"^(?:\-?(?:(?:[0-9]+)|(?:[0-9]+\.|(?:\.[0-9]+))[0-9]*))(?:x10\^\-?[1-9][0-9]*)?$";
    string zeroRegexString = @"^\-?0+\.?0*(?:x10\^\-?[0-9]*)?$";
    Regex zeroRegex;
    Regex finalRegex;
    Warning warning;

    void Awake()
    {
        warning = FindFirstObjectByType<Warning>();
    }

    void Start()
    {
        finalRegex = new Regex(finalRegexString);
        zeroRegex = new Regex(zeroRegexString);
    }

    public string ConvertUnits(string inputNum, int startPref, int startUnit, int endPref, int endUnit)
    {
        string result = "";
        List<string> charas = EquationHandler.SplitString(inputNum);
        bool foundExpo = false;
        bool foundX = false;
        string numToSend = "";
        string expoString = "";
        int expoNum = 0;
        if (zeroRegex.IsMatch(inputNum))
        {
            result += "0 " + EquationHandler.GetPrefix(endPref).prefix + EquationHandler.GetUnit(endUnit);
        }
        else
        {
            if (inputNum.Contains("^"))
            {
                foreach (string chara in charas)
                {
                    if (foundExpo)
                    {
                        expoString += chara;
                    }
                    else if (chara == "^")
                    {
                        foundExpo = true;
                    }
                    else if (chara == "x")
                    {
                        foundX = true;
                    }
                    else if (!foundX)
                    {
                        numToSend += chara;
                    }
                }
                expoNum = System.Convert.ToInt32(expoString);
            }
            result = EquationHandler.ConvertUnits(numToSend, startPref, startUnit, endPref, endUnit, expoNum);
        }
        string output = result;
        return output;
    }

    void HandleConversion()
    {
        string inputNum = numInputField.text;
        int startPref = startPrefInput.value;
        int startUnit = startUnitInput.value;
        int endPref = endPrefInput.value;
        int endUnit = endUnitInput.value;
        List<string> charas = EquationHandler.SplitString(inputNum);
        bool unitsMatch = false;
        int literI = 1;
        int mCubeI = 3;
        string result = "Result: ";
        if (startUnit == endUnit || (startUnit == literI && endUnit == mCubeI) || (startUnit == mCubeI && endUnit == literI))
        {
            unitsMatch = true;
        }
        if (finalRegex.IsMatch(inputNum) && unitsMatch)
        {
            result += ConvertUnits(inputNum, startPref, startUnit, endPref, endUnit);
            resultText.SetText(result);
        }
        else if (!unitsMatch)
        {
            warning.ShowWarning("Invalid: Mismatched Units");
        }
        else
        {
            warning.ShowWarning("Invalid: Number Input");
        }
    }

    public void Convert()
    {
        if (!warning.GetWarningVisible())
        {
            HandleConversion();
        }
    }
}
