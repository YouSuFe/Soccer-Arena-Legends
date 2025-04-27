using UnityEngine;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }

    [Header("Popup Prefab")]
    [SerializeField] private PopupMessageUI popupPrefab;

    [Header("Default Popup Parent")]
    [SerializeField] private Transform defaultParent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ShowPopup(string title, string description, PopupMessageType type = PopupMessageType.Info, Transform parent = null)
    {
        if (popupPrefab == null)
        {
            Debug.LogError("[PopupManager] Popup prefab not assigned!");
            return;
        }

        if (parent == null)
        {
            parent = defaultParent; // Use persistent canvas by default
        }

        if (parent == null)
        {
            Debug.LogError("[PopupManager] Parent Transform must be provided!");
            return;
        }

        PopupMessageUI popup = Instantiate(popupPrefab, parent);
        popup.transform.localPosition = Vector3.zero;
        popup.transform.localRotation = Quaternion.identity;
        popup.transform.localScale = Vector3.one;

        popup.Show(title, description, type);
    }

}
