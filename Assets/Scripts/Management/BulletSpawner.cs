using HalloGames.Architecture.PoolSystem;
using HalloGames.Architecture.Services;
using HalloGames.SpaceRTS.Data.Projectel;
using HalloGames.SpaceRTS.Gameplay.Projectel;
using HalloGames.SpaceRTS.Gameplay.Targets;
using HalloGames.SpaceRTS.Management.ProjectelManagement;
using UnityEngine;

namespace HalloGames.SpaceRTS.Management.Factories
{
    public interface IProjecterCreator : IService
    {
        public ProjectileWrapper InstantiateProjectile(ProjectelData projectelData, ITargetable targetable, Transform parent, int damage, Vector3 direction);
    }

    public class BulletSpawner : IProjecterCreator
    {
        private readonly BulletsController _bulletsController;

        public BulletSpawner(BulletsController bulletsController)
        {
            _bulletsController = bulletsController;
        }

        public ProjectileWrapper InstantiateProjectile(ProjectelData projectelData, ITargetable targetable, Transform parent, int damage, Vector3 direction)
        {
            BulletData bulletData = projectelData as BulletData;
            if (bulletData == null)
                return null;

            PoolObject bulletPoolObject = PoolManager.Instance[bulletData.ProjectelPrefab].SpawnObject();
            bulletPoolObject.transform.position = parent.position;
            bulletPoolObject.transform.rotation = Quaternion.LookRotation(direction);

            BulletWrapper bulletWrapper = new BulletWrapper(bulletPoolObject.GetComponent<ProjectelObject>(), targetable, bulletData.Lifetime, damage, direction, bulletData.Speed);
            _bulletsController.AddProjectel(bulletWrapper);

            return bulletWrapper;
        }
    }

    public class RocketSpawner : IProjecterCreator
    {
        private readonly RocketsController _rocketsController;

        public RocketSpawner(RocketsController rocketsController)
        {
            _rocketsController = rocketsController;
        }

        public ProjectileWrapper InstantiateProjectile(ProjectelData projectelData, ITargetable targetable, Transform parent, int damage, Vector3 direction)
        {
            RocketData rocketData = projectelData as RocketData;
            if (rocketData == null)
                return null;

            PoolObject rocketPoolObject = PoolManager.Instance[rocketData.ProjectelPrefab].SpawnObject();
            rocketPoolObject.transform.position = parent.position;
            rocketPoolObject.transform.rotation = Quaternion.LookRotation(direction);

            RocketWrapper rocketWrapper = new RocketWrapper(rocketPoolObject.GetComponent<ProjectelObject>(), targetable, rocketData.Lifetime, damage, rocketData.RotationSpeed,
                rocketData.Speed, rocketData.Acceleration, rocketData.RotationAcceleration, rocketData.StartSpeed, direction);

            _rocketsController.AddProjectel(rocketWrapper);

            return rocketWrapper;
        }
    }
}
