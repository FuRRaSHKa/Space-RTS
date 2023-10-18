using HalloGames.SpaceRTS.Data.Team;
using HalloGames.SpaceRTS.Management.ShipManagement;
using System.Collections.Generic;
using UnityEngine;

namespace HalloGames.SpaceRTS.Management.Initialization
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private List<TeamData> _teams;

        private IShipsManager _shipManager;

        private void Start()
        {
            StartGame();
        }

        public void InitController(IShipsManager shipManager)
        {
            _shipManager = shipManager;
        }

        public void StartGame()
        {
            _shipManager.IntallTeams(_teams);

            _shipManager.StartObserving();
        }
    }
}

