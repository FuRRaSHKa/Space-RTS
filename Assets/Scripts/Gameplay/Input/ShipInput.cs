using HalloGames.Architecture.Frames;
using HalloGames.SpaceRTS.Data.Enums;
using HalloGames.SpaceRTS.Gameplay.Ship;
using HalloGames.SpaceRTS.Gameplay.Ship.Control;
using HalloGames.SpaceRTS.Gameplay.Targets;
using HalloGames.SpaceRTS.Management.ShipManagement;
using HalloGames.SpaceRTS.UI;
using System.Collections.Generic;
using UnityEngine;

namespace HalloGames.SpaceRTS.Management.Input
{
    public class ShipInput : MonoBehaviour, IUpdatable
    {
        private const float DragThresholdPixels = 8f;

        [SerializeField] private SideData _playerSide;
        [SerializeField] private SelectionBoxView _selectionBoxView;

        private ShipSelection _selection = new ShipSelection();
        private IInput _input;
        private IShipRegistry _shipRegistry;
        private ObjectClicker _objectClicker;
        private Camera _camera;

        private bool _isPressed;
        private bool _isDragging;
        private Vector2 _pressScreenPos;

        private void Awake()
        {
            _camera = Camera.main;
        }

        private void OnEnable()
        {
            TickManager.RegisterUpdate(this);
        }

        private void OnDisable()
        {
            TickManager.UnregisterUpdate(this);
        }

        public void Initialize(IInput input, IShipRegistry shipRegistry, ObjectClicker objectClicker)
        {
            _input = input;
            _shipRegistry = shipRegistry;
            _objectClicker = objectClicker;

            _input.OnChoosingPress += ChoosePress;
            _input.OnChoosingRelease += ChooseRelease;
            _input.OnTargetingClick += TargetClick;
        }

        private void OnDestroy()
        {
            _selection.Clear();

            if (_input == null)
                return;

            _input.OnChoosingPress -= ChoosePress;
            _input.OnChoosingRelease -= ChooseRelease;
            _input.OnTargetingClick -= TargetClick;
        }

        public void UpdateTick(float deltaTime)
        {
            if (_input == null || !_isPressed)
                return;

            var mousePos = _input.MouseScreenPosition;
            if (!_isDragging)
            {
                if ((mousePos - _pressScreenPos).sqrMagnitude < DragThresholdPixels * DragThresholdPixels)
                    return;

                _isDragging = true;
                if (_selectionBoxView != null)
                    _selectionBoxView.Show(_pressScreenPos);
            }

            if (_selectionBoxView != null)
                _selectionBoxView.UpdateBox(_pressScreenPos, mousePos);
        }

        private void ChoosePress()
        {
            _isPressed = true;
            _isDragging = false;
            _pressScreenPos = _input.MouseScreenPosition;
        }

        private void ChooseRelease()
        {
            if (!_isPressed)
                return;

            _isPressed = false;

            if (_isDragging)
            {
                _isDragging = false;
                if (_selectionBoxView != null)
                    _selectionBoxView.Hide();

                SelectInRect(_pressScreenPos, _input.MouseScreenPosition);
            }
            else
            {
                SelectClicked();
            }
        }

        private void SelectClicked()
        {
            if (_objectClicker == null)
            {
                _selection.Clear();
                return;
            }

            var chosenObject = _objectClicker.GetCurrentObject();
            if (chosenObject != null && chosenObject.TryGetComponent(out IControllable controllable))
            {
                var entity = chosenObject.GetComponentInParent<ShipEntity>();
                if (entity != null)
                {
                    _selection.SetSingle(entity, controllable);
                    return;
                }
            }

            _selection.Clear();
        }

        private void SelectInRect(Vector2 firstCorner, Vector2 secondCorner)
        {
            if (_camera == null)
                return;

            var min = Vector2.Min(firstCorner, secondCorner);
            var max = Vector2.Max(firstCorner, secondCorner);
            var selectionRect = new Rect(min, max - min);

            var selectedShips = new List<(ShipEntity entity, IControllable controllable)>();
            foreach (var ship in _shipRegistry.Ships)
            {
                if (ship == null || ship.DeathController == null || ship.DeathController.IsDead)
                    continue;

                var screenPos = _camera.WorldToScreenPoint(ship.transform.position);
                if (screenPos.z <= 0 || !selectionRect.Contains((Vector2)screenPos))
                    continue;

                var controllable = ship.GetComponentInChildren<IControllable>();
                if (controllable == null || !controllable.IsEnableToControl(_playerSide))
                    continue;

                selectedShips.Add((ship, controllable));
            }

            if (selectedShips.Count > 0)
                _selection.SetGroup(selectedShips);
            else
                _selection.Clear();
        }

        private void TargetClick()
        {
            if (_objectClicker == null)
                return;

            var gameObject = _objectClicker.GetCurrentObject();
            if (gameObject != null && gameObject.TryGetComponent(out ITargetable targetable))
                _selection.TargetAll(targetable, _playerSide);
            else
                _selection.TargetPositionAll(_objectClicker.GetWorldMousePos(), _playerSide);
        }
    }
}

namespace HalloGames.SpaceRTS.Gameplay.Ship.Control
{
    public interface ISelectable
    {
        public void Select();
        public void DeSelect();
    }

    public interface IControllable : ISelectable
    {
        public bool IsEnableToControl(SideData side);
        public void Target(ITargetable target);
        public void TargetPosition(Vector3 target);
    }
}
