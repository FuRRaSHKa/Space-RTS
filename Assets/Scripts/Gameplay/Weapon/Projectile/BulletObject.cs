using UnityEngine;

namespace HalloGames.SpaceRTS.Gameplay.Projectile
{
    public class BulletObject : ProjectileObject
    {
        [SerializeField] private TrailRenderer _trail;

        public override void EnableObject()
        {
            base.EnableObject();

            _trail.Clear();
        }
    }
}
