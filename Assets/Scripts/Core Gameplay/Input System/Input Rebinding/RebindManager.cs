using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class RebindManager : MonoBehaviour
{
    public static RebindManager Instance;

    private const string Prefs_Keys = "rebinds";

    [Header("Input")]
    [SerializeField] private InputActionAsset inputAsset;

    [Header("Overlay UI")]
    [SerializeField] private GameObject overlayPanel;
    [SerializeField] private TMP_Text overlayText;

    private string pendingOverrideJson;
    private readonly List<InputRebindUI> rebindUIs = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        LoadBindings();
        HideOverlay();
    }

    public void Register(InputRebindUI ui)
    {
        if (!rebindUIs.Contains(ui))
            rebindUIs.Add(ui);
    }

    public void StartInteractiveRebind(InputRebindUI caller, InputAction action, int bindingIndex)
    {
        ShowOverlay($"Press a key for '{action.name}'...\n(ESC to cancel)");
        action.Disable();

        action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(op =>
            {
                op.Dispose();
                action.Enable();
                caller.RefreshBindingDisplay();
                HideOverlay();

                // Save all pending changes in-memory
                pendingOverrideJson = inputAsset.SaveBindingOverridesAsJson();
            })
            .Start();
    }

    public bool HasUnsavedChanges()
    {
        return !string.IsNullOrEmpty(pendingOverrideJson);
    }

    public void ApplyPendingRebinds()
    {
        if (!string.IsNullOrEmpty(pendingOverrideJson))
        {
            PlayerPrefs.SetString(Prefs_Keys, pendingOverrideJson);
            PlayerPrefs.Save();
            pendingOverrideJson = null;
        }
    }

    public void RevertToInitial()
    {
        LoadBindings();
        pendingOverrideJson = null;

        foreach (var ui in rebindUIs)
            ui.RefreshBindingDisplay();
    }

    public void LoadBindings()
    {
        if (PlayerPrefs.HasKey(Prefs_Keys))
        {
            string json = PlayerPrefs.GetString(Prefs_Keys);
            inputAsset.LoadBindingOverridesFromJson(json);
        }
    }

    public void ResetAllBindings()
    {
        inputAsset.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(Prefs_Keys);
        PlayerPrefs.Save();

        foreach (var ui in rebindUIs)
            ui.RefreshBindingDisplay();
    }

    private void ShowOverlay(string message)
    {
        if (overlayPanel != null)
        {
            overlayPanel.SetActive(true);
            if (overlayText != null)
                overlayText.text = message;
        }
    }

    private void HideOverlay()
    {
        if (overlayPanel != null)
            overlayPanel.SetActive(false);
    }
}

