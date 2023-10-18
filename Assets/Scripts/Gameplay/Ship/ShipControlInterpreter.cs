using HalloGames.SpaceRTS.Data.Enums;
using HalloGames.SpaceRTS.Gameplay.Targets;
using UnityEngine;

namespace HalloGames.SpaceRTS.Gameplay.Ship.Control
{
    public class ShipControlInterpreter : MonoBehaviour, IControllable
    {
        private SelectGFXController _selectGFXController;
        private ShipEntity _entity;

        private void Awake()
        {
            _selectGFXController = GetComponentInParent<SelectGFXController>();
            _entity = GetComponentInParent<ShipEntity>();
        }

        public void DeSelect()
        {
            _selectGFXController.DeSelect();
        }

        public void Select()
        {
            _selectGFXController.Select();
        }

        public void Target(ITargetable target)
        {
            if (target.Side != _entity.Side)
                _entity.WeaponController.Shoot(target);
        }

        public void TargetPosition(Vector3 target)
        {
            _entity.ShipMovement.MoveTo(target);
        }

        public bool IsEnableToControl(SideData side)
        {
            return _entity.Side == side;
        }
    }
}
