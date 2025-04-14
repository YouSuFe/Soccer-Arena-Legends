using UnityEngine;
using UnityEngine.UI;

public class OptionTabsManager : MonoBehaviour
{
    public GameObject[] Tabs;
    public Image[] TabButtons;
    public float activeScaleFactor = 1.1f;  // Scaling factor for active tab buttons
    
    // Inactive and active colors using hexadecimal values
    private Color InactiveTabButtonColor = new Color32(0x32, 0xFF, 0x7E, 0xFF);  // #32FF7E
    private Color ActiveTabButtonColor = new Color32(0xFF, 0xAF, 0x40, 0xFF);   // #FFAF40

    // Store original sizes to reset properly
    private Vector2[] originalSizes;

    private void Start()
    {
        // Store the original size of each tab button
        originalSizes = new Vector2[TabButtons.Length];
        for (int i = 0; i < TabButtons.Length; i++)
        {
            originalSizes[i] = TabButtons[i].rectTransform.sizeDelta;
        }

        // Initialize with the first tab open
        SwitchTab(0);
    }

    public void SwitchTab(int TabId)
    {
        // Deactivate all tabs
        for (int i = 0; i < Tabs.Length; i++)
        {
            Tabs[i].SetActive(false);
        }

        // Activate the selected tab
        Tabs[TabId].SetActive(true);

        // Reset all buttons to inactive state
        for (int i = 0; i < TabButtons.Length; i++)
        {
            if (i != TabId) // Only reset size if it's not the selected tab
            {
                TabButtons[i].color = InactiveTabButtonColor;  // Change color to inactive
                TabButtons[i].rectTransform.sizeDelta = originalSizes[i];  // Reset to original size
            }
        }

        // Set the selected tab's button to active state
        TabButtons[TabId].color = ActiveTabButtonColor;  // Change color to active
        TabButtons[TabId].rectTransform.sizeDelta = originalSizes[TabId] * activeScaleFactor;  // Scale up the button
    }
}
