using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using System;

public class FeedItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI killerText;
    [SerializeField] private TextMeshProUGUI victimText;
    [SerializeField] private Image iconImage;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    private Vector2 onScreenPos;
    private Vector2 offScreenPos;

    private static readonly Color TeamBlue = new Color(0.3f, 0.6f, 1f);
    private static readonly Color TeamRed = new Color(1f, 0.4f, 0.4f);
    private static readonly Color Neutral = new Color(0.9f, 0.9f, 0.9f);

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        onScreenPos = rectTransform.anchoredPosition;
        offScreenPos = onScreenPos + new Vector2(300, 0); // Slide in from the right
    }

    /// <summary>
    /// Setup the kill feed entry.
    /// </summary>
    public void Setup(string killer, string victim, Sprite icon, int killerTeamIndex, int victimTeamIndex)
    {
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;

        killerText.text = killer;
        victimText.text = victim;

        killerText.color = GetTeamColor(killerTeamIndex);
        victimText.color = GetTeamColor(victimTeamIndex);

        killerText.gameObject.SetActive(!string.IsNullOrWhiteSpace(killer));
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

    /// <summary>
    /// Animate entry into view.
    /// </summary>
    public void PlayEntry()
    {
        rectTransform.anchoredPosition = offScreenPos;
        canvasGroup.alpha = 0;
        rectTransform.DOAnchorPos(onScreenPos, 0.4f).SetEase(Ease.OutBack);
        canvasGroup.DOFade(1f, 0.3f);
    }

    /// <summary>
    /// Fade out and return to pool.
    /// </summary>
    public void FadeOut(Action onDone)
    {
        canvasGroup.DOFade(0f, 0.4f).OnComplete(() =>
        {
            onDone?.Invoke();
        });
    }
}
