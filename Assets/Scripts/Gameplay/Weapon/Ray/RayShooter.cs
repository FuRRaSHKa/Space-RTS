using HalloGames.Architecture.Initilizer;
using HalloGames.SpaceRTS.Data.Weapon;
using HalloGames.SpaceRTS.Gameplay.Targets;
using System;
using UnityEngine;

namespace HalloGames.SpaceRTS.Gameplay.Guns.Targeter
{
    public interface IShooter
    {
        public event Action<Vector3, Vector3> OnDealDamage;
        public event Action<ITargetable> OnShooting;

        public void Shoot(ITargetable targetable);
    }

    public class RayShooter : MonoBehaviour, IShooter, IInitilizable<WeaponData>
    {
        private int _damage;

        public event Action<Vector3, Vector3> OnDealDamage;
        public event Action<ITargetable> OnShooting;

        public void Init(WeaponData data)
        {
            _damage = data.Damage;
        }

        public void Shoot(ITargetable targetable)
        {
            targetable.DealDamage(_damage);
            OnShooting?.Invoke(targetable);
        }
    }
}

