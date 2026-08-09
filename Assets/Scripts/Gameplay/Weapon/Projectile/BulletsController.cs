using HalloGames.Architecture.Services;
using HalloGames.SpaceRTS.Gameplay.Projectile;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace HalloGames.SpaceRTS.Management.ProjectileManagement
{
    public abstract class ProjectileController<TProjectile> : MonoBehaviour where TProjectile : ProjectileWrapper
    {
        [SerializeField] protected LayerMask layerMask;

        protected List<TProjectile> shotProjectile = new List<TProjectile>();

        protected NativeList<RaycastHit> results;
        protected NativeList<int> colliderIDs;
        protected NativeList<RaycastCommand> raycastCommands;

        protected float deltaTime;

        private void Awake()
        {
            colliderIDs = new NativeList<int>(10, Allocator.Persistent);
            raycastCommands = new NativeList<RaycastCommand>(10, Allocator.Persistent);
            results = new NativeList<RaycastHit>(10, Allocator.Persistent);
        }

        public void AddProjectile(TProjectile projectileWrapper)
        {
            shotProjectile.Add(projectileWrapper);
        }

        private void Update()
        {
            ClearData();
            if (shotProjectile.Count == 0)
                return;

            CollectData();
            ScheduleMoving();
            Raycasts();
            UpdateLifeTime();
        }

        private void ClearData()
        {
            colliderIDs.Clear();
            raycastCommands.Clear();
            results.Clear();
        }

        private void CollectData()
        {
            deltaTime = Time.deltaTime;

            if (shotProjectile.Count > colliderIDs.Capacity)
            {
                colliderIDs.Capacity = shotProjectile.Count;
                raycastCommands.Capacity = shotProjectile.Count;

            }

            results.ResizeUninitialized(shotProjectile.Count);
            for (int i = 0; i < shotProjectile.Count; i++)
            {
                colliderIDs.AddNoResize(shotProjectile[i].ColliderInstanceId);
            }
        }

        private void DisposeData()
        {
            colliderIDs.Dispose();
            results.Dispose();
            raycastCommands.Dispose();
        }

        protected abstract void ScheduleMoving();

        private void Raycasts()
        {
            NativeList<int> filtered = new NativeList<int>(shotProjectile.Count, Allocator.TempJob);

            ProjectileRaycaster.Raycasts(raycastCommands, filtered, results, colliderIDs, shotProjectile.Count);

            for (int i = 0; i < filtered.Length; i++)
            {
                int id = filtered[i];
                shotProjectile[id].ExecuteHit(results[id].point, results[id].normal);
            }

            filtered.Dispose();
        }

        private void UpdateLifeTime()
        {
            for (int i = 0; i < shotProjectile.Count; i++)
            {
                if (shotProjectile[i].UpdateLifeTime() || shotProjectile[i].ToReturn)
                {
                    shotProjectile.RemoveAt(i);
                    i--;
                }
            }
        }

        private void OnDisable()
        {
            DisposeData();
        }
    }

    public class BulletsController : ProjectileController<BulletWrapper>, IService
    {
        protected override void ScheduleMoving()
        {
            deltaTime = Time.deltaTime;

            for (int i = 0; i < shotProjectile.Count; i++)
            {
                Vector3 pos = shotProjectile[i].Transform.position;
                Vector3 moveDelta = shotProjectile[i].Direction * shotProjectile[i].Velocity * deltaTime;

                RaycastCommand raycastCommand = new RaycastCommand(pos, moveDelta, moveDelta.magnitude, layerMask.value);
                raycastCommands.AddNoResize(raycastCommand);

                shotProjectile[i].Transform.position += moveDelta;
            }
        }
    }

    [BurstCompile]
    public struct RaycastResultJob : IJobParallelFor
    {
        [ReadOnly] public NativeList<RaycastHit> results;
        [ReadOnly] public NativeList<int> colliderIDs;
        [WriteOnly] public NativeList<int>.ParallelWriter filtered;
        public void Execute(int index)
        {
            if (results[index].colliderInstanceID == colliderIDs[index])
                filtered.AddNoResize(index);
        }
    }
}