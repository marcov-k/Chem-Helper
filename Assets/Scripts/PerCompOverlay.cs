using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using System.Text.RegularExpressions;

public class PerCompOverlay : MonoBehaviour
{
    [SerializeField] TMP_InputField input;
    [SerializeField] GameObject resultPrefab;
    [SerializeField] GameObject resultPlaceholder;
    List<GameObject> outputObjects = new List<GameObject>();
    int resultsPerLine = 4;
    int outputSigfigs = 4;
    [SerializeField] ElementContainerSO elemCont;
    string finalCompRegexString = @"^(?:(?:\((?:[A-Z][a-z]?[0-9]*)+\)[0-9]*)|(?:[A-Z][a-z]?[0-9]*))+$";
    Regex finalCompRegex;
    Warning warning;

    void Awake()
    {
        warning = FindFirstObjectByType<Warning>();
    }

    void Start()
    {
        finalCompRegex = new Regex(finalCompRegexString);
        resultPlaceholder.SetActive(true);
    }

    public List<PerCompData> CalculatePerComp(string input)
    {
        List<PerCompData> output = new List<PerCompData>();
        List<Element> elems = EquationHandler.SplitCompound(input);
        string compMass = EquationHandler.CalculateCompoundMass(input);
        foreach (Element elem in elems)
        {
            string elemMass = elemCont.GetElement(elem.symbol, ElemSearchMode.atomicSymbol).mass;
            string equation = $"((({elemMass})x{elem.count})/({compMass}))x100";
            string percent = EquationHandler.SolveEquation(equation, true, outputSigfigs);
            PerCompData newData = new PerCompData(elem.symbol, percent);
            output.Add(newData);
        }
        return output;
    }

    void HandlePerComp()
    {
        string warningText = "Invalid: ";
        bool showWarning = false;
        string comp = input.text;
        if (comp == "")
        {
            warningText += "Missing Input";
            showWarning = true;
        }
        else if (!finalCompRegex.IsMatch(comp) || !EquationHandler.ValidateCompound(comp))
        {
            warningText += "Compound";
            showWarning = true;
        }
        if (showWarning)
        {
            warning.ShowWarning(warningText);
        }
        else
        {
            ResetResults();
            resultPlaceholder.SetActive(false);
            List<PerCompData> results = CalculatePerComp(comp);
            int outputLines = Mathf.CeilToInt((float)results.Count / resultsPerLine);
            float outputHeight = resultPlaceholder.GetComponent<RectTransform>().rect.height;
            for (int i = 0; i < outputLines; i++)
            {
                GameObject newLine = Instantiate(resultPrefab, transform.GetChild(0));
                RectTransform rect = newLine.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y - outputHeight * i);
                outputObjects.Add(newLine);
            }
            int lineIndex = -1;
            int index = 0;
            int indexInLine = 0;
            foreach (PerCompData result in results)
            {
                if (index % resultsPerLine == 0)
                {
                    lineIndex++;
                    indexInLine = 0;
                }
                ResultLocker resultText = outputObjects[lineIndex].GetComponent<ResultLocker>();
                string newText = resultText.GetText();
                if (lineIndex > 0)
                {
                    newText = newText.Replace("Result: ", string.Empty);
                }
                if (indexInLine > 0 || lineIndex > 0)
                {
                    newText += " + ";
                }
                newText += $"{result.symbol}: {result.percent}%";
                resultText.SetText(newText);
                index++;
                indexInLine++;
            }
        }
    }

    void ResetResults()
    {
        foreach (GameObject output in outputObjects.ToList())
        {
            Destroy(output);
        }
        outputObjects.Clear();
    }

    public void Calculate()
    {
        if (!warning.GetWarningVisible())
        {
            HandlePerComp();
        }
    }
}

public class PerCompData
{
    public string symbol;
    public string percent;

    public PerCompData(string symbol, string percent)
    {
        this.symbol = symbol;
        this.percent = percent;
    }
}
