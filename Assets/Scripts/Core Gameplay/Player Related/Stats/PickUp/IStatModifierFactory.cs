public interface IStatModifierFactory
{
    StatModifier Create(OperatorType operatorType, StatType statType, float value, float duration, ModifierSourceTag sourceTag);
}
