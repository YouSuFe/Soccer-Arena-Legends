using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuManager : MonoBehaviour
{
    public GameObject settingsPanel;
    public GameObject savePopup;

    public Button confirmButton;
    public Button backButton;
    public Button popupYesButton;
    public Button popupNoButton;

    private GameObject previousMenu;

    public List<MonoBehaviour> tabComponents; // Drag GraphicsTab, AudioTab, etc.

    private List<ISettingsTab> tabs;

    private void Awake()
    {
        Debug.Log($"[SettingsMenuManager] Awake Method");

        // Cache casted references
        tabs = new List<ISettingsTab>();
        foreach (var comp in tabComponents)
        {
            if (comp is ISettingsTab tab)
            {
                Debug.Log($"[SettingsMenuManager] Registered tab: {tab.GetType().Name}");
                tabs.Add(tab);
            }
            else
            {
                Debug.LogWarning($"[SettingsMenuManager] Invalid tab component: {comp.name}");
            }
        }

        // Button bindings
        confirmButton.onClick.AddListener(OnConfirmClicked);
        backButton.onClick.AddListener(OnBackClicked);
        popupYesButton.onClick.AddListener(OnPopupYes);
        popupNoButton.onClick.AddListener(OnPopupNo);
    }

    private void Start()
    {
        savePopup.SetActive(false);
        settingsPanel.SetActive(false);
    }

    private void Update()
    {
        confirmButton.interactable = AnyTabHasChanges();
    }

    public void OpenSettings()
    {
        OpenSettings(null); // no previous menu to reopen
    }

    public void OpenSettings(GameObject fromMenu)
    {
        previousMenu = fromMenu;
        settingsPanel.SetActive(true);
        savePopup.SetActive(false);

        if (previousMenu != null)
            previousMenu.SetActive(false);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        savePopup.SetActive(false);

        if (previousMenu != null)
            previousMenu.SetActive(true);

        previousMenu = null;
    }

    private void OnConfirmClicked()
    {
        foreach (var tab in tabs)
        {
            tab.ApplySettings();
        }
    }

    private void OnBackClicked()
    {
        if (AnyTabHasChanges())
        {
            savePopup.SetActive(true);
            SetTabsInteractable(false);
        }
        else
        {
            CloseSettings();
        }
    }

    private void OnPopupYes()
    {
        foreach (var tab in tabs)
        {
            tab.ApplySettings();
        }

        savePopup.SetActive(false);
        SetTabsInteractable(true);
        CloseSettings();
    }

    private void OnPopupNo()
    {
        foreach (var tab in tabs)
        {
            tab.RevertToInitial();
        }

        savePopup.SetActive(false);
        SetTabsInteractable(true);
        CloseSettings();
    }

    private bool AnyTabHasChanges()
    {
        foreach (var tab in tabs)
        {
            if (tab.HasUnsavedChanges())
                return true;
        }
        return false;
    }

    private void SetTabsInteractable(bool state)
    {
        foreach (var tab in tabs)
        {
            tab.SetInteractable(state);
        }
    }
}
