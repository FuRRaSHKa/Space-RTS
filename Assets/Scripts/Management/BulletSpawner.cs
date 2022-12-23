using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IProjecterCreator
{
    public void InstantiateProjectile(BulletData bulletData, ITargetable targetable, Transform parent, int damage, Vector3 direction);
}

public class BulletSpawner :  IProjecterCreator
{
    private IBulletsController _bulletsController;

    public BulletSpawner(IBulletsController bulletsController)
    {
        _bulletsController = bulletsController;
    }

    public void InstantiateProjectile(BulletData bulletData, ITargetable targetable, Transform parent, int damage, Vector3 direction)
    {
        PoolObject bulletPoolObject = PoolManager.Instance[bulletData.BulletPrefab].GetObject();
        bulletPoolObject.transform.position = parent.position;
        bulletPoolObject.transform.rotation = Quaternion.LookRotation(direction);

        BulletWrapper bulletWrapper = new BulletWrapper(direction, bulletPoolObject, targetable, bulletData.Lifetime, bulletData.Speed, damage);
        _bulletsController.AddBullet(bulletWrapper);
    }
}
