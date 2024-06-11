using UnityEngine;

namespace VehiclePhysics
{
    public class Vehicle : MonoBehaviour
    {
        public float rpm = 0.0f;
        public float torque = 0.0f;
        public Engine engine = new Engine();

        [Header("Wheels")]
        [SerializeField] private Tyre wheelFrontLeft = default;
        [SerializeField] private Tyre wheelFrontRight = default;
        [SerializeField] private Tyre wheelRearLeft = default;
        [SerializeField] private Tyre wheelRearRight = default;

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

            float tr = (wheelFrontLeft.ReactionTorque + wheelFrontRight.ReactionTorque) * 0.5f;

            engine.EngineInput(Time.fixedDeltaTime, tr);

            engine.Throttle = throttleInput;
            engine.EngineOutput(Time.fixedDeltaTime);
            torque = engine.Torque;
            rpm = engine.RPM;

            //torque = engine.EvaluateRPM(rpm) * throttleInput;

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

        private void FixedUpdate()
        {
            
        }
    }
}