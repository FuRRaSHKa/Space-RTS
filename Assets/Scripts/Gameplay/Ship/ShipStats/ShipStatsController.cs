using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IStatsController
{
    public TStat GetStat<TStat>() where TStat : Stat;
    public void ChangeStat<TStat>(int delta) where TStat : Stat;
    public int GetStatValue<TStat>() where TStat : Stat;
}

public class ShipStatsController : MonoBehaviour, IStatsController, IInitilizable<ShipInitilizationData>
{
    private Dictionary<Type, Stat> _stats = new Dictionary<Type, Stat>();

    public void Init(ShipInitilizationData data)
    {
        
    }

    public void ChangeStat<TStat>(int delta) where TStat : Stat
    {
        _stats[typeof(TStat)].ChangeStat(delta);
    }

    public TStat GetStat<TStat>() where TStat : Stat
    {
        return _stats[typeof(TStat)] as TStat;
    }

    public int GetStatValue<TStat>() where TStat : Stat
    {
        return _stats[typeof(TStat)].GetValue();
    }
}
