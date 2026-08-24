using HalloGames.Architecture.Services;
using System;
using UnityEngine;

namespace HalloGames.SpaceRTS.Management.Input
{
    public class KeyboardInput : IKeyboardInput
    {
        private PlayerInputMaps _inputActions;

        public Vector2 Direction => _inputActions.Camera.Direction.ReadValue<Vector2>();

        public KeyboardInput()
        {
            _inputActions = new PlayerInputMaps();
            _inputActions.Enable();
        }

        public void Dispose()
        {
            _inputActions.Dispose();
        }
    }

    public interface IKeyboardInput : IService, IDisposable
    {
        public Vector2 Direction
        {
            get;
        }
    }
}
