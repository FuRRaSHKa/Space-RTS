using UnityEngine;

namespace HalloGames.SpaceRTS.Data.Enums
{
    [CreateAssetMenu(fileName = "StatData", menuName = "Data/Enums/StatData")]
    public class StatData : ScriptableObject
    {
        [SerializeField] private string _destricption;
        [SerializeField] private Color _color;

        public string Description => _destricption;
        public Color Color => _color;
    }
}
