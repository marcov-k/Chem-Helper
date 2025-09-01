using UnityEngine;
using TMPro;

public class ResultLocker : MonoBehaviour
{
    TMP_InputField myInputField;
    string text = "Result: ";

    void Awake()
    {
        myInputField = GetComponent<TMP_InputField>();
    }

    void Start()
    {
        text = myInputField.text;
    }

    public void SetText(string newText)
    {
        text = newText;
        myInputField.text = text;
    }

    public string GetText()
    {
        return text;
    }

    public void OnValueChanged()
    {
        myInputField.text = text;
    }
}
