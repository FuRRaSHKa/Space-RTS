using HalloGames.Architecture.Initializer;
using HalloGames.SpaceRTS.Data.Enums;
using HalloGames.SpaceRTS.Data.Ships;
using HalloGames.SpaceRTS.Gameplay.Ship.Control;
using HalloGames.SpaceRTS.Gameplay.Ship.Stats;
using HalloGames.SpaceRTS.Gameplay.Ship.Weapons;
using HalloGames.SpaceRTS.Gameplay.Targets;
using HalloGames.SpaceRTS.Management.Initialization;
using UnityEngine;

namespace HalloGames.SpaceRTS.Gameplay.Ship
{
    public class ShipEntity : MonoBehaviour, IInitializable<ShipInitializationData>
    {
        private IMovementController _shipMovementController;
        private ITargetable _shipTarget;
        private IStatsController _statsController;
        private IDeathController _deathController;
        private IWeaponController _weaponController;

        private SideData _side;
        private ShipData _shipData;

        public SideData Side => _side;
        public IMovementController ShipMovement => _shipMovementController;
        public ITargetable ShipTarget => _shipTarget;
        public IStatsController StatsController => _statsController;
        public IDeathController DeathController => _deathController;
        public ShipData ShipData => _shipData;
        public IWeaponController WeaponController => _weaponController;

        public void Init(ShipInitializationData data)
        {
            _side = data.SideData;
            _shipData = data.ShipData;
        }

        private void Awake()
        {
            _deathController = GetComponent<IDeathController>();
            _weaponController = GetComponent<IWeaponController>();
            _shipMovementController = GetComponent<IMovementController>();
            _shipTarget = GetComponent<ITargetable>();
            _statsController = GetComponent<IStatsController>();
        }
    }
}