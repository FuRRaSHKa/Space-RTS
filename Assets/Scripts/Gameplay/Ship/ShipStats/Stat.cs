using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HalloGames.SpaceRTS.Gameplay.Ship.Stats
{
    public class Stat
    {
        private int _maxStat;
        private int _currentStat;
        private int _damageOrder;

        public int DamageOrder => _damageOrder;

        public event Action OnStatChange;

        public Stat(int maxStat, int damageOrder)
        {
            _maxStat = maxStat;
            _currentStat = maxStat;

            _damageOrder = damageOrder;
        }

        public void ChangeStat(int delta)
        {
            _currentStat += delta;
            _currentStat = Mathf.Clamp(_currentStat, 0, _maxStat);
            OnStatChange?.Invoke();
        }

        public int GetValue()
        {
            return _currentStat;
        }
    }
}
