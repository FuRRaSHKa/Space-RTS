using UnityEngine;

namespace HalloGames.SpaceRTS.Gameplay.Projectel
{
    public class BulletObject : ProjectelObject
    {
        [SerializeField] private TrailRenderer _trail;

        public override void EnableObject()
        {
            base.EnableObject();

            _trail.Clear();
        }
    }
}
