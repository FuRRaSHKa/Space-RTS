using UnityEngine;

namespace HalloGames.SpaceRTS.Gameplay.Ship.Control
{
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
}


