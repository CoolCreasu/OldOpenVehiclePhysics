using UnityEngine;

namespace VehiclePhysics
{
    public class Tyre : MonoBehaviour
    {
        #region Variables
        public float brakeTorque = 0.0f; // make property if working correctly!
        public float motorTorque = 0.0f;

        [Header("Wheel")]
        [SerializeField] private float radius = 0.34f;
        [SerializeField] private float mass = 20.0f;

        [Header("Friction")]
        [SerializeField] private float lowSpeedFrictionThreshold = 3.0f; // alternative friction to prevent sliding at low slip speed
        [SerializeField] private AnimationCurve frictionCurve = new AnimationCurve();
        
        [Header("Suspension")]
        [SerializeField] private float suspensionDistance = 0.5f;
        [SerializeField] private float springRate = 50000.0f;
        [SerializeField] private float damperRate = 2500.0f;
        
        [Header("Collision")]
        [SerializeField] private LayerMask collisionLayers = -1;

        private Transform cachedTransform = default;
        private Transform visualTransform = default;
        private Rigidbody cachedRigidbody = default;

        private Vector3 cachedPosition = Vector3.zero;
        private Quaternion orientation = Quaternion.identity;
        private Vector3 wheelForward = Vector3.zero;
        private Vector3 wheelRight = Vector3.zero;
        private Vector3 wheelUp = Vector3.zero;

        private bool isGrounded = false;
        private RaycastHit hitResult = default;

        private Vector3 projectedForward = Vector3.zero;
        private Vector3 projectedRight = Vector3.zero;

        private float previousSuspensionDistance = 0.0f;
        private float currentSuspensionDistance = 0.0f;

        private Vector3 globalVelocity = Vector3.zero;
        private Vector2 localVelocity = Vector2.zero;

        private Vector2 slip = Vector2.zero;
        private bool lowSpeedFriction = false;

        private Vector2 localGravitationalForce = Vector2.zero; // required force to prevent sliding due to gravity.
        private Vector2 localVelocityCounterForce = Vector2.zero; // required force to stop car's velocity (not accounting gravity)
        private Vector2 combinedCounterForce = Vector2.zero; // required force to keep car still

        private float inertia = 0.0f;

        private Vector3 wheelForce = Vector3.zero;

        private float visualRotation = 0.0f;
        #endregion

        #region Properties
        public float SteerAngle { get; set; } = 0.0f;
        public float Load { get; private set; } = 0.0f;
        public float AngularVelocity { get; private set; } = 0.0f;
        public float ReactionTorque { get; private set; } = 0.0f;
        #endregion

