using HalloGames.Architecture.Services;
using HalloGames.Extensions.Math;
using HalloGames.SpaceRTS.Data.Enums;
using HalloGames.SpaceRTS.Data.Ships;
using HalloGames.SpaceRTS.Gameplay.Ship;
using HalloGames.SpaceRTS.Management.Initialization;
using System.Collections.Generic;
using UnityEngine;

namespace HalloGames.SpaceRTS.Management.Factories
{
    public interface IShipsFactory : IService
    {
        public List<ShipEntity> CreateShips(List<ShipData> shipDatas, SideData gameSide);
    }

    public class ShipSpawner : MonoBehaviour, IShipsFactory
    {
        [SerializeField] private Transform _shipParent;
        [SerializeField] private List<SpawnZone> _shipSpawnZones;

        private IServiceProvider _serviceProvider;

        public void InitProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public List<ShipEntity> CreateShips(List<ShipData> shipDatas, SideData gameSide)
        {
            List<ShipEntity> ships = new List<ShipEntity>();

            foreach (var ship in shipDatas)
            {
                ShipEntity shipEntity = SpawnShip(ship, gameSide);
                ships.Add(shipEntity);
            }

            return ships;
        }

        private ShipEntity SpawnShip(ShipData shipData, SideData gameSide)
        {
            ShipInitilizationData shipInitilizationData = new ShipInitilizationData(shipData, gameSide);

            ShipEntity shipEntity = Instantiate(shipData.ShipHullData.HullPrefab).GetComponent<ShipEntity>();
            ShipInitilizer shipInitilizer = shipEntity.GetComponent<ShipInitilizer>();
            shipInitilizer.InitServices(_serviceProvider.GetService<IWeaponFactory>());
            shipInitilizer.Initilize(shipInitilizationData);

            shipEntity.transform.SetParent(_shipParent);
            shipEntity.transform.position = GetSpawnPos(gameSide);

            return shipEntity;
        }

        private Vector3 GetSpawnPos(SideData gameSide)
        {
            Transform place = _shipSpawnZones.Find(f => f.GameSide == gameSide).Zone;
            return place.position.GetRandomPosInsideCircle(Vector3.up, 4);
        }
    }

    public readonly struct ShipsSpawnData
    {
        public readonly List<ShipData> ShipDatas;
        public readonly SideData SideData;

        public ShipsSpawnData(List<ShipData> shipDatas, SideData sideData)
        {
            ShipDatas = shipDatas;
            SideData = sideData;
        }
    }

    [System.Serializable]
    public class SpawnZone
    {
        [SerializeField] private Transform _zone;
        [SerializeField] private SideData _gameSide;

        public Transform Zone => _zone;
        public SideData GameSide => _gameSide;
    }
}

