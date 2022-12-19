using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public interface IMovementController
{
    public void MoveTo(Vector3 targetPos);
}

public class ShipMovementController : MonoBehaviour, IMovementController, IInitilizable<ShipInitilizationData>
{
    [SerializeField] private NavMeshAgent _navMesh;

    public void Init(ShipInitilizationData data)
    {
        _navMesh.acceleration = data.ShipData.ShipHullData.DefaultShipAcceleration;
        _navMesh.speed = data.ShipData.ShipHullData.DefaultShipSpeed;
        _navMesh.angularSpeed = data.ShipData.ShipHullData.DefaultShipRotationSpeed;
    }

    public void MoveTo(Vector3 targetPos)
    {
        _navMesh.SetDestination(targetPos);
    }
}
