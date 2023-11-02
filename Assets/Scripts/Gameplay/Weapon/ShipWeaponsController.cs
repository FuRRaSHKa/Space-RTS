using HalloGames.Architecture.Initilizer;
using HalloGames.SpaceRTS.Data.Weapon;
using HalloGames.SpaceRTS.Gameplay.Guns;
using HalloGames.SpaceRTS.Gameplay.Targets;
using HalloGames.SpaceRTS.Management.Factories;
using HalloGames.SpaceRTS.Management.Initialization;
using System.Collections.Generic;
using UnityEngine;

namespace HalloGames.SpaceRTS.Gameplay.Ship.Weapons
{
    public interface IWeaponController
    {
        public void Shoot(ITargetable targetable);
        public void StopShooting();
    }

    public class ShipWeaponsController : MonoBehaviour, IInitilizable<ShipInitilizationData>, IWeaponController
    {
        [SerializeField] private List<Transform> _gunPositions;

        private List<IWeapon> _weaponList = new List<IWeapon>();
        private IWeaponFactory _weaponFactory;
        private ITargetable _currentTarget;

        private void OnDrawGizmos()
        {
            foreach (var postion in _gunPositions)
            {
                Ray r = new Ray(postion.position, postion.up);
                Gizmos.DrawRay(r);

                r = new Ray(postion.position, postion.forward);
                Gizmos.DrawRay(r);
            }
        }

        public void InitWeaponeFactory(IWeaponFactory weaponFactory)
        {
            _weaponFactory = weaponFactory;
        }

        public void Init(ShipInitilizationData data)
        {
            WeaponData weaponData = data.ShipData.WeaponData;
            for (int i = 0; i < _gunPositions.Count; i++)
            {
                IWeapon weapon = _weaponFactory.CreateWeapon(weaponData, _gunPositions[i]);
                _weaponList.Add(weapon);
            }
        }

        public void Shoot(ITargetable targetable)
        {
            if (targetable.TargetDataObservable.DeathHandler.IsDead)
                return;

            _currentTarget = targetable;
            _currentTarget.TargetDataObservable.DeathHandler.OnDeath += StopShooting;

            foreach (var weapon in _weaponList)
            {
                weapon.StartShooting(targetable);
            }
        }

        public void StopShooting()
        {
            _currentTarget = null;

            foreach (var weapon in _weaponList)
            {
                weapon.StopShooting();
            }
        }
    }

    public readonly struct WeaponSpawnData
    {
        public readonly WeaponData WeaponData;
        public readonly Transform Parent;

        public WeaponSpawnData(WeaponData weaponData, Transform parent)
        {
            WeaponData = weaponData;
            Parent = parent;
        }
    }
}