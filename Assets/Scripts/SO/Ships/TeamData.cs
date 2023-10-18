using HalloGames.SpaceRTS.Data.Enums;
using HalloGames.SpaceRTS.Data.Ships;
using System.Collections.Generic;
using UnityEngine;

namespace HalloGames.SpaceRTS.Data.Team
{
    [CreateAssetMenu(fileName = "TeamData", menuName = "Data/Team/TeamData")]
    public class TeamData : ScriptableObject
    {
        [SerializeField] private SideData _sideData;
        [SerializeField] private List<ShipData> _shipDatas;

        public SideData SideData => _sideData;
        public List<ShipData> ShipDatas => _shipDatas;
    }
}

