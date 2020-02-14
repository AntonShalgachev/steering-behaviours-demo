using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace UnityPrototype
{
    [RequireComponent(typeof(SteeringBehaviourController))]
    public abstract class ISteeringBehaviour : MonoBehaviour
    {
        [System.Serializable]
        public class CommonBehaviourParameters
        {
            [Min(0.0f)] public float maxSpeedMultiplier = 1.0f;
            [Min(0.0f)] public float maxForceMultiplier = 1.0f;

            [Min(0.0f)] public float velocityAngleAttenuation = 15.0f;
            [Min(0.0f)] public float velocityMagnitudeAttenuation = 3.0f;
        }

        [SerializeField, Min(0.0f)] private float m_weight = 1.0f;
        [SerializeField] private CommonBehaviourParameters m_commonParameters = new CommonBehaviourParameters();

        public float weight => m_weight;

        private SteeringBehaviourController m_cachedController = null;
        protected SteeringBehaviourController m_controller
        {
            get
            {
                if (m_cachedController == null)
                {
                    m_cachedController = GetComponent<SteeringBehaviourController>();
                    if (m_cachedController == null)
                        Debug.LogError($"No {nameof(SteeringBehaviourController)} was found");
                }

                return m_cachedController;
            }
        }

        public Vector2 position => m_controller.position;
        public Vector2 velocity => m_controller.velocity;
        [ShowNativeProperty] public float maxSpeed => m_controller.maxSpeed * m_commonParameters.maxSpeedMultiplier;
        [ShowNativeProperty] public float maxSteeringForce => m_controller.maxSteeringForce * m_commonParameters.maxForceMultiplier;
        [ShowNativeProperty] public float maxAccelerationForce => m_controller.maxAccelerationForce * m_commonParameters.maxForceMultiplier;
        [ShowNativeProperty] public float maxBrakingForce => m_controller.maxBrakingForce * m_commonParameters.maxForceMultiplier;
        protected Vector2 m_forward => m_controller.forward;
        protected Vector2 m_right => m_controller.right;

        private Vector2 m_lastAppliedForce = Vector2.zero;
        private Vector2 m_lastAppliedForceComponents = Vector2.zero;
        [ShowNativeProperty] private float m_lastAppliedForceMagnitude => m_lastAppliedForce.magnitude;

        private int m_behaviourIndex = -1;
        private Color[] m_debugColors = new Color[]{
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.magenta,
            Color.cyan,
            Color.black,
            Color.white,
            Color.gray,
        };

        private void OnEnable()
        {
            m_behaviourIndex = m_controller.AddBehaviour(this);
            Debug.Assert(m_behaviourIndex >= 0);

            Debug.Assert(m_weight >= 0.0f);

            Initialize();
        }

        private void OnDisable()
        {
            m_controller.RemoveBehaviour(this);
        }

        public Vector2? CalculateLocalForce(float dt)
        {
            var force = CalculateForceComponentsInternal(dt);

            m_lastAppliedForceComponents = force.GetValueOrDefault(Vector2.zero) * m_weight;
            m_lastAppliedForce = m_controller.CalculateForceFromComponents(m_lastAppliedForceComponents);

            return force;
        }

        protected virtual void Initialize() { }

        protected abstract Vector2? CalculateForceComponentsInternal(float dt);

        public Vector2 CalculateForceForDirection(Vector2 direction, float speedMultiplier = 1.0f)
        {
            float epsilon = 1e-5f;

            Debug.Assert(Mathf.Abs(direction.magnitude - 1.0f) < epsilon, "Direction isn't normalized");
            return CalculateForceForVelocity(direction * maxSpeed * speedMultiplier);
        }

        public Vector2 CalculateForceForVelocity(Vector2 targetVelocity)
        {
            float stabilizationCoefficient = 0.0f;

            var speed = m_controller.speed;
            var targetSpeed = targetVelocity.magnitude;

            var angle = m_controller.angle;
            var targetAngle = Vector2.SignedAngle(Vector2.up, targetVelocity) - stabilizationCoefficient * m_controller.angularVelocity;

            var deltaSpeed = targetSpeed - speed;
            var deltaAngle = Mathf.DeltaAngle(angle, targetAngle);

            if (Mathf.Abs(targetSpeed) < Mathf.Epsilon)
                deltaAngle = 0.0f;

            var tangentMultiplier = Mathf.Clamp(deltaSpeed / m_commonParameters.velocityMagnitudeAttenuation, -1.0f, 1.0f);
            var normalMultiplier = -Mathf.Clamp(deltaAngle / m_commonParameters.velocityAngleAttenuation, -1.0f, 1.0f);

            var maxTangentForce = tangentMultiplier > 0.0f ? maxAccelerationForce : maxBrakingForce;

            var tangentForce = tangentMultiplier * maxTangentForce;
            var normalForce = normalMultiplier * maxSteeringForce;

            return new Vector2(normalForce, tangentForce);
        }

        protected virtual void DrawGizmos()
        {
            if (m_behaviourIndex < 0)
                return;

            var colorIndex = m_behaviourIndex % m_debugColors.Length;
            Gizmos.color = m_debugColors[colorIndex];

            // Gizmos.DrawLine(transform.position, transform.position + (Vector3)m_lastAppliedForce);

            GizmosHelper.DrawVector(transform.position, m_right * m_lastAppliedForceComponents.x);
            GizmosHelper.DrawVector(transform.position, m_forward * m_lastAppliedForceComponents.y);
        }

        private void OnDrawGizmos()
        {
            DrawGizmos();
        }
    }
}
