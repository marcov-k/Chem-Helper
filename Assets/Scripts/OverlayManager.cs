using UnityEngine;
using System.Collections.Generic;

public class OverlayManager : MonoBehaviour
{
    readonly List<GameObject> overlays = new List<GameObject>();
    [SerializeField] GameObject mainCanvas;
    [SerializeField] ElementContainerSO elemContainer;

    void Start()
    {
        SetOverlays();
        ReactionHandler.SetElemCont(elemContainer);
    }

    void SetOverlays()
    {
        for (int i = 2; i < mainCanvas.transform.childCount - 2; i++)
        {
            overlays.Add(mainCanvas.transform.GetChild(i).gameObject);
            overlays[i - 2].SetActive(false);
        }
    }

    public void ShowOverlay(int index)
    {
        foreach (GameObject overlay in overlays)
        {
            overlay.SetActive(false);
        }
        overlays[index].SetActive(true);
    }
}
