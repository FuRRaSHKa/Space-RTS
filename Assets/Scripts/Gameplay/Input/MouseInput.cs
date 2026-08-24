using HalloGames.Architecture.Frames;
using HalloGames.Architecture.Services;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HalloGames.SpaceRTS.Management.Input
{
    public class MouseInput : IInput, IUpdatable
    {
        private PlayerInputMaps _inputActions;
        private Vector2 _mouseScreenPosition;
        private bool _isMouseOnScreen;

        public Vector2 MouseDelta => _inputActions.Camera.MouseDelta.ReadValue<Vector2>();
        public Vector2 MouseScreenPosition => _mouseScreenPosition;
        public bool IsMouseOnScreen => _isMouseOnScreen;

        public event Action OnChoosingPress;
        public event Action OnChoosingRelease;
        public event Action OnTargetingClick;
        public event Action OnErase;
        public event Action<float> OnScrollChange;
        public event Action<bool> OnScrollPressed;

        public void UpdateTick(float deltaTime)
        {
            RefreshMouseState();
        }

        private void RefreshMouseState()
        {
            var pos = Mouse.current.position.ReadValue();
            _isMouseOnScreen = pos.x >= 0f && pos.x <= Screen.width && pos.y >= 0f && pos.y <= Screen.height;
            if (!_isMouseOnScreen)
            {
                pos.x = Mathf.Clamp(pos.x, 0f, Screen.width);
                pos.y = Mathf.Clamp(pos.y, 0f, Screen.height);
            }

            _mouseScreenPosition = pos;
        }

        public MouseInput()
        {
            _inputActions = new PlayerInputMaps();
            _inputActions.Enable();

            _inputActions.Camera.MouseScrollDelta.performed += MouseScroll;
            _inputActions.Camera.MouseScroll.performed += MouseScrollPressed;
            _inputActions.Camera.MouseScroll.canceled += MouseScrollPressed;

            _inputActions.Input.ChoosingClick.performed += ChoosePress;
            _inputActions.Input.ChoosingClick.canceled += ChooseRelease;
            _inputActions.Input.TargetingClick.performed += TargetClick;

            RefreshMouseState();
            TickManager.RegisterUpdate(this);
        }

        private void ChoosePress(InputAction.CallbackContext callbackContext)
        {
            OnChoosingPress?.Invoke();
        }

        private void ChooseRelease(InputAction.CallbackContext callbackContext)
        {
            OnChoosingRelease?.Invoke();
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
            TickManager.UnregisterUpdate(this);
            _inputActions.Dispose();
            _inputActions.Disable();
        }
    }

    public interface IInput : IService, IDisposable
    {
        public event Action OnChoosingPress;
        public event Action OnChoosingRelease;
        public event Action OnTargetingClick;
        public event Action OnErase;
        public event Action<float> OnScrollChange;
        public event Action<bool> OnScrollPressed;

        public Vector2 MouseDelta
        {
            get;
        }

        public Vector2 MouseScreenPosition
        {
            get;
        }

        public bool IsMouseOnScreen
        {
            get;
        }
    }
}

