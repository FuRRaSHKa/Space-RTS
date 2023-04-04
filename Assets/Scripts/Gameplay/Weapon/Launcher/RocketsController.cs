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
            CalculateRocketMove(shootedProjectel[i], out Vector3 pos, out Vector3 moveDelta);

            RaycastCommand raycastCommand = new RaycastCommand(pos, moveDelta, moveDelta.magnitude, layerMask.value);
            raycastCommands.AddNoResize(raycastCommand);

            shootedProjectel[i].Transform.position += moveDelta;
        }
    }

    private void CalculateRocketMove(RocketWrapper rocket, out Vector3 pos, out Vector3 moveDelta)
    {
        pos = rocket.Transform.position;
        RocketMovementStruct rocketMovementStruct = rocket.RocketMovement;

        Vector3 targetDirection = (rocket.TargetPos - pos).normalized;
        Vector3 currentDirection = Vector3.Lerp(rocketMovementStruct.currentVelocity, targetDirection * rocketMovementStruct.maxVelocity, rocketMovementStruct.inertia);

        moveDelta =  currentDirection * deltaTime;
        rocket.MoveRocket(currentDirection);
    }
}
