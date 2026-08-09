using HalloGames.Architecture.Initializer;
using HalloGames.SpaceRTS.Data.Enums;
using HalloGames.SpaceRTS.Data.Ships;
using HalloGames.SpaceRTS.Gameplay.Ship.Weapons;
using HalloGames.SpaceRTS.Management.Factories;
using UnityEngine;

namespace HalloGames.SpaceRTS.Management.Initialization
{
    public class ShipInitilizer : AbstractInitilizer<ShipInitializationData>
    {
        [SerializeField] private ShipWeaponsController _shipWeapons;

        protected override void Awake()
        {
            base.Awake();
        }

        public void InitServices(IWeaponFactory weaponFactory)
        {
            _shipWeapons.InitWeaponFactory(weaponFactory);
        }
    }

    public readonly struct ShipInitializationData
    {
        public readonly SideData SideData;
        public readonly ShipData ShipData;

        public ShipInitializationData(ShipData shipData, SideData sideData)
        {
            SideData = sideData;
            ShipData = shipData;
        }
    }
}