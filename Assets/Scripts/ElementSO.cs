using UnityEngine;

[CreateAssetMenu(fileName = "ElementSO", menuName = "Scriptable Objects/ElementSO")]
public class ElementSO : ScriptableObject
{
    public string number;
    public string symbol;
    public string elemName;
    public string mass;
    public string vElecs;
    public string oxiNum;
    public string family;
    public string period;
    public string group;
    public bool diatomic = false;
    public string type;
    public string[] quantNums = new string[4]; // in the order: n, l, ml, ms
    public string eNota;
    public string ngNota;
}
