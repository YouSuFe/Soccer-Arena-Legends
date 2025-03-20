using System.Collections.Generic;
public interface IStatModifierApplicationOrder
{
    float Apply(IEnumerable<StatModifier> statModifiers, float baseValue);
}
