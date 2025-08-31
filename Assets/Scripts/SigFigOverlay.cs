using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class SigFigOverlay : MonoBehaviour
{
    [SerializeField] ResultLocker resultText;
    [SerializeField] ResultLocker sigfigCountText;
    [SerializeField] TMP_InputField inputField;
    Warning warning;

    void Awake()
    {
        warning = FindFirstObjectByType<Warning>();
    }

    void Start()
    {
        inputField.onEndEdit.AddListener(delegate { ValidateParentheses(); });
    }

    void PerformCalculation()
    {
        if (ValidateParentheses())
        {
            string input = inputField.text;
            string result = input;
            if (EquationHandler.OperationCount(input) > 0)
            {
                result = EquationHandler.SolveEquation(input, true);
            }
            int sigfigCount = -1;
            if (result != "Undefined")
            {
                sigfigCount = EquationHandler.SigFigCount(result);
            }
            DisplayResult(result, sigfigCount);
        }
        else
        {
            warning.ShowWarning("Invalid: Parentheses");
        }
    }

    void DisplayResult(string result, int sigfigCount)
    {
        resultText.SetText("Result: " + result);
        if (sigfigCount != -1)
        {
            sigfigCountText.SetText("SigFig Count: " + sigfigCount);
        }
        else
        {
            sigfigCountText.SetText("SigFig Count: N/A");
        }
    }

    bool ValidateParentheses()
    {
        bool output = true;
        if (inputField.text.Contains('(') || inputField.text.Contains(')'))
        {
            string input = inputField.text;
            List<string> characters = EquationHandler.SplitString(inputField.text);
            int openCount = input.Count(s => s == '(');
            int closeCount = input.Count(s => s == ')');
            if (openCount != closeCount)
            {
                output = false;
            }
        }
        return output;
    }

    public void Calculate()
    {
        if (!warning.GetWarningVisible())
        {
            PerformCalculation();
        }
    }
}
