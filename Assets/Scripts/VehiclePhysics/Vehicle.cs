using UnityEngine;

namespace VehiclePhysics
{
    public class Vehicle : MonoBehaviour
    {
        [Header("Wheels")]
        [SerializeField] private Wheel wheelFrontLeft = default;
        [SerializeField] private Wheel wheelFrontRight = default;
        [SerializeField] private Wheel wheelRearLeft = default;
        [SerializeField] private Wheel wheelRearRight = default;

        [SerializeField] private float maxSteerAngle = 45.0f;
        [SerializeField] private float motorTorque = 1000.0f;
        [SerializeField] private float brakeTorque = 8000.0f;

        private float steerInput;
        private float throttleInput;

        private void Update()
        {
            // steering
            steerInput = Input.GetAxis("Horizontal");

            wheelFrontLeft.SteerAngle = maxSteerAngle * steerInput;
            wheelFrontRight.SteerAngle = maxSteerAngle * steerInput;

            // motor torque
            throttleInput = Input.GetAxis("Vertical");

            wheelRearLeft.motorTorque = throttleInput * motorTorque;
            wheelRearRight.motorTorque = throttleInput * motorTorque;

            // braking
            float brakeInput = Input.GetAxis("Jump");
            float brakeForce = brakeInput * brakeTorque;
            wheelFrontLeft.brakeTorque = brakeForce;
            wheelFrontRight.brakeTorque = brakeForce;
            wheelRearLeft.brakeTorque = brakeForce;
            wheelRearRight.brakeTorque = brakeForce;
        }
    }
}