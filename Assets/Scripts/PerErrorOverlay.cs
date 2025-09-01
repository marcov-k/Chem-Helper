using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class PerErrorOverlay : MonoBehaviour
{
    [SerializeField] ResultLocker resultText;
    [SerializeField] TMP_InputField experiInput;
    [SerializeField] TMP_InputField theorInput;
    const string finalRegexString = @"^(?:\-?(?:(?:[0-9]+)|(?:[0-9]+\.|(?:\.[0-9]+))[0-9]*))(?:x10\^\-?[1-9][0-9]*)?$";
    const string zeroRegexString = @"^\-?0+\.?0*(?:x10\^\-?[0-9]*)?$";
    Warning warning;
    Regex finalRegex;
    Regex zeroRegex;
    
    void Awake()
    {
        warning = FindFirstObjectByType<Warning>();
    }

    void Start()
    {
        finalRegex = new Regex(finalRegexString);
        zeroRegex = new Regex(zeroRegexString);
    }

    public string CalculateError(string experiNum, string theorNum)
    {
        string equation = $"(({experiNum}-{theorNum}))/{theorNum})100.";
        string result = EquationHandler.SolveEquation(equation, true);
        string output = result;
        return output;
    }

    void HandleCalculation()
    {
        string result = "Percent Error: ";
        string experi = experiInput.text;
        string theor = theorInput.text;
        if (!finalRegex.IsMatch(experi) || !finalRegex.IsMatch(theor))
        {
            warning.ShowWarning("Invalid: Number Input");
        }
        else if (zeroRegex.IsMatch(theor))
        {
            warning.ShowWarning("Invalid: Theoratical Cannot Equal Zero");
        }
        else
        {
            result += CalculateError(experi, theor) + "%";
            resultText.SetText(result);
        }
    }

    public void Calculate()
    {
        if (!warning.GetWarningVisible())
        {
            HandleCalculation();
        }
    }
}
