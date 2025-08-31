using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

[CreateAssetMenu(fileName = "SigFig Input Validator", menuName = "SigFig Input Validator")]
public class SigFigValidator : TMP_InputValidator
{
    [SerializeField] public string regexFull;
    Regex regex;
    string fullString;

    public override char Validate(ref string text, ref int pos, char ch)
    {
        fullString = text;
        if (fullString == "")
        {
            fullString = ch.ToString();
        }
        else
        {
            fullString = fullString.Insert(pos, ch.ToString());
        }
        if (regex == null)
        {
            regex = new Regex(regexFull);
        }
        if (regex.IsMatch(fullString))
        {
            text = fullString;
            pos++;
            return ch;
        }
        else
        {
            return '\0';
        }
    }
}

