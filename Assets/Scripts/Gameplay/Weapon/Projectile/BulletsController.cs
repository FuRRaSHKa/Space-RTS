using HalloGames.Architecture.Frames;
using HalloGames.Architecture.Services;
using HalloGames.SpaceRTS.Gameplay.Projectile;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Serialization;

namespace HalloGames.SpaceRTS.Management.ProjectileManagement
{
    public abstract class ProjectileController<TProjectile> : MonoBehaviour, IUpdatable where TProjectile : ProjectileWrapper
    {
        [FormerlySerializedAs("layerMask")]
        [SerializeField] private LayerMask _layerMask;

        private NativeList<RaycastHit> _results;
        private NativeList<int> _colliderIDs;
        private NativeList<RaycastCommand> _raycastCommands;
        private NativeList<int> _filtered;

        private readonly List<int> _commandOwners = new List<int>();

        protected List<TProjectile> ShotProjectile { get; } = new List<TProjectile>();
        protected float DeltaTime { get; private set; }
        protected int LayerMaskValue => _layerMask.value;

        private void OnEnable()
        {
            _colliderIDs = new NativeList<int>(10, Allocator.Persistent);
            _raycastCommands = new NativeList<RaycastCommand>(10, Allocator.Persistent);
            _results = new NativeList<RaycastHit>(10, Allocator.Persistent);
            _filtered = new NativeList<int>(10, Allocator.Persistent);

            TickManager.RegisterUpdate(this);
        }

        public void AddProjectile(TProjectile projectileWrapper)
        {
            ShotProjectile.Add(projectileWrapper);
        }

        public void UpdateTick(float delta)
        {
            DeltaTime = delta;

            ClearData();
            if (ShotProjectile.Count == 0)
                return;

            EnsureCapacity();
            ScheduleMoving();
            Raycasts();
            UpdateLifeTime();
        }

        private void ClearData()
        {
            _colliderIDs.Clear();
            _raycastCommands.Clear();
            _results.Clear();
            _filtered.Clear();
            _commandOwners.Clear();
        }

        private void EnsureCapacity()
        {
            if (ShotProjectile.Count > _colliderIDs.Capacity)
            {
                _colliderIDs.Capacity = ShotProjectile.Count;
                _raycastCommands.Capacity = ShotProjectile.Count;
                _filtered.Capacity = ShotProjectile.Count;

            }
        }

        private void DisposeData()
        {
            _colliderIDs.Dispose();
            _results.Dispose();
            _raycastCommands.Dispose();
            _filtered.Dispose();
        }

        protected abstract void ScheduleMoving();

        protected void AddRaycastCommand(int projectileIndex, RaycastCommand raycastCommand)
        {
            _raycastCommands.AddNoResize(raycastCommand);
            _colliderIDs.AddNoResize(ShotProjectile[projectileIndex].ColliderInstanceId);
            _commandOwners.Add(projectileIndex);
        }

        private void Raycasts()
        {
            if (_raycastCommands.Length == 0)
                return;

            _results.ResizeUninitialized(_raycastCommands.Length);
            ProjectileRaycaster.Raycasts(_raycastCommands, _filtered, _results, _colliderIDs, _raycastCommands.Length);

            for (int i = 0; i < _filtered.Length; i++)
            {
                int id = _filtered[i];
                ShotProjectile[_commandOwners[id]].ExecuteHit(_results[id].point, _results[id].normal);
            }
        }

        private void UpdateLifeTime()
        {
            for (int i = 0; i < ShotProjectile.Count; i++)
            {
                if (ShotProjectile[i].UpdateLifeTime(DeltaTime) || ShotProjectile[i].ToReturn)
                {
                    ShotProjectile.RemoveAt(i);
                    i--;
                }
            }
        }

        private void OnDisable()
        {
            TickManager.UnregisterUpdate(this);

            DisposeData();
        }
    }

    public class BulletsController : ProjectileController<BulletWrapper>, IService
    {
        protected override void ScheduleMoving()
        {
            for (int i = 0; i < ShotProjectile.Count; i++)
            {
                Vector3 pos = ShotProjectile[i].Transform.position;
                Vector3 moveDelta = ShotProjectile[i].Direction * ShotProjectile[i].Velocity * DeltaTime;

                float distance = moveDelta.magnitude;
                if (distance > Vector3.kEpsilon)
                    AddRaycastCommand(i, new RaycastCommand(pos, moveDelta / distance, distance, LayerMaskValue));

                ShotProjectile[i].Transform.position += moveDelta;
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