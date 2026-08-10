using HalloGames.Architecture.Frames;
using HalloGames.Architecture.Services;
using HalloGames.SpaceRTS.Management.Factories;
using HalloGames.SpaceRTS.Management.Input;
using HalloGames.SpaceRTS.Management.ProjectileManagement;
using UnityEngine;
using UnityEngine.Assertions;

namespace HalloGames.SpaceRTS.Management.Initialization
{
    public class ProviderBuilder : MonoBehaviour
    {
        [SerializeField] private GameInitiliazer _gameInitiliazer;
        [SerializeField] private ShipSpawner _shipSpawner;
        [SerializeField] private BulletsController _bulletsController;
        [SerializeField] private RocketsController _rocketsController;
        [SerializeField] private CentralTicker _centralTicker;

        private IServiceProvider _serviceProvider;

        private void Awake()
        {
            CreateServiceProvider();

            InitializeGame();
        }

        private void InitializeGame()
        {
            _gameInitiliazer.Initialize(_serviceProvider);
        }

        private void CreateServiceProvider()
        {
            _serviceProvider = new ServiceProvider();

            _shipSpawner.InitProvider(_serviceProvider);

            RegisterServices();
        }

        private void RegisterServices()
        {
            Assert.IsNotNull(_centralTicker, $"{nameof(ProviderBuilder)}: {nameof(_centralTicker)} is not assigned");

            _serviceProvider.AddService<IUpdateTicksDispatcher>(_centralTicker);
            _serviceProvider.AddService<ILogicTicksDispatcher>(_centralTicker);
            _serviceProvider.AddService<IFixedUpdateTicksDispatcher>(_centralTicker);

            _serviceProvider.AddService<IShipsFactory>(_shipSpawner);

            IInput input = new MouseInput();
            _serviceProvider.AddService(input);

            IWeaponFactory weaponFactory = new WeaponFactory(_serviceProvider);
            _serviceProvider.AddService(weaponFactory);

            _serviceProvider.AddService<BulletsController>(_bulletsController);
            _serviceProvider.AddService<RocketsController>(_rocketsController);

            BulletSpawner bulletSpawner = new BulletSpawner(_bulletsController);
            RocketSpawner rocketsController = new RocketSpawner(_rocketsController);

            _serviceProvider.AddService(bulletSpawner);
            _serviceProvider.AddService(rocketsController);
        }
    }
}


