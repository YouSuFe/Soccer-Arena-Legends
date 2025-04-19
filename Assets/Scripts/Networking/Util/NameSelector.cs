using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NameSelector : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameField;
    [SerializeField] private Button connectButton;

    // For naming validation, this way is easly hackable and player put long or less name,
    // Usually, we are validating it on Server
    [SerializeField] private int minNameLength = 1;
    [SerializeField] private int maxNameLength = 16;

    public const string PlayerNameKey = "PlayerName";

    private void Start()
    {
        // With this, we are understanding that we have a dedicated(headless) server
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            return;
        }

        connectButton.onClick.AddListener(Connect);
        nameField.onValueChanged.AddListener(HandleTextChanged);
        nameField.text = PlayerPrefs.GetString(PlayerNameKey, string.Empty);
        HandleNameChanged();
    }

    private void HandleTextChanged(string fieldText)
    {
        connectButton.interactable =
            fieldText.Length >= minNameLength &&
            fieldText.Length <= maxNameLength;
    }

    public void HandleNameChanged()
    {
        connectButton.interactable =
            nameField.text.Length >= minNameLength &&
            nameField.text.Length <= maxNameLength;

    }

    public void Connect()
    {
        PlayerPrefs.SetString(PlayerNameKey, nameField.text);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}

