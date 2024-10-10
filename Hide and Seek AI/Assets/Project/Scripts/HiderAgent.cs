using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class HiderAgent : Agent
{
    public float moveSpeed = 100f;
    public float turnSpeed = 300f;
    private RayPerceptionSensorComponent3D rayPerceptionSensor;
    private Rigidbody rBody;

    public override void Initialize()
    {
        rayPerceptionSensor = GetComponent<RayPerceptionSensorComponent3D>();
        rBody = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = new Vector3(0, 0.5f, 0);
        transform.rotation = Quaternion.identity;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(transform.rotation.y);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float moveX = actionBuffers.ContinuousActions[0];
        float moveZ = actionBuffers.ContinuousActions[1];
        float turn = actionBuffers.ContinuousActions[2]; 

        Vector3 move = new Vector3(moveX, 0, moveZ) * moveSpeed * Time.deltaTime;
        rBody.AddForce(move, ForceMode.VelocityChange);

        transform.Rotate(0, turn * turnSpeed * Time.deltaTime, 0);

        AddReward(0.01f);

        var rayOutputs = RayPerceptionSensor.Perceive(rayPerceptionSensor.GetRayPerceptionInput()).RayOutputs;

        foreach (var rayOutput in rayOutputs)
        {
            if (rayOutput.HitGameObject.CompareTag("Seeker"))
            {
                float distance = Vector3.Distance(transform.localPosition, rayOutput.HitGameObject.transform.localPosition);

                float penalty = Mathf.Clamp(1 / distance, 0.01f, 1f);
                AddReward(-penalty);
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
            AddReward(-100f);
            EndEpisode();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Seeker"))
        {
            AddReward(-0.1f);
        }
    }
}
