using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProviderBuidler : MonoBehaviour
{
    [SerializeField] private GameInitiliazer _gameInitiliazer;

    private IServiceProvider _serviceProvider;

    private void Awake()
    {
        CreateServiceProvider();

        BindInput();
        InitilizerGame();
    }

    private void InitilizerGame()
    {
        _gameInitiliazer.Initilize(_serviceProvider);
    }

    private void BindInput()
    {
        IInput input = new MouseInput();
        _serviceProvider.AddService(input);
    }

    private void CreateServiceProvider()
    {
        _serviceProvider = new ServiceProvider();
    }
}
