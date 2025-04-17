using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class InputRebindUI : MonoBehaviour
{
    [Header("Input Reference")]
    public InputActionReference actionReference;
    public int bindingIndex = 0;

    [Header("UI References")]
    public TMP_Text actionLabel;
    public TMP_Text bindingDisplay;
    public Button rebindButton;

    private void Start()
    {
        if (RebindManager.Instance != null)
            RebindManager.Instance.Register(this);

        if (actionLabel != null)
            actionLabel.text = actionReference.action.name;

        RefreshBindingDisplay();
        rebindButton.onClick.AddListener(OnRebindClick);
    }

    private void OnRebindClick()
    {
        RebindManager.Instance?.StartInteractiveRebind(this, actionReference.action, bindingIndex);
    }

    public void RefreshBindingDisplay()
    {
        var binding = actionReference.action.bindings[bindingIndex];
        bindingDisplay.text = InputControlPath.ToHumanReadableString(
            binding.effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice
        );
    }
}
