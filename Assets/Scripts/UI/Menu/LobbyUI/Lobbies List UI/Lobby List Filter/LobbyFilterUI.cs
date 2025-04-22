using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyFilterUI : MonoBehaviour
{
    private const string ALL_STRING = "All";

    [Header("Dropdowns")]
    [SerializeField] private TMP_Dropdown gameModeDropdown;
    [SerializeField] private TMP_Dropdown mapDropdown;
    [SerializeField] private TMP_Dropdown regionDropdown;
    [SerializeField] private TMP_Dropdown maxPlayersDropdown;

    [Header("Buttons")]
    [SerializeField] private Button resetButton;

    public event Action<FilterData> OnFilterChanged;

    private void Awake()
    {
        SetupEnumDropdown<GameEnumsUtil.Region>(regionDropdown);
        SetupEnumDropdown<GameEnumsUtil.Map>(mapDropdown);
        SetupEnumDropdown<GameEnumsUtil.GameMode>(gameModeDropdown);
        SetupIntDropdown(maxPlayersDropdown, new int[] { 2, 4, 6, 8, 10 });

        resetButton.onClick.AddListener(ResetFilters);
    }

    private void OnDisable()
    {
        ResetFilters();
    }

    private void SetupEnumDropdown<T>(TMP_Dropdown dropdown) where T : Enum
    {
        dropdown.ClearOptions();
        var options = new List<string> { ALL_STRING };
        options.AddRange(Enum.GetNames(typeof(T)));
        dropdown.AddOptions(options);
        dropdown.onValueChanged.AddListener(_ => NotifyFilterChange());
    }

    private void SetupIntDropdown(TMP_Dropdown dropdown, int[] values)
    {
        dropdown.ClearOptions();
        var options = new List<string> { ALL_STRING };
        options.AddRange(values.Select(v => v.ToString()));
        dropdown.AddOptions(options);
        dropdown.onValueChanged.AddListener(_ => NotifyFilterChange());
    }

    private void ResetFilters()
    {
        regionDropdown.value = 0;
        mapDropdown.value = 0;
        gameModeDropdown.value = 0;
        maxPlayersDropdown.value = 0;

        NotifyFilterChange();
    }

    private void NotifyFilterChange()
    {
        OnFilterChanged?.Invoke(GetFilterData());
    }

    public FilterData GetFilterData()
    {
        return new FilterData
        {
            Region = regionDropdown.value == 0 ? null : regionDropdown.options[regionDropdown.value].text,
            Map = mapDropdown.value == 0 ? null : mapDropdown.options[mapDropdown.value].text,
            GameMode = gameModeDropdown.value == 0 ? null : gameModeDropdown.options[gameModeDropdown.value].text,
            MaxPlayers = maxPlayersDropdown.value == 0 ? null : maxPlayersDropdown.options[maxPlayersDropdown.value].text,
        };
    }
}


