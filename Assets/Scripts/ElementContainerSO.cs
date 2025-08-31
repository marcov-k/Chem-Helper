using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

[CreateAssetMenu(fileName = "ElementContainerSO", menuName = "Scriptable Objects/ElementContainerSO")]
public class ElementContainerSO : ScriptableObject
{
    [SerializeField] List<ElementSO> elements = new List<ElementSO>();

    public ElementSO GetElement(object parameter, string searchMode)
    {
        ElementSO output = null;
        foreach (ElementSO element in elements)
        {
            var property = element.GetType().GetField(searchMode).GetValue(element);
            string[] array;
            if (property.GetType().IsArray)
            {
                array = (string[])element.GetType().GetField(searchMode).GetValue(element);
                if (array.SequenceEqual((string[])parameter))
                {
                    output = element;
                    break;
                }
            }
            else
            {
                string prop = property.ToString();
                string param = parameter.ToString();
                if (prop == param)
                {
                    output = element;
                    break;
                }
            }
        }
        return output;
    }

    public List<ElementSO> GetElementsWithAttribute(object parameter, string searchMode)
    {
        List<ElementSO> output = new List<ElementSO>();
        foreach (ElementSO element in elements)
        {
            var property = element.GetType().GetField(searchMode).GetValue(element);
            string[] array;
            if (property == parameter)
            {
                output.Add(element);
            }
            else if (property.GetType().IsArray)
            {
                array = (string[])element.GetType().GetField(searchMode).GetValue(element);
                if (array.SequenceEqual((string[])parameter))
                {
                    output.Add(element);
                }
            }
        }
        return output.ToList();
    }
}

public class ElemSearchMode
{
    public static readonly string atomicNumber = "number";
    public static readonly string atomicSymbol = "symbol";
    public static readonly string elementName = "elemName";
    public static readonly string atomicMass = "mass";
    public static readonly string valenceElectrons = "vElecs";
    public static readonly string oxidationNumber = "oxiNum";
    public static readonly string family = "family";
    public static readonly string period = "period";
    public static readonly string group = "group";
    public static readonly string diatomic = "diatomic";
    public static readonly string type = "type";
    public static readonly string quantumNumbers = "quantNums";
    public static readonly string electronNotation = "eNota";
    public static readonly string nobelGasNotation = "ngNota";
}
