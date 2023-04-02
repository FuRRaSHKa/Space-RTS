using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketsController : ProjectelController<RocketWrapper>, IService
{
    protected override void SheludeMoving()
    {
        deltaTime = Time.deltaTime;

        for (int i = 0; i < shootedProjectel.Count; i++)
        {
            CalculateRoketMove(shootedProjectel[i]);

            RaycastCommand raycastCommand = new RaycastCommand(pos, moveDelta, moveDelta.magnitude, layerMask.value);
            raycastCommands.AddNoResize(raycastCommand);

            shootedProjectel[i].Transform.position += moveDelta;
        }
    }

    private void CalculateRoketMove(RocketWrapper rocket)
    {
        Vector3 pos = shootedProjectel[i].Transform.position;
        Vector3 moveDelta = shootedProjectel[i].Direction * shootedProjectel[i].Velocity * deltaTime;
    }
}
