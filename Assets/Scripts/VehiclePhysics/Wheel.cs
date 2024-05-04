using System;
using UnityEngine;

namespace VehiclePhysics
{
    public class Wheel : MonoBehaviour
    {
        #region Variables
        [Header("Wheel")]
        [SerializeField] private float radius = 0.34f;
        [SerializeField] private float mass = 20.0f;
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

        private float inertia = 0.0f;

        private Vector3 wheelForce = Vector3.zero;

        private float visualRotation = 0.0f;
        #endregion

        #region Properties
        public float SteerAngle { get; set; } = 0.0f;
        public float Load { get; private set; } = 0.0f;
        public float AngularVelocity { get; private set; } = 0.0f;
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
            //  Force related calculations
            // =================================================================================================================== //

            // torques
            float frictionTorque = frictionCurve.Evaluate(localVelocity.y / radius - AngularVelocity) * Load * radius;
            float frictionTorqueLimit = (localVelocity.y / radius - AngularVelocity) / deltaTime * inertia;

            // forces
            float netForce = 0.0f;
            if (frictionTorqueLimit > 0.0f)
            {
                netForce = -(frictionTorque > frictionTorqueLimit ? frictionTorqueLimit : frictionTorque);
            }
            else
            {
                netForce = -(frictionTorque < frictionTorqueLimit ? frictionTorqueLimit : frictionTorque);
            }
            netForce = netForce / radius;

            wheelForce = projectedForward * (netForce) + projectedRight * (0.0f);
            cachedRigidbody.AddForceAtPosition(wheelForce, cachedPosition - wheelUp * currentSuspensionDistance);

            // =================================================================================================================== //
            //  Visual related calculations
            // =================================================================================================================== //

            // visuals
            visualTransform.localPosition = new Vector3(0, -currentSuspensionDistance, 0);
            visualRotation = visualRotation + (AngularVelocity * Mathf.Rad2Deg * deltaTime);
            visualTransform.localEulerAngles = new Vector3(visualRotation, SteerAngle, 0.0f);
        }
        #endregion
    }
}