//
// Copyright (c) Brian Hernandez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using UnityEngine;

namespace MFlight.Demo
{
    /// <summary>
    /// Enhanced version of the plane controller with realistic aerodynamics.
    /// Includes lift, drag, angle of attack, and stall mechanics.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Plane : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private MouseFlightController controller = null;

        [Header("Physics")]
        [Tooltip("Maximum engine thrust in Newtons")] 
        public float maxThrust = 10000f; // Réduit pour une vitesse plus réaliste
        [Tooltip("Current throttle level (0-1)")] 
        public float throttle = 1f;
        [Tooltip("Pitch, Yaw, Roll control power")] 
        public Vector3 turnTorque = new Vector3(45f, 15f, 25f); // Réduit pour Unity 6
        [Tooltip("Force multiplier for all forces")] 
        public float forceMult = 20f; // Ajusté pour Unity 6

        [Header("Aerodynamics")]
        public AerodynamicProperties aero;

        [Header("Autopilot")]
        [Tooltip("Sensitivity for autopilot flight.")] 
        public float sensitivity = 5f;
        [Tooltip("Angle at which airplane banks fully into target.")] 
        public float aggressiveTurnAngle = 10f;

        [Header("Input")]
        [SerializeField] [Range(-1f, 1f)] private float pitch = 0f;
        [SerializeField] [Range(-1f, 1f)] private float yaw = 0f;
        [SerializeField] [Range(-1f, 1f)] private float roll = 0f;

        public float Pitch { set { pitch = Mathf.Clamp(value, -1f, 1f); } get { return pitch; } }
        public float Yaw { set { yaw = Mathf.Clamp(value, -1f, 1f); } get { return yaw; } }
        public float Roll { set { roll = Mathf.Clamp(value, -1f, 1f); } get { return roll; } }

        // Flight data
        private float angleOfAttack = 0f;
        private float airspeed = 0f;
        private bool isStalled = false;
        
        private Rigidbody rigid;
        private bool rollOverride = false;
        private bool pitchOverride = false;

        private void Awake()
        {
            rigid = GetComponent<Rigidbody>();
            if (rigid != null)
            {
                rigid.useGravity = true;
                rigid.linearDamping = 0f; // We'll handle drag manually
                rigid.angularDamping = 0f; // We'll handle angular drag manually
            }

            if (controller == null)
                Debug.LogError(name + ": Plane - Missing reference to MouseFlightController!");
        }

        private void Update()
        {
            HandleInput();
            UpdateFlightData();
        }

        private void HandleInput()
        {
            // When the player commands their own stick input, it should override what the
            // autopilot is trying to do.
            rollOverride = false;
            pitchOverride = false;

            float keyboardRoll = Input.GetAxis("Horizontal");
            if (Mathf.Abs(keyboardRoll) > .25f)
            {
                rollOverride = true;
            }

            float keyboardPitch = Input.GetAxis("Vertical");
            if (Mathf.Abs(keyboardPitch) > .25f)
            {
                pitchOverride = true;
                rollOverride = true;
            }

            // Throttle control
            if (Input.GetKey(KeyCode.LeftShift))
                throttle = Mathf.Min(1f, throttle + Time.deltaTime);
            if (Input.GetKey(KeyCode.LeftControl))
                throttle = Mathf.Max(0f, throttle - Time.deltaTime);

            // Calculate the autopilot stick inputs.
            float autoYaw = 0f;
            float autoPitch = 0f;
            float autoRoll = 0f;
            if (controller != null)
                RunAutopilot(controller.MouseAimPos, out autoYaw, out autoPitch, out autoRoll);

            // Use either keyboard or autopilot input.
            yaw = autoYaw;
            pitch = (pitchOverride) ? keyboardPitch : autoPitch;
            roll = (rollOverride) ? keyboardRoll : autoRoll;
        }

        private void UpdateFlightData()
        {
            if (rigid == null) return;

            // Calculate airspeed (magnitude of velocity)
            airspeed = rigid.linearVelocity.magnitude;

            // Calculate angle of attack (angle between velocity and forward direction)
            if (airspeed > 1f) // Only calculate AoA when moving
            {
                Vector3 velocityDir = rigid.linearVelocity.normalized;
                angleOfAttack = Vector3.SignedAngle(transform.forward, velocityDir, transform.right);
            }

            // Update stall state
            isStalled = Mathf.Abs(angleOfAttack) > aero.stallAngle;
        }

