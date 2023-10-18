using HalloGames.Architecture.Initilizer;
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
    public class ShipEntity : MonoBehaviour, IInitilizable<ShipInitilizationData>
    {
        private IMovementController _shipMovementController;
        private ITargetable _shipTarget;
        private IStatsController _statsController;
        private IDeathHandler _deathHandler;
        private IWeaponController _weaponController;

        private SideData _side;
        private ShipData _shipData;

        public SideData Side => _side;
        public IMovementController ShipMovement => _shipMovementController;
        public ITargetable ShipTarget => _shipTarget;
        public IStatsController StatsController => _statsController;
        public IDeathHandler DeathHandler => _deathHandler;
        public ShipData ShipData => _shipData;
        public IWeaponController WeaponController => _weaponController;

        public void Init(ShipInitilizationData data)
        {
            _side = data.SideData;
            _shipData = data.ShipData;
        }

        private void Awake()
        {
            _deathHandler = GetComponent<IDeathHandler>();
            _weaponController = GetComponent<IWeaponController>();
            _shipMovementController = GetComponent<IMovementController>();
            _shipTarget = GetComponent<ITargetable>();
            _statsController = GetComponent<IStatsController>();
        }
    }
}