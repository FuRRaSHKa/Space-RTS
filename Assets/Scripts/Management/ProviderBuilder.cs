using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProviderBuilder : MonoBehaviour
{
    [SerializeField] private GameInitiliazer _gameInitiliazer;
    [SerializeField] private ShipSpawner _shipSpawner;
    [SerializeField] private BulletsController _bulletsController;

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
        _serviceProvider.AddService<IShipsFactory>(_shipSpawner);

        IInput input = new MouseInput();
        _serviceProvider.AddService(input);

        IWeaponFactory weaponFactory = new WeaponFactory(_serviceProvider);
        _serviceProvider.AddService(weaponFactory);

        _serviceProvider.AddService<BulletsController>(_bulletsController);
    }
}
