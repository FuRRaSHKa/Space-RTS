using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectGFXController : MonoBehaviour, ISelectable
{
    [SerializeField] private GameObject _gfxObject;

    public void Select()
    {
        _gfxObject.SetActive(true);
    }

    public void DeSelect()
    {
        _gfxObject.SetActive(false);
    }
}
