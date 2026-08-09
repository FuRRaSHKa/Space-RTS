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

        public string Description => _description;
        public Color Color => _color;
    }
}
