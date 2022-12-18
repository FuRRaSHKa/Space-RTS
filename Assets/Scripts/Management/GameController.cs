using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
