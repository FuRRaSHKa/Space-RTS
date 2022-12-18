using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProviderBuidler : MonoBehaviour
{
    [SerializeField] private GameInitiliazer _gameInitiliazer;
    [SerializeField] private ShipSpawner _shipSpawner;

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

        _serviceProvider.AddService<IShipsFactory>(_shipSpawner);

        IInput input = new MouseInput();
        _serviceProvider.AddService(input);
    }
}
