using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using System.Text.RegularExpressions;

public class ReactBalanceOverlay : MonoBehaviour
{
    [SerializeField] TMP_InputField reactInput;
    [SerializeField] TMP_InputField prodInput;
    [SerializeField] ResultLocker resultText;
    const string finalRegexString = @"^(?:(?:[0-9]*\((?:[A-Z][a-z]?[0-9]*)+\)[0-9]*)|(?:[0-9]*[A-Z][a-z]?[0-9]*)|\+(?=[^\+]))+$";
    Regex finalRegex;
    Warning warning;

    void Awake()
    {
        warning = FindFirstObjectByType<Warning>();
    }

    void Start()
    {
        finalRegex = new Regex(finalRegexString);
    }

    public List<string> DetermineBalance(List<string> input)
    {
        List<string> output = ReactionHandler.BalanceReaction(input.ToPList());
        return output.ToList();
    }

    void HandleBalance()
    {
        string warningText = "Invalid: ";
        bool showWarning = false;
        string react = reactInput.text;
        string prod = prodInput.text;
        List<string> reaction = new List<string> { react, prod };
        if (react == "" || prod == "")
        {
            warningText += "Missing Input";
            showWarning = true;
        }
        else if (!finalRegex.IsMatch(react) || !finalRegex.IsMatch(prod) || !ReactionHandler.ValidateReaction(reaction.ToPList()))
        {
            warningText += "Reaction";
            showWarning = true;
        }
        if (showWarning)
        {
            warning.ShowWarning(warningText);
        }
        else
        {
            List<string> result = DetermineBalance(reaction);
            string output = "Unbalanceable";
            if (result != null)
            {
                output = $"{result[0]}->{result[1]}";
            }
            resultText.SetText(output);
        }
    }

    public void Balance()
    {
        if (!warning.GetWarningVisible())
        {
            HandleBalance();
        }
    }
}
