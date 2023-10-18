using HalloGames.Architecture.Initilizer;
using HalloGames.SpaceRTS.Data.Enums;
using HalloGames.SpaceRTS.Data.Ships;
using HalloGames.SpaceRTS.Gameplay.Ship.Weapons;
using HalloGames.SpaceRTS.Management.Factories;
using UnityEngine;

namespace HalloGames.SpaceRTS.Management.Initialization
{
    public class ShipInitilizer : AbstractInitilizer<ShipInitilizationData>
    {
        [SerializeField] private ShipWeaponsController _shipWeapons;

        protected override void Awake()
        {
            base.Awake();
        }

        public void InitServices(IWeaponFactory weaponFactory)
        {
            _shipWeapons.InitWeaponeFactory(weaponFactory);
        }
    }

    public readonly struct ShipInitilizationData
    {
        public readonly SideData SideData;
        public readonly ShipData ShipData;

        public ShipInitilizationData(ShipData shipData, SideData sideData)
        {
            SideData = sideData;
            ShipData = shipData;
        }
    }
}