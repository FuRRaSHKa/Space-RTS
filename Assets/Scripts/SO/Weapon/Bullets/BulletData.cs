using HalloGames.Architecture.PoolSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HalloGames.SpaceRTS.Data.Projectile
{
    [CreateAssetMenu(fileName = "BulletData", menuName = "Data/Bullet/BulletData")]
    public class BulletData : ProjectileData
    {

    }

    public abstract class ProjectileData : ScriptableObject
    {
        [SerializeField] private PoolObject _projectilePrefab;
        [SerializeField] private float _speed;
        [SerializeField] private float _lifetime;

        public float Speed => _speed;
        public float Lifetime => _lifetime;
        public PoolObject ProjectilePrefab => _projectilePrefab;
    }
}

