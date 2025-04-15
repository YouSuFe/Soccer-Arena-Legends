public interface ISettingsTab
{
    void ApplySettings();           // Apply current buffer
    void RevertToInitial();        // Discard buffer, reset to original
    bool HasUnsavedChanges();      // Compare buffer with initial
    void SetInteractable(bool state); // Optional: disable tab on popup
}
