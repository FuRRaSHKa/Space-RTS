using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace HalloGames.SpaceRTS.Management.ProjectileManagement
{
    public static class ProjectileRaycaster
    {
        public static void Raycasts(NativeList<RaycastCommand> raycastCommand, NativeList<int> filtered, NativeList<RaycastHit> results, NativeList<int> colliderIDs, int count)
        {
            RaycastResultJob raycastResultJob = new RaycastResultJob()
            {
                results = results,
                colliderIDs = colliderIDs,

                filtered = filtered.AsParallelWriter()
            };

            var raycasts = RaycastCommand.ScheduleBatch(raycastCommand, results, 1);
            JobHandle jobHandle = raycastResultJob.Schedule(count, 32, raycasts);

            raycasts.Complete();
            jobHandle.Complete();
        }

    }
}