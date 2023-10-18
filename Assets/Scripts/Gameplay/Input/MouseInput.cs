using HalloGames.Architecture.Services;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HalloGames.SpaceRTS.Management.Input
{
    public class MouseInput : IInput
    {
        private PlayerInputMaps _inputActions;

        public Vector2 MouseDelta => _inputActions.Camera.MouseDelta.ReadValue<Vector2>();

        public event Action OnChoosingClick;
        public event Action OnTargetingClick;
        public event Action OnErase;
        public event Action<float> OnScrollChange;
        public event Action<bool> OnScrollPressed;

        public MouseInput()
        {
            _inputActions = new PlayerInputMaps();
            _inputActions.Enable();

            _inputActions.Camera.MouseScrollDelta.performed += MouseScroll;
            _inputActions.Camera.MouseScroll.performed += MouseScrollPressed;
            _inputActions.Camera.MouseScroll.canceled += MouseScrollPressed;

            _inputActions.Input.ChoosingClick.performed += ChoseClick;
            _inputActions.Input.TargetingClick.performed += TargetClick;
        }

        private void ChoseClick(InputAction.CallbackContext callbackContext)
        {
            OnChoosingClick?.Invoke();
        }

        private void TargetClick(InputAction.CallbackContext callbackContext)
        {
            OnTargetingClick?.Invoke();
        }

        private void MouseScroll(InputAction.CallbackContext callbackContext)
        {
            if (!callbackContext.performed)
                return;

            OnScrollChange?.Invoke(callbackContext.ReadValue<float>());
        }

        private void MouseScrollPressed(InputAction.CallbackContext callbackContext)
        {
            OnScrollPressed?.Invoke(callbackContext.performed);
        }

        public void Dispose()
        {
            _inputActions.Dispose();
            _inputActions.Disable();
        }
    }

    public interface IInput : IService, IDisposable
    {
        public event Action OnChoosingClick;
        public event Action OnTargetingClick;
        public event Action OnErase;
        public event Action<float> OnScrollChange;
        public event Action<bool> OnScrollPressed;

        public Vector2 MouseDelta
        {
            get;
        }
    }
}

