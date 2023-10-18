using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HalloGames.SpaceRTS.Data.Enums
{
    [CreateAssetMenu(fileName = "Side", menuName = "Data/Enums/Side")]
    public class SideData : ScriptableObject
    {
        [SerializeField] private Color _color;


        public Color Color => _color;
    }
}