        #region Methods
        private void OnEnable()
        {
            // components
            cachedTransform = GetComponent<Transform>();
            visualTransform = cachedTransform.GetChild(0);
            cachedRigidbody = GetComponentInParent<Rigidbody>();

            if (cachedRigidbody == null)
            {
                Debug.LogWarning("Disabling wheel, rigidbody not found.");
                enabled = false;
                return;
            }

            // initialization
            currentSuspensionDistance = suspensionDistance;
            inertia = radius * radius * mass * 0.5f;
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;

            // transform related vectors
            cachedPosition = cachedTransform.position;
            orientation.eulerAngles = new Vector3(0.0f, SteerAngle, 0.0f);
            wheelForward = transform.TransformDirection(orientation * Vector3.forward);
            wheelRight = transform.TransformDirection(orientation * Vector3.right);
            wheelUp = transform.TransformDirection(orientation * Vector3.up);

            // collision detection
            isGrounded = Physics.Raycast(cachedPosition, -wheelUp, out hitResult, suspensionDistance + radius, collisionLayers, QueryTriggerInteraction.Ignore);

            // surface projection
            projectedForward = Vector3.Cross(hitResult.normal, -wheelRight);
            projectedRight = Vector3.Cross(hitResult.normal, wheelForward);

            // =================================================================================================================== //
            //  Suspension related calculations
            // =================================================================================================================== //

            // suspension
            previousSuspensionDistance = currentSuspensionDistance;
            currentSuspensionDistance = isGrounded ? hitResult.distance - radius : suspensionDistance;
            Load = ((suspensionDistance - currentSuspensionDistance) * springRate) + (((previousSuspensionDistance - currentSuspensionDistance) / deltaTime) * damperRate);
            Load = Load > 0 ? Load : 0;
            cachedRigidbody.AddForceAtPosition(Load * hitResult.normal, cachedPosition);

            // =================================================================================================================== //
            //  Velocity calculations
            // =================================================================================================================== //

            globalVelocity = cachedRigidbody.GetPointVelocity(cachedPosition - wheelUp * currentSuspensionDistance);
            globalVelocity = Vector3.ProjectOnPlane(globalVelocity, hitResult.normal);
            localVelocity.y = Vector3.Dot(globalVelocity, projectedForward);
            localVelocity.x = Vector3.Dot(globalVelocity, projectedRight);

            // =================================================================================================================== //
            //  Slip calculations
            // =================================================================================================================== //

            slip.y = localVelocity.y - AngularVelocity * radius;
            slip.x = -localVelocity.x;
            lowSpeedFriction = slip.magnitude <= lowSpeedFrictionThreshold;

            // =================================================================================================================== //
            //  Counter force calculation
            // =================================================================================================================== //

            Vector3 gravitationalForce = -Physics.gravity.normalized * Load * Vector3.Dot(-Physics.gravity.normalized, hitResult.normal);
            gravitationalForce = Vector3.ProjectOnPlane(gravitationalForce, hitResult.normal);
            localGravitationalForce.y = Vector3.Dot(gravitationalForce, projectedForward);
            localGravitationalForce.x = Vector3.Dot(gravitationalForce, projectedRight);

            localVelocityCounterForce = -localVelocity * (Load / Physics.gravity.magnitude) / deltaTime;

            combinedCounterForce = localGravitationalForce + localVelocityCounterForce;

            // =================================================================================================================== //
            //  Force related calculations
            // =================================================================================================================== //

            // Angular Velocity
            
            // calculate the friction torque and the max possible friction torque
            float frictionTorque = frictionCurve.Evaluate(slip.y) * Load * radius;
            float frictionTorqueLimit = (slip.y / radius) / deltaTime * inertia;

            float absBrakeTorque = brakeTorque >= 0 ? brakeTorque : -brakeTorque;
            float signedBrakeTorque = -absBrakeTorque * Mathf.Sign(AngularVelocity);
            float brakeTorqueLimit = -AngularVelocity / deltaTime * inertia;

            // caalculate the net torque
            float netTorque = frictionTorque + signedBrakeTorque + motorTorque;

            // ensure net torque does not exceed limits
            //if ((Mathf.Sign(netTorque) == Mathf.Sign(frictionTorqueLimit)) && (Mathf.Abs(netTorque) > Mathf.Abs(frictionTorqueLimit)))
            if ((Mathf.Sign(netTorque) == Mathf.Sign(frictionTorqueLimit)) && (Mathf.Abs(netTorque) > Mathf.Abs(frictionTorqueLimit)) && (Mathf.Abs(frictionTorque) >= Mathf.Abs(netTorque - frictionTorque)))
            {
                netTorque = frictionTorqueLimit;
            }

            //if ((Mathf.Sign(netTorque) == Mathf.Sign(signedBrakeTorque)) && (Mathf.Abs(netTorque) > Mathf.Abs(brakeTorqueLimit)) && (absBrakeTorque > 0))
            if ((Mathf.Sign(netTorque) == Mathf.Sign(signedBrakeTorque)) && (Mathf.Abs(netTorque) > Mathf.Abs(brakeTorqueLimit)) && (absBrakeTorque > 0) && (Mathf.Abs(signedBrakeTorque) >= Mathf.Abs(netTorque - signedBrakeTorque)))
            {
                netTorque = brakeTorqueLimit;
            }

            // calculate the new angular velocity
            AngularVelocity = AngularVelocity + (netTorque / inertia * deltaTime);
            ReactionTorque = netTorque - motorTorque;

            // Linear velocity

            // Longitudinal force
            float longitudinalForce = 0.0f;

            float brakeForce = Mathf.Clamp(combinedCounterForce.y * radius, -absBrakeTorque, absBrakeTorque);

            if (frictionTorqueLimit > 0.0f)
            {
                // TODO create different calculation for low speed and high speed brake torque.
                float friction = -(frictionTorque > frictionTorqueLimit ? frictionTorqueLimit : frictionTorque);

                longitudinalForce = lowSpeedFriction ? brakeForce + friction : Mathf.Clamp(brakeForce, -Mathf.Abs(friction), Mathf.Abs(friction)) + friction;
            }
            else
            {
                // TODO create different calculation for low speed and high speed brake torque.
                float friction = -(frictionTorque > frictionTorqueLimit ? frictionTorqueLimit : frictionTorque);

                longitudinalForce = lowSpeedFriction ? brakeForce + friction : Mathf.Clamp(brakeForce, -Mathf.Abs(friction), Mathf.Abs(friction)) + friction;
            }
            longitudinalForce = longitudinalForce / radius;

            // Lateral force, alternative friction : regular friction
            float lateralForce = lowSpeedFriction ? Load * Mathf.InverseLerp(lowSpeedFrictionThreshold, 0, slip.x) : frictionCurve.Evaluate(-localVelocity.x) * Load;
            lateralForce = Mathf.Clamp(combinedCounterForce.x, -Mathf.Abs(lateralForce), Mathf.Abs(lateralForce));

            wheelForce = projectedForward * (longitudinalForce) + projectedRight * (lateralForce);
            cachedRigidbody.AddForceAtPosition(wheelForce, cachedPosition - wheelUp * currentSuspensionDistance);

            // =================================================================================================================== //
            //  Visual related calculations
            // =================================================================================================================== //

            // visuals
            visualTransform.localPosition = new Vector3(0, -currentSuspensionDistance, 0);
            visualRotation = visualRotation + (AngularVelocity * Mathf.Rad2Deg * deltaTime);
            visualRotation = visualRotation > 360 ? visualRotation - 360 : visualRotation < 0 ? visualRotation + 360 : visualRotation;
            visualTransform.localEulerAngles = new Vector3(visualRotation, SteerAngle, 0.0f);
        }
        #endregion
    }
}