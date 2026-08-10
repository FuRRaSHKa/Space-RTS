using HalloGames.Architecture.Initializer;
using HalloGames.SpaceRTS.Data.Enums;
using HalloGames.SpaceRTS.Data.Ships;
using HalloGames.SpaceRTS.Management.Initialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HalloGames.SpaceRTS.Gameplay.Ship.Stats
{
    public interface IStatsController
    {
        public event Action<StatData, int> OnStatChange;

        public Stat GetStat(StatData statsData);
        public void ChangeStat(StatData statsData, int delta);
        public int GetStatValue(StatData statsData);
        public void DealDamage(int damage);
    }

    public interface IDeathController
    {
        public bool IsDead
        {
            get;
        }

        public event Action OnDeath;
    }

    public class ShipStatsController : MonoBehaviour, IStatsController, IInitializable<ShipInitializationData>, IDeathController
    {
        private Dictionary<StatData, Stat> _stats = new Dictionary<StatData, Stat>(); 
        private List<Stat> _damageOrderedStats = new List<Stat>();

        private Stat _criticalStat;

        private bool _isDead = false;

        public bool IsDead => _isDead;

        public event Action<StatData, int> OnStatChange;
        public event Action OnDeath;

        public void Init(ShipInitializationData data)
        {
            _isDead = false;
            List<StatStruct> statDatas = data.ShipData.StatDatas;
            foreach (var statData in statDatas)
            {
                Stat stat = new Stat(statData.StartValue, statData.StatData.DamageOrder);

                stat.OnStatChange += () =>
                {
                    OnStatChange?.Invoke(statData.StatData, stat.GetValue());
                };

                _stats.Add(statData.StatData, stat);
            }

            _damageOrderedStats = _stats.Values.Where(w => w.DamageOrder >= 0).OrderByDescending(w => w.DamageOrder).ToList();
            _criticalStat = _damageOrderedStats.Last();
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
            damage = Mathf.Abs(damage);
            foreach (var stat in _damageOrderedStats)
            {
                var statValue = stat.GetValue();
                stat.ChangeStat(-damage);
                if (statValue > damage)
                    break;

                damage -= statValue;
                if (damage < 0)
                    break;
            }

            int health = _criticalStat.GetValue();
            if (health <= 0)
                Death();
        }

        private void Death()
        {
            _isDead = true;
            OnDeath?.Invoke();

            OnDeath = null;
        }
    }

}

