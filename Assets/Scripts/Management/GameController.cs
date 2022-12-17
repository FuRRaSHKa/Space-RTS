using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    private IShipsManager _shipManager;

    public void InitController(IShipsManager shipManager)
    {
        _shipManager = shipManager;
    }

    public void StartGame()
    {

    }
}
