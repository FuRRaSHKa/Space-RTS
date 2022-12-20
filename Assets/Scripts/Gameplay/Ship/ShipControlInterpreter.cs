using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
