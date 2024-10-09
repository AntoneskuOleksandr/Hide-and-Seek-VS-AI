using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class HiderAgent : Agent
{
    public float moveSpeed = 5f;
    public float turnSpeed = 180f;
    private Rigidbody rBody;

    public override void Initialize()
    {
        rBody = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        // Reset the position and velocity of the agent
        if (this.transform.localPosition.y < 0)
        {
            this.rBody.angularVelocity = Vector3.zero;
            this.rBody.velocity = Vector3.zero;
            this.transform.localPosition = new Vector3(0, 0.5f, 0);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Add agent's velocity to observations
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Actions, size = 2
        float moveX = actionBuffers.ContinuousActions[0];
        float moveZ = actionBuffers.ContinuousActions[1];

        Vector3 move = new Vector3(moveX, 0, moveZ) * moveSpeed * Time.deltaTime;
        rBody.AddForce(move, ForceMode.VelocityChange);

        // Rewards
        float distanceToCenter = Vector3.Distance(this.transform.localPosition, Vector3.zero);
        if (distanceToCenter < 1.42f)
        {
            SetReward(1.0f);
        }
        else
        {
            SetReward(-0.1f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }
}
