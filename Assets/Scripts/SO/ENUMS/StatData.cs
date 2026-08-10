using UnityEngine;
using UnityEngine.Serialization;

namespace HalloGames.SpaceRTS.Data.Enums
{
    [CreateAssetMenu(fileName = "StatData", menuName = "Data/Enums/StatData")]
    public class StatData : ScriptableObject
    {
        [FormerlySerializedAs("_destricption")]
        [SerializeField] private string _description;
        [SerializeField] private Color _color;
        [SerializeField] private int _damageOrder;

        public string Description => _description;
        public Color Color => _color;
        public int DamageOrder => _damageOrder;
    }
}
