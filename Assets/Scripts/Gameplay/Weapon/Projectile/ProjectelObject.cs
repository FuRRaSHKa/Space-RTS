using HalloGames.Architecture.PoolSystem;
using UnityEngine;

namespace HalloGames.SpaceRTS.Gameplay.Projectel
{
    public class ProjectelObject : MonoBehaviour
    {
        [SerializeField] private PoolObject _poolObject;

        public virtual void EnableObject()
        {

        }

        public virtual void DisableObject()
        {
            _poolObject.ForceDisable();
        }
    }
}