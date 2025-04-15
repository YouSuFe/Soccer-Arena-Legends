using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderValueLabel : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI valueLabel;

    [Tooltip("How many decimals to show")]
    public int decimalPlaces = 2;

    private void Start()
    {
        if (slider != null)
        {
            slider.onValueChanged.AddListener(UpdateLabel);
            UpdateLabel(slider.value); // Set initial label
        }
    }

    private void UpdateLabel(float value)
    {
        if (valueLabel != null)
        {
            float percent = value * 100f;
            string formatted = percent.ToString($"F{decimalPlaces}");
            valueLabel.text = $"{formatted}";
        }
    }

}
