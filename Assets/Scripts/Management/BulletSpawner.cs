using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IProjecterCreator
{
    public BulletWrapper InstantiateProjectile(BulletData bulletData, ITargetable targetable, Transform parent, int damage, Vector3 direction);
}

public class BulletSpawner :  IProjecterCreator
{
    private BulletsController _bulletsController;

    public BulletSpawner(BulletsController bulletsController)
    {
        _bulletsController = bulletsController;
    }

    public BulletWrapper InstantiateProjectile(BulletData bulletData, ITargetable targetable, Transform parent, int damage, Vector3 direction)
    {
        PoolObject bulletPoolObject = PoolManager.Instance[bulletData.BulletPrefab].GetObject();
        bulletPoolObject.transform.position = parent.position;
        bulletPoolObject.transform.rotation = Quaternion.LookRotation(direction);

        BulletWrapper bulletWrapper = new BulletWrapper(bulletPoolObject, targetable, bulletData.Lifetime, damage, direction, bulletData.Speed);
        _bulletsController.AddBullet(bulletWrapper);

        return bulletWrapper;
    }
}
