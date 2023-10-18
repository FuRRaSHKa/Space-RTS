using HalloGames.SpaceRTS.Data.Enums;
using UnityEngine;

namespace HalloGames.SpaceRTS.Data.Ships
{
    [CreateAssetMenu(fileName = "ShipHullData", menuName = "Data/Ships/ShipHullData")]
    public class ShipHullData : ScriptableObject
    {
        [SerializeField] private ShipSize _shipSize;
        [SerializeField] private GameObject _hullPrefab;
        [SerializeField] private float _defaultShipSpeed;
        [SerializeField] private float _defaultShipAcceleration;
        [SerializeField] private float _defaultShipRotationSpeed;

        public GameObject HullPrefab => _hullPrefab;
        public ShipSize ShipSize => _shipSize;
        public float DefaultShipSpeed => _defaultShipSpeed;
        public float DefaultShipAcceleration => _defaultShipAcceleration;
        public float DefaultShipRotationSpeed => _defaultShipRotationSpeed;
    }
}