        private void RunAutopilot(Vector3 flyTarget, out float yaw, out float pitch, out float roll)
        {
            // This is my usual trick of converting the fly to position to local space.
            // You can derive a lot of information from where the target is relative to self.
            var localFlyTarget = transform.InverseTransformPoint(flyTarget).normalized * sensitivity;
            var angleOffTarget = Vector3.Angle(transform.forward, flyTarget - transform.position);

            // IMPORTANT!
            // These inputs are created proportionally. This means it can be prone to
            // overshooting. The physics in this example are tweaked so that it's not a big
            // issue, but in something with different or more realistic physics this might
            // not be the case. Use of a PID controller for each axis is highly recommended.

            // ====================
            // PITCH AND YAW
            // ====================

            // Yaw/Pitch into the target so as to put it directly in front of the aircraft.
            // A target is directly in front the aircraft if the relative X and Y are both
            // zero. Note this does not handle for the case where the target is directly behind.
            yaw = Mathf.Clamp(localFlyTarget.x, -1f, 1f);
            pitch = -Mathf.Clamp(localFlyTarget.y, -1f, 1f);

            // ====================
            // ROLL
            // ====================

            // Roll is a little special because there are two different roll commands depending
            // on the situation. When the target is off axis, then the plane should roll into it.
            // When the target is directly in front, the plane should fly wings level.

            // An "aggressive roll" is input such that the aircraft rolls into the target so
            // that pitching up (handled above) will put the nose onto the target. This is
            // done by rolling such that the X component of the target's position is zeroed.
            var agressiveRoll = Mathf.Clamp(localFlyTarget.x, -1f, 1f);

            // A "wings level roll" is a roll commands the aircraft to fly wings level.
            // This can be done by zeroing out the Y component of the aircraft's right.
            var wingsLevelRoll = transform.right.y;

            // Blend between auto level and banking into the target.
            var wingsLevelInfluence = Mathf.InverseLerp(0f, aggressiveTurnAngle, angleOffTarget);
            roll = Mathf.Lerp(wingsLevelRoll, agressiveRoll, wingsLevelInfluence);
        }

        private void FixedUpdate()
        {
            if (rigid == null || aero == null) return;

            // Get the current control effectiveness based on airspeed
            float controlEffect = aero.GetControlEffectiveness(airspeed);
            if (isStalled) controlEffect *= aero.stallControlEffectiveness;

            // Calculate lift coefficient
            float liftCoef = aero.CalculateLiftCoefficient(angleOfAttack * Mathf.Deg2Rad);
            
            // Calculate drag coefficient
            float dragCoef = aero.CalculateDragCoefficient(angleOfAttack * Mathf.Deg2Rad, liftCoef);

            // Calculate dynamic pressure
            float dynamicPressure = 0.5f * aero.airDensity * airspeed * airspeed;

            // Calculate lift and drag forces
            float liftForce = dynamicPressure * aero.wingArea * liftCoef;
            float dragForce = dynamicPressure * aero.wingArea * dragCoef;

            // Apply lift (perpendicular to velocity)
            Vector3 liftDirection = Vector3.Cross(rigid.linearVelocity.normalized, transform.right);
            rigid.AddForce(liftDirection * liftForce * aero.responseMultiplier, ForceMode.Force);

            // Apply drag (opposite to velocity)
            if (airspeed > 0.1f) // Prevent NaN issues at very low speeds
            {
                rigid.AddForce(-rigid.linearVelocity.normalized * dragForce * aero.responseMultiplier, ForceMode.Force);
            }

            // Apply engine thrust
            rigid.AddForce(transform.forward * maxThrust * throttle * forceMult, ForceMode.Force);

            // Apply control surface forces with effectiveness scaling
            Vector3 controlTorque = new Vector3(
                turnTorque.x * pitch,
                turnTorque.y * yaw,
                -turnTorque.z * roll
            );
            rigid.AddRelativeTorque(controlTorque * controlEffect * forceMult, ForceMode.Force);

            // Add stall turbulence if stalled
            if (isStalled)
            {
                Vector3 turbulence = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                );
                rigid.AddTorque(turbulence * aero.stallTurbulence * forceMult, ForceMode.Force);
            }
        }

        void OnGUI()
        {
            // Debug display
            GUILayout.Label($"Airspeed: {airspeed:F1} m/s");
            GUILayout.Label($"AoA: {angleOfAttack:F1}°");
            GUILayout.Label($"Stalled: {isStalled}");
            GUILayout.Label($"Throttle: {throttle:P0}");
        }
    }
}
