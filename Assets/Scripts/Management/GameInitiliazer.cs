using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInitiliazer : MonoBehaviour
{
    [SerializeField] private CameraMover _cameraMover;
    [SerializeField] private ShipInput _shipInput;
    [SerializeField] private GameController _gameController;

    private IShipsManager _shipsManager;
    private IServiceProvider _serviceProvider;

    public void Initilize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        InitInput();
        InitilizeShips();

        InitGameController();
    }

    private void InitilizeShips()
    {
        _shipsManager = new ShipsManager(_serviceProvider.GetService<IShipsFactory>());
    }

    private void InitGameController()
    {
        _gameController.InitController(_shipsManager);
    }

    private void InitInput()
    {
        _shipInput.Initilize(_serviceProvider.GetService<IInput>());
        _cameraMover.Initilize(_serviceProvider.GetService<IInput>());
    }
}
