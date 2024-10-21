using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents;
using UnityEngine;

public class HiderAgent : Agent
{
    public float moveSpeed = 3f;
    private RayPerceptionSensorComponent3D rayPerceptionSensor;
    private Rigidbody rBody;
    private Vector3 lastKnownPosition;
    private bool seekerVisible;
    private float distanceToSeeker;
    private Vector3 nearestObstaclePosition;

    public override void Initialize()
    {
        rayPerceptionSensor = GetComponent<RayPerceptionSensorComponent3D>();
        rBody = GetComponent<Rigidbody>();
        lastKnownPosition = Vector3.zero;
        seekerVisible = false;
        distanceToSeeker = 0f;
        nearestObstaclePosition = Vector3.zero;
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = new Vector3(Random.Range(-10f, 10f), 0.5f, Random.Range(-10f, 10f));
        rBody.velocity = Vector3.zero;
        rBody.angularVelocity = Vector3.zero;
        lastKnownPosition = Vector3.zero;
        seekerVisible = false;
        distanceToSeeker = 0f;
        nearestObstaclePosition = Vector3.zero;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(rayPerceptionSensor);
        sensor.AddObservation(transform.InverseTransformPoint(lastKnownPosition));
        sensor.AddObservation(distanceToSeeker);
        sensor.AddObservation(transform.InverseTransformPoint(nearestObstaclePosition));
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float moveX = actionBuffers.ContinuousActions[0];
        float moveZ = actionBuffers.ContinuousActions[1];
        Vector3 move = transform.TransformDirection(new Vector3(moveX, 0, moveZ)) * moveSpeed * 100f * Time.deltaTime;
        rBody.velocity = move;

        seekerVisible = false;
        nearestObstaclePosition = Vector3.zero;

        var rayOutputs = RayPerceptionSensor.Perceive(rayPerceptionSensor.GetRayPerceptionInput(), batched: true).RayOutputs;

        foreach (var detectedObject in rayOutputs)
        {
            if (detectedObject.HitGameObject != null)
            {
                if (detectedObject.HitGameObject.CompareTag("Seeker"))
                {
                    seekerVisible = true;
                    lastKnownPosition = transform.InverseTransformPoint(detectedObject.HitGameObject.transform.position);
                    distanceToSeeker = Vector3.Distance(transform.position, detectedObject.HitGameObject.transform.position);
                    AddReward(-0.5f / distanceToSeeker);
                }
                else if (detectedObject.HitGameObject.CompareTag("Obstacle"))
                {
                    nearestObstaclePosition = transform.InverseTransformPoint(detectedObject.HitGameObject.transform.position);
                }
            }
        }
        if (!seekerVisible)
        {
            AddReward(0.01f);
        }

        if (nearestObstaclePosition != Vector3.zero && Vector3.Distance(transform.position, nearestObstaclePosition) < 1.0f)
        {
            AddReward(0.1f);
        }
        AddReward(0.1f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Seeker"))
        {
            AddReward(-100f);
            EndEpisode();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Seeker"))
        {
            AddReward(-1f);
        }
    }
}
