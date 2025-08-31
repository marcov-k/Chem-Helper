using UnityEngine;

public class OverlayButton : MonoBehaviour
{
    OverlayManager overlayManager;
    Warning warning;
    int index;

    void Awake()
    {
        overlayManager = FindFirstObjectByType<OverlayManager>();
        warning = FindFirstObjectByType<Warning>();
    }

    void Start()
    {
        SetIndex();
    }

    void SetIndex()
    {
        index = transform.GetSiblingIndex();
    }

    public void ShowOverlay()
    {
        if (!warning.GetWarningVisible())
        {
            overlayManager.ShowOverlay(index);
        }
    }
}
