using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class SciNotaOverlay : MonoBehaviour
{
    [SerializeField] ResultLocker resultText;
    [SerializeField] TMP_InputField inputField;
    Warning warning;
    string finalRegex = @"^(?:\-?(?:(?:[0-9]+)|(?:[0-9]+\.|(?:\.[0-9]+))[0-9]*))(?:x10\^\-?[1-9][0-9]*)?$";
    Regex regex;

    void Awake()
    {
        warning = FindFirstObjectByType<Warning>();
    }

    void Start()
    {
        regex = new Regex(finalRegex);
    }

    public string ConvertNotation(string input)
    {
        string result = "";
        if (input.Contains('x'))
        {
            result = EquationHandler.Sci2Normal(input);
        }
        else
        {
            result = EquationHandler.SigFigSciNota(input, EquationHandler.SigFigCount(input));
        }
        string output = result;
        return output;
    }

    void HandleConversion()
    {
        string input = inputField.text;
        string result = "";
        if (regex.IsMatch(input))
        {
            if (input.Contains('x'))
            {
                result = "Normal Notation: ";
            }
            else
            {
                result = "Sci Notation: ";
            }
            result += ConvertNotation(input);
            resultText.SetText(result);
        }
        else
        {
            warning.ShowWarning("Invalid: Input");
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
