using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class HiderAgent : Agent
{
    public float moveSpeed = 25f;
    public float turnSpeed = 300f;
    private float previousDistance;
    private RayPerceptionSensorComponent3D rayPerceptionSensor;
    private Rigidbody rBody;

    public override void Initialize()
    {
        rayPerceptionSensor = GetComponent<RayPerceptionSensorComponent3D>();
        rBody = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = new Vector3(Random.Range(-10f, 10f), 0.5f, -8);
        transform.rotation = Quaternion.identity;
        rBody.velocity = Vector3.zero;
        rBody.angularVelocity = Vector3.zero;
        previousDistance = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(transform.rotation.y);

        var rayOutputs = RayPerceptionSensor.Perceive(rayPerceptionSensor.GetRayPerceptionInput()).RayOutputs;
        foreach (var rayOutput in rayOutputs)
        {
            sensor.AddObservation(rayOutput.HasHit ? 1.0f : 0.0f);
            sensor.AddObservation(rayOutput.HitFraction);
            sensor.AddObservation(rayOutput.HitGameObject ? rayOutput.HitGameObject.CompareTag("Seeker") ? 1.0f : 0.0f : 0.0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float moveX = actionBuffers.ContinuousActions[0];
        float moveZ = actionBuffers.ContinuousActions[1];
        float turn = actionBuffers.ContinuousActions[2];

        Vector3 move = new Vector3(moveX, 0, moveZ) * moveSpeed;
        rBody.velocity = move;

        transform.Rotate(0, turn * turnSpeed * Time.deltaTime, 0);

        AddReward(0.5f);

        var rayOutputs = RayPerceptionSensor.Perceive(rayPerceptionSensor.GetRayPerceptionInput()).RayOutputs;

        foreach (var rayOutput in rayOutputs)
        {
            if (rayOutput.HasHit)
            {
                if (rayOutput.HitGameObject.CompareTag("Seeker"))
                {
                    float distance = Vector3.Distance(transform.localPosition, rayOutput.HitGameObject.transform.localPosition);
                    float penalty = Mathf.Clamp(1 / distance, 0.01f, 1f);
                    AddReward(-penalty);

                    if (distance > previousDistance)
                    {
                        AddReward(0.1f);
                    }
                    previousDistance = distance;
                }
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
        continuousActionsOut[2] = Input.GetAxis("Mouse X");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Seeker"))
        {
            AddReward(-10f);
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
