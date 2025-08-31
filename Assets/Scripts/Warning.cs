using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class Warning : MonoBehaviour
{
    Image background;
    TextMeshProUGUI myText;
    [SerializeField] float showTime = 0.5f;
    [SerializeField] float hideDelay = 0.5f;
    [SerializeField] float hideTime = 1;
    IEnumerator showCoroutine;
    IEnumerator hideCoroutine;
    bool warningVisible = false;

    void Awake()
    {
        background = GetComponent<Image>();
        myText = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        background.color = new Color(background.color.r, background.color.g, background.color.b, 0);
        myText.color = new Color(myText.color.r, myText.color.g, myText.color.b, 0);
    }

    public void ShowWarning(string warning)
    {
        myText.text = warning;
        warningVisible = true;
        background.raycastTarget = true;
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
        }
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        showCoroutine = OpacityCoroutine(showTime, 1);
        StartCoroutine(showCoroutine);
    }

    IEnumerator OpacityCoroutine(float duration, float targetA)
    {
        float startA = background.color.a;
        float factor = startA;
        if (targetA == 0)
        {
            factor = 1 - startA;
        }
        float time = duration * factor;
        float currentA;
        while (time < duration)
        {
            currentA = Mathf.Lerp(startA, targetA, time / duration);
            background.color = new Color(background.color.r, background.color.g, background.color.b, currentA);
            myText.color = new Color(myText.color.r, myText.color.g, myText.color.b, currentA);
            time += Time.deltaTime;
            yield return null;
        }
        background.color = new Color(background.color.r, background.color.g, background.color.b, targetA);
        myText.color = new Color(myText.color.r, myText.color.g, myText.color.b, targetA);
        if (targetA == 1)
        {
            yield return new WaitForSeconds(hideDelay);
            hideCoroutine = OpacityCoroutine(hideTime, 0);
            showCoroutine = null;
            StartCoroutine(hideCoroutine);
        }
        else
        {
            background.raycastTarget = false;
            hideCoroutine = null;
            warningVisible = false;
        }
    }

    public bool GetWarningVisible()
    {
        return warningVisible;
    }
}
