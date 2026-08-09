using HalloGames.Architecture.Services;
using HalloGames.SpaceRTS.Management.CameraManagement;
using HalloGames.SpaceRTS.Management.Factories;
using HalloGames.SpaceRTS.Management.Initialization;
using HalloGames.SpaceRTS.Management.Input;
using HalloGames.SpaceRTS.Management.ShipManagement;
using UnityEngine;

namespace HalloGames.SpaceRTS.Management
{
    public class GameInitiliazer : MonoBehaviour
    {
        [SerializeField] private CameraMover _cameraMover;
        [SerializeField] private ShipInput _shipInput;
        [SerializeField] private GameController _gameController;

        private IShipsManager _shipsManager;
        private IServiceProvider _serviceProvider;

        public void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            InitInput();
            InitializeShips();

            InitGameController();
        }

        private void InitializeShips()
        {
            _shipsManager = new ShipsManager(_serviceProvider.GetService<IShipsFactory>());
        }

        private void InitGameController()
        {
            _gameController.InitController(_shipsManager);
        }

        private void InitInput()
        {
            _shipInput.Initialize(_serviceProvider.GetService<IInput>());
            _cameraMover.Initialize(_serviceProvider.GetService<IInput>());
        }
    }
}
