using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class OptionsUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainOptionsPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject confirmLeavePanel;

    [Header("Buttons")]
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button confirmLeaveYesButton;
    [SerializeField] private Button confirmLeaveNoButton;

    private void Awake()
    {
        // Set up button listeners here
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenSettings);

        if (leaveButton != null)
            leaveButton.onClick.AddListener(OpenConfirmLeavePanel);

        if (confirmLeaveYesButton != null)
            confirmLeaveYesButton.onClick.AddListener(ConfirmExit);

        if (confirmLeaveNoButton != null)
            confirmLeaveNoButton.onClick.AddListener(CancelLeave);

        HideAllPanels();
    }

    private void HideAllPanels()
    {
        mainOptionsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        confirmLeavePanel.SetActive(false);
    }

    private void ResetPanels()
    {
        mainOptionsPanel.SetActive(true);
        settingsPanel.SetActive(false);
        confirmLeavePanel.SetActive(false);
    }

    public void ToggleOptionsMenu()
    {
        bool opening = !mainOptionsPanel.activeSelf;

        if (opening)
        {
            ResetPanels(); // Always reset when opening

            CursorController.UnlockCursor();
        }
        else
        {
            CursorController.LockCursor();
        }

        mainOptionsPanel.SetActive(opening);
    }

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        mainOptionsPanel.SetActive(false);
    }

    public void OpenConfirmLeavePanel()
    {
        confirmLeavePanel.SetActive(true);
        mainOptionsPanel.SetActive(false);
    }

    public void CancelLeave()
    {
        confirmLeavePanel.SetActive(false);
        mainOptionsPanel.SetActive(true);
    }

    public void ConfirmExit()
    {
        Debug.Log("Player confirmed to exit.");
        CursorController.UnlockCursor();

        if (NetworkManager.Singleton.IsHost)
        {
            // Host case
            HostSingleton.Instance.GameManager.ShutDown();
            NetworkManager.Singleton.Shutdown();
            UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
        }
        else
        {
            // Client case
            ClientSingleton.Instance.GameManager.Disconnect();
        }
    }
}
