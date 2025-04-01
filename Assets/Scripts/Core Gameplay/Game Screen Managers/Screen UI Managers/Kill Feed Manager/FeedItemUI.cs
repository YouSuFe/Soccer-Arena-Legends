using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class FeedItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI killerText;
    [SerializeField] private TextMeshProUGUI victimText;
    [SerializeField] private Image iconImage;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private static readonly Color TeamBlue = new Color(0.3f, 0.6f, 1f);
    private static readonly Color TeamRed = new Color(1f, 0.4f, 0.4f);
    private static readonly Color Neutral = new Color(0.9f, 0.9f, 0.9f);

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Setup(string killer, string victim, Sprite icon, int killerTeamIndex, int victimTeamIndex)
    {
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;

        killerText.text = killer;
        victimText.text = victim;

        killerText.color = GetTeamColor(killerTeamIndex);
        victimText.color = GetTeamColor(victimTeamIndex);

        killerText.gameObject.SetActive(!string.IsNullOrWhiteSpace(killer));

        canvasGroup.alpha = 0f;
        rectTransform.localScale = new Vector3(1f, 0f, 1f); // Prepare for scale animation
    }

    private Color GetTeamColor(int teamIndex)
    {
        return teamIndex switch
        {
            0 => TeamBlue,
            1 => TeamRed,
            _ => Neutral
        };
    }

    public void PlayEntry()
    {
        canvasGroup.DOFade(1f, 0.3f);
        rectTransform.DOScaleY(1f, 0.3f).SetEase(Ease.OutBack);
    }

    public void FadeOut(Action onDone)
    {
        // Instantly hide (or you could fade if you want)
        canvasGroup.DOFade(0f, 0.3f).OnComplete(() =>
        {
            rectTransform.localScale = new Vector3(1f, 0f, 1f); // Collapse again for reuse
            onDone?.Invoke();
        });
    }
}
