using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StatModifierConfig", menuName = "Game Screen Data/StatsModifier/Stat Modifier Config")]
public class StatModifierConfig : ScriptableObject
{
    // ToDo: Add ModifierSourceTag for clarification
    [SerializeField] private ModifierSourceTag modifierSourceTag;
    [SerializeField] private StatType statType;
    [SerializeField] private OperatorType operatorType;
    [SerializeField] private float value; 
    [SerializeField] private float duration;

    public ModifierSourceTag ModifierSourceTag => modifierSourceTag;
    public StatType StatType => statType;
    public OperatorType OperatorType => operatorType;
    public float Value => value; 
    public float Duration => duration;
}
