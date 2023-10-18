using HalloGames.SpaceRTS.Data.Enums;
using HalloGames.SpaceRTS.Data.Team;
using HalloGames.SpaceRTS.Gameplay.Ship;
using HalloGames.SpaceRTS.Management.Factories;
using System.Collections.Generic;


namespace HalloGames.SpaceRTS.Management.ShipManagement
{
    public interface IShipsManager
    {
        public void IntallTeams(List<TeamData> teams);
        public void StartObserving();
        public void ResetTeams();
    }

    public class ShipsManager : IShipsManager
    {
        private IShipsFactory _shipsFactory;
        private List<ShipTeamObserver> _teams = new List<ShipTeamObserver>();

        public ShipsManager(IShipsFactory shipsFactory)
        {
            _shipsFactory = shipsFactory;
        }

        public void IntallTeams(List<TeamData> teams)
        {
            foreach (var team in teams)
            {
                List<ShipEntity> ships = _shipsFactory.CreateShips(team.ShipDatas, team.SideData);
                ShipTeamObserver shipTeamObserver = new ShipTeamObserver(team.SideData, ships);

                _teams.Add(shipTeamObserver);
            }
        }

        public void StartObserving()
        {
            foreach (var team in _teams)
            {
                team.StartObserving();
            }
        }

        private void AllDies(SideData sideData)
        {
        }

        public void ResetTeams()
        {

        }
    }


}
