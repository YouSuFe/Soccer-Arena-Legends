using System.Collections;
using UnityEngine;
using TMPro;

public enum PopupMessageType
{
    Info,
    Warning,
    Error
}

public class PopupMessageUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject root;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Fade Settings")]
    [SerializeField] private float showDuration = 3f;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("Type Styling")]
    [SerializeField] private Color infoColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color errorColor = Color.red;

    [Header("Pop Ups")]
    [SerializeField] private GameObject popUpInfo;
    [SerializeField] private GameObject popUpWarning;
    [SerializeField] private GameObject popUpError;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        root.SetActive(false);
        canvasGroup.alpha = 0;
    }

    public void Show(string title, string message, PopupMessageType type = PopupMessageType.Info)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        titleText.text = title;
        descriptionText.text = message;

        ApplyStyle(type);

        root.SetActive(true);
        canvasGroup.alpha = 1f;

        fadeCoroutine = StartCoroutine(FadeOutCoroutine());
    }

    private void ApplyStyle(PopupMessageType type)
    {
        // Disable all first
        popUpInfo.SetActive(false);
        popUpWarning.SetActive(false);
        popUpError.SetActive(false);

        switch (type)
        {
            case PopupMessageType.Warning:
                titleText.color = warningColor;
                popUpWarning.SetActive(true);
                break;
            case PopupMessageType.Error:
                titleText.color = errorColor;
                popUpError.SetActive(true);
                break;
            case PopupMessageType.Info:
            default:
                titleText.color = infoColor;
                popUpInfo.SetActive(true);
                break;
        }
    }


    private IEnumerator FadeOutCoroutine()
    {
        yield return new WaitForSeconds(showDuration);

        float t = 0f;
        while (t < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            t += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 0f;
        root.SetActive(false);

        // ToDo: Make it persistent accross scenes and reusable
        Destroy(gameObject);
    }
}
