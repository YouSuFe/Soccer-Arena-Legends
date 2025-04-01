using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillFeedUIController : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool simulateTestKills = false;
    [SerializeField] private float testInterval = 2f;

    private float testTimer = 0f;
    private string[] dummyNames = { "Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot" };

    [Header("Settings")]
    [SerializeField] private Transform feedContainer;
    [SerializeField] private GameObject feedItemPrefab;
    [SerializeField] private int maxItems = 6;

    [Header("Icons")]
    [SerializeField] private Sprite knifeIcon;
    [SerializeField] private Sprite skillIcon;
    [SerializeField] private Sprite ballIcon;
    [SerializeField] private Sprite fallIcon;
    [SerializeField] private Sprite zoneIcon;

    private Queue<FeedItemUI> itemPool = new();
    private List<FeedItemUI> activeItems = new();

    private void Awake()
    {
        for (int i = 0; i < maxItems; i++)
        {
            var obj = Instantiate(feedItemPrefab, feedContainer);
            var item = obj.GetComponent<FeedItemUI>();
            obj.SetActive(false);
            itemPool.Enqueue(item);
        }
    }

    private void Update()
    {
        if (!simulateTestKills) return;

        testTimer += Time.deltaTime;
        if (testTimer >= testInterval)
        {
            testTimer = 0f;

            // Simulate a kill feed entry
            var entry = new KillFeedEntry
            {
                KillerName = GetRandomName(),
                VictimName = GetRandomName(),
                DeathType = GetRandomDeathType(),
                KillerTeamIndex = Random.Range(0, 2) // 0 or 1
            };

            DisplayKill(entry); // Directly test the UI system
        }
    }

    private string GetRandomName()
    {
        return dummyNames[Random.Range(0, dummyNames.Length)];
    }

    private DeathType GetRandomDeathType()
    {
        var values = System.Enum.GetValues(typeof(DeathType));
        return (DeathType)values.GetValue(Random.Range(0, values.Length));
    }

    private void OnEnable()
    {
        KillFeedManager.Instance.OnKillFeedReceived += DisplayKill;
    }

    private void OnDisable()
    {
        if (KillFeedManager.Instance != null)
            KillFeedManager.Instance.OnKillFeedReceived -= DisplayKill;
    }

    private void DisplayKill(KillFeedEntry entry)
    {
        if (itemPool.Count == 0)
        {
            var oldest = activeItems[0];
            activeItems.RemoveAt(0);
            itemPool.Enqueue(oldest);
        }

        var item = itemPool.Dequeue();
        activeItems.Add(item);

        string killer = entry.KillerName.ToString();
        string victim = entry.VictimName.ToString();
        Sprite icon = GetIconSprite(entry.DeathType);

        int killerTeam = entry.KillerTeamIndex;
        int victimTeam = (entry.KillerTeamIndex == 0) ? 1 :
                        (entry.KillerTeamIndex == 1) ? 0 : -1;

        item.Setup(killer, victim, icon, killerTeam, victimTeam);
        item.gameObject.SetActive(true);
        item.transform.SetAsFirstSibling();

        Canvas.ForceUpdateCanvases(); // Make sure layout is applied before animating
        item.PlayEntry();

        StartCoroutine(ReturnToPoolAfterDelay(item, 5f));
    }

    private IEnumerator ReturnToPoolAfterDelay(FeedItemUI item, float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log($"[KillFeed] Fading out item for victim: {item.name}");

        item.FadeOut(() =>
        {
            Debug.Log($"[KillFeed] Recycled item: {item.name}");

            item.gameObject.SetActive(false);
            activeItems.Remove(item);
            itemPool.Enqueue(item);
        });
    }

    private Sprite GetIconSprite(DeathType type)
    {
        return type switch
        {
            DeathType.Knife => knifeIcon,
            DeathType.Skill => skillIcon,
            DeathType.Ball => ballIcon,
            DeathType.Fall => fallIcon,
            DeathType.Zone => zoneIcon,
            _ => null
        };
    }
}


