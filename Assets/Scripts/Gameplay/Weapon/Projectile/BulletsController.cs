using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public abstract class ProjectelController<TProjectel> : MonoBehaviour where TProjectel : ProjectileWrapper
{
    [SerializeField] protected LayerMask layerMask;

    protected List<TProjectel> shootedProjectel = new List<TProjectel>();

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

    public void AddBullet(TProjectel projectelWrapper)
    {
        shootedProjectel.Add(projectelWrapper);
    }

    private void Update()
    {
        ClearData();
        if (shootedProjectel.Count == 0)
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

        if (shootedProjectel.Count > colliderIDs.Capacity)
        {
            colliderIDs.Capacity = shootedProjectel.Count;
            raycastCommands.Capacity = shootedProjectel.Count;

        }

        results.ResizeUninitialized(shootedProjectel.Count);
        for (int i = 0; i < shootedProjectel.Count; i++)
        {
            colliderIDs.AddNoResize(shootedProjectel[i].ColliderInstanceId);
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
        NativeList<int> filtered = new NativeList<int>(shootedProjectel.Count, Allocator.TempJob);

        ProjectileRaycaster.Raycasts(raycastCommands, filtered, results, colliderIDs, shootedProjectel.Count);

        for (int i = 0; i < filtered.Length; i++)
        {
            int id = filtered[i];
            shootedProjectel[id].ExecuteHit(results[id].point, results[id].normal);
        }

        filtered.Dispose();
    }

    private void UpdateLifeTime()
    {
        for (int i = 0; i < shootedProjectel.Count; i++)
        {
            if (shootedProjectel[i].UpdateLifeTime() || shootedProjectel[i].ToReturn)
            {
                shootedProjectel.RemoveAt(i);
                i--;
            }
        }
    }

    private void OnDisable()
    {
        DisposeData();
    }
}

public class BulletsController : ProjectelController<BulletWrapper>, IService
{
    protected override void SheludeMoving()
    {
        deltaTime = Time.deltaTime;

        for (int i = 0; i < shootedProjectel.Count; i++)
        {
            Vector3 pos = shootedProjectel[i].Transform.position;
            Vector3 moveDelta = shootedProjectel[i].Direction * shootedProjectel[i].Velocity * deltaTime;

            RaycastCommand raycastCommand = new RaycastCommand(pos, moveDelta, moveDelta.magnitude, layerMask.value);
            raycastCommands.AddNoResize(raycastCommand);

            shootedProjectel[i].Transform.position += moveDelta;
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
        {
            filtered.AddNoResize(index);
        }
    }
}