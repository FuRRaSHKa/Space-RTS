using HalloGames.SpaceRTS.Data.Enums;
using HalloGames.SpaceRTS.Gameplay.Ship.Control;
using HalloGames.SpaceRTS.Gameplay.Targets;
using UnityEngine;

namespace HalloGames.SpaceRTS.Management.Input
{
    public class ShipInput : MonoBehaviour
    {
        [SerializeField] private SideData _playerSide;

        private IControllable _currentObject;
        private IInput _input;

        public void Initilize(IInput input)
        {
            _input = input;

            _input.OnChoosingClick += ChooseClick;
            _input.OnTargetingClick += TargetClick;
        }

        private void ChooseClick()
        {
            GameObject chosedObject = ObjectClicker.Instance.GetCurrentObject();
            if (chosedObject != null)
            {
                if (chosedObject.TryGetComponent(out IControllable controllable))
                {
                    if (controllable != _currentObject)
                    {
                        _currentObject?.DeSelect();
                        _currentObject = controllable;
                        _currentObject?.Select();
                    }

                    return;
                }
            }

            if (_currentObject != null)
            {
                _currentObject.DeSelect();
                _currentObject = null;
            }
        }

        private void TargetClick()
        {
            if (_currentObject != null && _currentObject.IsEnableToControl(_playerSide))
            {
                GameObject gameObject = ObjectClicker.Instance.GetCurrentObject();
                if (gameObject != null && gameObject.TryGetComponent(out ITargetable targetable))
                    _currentObject.Target(targetable);
                else
                    _currentObject.TargetPosition(ObjectClicker.Instance.GetWorldMousePos());
            }
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
