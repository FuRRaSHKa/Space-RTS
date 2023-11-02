using HalloGames.Architecture.Services;
using HalloGames.SpaceRTS.Gameplay.Projectile;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace HalloGames.SpaceRTS.Management.ProjectileManagement
{
    public abstract class ProjectilelController<TProjectile> : MonoBehaviour where TProjectile : ProjectileWrapper
    {
        [SerializeField] protected LayerMask layerMask;

        protected List<TProjectile> shootedProjectile = new List<TProjectile>();

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

        public void AddProjectile(TProjectile projectilelWrapper)
        {
            shootedProjectile.Add(projectilelWrapper);
        }

        private void Update()
        {
            ClearData();
            if (shootedProjectile.Count == 0)
                return;

            CollectData();
            SheludeMoving();
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

            if (shootedProjectile.Count > colliderIDs.Capacity)
            {
                colliderIDs.Capacity = shootedProjectile.Count;
                raycastCommands.Capacity = shootedProjectile.Count;

            }

            results.ResizeUninitialized(shootedProjectile.Count);
            for (int i = 0; i < shootedProjectile.Count; i++)
            {
                colliderIDs.AddNoResize(shootedProjectile[i].ColliderInstanceId);
            }
        }

        private void DisposeData()
        {
            colliderIDs.Dispose();
            results.Dispose();
            raycastCommands.Dispose();
        }

        protected abstract void SheludeMoving();

        private void Raycasts()
        {
            NativeList<int> filtered = new NativeList<int>(shootedProjectile.Count, Allocator.TempJob);

            ProjectileRaycaster.Raycasts(raycastCommands, filtered, results, colliderIDs, shootedProjectile.Count);

            for (int i = 0; i < filtered.Length; i++)
            {
                int id = filtered[i];
                shootedProjectile[id].ExecuteHit(results[id].point, results[id].normal);
            }

            filtered.Dispose();
        }

        private void UpdateLifeTime()
        {
            for (int i = 0; i < shootedProjectile.Count; i++)
            {
                if (shootedProjectile[i].UpdateLifeTime() || shootedProjectile[i].ToReturn)
                {
                    shootedProjectile.RemoveAt(i);
                    i--;
                }
            }
        }

        private void OnDisable()
        {
            DisposeData();
        }
    }

    public class BulletsController : ProjectilelController<BulletWrapper>, IService
    {
        protected override void SheludeMoving()
        {
            deltaTime = Time.deltaTime;

            for (int i = 0; i < shootedProjectile.Count; i++)
            {
                Vector3 pos = shootedProjectile[i].Transform.position;
                Vector3 moveDelta = shootedProjectile[i].Direction * shootedProjectile[i].Velocity * deltaTime;

                RaycastCommand raycastCommand = new RaycastCommand(pos, moveDelta, moveDelta.magnitude, layerMask.value);
                raycastCommands.AddNoResize(raycastCommand);

                shootedProjectile[i].Transform.position += moveDelta;
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