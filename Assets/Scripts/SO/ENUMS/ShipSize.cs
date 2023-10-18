using UnityEngine;

namespace HalloGames.SpaceRTS.Data.Enums
{
    [CreateAssetMenu(fileName = "ShipSize", menuName = "Data/Enums/ShipSize")]
    public class ShipSize : ScriptableObject
    {
        [SerializeField] private string _sizeName;
        public string SizeName => _sizeName;
    }

}

