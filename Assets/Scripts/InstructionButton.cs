using UnityEngine;

public class InstructionButton : MonoBehaviour
{
    [SerializeField] string myInstructions;
    InstructionManager manager;

    void Awake()
    {
        manager = FindFirstObjectByType<InstructionManager>();
    }

    public void ShowInstructions()
    {
        manager.ShowInstructions(myInstructions);
    }
}
