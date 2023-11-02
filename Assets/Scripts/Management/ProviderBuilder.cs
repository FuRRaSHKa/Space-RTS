using HalloGames.Architecture.Services;
using HalloGames.SpaceRTS.Management.Factories;
using HalloGames.SpaceRTS.Management.Input;
using HalloGames.SpaceRTS.Management.ProjectileManagement;
using UnityEngine;

namespace HalloGames.SpaceRTS.Management.Initialization
{
    public class ProviderBuilder : MonoBehaviour
    {
        [SerializeField] private GameInitiliazer _gameInitiliazer;
        [SerializeField] private ShipSpawner _shipSpawner;
        [SerializeField] private BulletsController _bulletsController;
        [SerializeField] private RocketsController _rocketsController;

        private IServiceProvider _serviceProvider;

        private void Awake()
        {
            CreateServiceProvider();

            InitilizeGame();
        }

        private void InitilizeGame()
        {
            _gameInitiliazer.Initilize(_serviceProvider);
        }

        private void CreateServiceProvider()
        {
            _serviceProvider = new ServiceProvider();

            _shipSpawner.InitProvider(_serviceProvider);

            RegisterServices();
        }

        private void RegisterServices()
        {
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


