using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.VisualScripting;

public class HiderAgent : Agent
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 180f;
    private RayPerceptionSensorComponent3D rayPerceptionSensor;
    private Rigidbody rBody;
    private Vector3 lastKnownPosition;
    private float distanceToSeeker;
    private Vector3 nearestObstaclePosition;

    public override void Initialize()
    {
        rayPerceptionSensor = GetComponent<RayPerceptionSensorComponent3D>();
        rBody = GetComponent<Rigidbody>();
        lastKnownPosition = Vector3.zero;
        distanceToSeeker = 0f;
        nearestObstaclePosition = Vector3.zero;
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
        rBody.velocity = Vector3.zero;
        rBody.angularVelocity = Vector3.zero;
        lastKnownPosition = Vector3.zero;
        distanceToSeeker = 0f;
        nearestObstaclePosition = Vector3.zero;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(lastKnownPosition);
        sensor.AddObservation(distanceToSeeker);
        sensor.AddObservation(nearestObstaclePosition);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float moveForward = actionBuffers.ContinuousActions[0];
        float rotate = actionBuffers.ContinuousActions[1];

        rBody.MovePosition(transform.position + transform.forward * moveForward * moveSpeed * Time.deltaTime);
        transform.Rotate(0f, rotate * rotationSpeed * Time.deltaTime, 0f, Space.Self);

        nearestObstaclePosition = Vector3.zero;

        var rayOutputs = RayPerceptionSensor.Perceive(rayPerceptionSensor.GetRayPerceptionInput(), batched: true).RayOutputs;

        foreach (var detectedObject in rayOutputs)
        {
            if (detectedObject.HitGameObject != null)
            {
                if (detectedObject.HitGameObject.CompareTag("Obstacle"))
                {
                    nearestObstaclePosition = detectedObject.HitGameObject.transform.position;
                }
                if (detectedObject.HitGameObject.CompareTag("Seeker"))
                {
                    lastKnownPosition = detectedObject.HitGameObject.transform.position;
                    distanceToSeeker = Vector3.Distance(transform.position, detectedObject.HitGameObject.transform.position);
                    AddReward(-0.5f / distanceToSeeker);
                    break;
                }
            }
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
        continuousActionsOut[0] = Input.GetAxis("Vertical");
        continuousActionsOut[1] = Input.GetAxis("Horizontal");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Seeker"))
        {
            AddReward(-100f);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Seeker"))
        {
            AddReward(-3f);
        }
    }
}
