using HalloGames.Architecture.PoolSystem;
using UnityEngine;

namespace HalloGames.SpaceRTS.Gameplay.Projectile
{
    public class ProjectileObject : MonoBehaviour
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