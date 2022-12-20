using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IStatsController
{
    public event Action<StatData, int> OnStatChange;

    public Stat GetStat(StatData statsData);
    public void ChangeStat(StatData statsData, int delta);
    public int GetStatValue(StatData statsData);

    public void DealDamage(int damage);
}

public class ShipStatsController : MonoBehaviour, IStatsController, IInitilizable<ShipInitilizationData>
{
    private Dictionary<StatData, Stat> _stats = new Dictionary<StatData, Stat>();

    public event Action<StatData, int> OnStatChange;

    public void Init(ShipInitilizationData data)
    {
        List<StatStruct> statDatas = data.ShipData.StatDatas;
        foreach (var statData in statDatas)
        {
            Stat stat = new Stat(statData.StartValue, statData.DamageOrder);
            
            stat.OnStatChange += () =>
            {
                OnStatChange?.Invoke(statData.StatData, stat.GetValue());
            };

            _stats.Add(statData.StatData, stat);
        }
    }

    public void ChangeStat(StatData statsData, int delta)
    {
        _stats[statsData].ChangeStat(delta);
    }

    public Stat GetStat(StatData statsData)
    {
        return _stats[statsData];
    }

    public int GetStatValue(StatData statsData)
    {
        return _stats[statsData].GetValue();
    }

    public void DealDamage(int damage)
    {
        List<Stat> damagableStats = _stats.Values.Where(w => w.DamageOrder > 0).OrderByDescending(w => w.DamageOrder).ToList();

        int tempDamage = Mathf.Abs(damage);
        foreach (var stat in damagableStats)
        {
            int statValue = stat.GetValue();
            stat.ChangeStat(-tempDamage);
            if (statValue > tempDamage)
                break;

            tempDamage -= statValue;
            if (tempDamage < 0)
                break;
        }

        int health = damagableStats.Sum(a => a.GetValue());
        if (health <= 0)
            Death();
    }

    private void Death()
    {
        Debug.Log("Death");
    }
}
