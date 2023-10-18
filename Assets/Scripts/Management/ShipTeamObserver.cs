using HalloGames.SpaceRTS.Data.Enums;
using HalloGames.SpaceRTS.Gameplay.Ship;
using System;
using System.Collections.Generic;

namespace HalloGames.SpaceRTS.Management.ShipManagement
{
    public class ShipTeamObserver
    {
        private SideData _sideData;
        private List<ShipEntity> _ships;

        public event Action OnAllDies;

        public ShipTeamObserver(SideData sideData, List<ShipEntity> ships)
        {
            _sideData = sideData;
            _ships = ships;
        }

        public void StartObserving()
        {
            foreach (var ship in _ships)
            {

            }
        }

        private void ShipDeath()
        {

        }
    }
}