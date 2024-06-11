using UnityEngine;

namespace VehiclePhysics
{
    public class Wheel : MonoBehaviour
    {
        #region Variables
        [Header("Wheel")]
        [SerializeField] private float radius = 0.34f;
        [SerializeField] private float mass = 20.0f;
        private float inertia = 1.156f;

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

        private float previousSuspensionDistance = 0.0f;
        private float currentSuspensionDistance = 0.0f;

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
            cachedTransform = GetComponent<Transform>();
            visualTransform = cachedTransform.GetChild(0);
            cachedRigidbody = GetComponentInParent<Rigidbody>();

            if (cachedRigidbody == null)
            {
                Debug.LogWarning("Disabling wheel. rigidbody not found.");
                enabled = false;
                return;
            }

            // initialization
            currentSuspensionDistance = suspensionDistance;
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;

            cachedPosition = cachedTransform.position;
            orientation.eulerAngles = new Vector3(0, SteerAngle, 0);
            wheelForward = cachedTransform.TransformDirection(orientation * Vector3.forward);
            wheelRight = cachedTransform.TransformDirection(orientation * Vector3.right);
            wheelUp = cachedTransform.TransformDirection(orientation * Vector3.up);

            isGrounded = Physics.Raycast(cachedPosition, -wheelUp, out hitResult, suspensionDistance + radius, collisionLayers, QueryTriggerInteraction.Ignore);

            previousSuspensionDistance = currentSuspensionDistance;
            currentSuspensionDistance = isGrounded ? hitResult.distance - radius : suspensionDistance;
            Load = ((suspensionDistance - currentSuspensionDistance) * springRate) + (((previousSuspensionDistance - currentSuspensionDistance) / deltaTime) * damperRate);
            Load = Load > 0 ? Load : 0;
            cachedRigidbody.AddForceAtPosition(Load * wheelUp, cachedPosition);

            visualTransform.localPosition = new Vector3(0, -currentSuspensionDistance, 0);
            visualRotation = visualRotation + (AngularVelocity * Mathf.Rad2Deg * deltaTime);
            visualRotation = visualRotation > 360 ? visualRotation - 360 : visualRotation < 0 ? visualRotation + 360 : visualRotation;
            visualTransform.localEulerAngles = new Vector3(visualRotation, SteerAngle, 0);
        }
        #endregion
    }
}