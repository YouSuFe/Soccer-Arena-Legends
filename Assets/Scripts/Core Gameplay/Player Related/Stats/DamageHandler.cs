using System;
using UnityEngine;

public class DamageHandler
{
    private DamageProcessor damageProcessor;
    private Stats stats;

    public DamageHandler(Stats stats, StatsMediator mediator)
    {
        this.stats = stats;
        this.damageProcessor = new DamageProcessor(stats, mediator);
    }

    public void DealDamage(int damageAmount)
    {
        damageProcessor.ProcessDamage(damageAmount);

        int currentHealth = stats.GetCurrentStat(StatType.Health); // Use Stats to get the updated health
        Debug.Log($"After dealing {damageAmount} damage, remaining health is: {currentHealth}");
    }
}
