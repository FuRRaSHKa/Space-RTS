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
        public event Action OnDeath;
        public event Action<StatData, int> OnStatChange;

        public Stat GetStat(StatData statsData);
        public void ChangeStat(StatData statsData, int delta);
        public int GetStatValue(StatData statsData);
        public void DealDamage(int damage);
    }

    public interface IDeathHandler
    {
        public bool IsDead
        {
            get;
        }

        public event Action OnDeath;
    }

    public class ShipStatsController : MonoBehaviour, IStatsController, IInitializable<ShipInitializationData>, IDeathHandler
    {
        private bool _isDead = false;
        private Dictionary<StatData, Stat> _stats = new Dictionary<StatData, Stat>();

        public bool IsDead => _isDead;

        public event Action<StatData, int> OnStatChange;
        public event Action OnDeath;

        public void Init(ShipInitializationData data)
        {
            _isDead = false;
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
            List<Stat> damageableStats = _stats.Values.Where(w => w.DamageOrder > 0).OrderByDescending(w => w.DamageOrder).ToList();

            int tempDamage = Mathf.Abs(damage);
            foreach (var stat in damageableStats)
            {
                int statValue = stat.GetValue();
                stat.ChangeStat(-tempDamage);
                if (statValue > tempDamage)
                    break;

                tempDamage -= statValue;
                if (tempDamage < 0)
                    break;
            }

            int health = damageableStats.Sum(a => a.GetValue());
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

