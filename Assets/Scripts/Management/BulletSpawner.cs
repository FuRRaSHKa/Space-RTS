using HalloGames.Architecture.PoolSystem;
using HalloGames.Architecture.Services;
using HalloGames.SpaceRTS.Data.Projectile;
using HalloGames.SpaceRTS.Gameplay.Projectile;
using HalloGames.SpaceRTS.Gameplay.Targets;
using HalloGames.SpaceRTS.Management.ProjectileManagement;
using UnityEngine;

namespace HalloGames.SpaceRTS.Management.Factories
{
    public interface IProjectileCreator : IService
    {
        public ProjectileWrapper InstantiateProjectile(ProjectileData projectileData, ITargetable targetable, Transform parent, int damage, Vector3 direction);
    }

    public class BulletSpawner : IProjectileCreator
    {
        private readonly BulletsController _bulletsController;

        public BulletSpawner(BulletsController bulletsController)
        {
            _bulletsController = bulletsController;
        }

        public ProjectileWrapper InstantiateProjectile(ProjectileData projectileData, ITargetable targetable, Transform parent, int damage, Vector3 direction)
        {
            BulletData bulletData = projectileData as BulletData;
            if (bulletData == null)
                return null;

            PoolObject bulletPoolObject = PoolManager.Instance[bulletData.ProjectilePrefab].SpawnObject();
            bulletPoolObject.transform.position = parent.position;
            bulletPoolObject.transform.rotation = Quaternion.LookRotation(direction);

            BulletWrapper bulletWrapper = new BulletWrapper(bulletPoolObject.GetComponent<ProjectileObject>(), targetable, bulletData.Lifetime, damage, direction, bulletData.Speed);
            _bulletsController.AddProjectile(bulletWrapper);

            return bulletWrapper;
        }
    }

    public class RocketSpawner : IProjectileCreator
    {
        private readonly RocketsController _rocketsController;

        public RocketSpawner(RocketsController rocketsController)
        {
            _rocketsController = rocketsController;
        }

        public ProjectileWrapper InstantiateProjectile(ProjectileData projectileData, ITargetable targetable, Transform parent, int damage, Vector3 direction)
        {
            RocketData rocketData = projectileData as RocketData;
            if (rocketData == null)
                return null;

            PoolObject rocketPoolObject = PoolManager.Instance[rocketData.ProjectilePrefab].SpawnObject();
            rocketPoolObject.transform.position = parent.position;
            rocketPoolObject.transform.rotation = Quaternion.LookRotation(direction);

            RocketWrapper rocketWrapper = new RocketWrapper(rocketPoolObject.GetComponent<ProjectileObject>(), targetable, rocketData.Lifetime, damage, rocketData.RotationSpeed,
                rocketData.Speed, rocketData.Acceleration, rocketData.RotationAcceleration, rocketData.StartSpeed, direction);

            _rocketsController.AddProjectile(rocketWrapper);

            return rocketWrapper;
        }
    }
}
