using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class HiderAgent : Agent
{
    public float moveSpeed = 100f;
    private Rigidbody rBody;
    private RayPerceptionSensorComponent3D rayPerceptionSensor;

    public override void Initialize()
    {
        rayPerceptionSensor = GetComponent<RayPerceptionSensorComponent3D>();
        rBody = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        this.rBody.angularVelocity = Vector3.zero;
        this.rBody.velocity = Vector3.zero;
        this.transform.localPosition = new Vector3(0, 0.5f, 0);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // �������� ���������� ��� �������� ������
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);

        // �������� ���������� ��� ������� ������
        sensor.AddObservation(this.transform.localPosition);

        // �������� ���������� ��� ������� Seeker
        GameObject seeker = GameObject.FindWithTag("Seeker");
        if (seeker != null)
        {
            sensor.AddObservation(seeker.transform.localPosition);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float moveX = actionBuffers.ContinuousActions[0];
        float moveZ = actionBuffers.ContinuousActions[1];

        Vector3 move = new Vector3(moveX, 0, moveZ) * moveSpeed * Time.deltaTime;
        rBody.AddForce(move, ForceMode.VelocityChange);

        // ������� �� ����� �����
        AddReward(0.01f);

        // ��������� ������ �� RayPerceptionSensor3D
        var rayOutputs = RayPerceptionSensor.Perceive(rayPerceptionSensor.GetRayPerceptionInput()).RayOutputs;

        // �������� ���������� �� Seeker � ������� RayPerceptionSensor3D
        foreach (var rayOutput in rayOutputs)
        {
            if (rayOutput.HitGameObject.CompareTag("Seeker"))
            {
                // ���������� ������������� �������, ���� Seeker ���������
                AddReward(-0.01f);
            }
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    private void OnCollisionEnter(Collision collision)
    {
        // �������� �� ������� ������� � ����� "Seeker"
        if (collision.gameObject.CompareTag("Seeker"))
        {
            Debug.Log("Collision With Seeker");
            AddReward(-1f);
            EndEpisode();
        }
    }
}
