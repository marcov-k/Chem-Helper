using UnityEngine;
using TMPro;

public class InstructionManager : MonoBehaviour
{
    [SerializeField] GameObject instructBg;
    TextMeshProUGUI instructText;

    void Awake()
    {
        instructText = instructBg.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        instructBg.SetActive(false);
    }

    public void ShowInstructions(string instructions)
    {
        instructBg.SetActive(true);
        instructText.text = instructions;
    }

    public void HideInstructions()
    {
        instructBg.SetActive(false);
    }
}
