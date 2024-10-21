using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class SeekerAgent : Agent
{
    public float moveSpeed = 3f;
    private RayPerceptionSensorComponent3D rayPerceptionSensor;
    private Rigidbody rBody;
    private Vector3 lastKnownPosition;
    private bool hiderVisible;
    private float distanceToHider;

    public override void Initialize()
    {
        rayPerceptionSensor = GetComponent<RayPerceptionSensorComponent3D>();
        rBody = GetComponent<Rigidbody>();
        lastKnownPosition = Vector3.zero;
        hiderVisible = false;
        distanceToHider = 0f;
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = Vector3.zero;
        rBody.velocity = Vector3.zero;
        rBody.angularVelocity = Vector3.zero;
        lastKnownPosition = Vector3.zero;
        hiderVisible = false;
        distanceToHider = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(rayPerceptionSensor);
        sensor.AddObservation(transform.InverseTransformPoint(lastKnownPosition));
        sensor.AddObservation(distanceToHider);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float moveX = actionBuffers.ContinuousActions[0];
        float moveZ = actionBuffers.ContinuousActions[1];
        Vector3 move = transform.TransformDirection(new Vector3(moveX, 0, moveZ)) * moveSpeed * 100f * Time.deltaTime;
        rBody.velocity = move;
        hiderVisible = false;
        var rayOutputs = RayPerceptionSensor.Perceive(rayPerceptionSensor.GetRayPerceptionInput(), batched: true).RayOutputs;
        foreach (var detectedObject in rayOutputs)
        {
            if (detectedObject.HitGameObject != null && detectedObject.HitGameObject.CompareTag("Hider"))
            {
                hiderVisible = true;
                lastKnownPosition = transform.InverseTransformPoint(detectedObject.HitGameObject.transform.position);
                distanceToHider = Vector3.Distance(transform.position, detectedObject.HitGameObject.transform.position);
                AddReward(0.5f / distanceToHider);
                break;
            }
        }
        if (!hiderVisible)
        {
            AddReward(-0.01f);
        }
        AddReward(-0.1f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Hider"))
        {
            AddReward(100f);
            EndEpisode();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Hider"))
        {
            AddReward(1f);
        }
    }
}
