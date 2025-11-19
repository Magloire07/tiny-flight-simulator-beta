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
        public float maxThrust = 13000f; // Ajusté pour permettre le décollage avec masse 750 kg
        [Tooltip("Current throttle level (0-1)")] 
        public float throttle = 0f;
        [Tooltip("Throttle change rate (units per second). Réduit pour ralentir l'incrémentation via Shift/Ctrl.")]
        public float throttleChangeRate = 0.5f; // Permet d'atteindre la pleine poussée plus rapidement
        [Tooltip("Pitch, Yaw, Roll control power")] 
        public Vector3 turnTorque = new Vector3(12f, 6f, 10f); // Ajusté pour un contrôle réaliste
        [Tooltip("Force multiplier for all forces")] 
        public float forceMult = 1f; // 1 = réaliste, >1 = arcade
        [Tooltip("Angular damping applied to the rigidbody (0 = aucun amortissement)")]
        public float angularDamping = 0f; // manual control: no passive angular drag by default
        [Tooltip("Enable Unity gravity on the Rigidbody (toggle in Inspector)")]
        public bool enableGravity = false;

        // Stabilization removed: manual control only (auto-level, yaw damping disabled)

        [Header("Aerodynamics")]
        public AerodynamicProperties aero;
        
        [Header("Filters")]
        [Tooltip("Smooth AoA when airspeed is below this (m/s)")]
        public float aoaSmoothingMaxSpeed = 20f;
        [Tooltip("Time constant for low-speed AoA smoothing (seconds)")]
        public float aoaSmoothingTime = 0.2f;
        private float aoaSmoothVel = 0f;

        [Header("Autopilot")]
        [Tooltip("Sensitivity for autopilot flight.")] 
        public float sensitivity = 5f;
        [Tooltip("Angle at which airplane banks fully into target.")] 
        public float aggressiveTurnAngle = 10f;

        [Header("Input")]
        [SerializeField] [Range(-1f, 1f)] private float pitch = 0f;
        [SerializeField] [Range(-1f, 1f)] private float yaw = 0f;
        [SerializeField] [Range(-1f, 1f)] private float roll = 0f;
        [SerializeField] [Range(-1f, 1f)] private float aileronSymmetric = 0f; // Up/Down arrows both ailerons same direction
        [Header("Direct Input Mapping")]
        [Tooltip("Use direct input mapping (mouse: yaw/pitch, arrows: roll). Disables autopilot inputs.")]
        public bool useDirectInputMapping = true;
        [Tooltip("Mouse sensitivity for yaw (Mouse X) -> rudder")]
        public float mouseYawSensitivity = 0.75f;
        [Tooltip("Mouse sensitivity for pitch (Mouse Y) -> elevator")]
        public float mousePitchSensitivity = 0.75f;
        [Tooltip("Keyboard sensitivity for roll (Horizontal + Vertical) -> ailerons")]
        public float keyboardRollSensitivity = 1.0f;
        [Tooltip("Keyboard sensitivity for pitch (Vertical arrows/W/S) -> elevator")]
        public float keyboardPitchSensitivity = 1.0f;
        [Tooltip("Keyboard sensitivity for yaw (Horizontal arrows/A/D) -> rudder (combined with roll)")]
        public float keyboardYawSensitivity = 1.0f;
        [Header("Mouse Input Options")] 
        [Tooltip("Allow mouse to influence pitch & yaw")] public bool enableMousePitchYaw = true;
        [Tooltip("Require a mouse button to be held for mouse pitch/yaw")] public bool requireMouseButton = false;
        [Tooltip("Mouse button index used when requireMouseButton is true (0=Left,1=Right,2=Middle)")] public int mouseButtonIndex = 1;
        [Tooltip("Ignore tiny mouse movements below this absolute axis value")] [Range(0f,0.2f)] public float mouseAxisDeadzone = 0.01f;

        public float Pitch { set { pitch = Mathf.Clamp(value, -1f, 1f); } get { return pitch; } }
        public float Yaw { set { yaw = Mathf.Clamp(value, -1f, 1f); } get { return yaw; } }
        public float Roll { set { roll = Mathf.Clamp(value, -1f, 1f); } get { return roll; } }
        public float AileronSymmetric { get { return aileronSymmetric; } }
        // Raw pilot/autopilot command values before stabilization corrections
        private float rawPitchCommand = 0f;
        private float rawRollCommand = 0f;
        private float rawYawCommand = 0f;
        public float CommandPitch { get { return rawPitchCommand; } }
        public float CommandRoll { get { return rawRollCommand; } }
        public float CommandYaw { get { return rawYawCommand; } }
        // Public read-only accessors for flight data
        public float Airspeed => airspeed;
        public float AngleOfAttack => angleOfAttack;
        public bool IsStalled => isStalled;

        // Flight data
    private float angleOfAttack = 0f;
    private float airspeed = 0f;
    private bool isStalled = false;

    // Ground properties removed for simplified air-only model
    
    [Header("Center of Mass")]
    [Tooltip("Offset for center of mass (X=left/right, Y=up/down, Z=forward/back)")]
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.5f, 0f);
    [Tooltip("Apply center of mass offset on start")]
    public bool applyCenterOfMass = true;
    [Tooltip("Continuously reapply center of mass each physics frame (prevents auto-recalculation when colliders move)")]
    public bool lockCenterOfMassEachFrame = true;
    
    [Header("Takeoff")]
    [Tooltip("Minimum airspeed before full lift & pitch authority (m/s)")] public float takeoffMinSpeed = 70f; // increased for longer ground roll
    [Tooltip("Lift fraction applied before takeoff criteria met (0-1)")] [Range(0f,1f)] public float earlyLiftFactor = 0.3f;
    [Tooltip("Positive pitch suppression before takeoff (0-1)")] [Range(0f,1f)] public float earlyPitchSuppression = 0.7f;
        
        private Rigidbody rigid;
        private bool rollOverride = false;
        private bool pitchOverride = false;
        // autoStabActive removed with stabilization logic

    // GUI debug style and background texture cache
    private GUIStyle debugStyle;
    private Texture2D debugBgTex;

        private void Awake()
        {
            rigid = GetComponent<Rigidbody>();
            if (rigid != null)
            {
                //rigid.drag = 1f; // We'll handle drag manually
                // Removed automatic angularDrag assignment for manual control
                
                // Apply center of mass offset to stabilize the plane
                if (applyCenterOfMass)
                {
                    rigid.centerOfMass = centerOfMassOffset;
                }
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
            // Throttle control (use configurable rate so increments can be smaller/slower)
            float throttleDelta = throttleChangeRate * Time.deltaTime;
            if (Input.GetKey(KeyCode.LeftShift))
                throttle = Mathf.Min(1f, throttle + throttleDelta);
            if (Input.GetKey(KeyCode.LeftControl))
                throttle = Mathf.Max(0f, throttle - throttleDelta);

            if (useDirectInputMapping)
            {
                // Stabilization disabled: no auto-level corrections applied
                // Mouse input (optional)
                float mouseX = 0f;
                float mouseY = 0f;
                bool mouseAllowed = enableMousePitchYaw && (!requireMouseButton || Input.GetMouseButton(mouseButtonIndex));
                if (mouseAllowed)
                {
                    float rawX = Input.GetAxis("Mouse X");
                    float rawY = Input.GetAxis("Mouse Y");
                    // Deadzone filter to stop tiny drifting
                    mouseX = Mathf.Abs(rawX) < mouseAxisDeadzone ? 0f : rawX;
                    mouseY = Mathf.Abs(rawY) < mouseAxisDeadzone ? 0f : rawY;
                }

                // Raw keyboard inputs (independent of legacy axes)
                float arrowLeft = Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f;
                float arrowRight = Input.GetKey(KeyCode.RightArrow) ? 1f : 0f;
                float arrowUp = Input.GetKey(KeyCode.UpArrow) ? 1f : 0f;
                float arrowDown = Input.GetKey(KeyCode.DownArrow) ? 1f : 0f;

                // Also support WASD for convenience
                float keyA = Input.GetKey(KeyCode.A) ? 1f : 0f;
                float keyD = Input.GetKey(KeyCode.D) ? 1f : 0f;
                float keyW = Input.GetKey(KeyCode.W) ? 1f : 0f;
                float keyS = Input.GetKey(KeyCode.S) ? 1f : 0f;

                // Roll from Left/Right arrows (bank the wings)
                float rollKeys = (arrowRight - arrowLeft);
                roll = Mathf.Clamp(rollKeys * keyboardRollSensitivity, -1f, 1f);

                // Pitch from Up/Down arrows (Up arrow pitches up)
                float pitchKeys = (arrowUp - arrowDown);
                float pitchFromKeys = pitchKeys * keyboardPitchSensitivity;
                float pitchFromMouse = -mouseY * mousePitchSensitivity; // inverted so positive mouseY (moving up) pitches down
                pitch = Mathf.Clamp(pitchFromKeys + pitchFromMouse, -1f, 1f);

                // Yaw from A/D (rudder) plus optional mouse X
                float yawKeys = (keyD - keyA);
                float yawFromKeys = yawKeys * keyboardYawSensitivity;
                float yawFromMouse = mouseX * mouseYawSensitivity;
                yaw = Mathf.Clamp(yawFromKeys + yawFromMouse, -1f, 1f);

                // Symmetric aileron deflection from vertical keys (optional/visual)
                aileronSymmetric = Mathf.Clamp((arrowUp - arrowDown) + (keyW - keyS), -1f, 1f);

                // Capture raw commands BEFORE stabilization modifies them
                rawPitchCommand = pitch;
                rawRollCommand = roll;
                rawYawCommand = yaw;

                // (Auto-stabilization removed)
            }
            else
            {
                // Original autopilot-driven mapping with keyboard overrides
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

                float autoYaw = 0f;
                float autoPitch = 0f;
                float autoRoll = 0f;
                if (controller != null)
                    RunAutopilot(controller.MouseAimPos, out autoYaw, out autoPitch, out autoRoll);

                yaw = autoYaw;
                pitch = (pitchOverride) ? keyboardPitch : autoPitch;
                roll = (rollOverride) ? keyboardRoll : autoRoll;
                aileronSymmetric = 0f;

                // Capture raw autopilot/keyboard commands (no stabilization in this branch)
                rawPitchCommand = pitch;
                rawRollCommand = roll;
                rawYawCommand = yaw;
            }
        }

        private void UpdateFlightData()
        {
            if (rigid == null) return;

            // Calculate airspeed (magnitude of velocity)
            airspeed = rigid.velocity.magnitude;

            // Calculate raw AoA (angle between forward and velocity projected onto pitch plane)
            float rawAoA = 0f;
            if (airspeed > 1f)
            {
                Vector3 velocityDir = rigid.velocity.normalized;
                Vector3 vProj = Vector3.ProjectOnPlane(velocityDir, transform.right).normalized; // project onto pitch plane
                rawAoA = Vector3.SignedAngle(transform.forward, vProj, transform.right);
            }
            else
            {
                rawAoA = 0f;
            }

            // Apply smoothing only at low speeds to avoid noisy AoA
            if (airspeed < aoaSmoothingMaxSpeed)
            {
                angleOfAttack = Mathf.SmoothDampAngle(angleOfAttack, rawAoA, ref aoaSmoothVel, Mathf.Max(0.01f, aoaSmoothingTime));
            }
            else
            {
                angleOfAttack = rawAoA;
                aoaSmoothVel = 0f;
            }

            // Update stall state (simple check with a small speed threshold)
            isStalled = (airspeed > 5f) && (Mathf.Abs(angleOfAttack) > aero.stallAngle);
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

            // Ensure center of mass stays fixed even if Unity tries to recompute it
            if (applyCenterOfMass && lockCenterOfMassEachFrame)
            {
                rigid.centerOfMass = centerOfMassOffset;
            }

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

            // Ground handling removed: use direct multipliers
            float thrustMult = 1f;
            float pitchMult = 1f;
            float liftMult = 1f;

            // Apply lift (perpendicular to velocity) with ground effect
            Vector3 liftDirection = Vector3.Cross(rigid.velocity.normalized, transform.right);
            bool takeoffUnlocked = (airspeed >= takeoffMinSpeed);
            float effectiveLiftMult = liftMult;
            if (!takeoffUnlocked)
            {
                // Before reaching takeoff speed, scale down lift
                effectiveLiftMult *= earlyLiftFactor;
            }
            rigid.AddForce(liftDirection * liftForce * aero.responseMultiplier * effectiveLiftMult, ForceMode.Force);

            // Apply drag (opposite to velocity)
            if (airspeed > 0.1f)
            {
                rigid.AddForce(-rigid.velocity.normalized * dragForce * aero.responseMultiplier, ForceMode.Force);
            }

            // Ground downforce and rolling resistance removed

            // Apply engine thrust (flattened if on ground)
            rigid.AddForce(transform.forward * maxThrust * throttle * forceMult * thrustMult, ForceMode.Force);

            // Apply control surface forces with effectiveness scaling (pitch reduced if on ground)
            float pitchInput = pitch;
            if (!takeoffUnlocked && pitchInput > 0f)
            {
                pitchInput *= (1f - earlyPitchSuppression); // suppress positive pitch before criteria met
            }
            Vector3 controlTorque = new Vector3(
                turnTorque.x * pitchInput * pitchMult,
                turnTorque.y * yaw,
                -turnTorque.z * roll
            );
            rigid.AddRelativeTorque(controlTorque * controlEffect * forceMult, ForceMode.Force);

            // Ground stickiness removed

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

        // Create a small texture filled with a color (used for label backgrounds).
        private Texture2D MakeTex(int width, int height, Color col)
        {
            var tex = new Texture2D(width, height);
            var cols = new Color[width * height];
            for (int i = 0; i < cols.Length; i++) cols[i] = col;
            tex.SetPixels(cols);
            tex.Apply();
            tex.hideFlags = HideFlags.DontSave;
            return tex;
        }

        // Ensure the debug GUIStyle is created and cached.
        private void EnsureDebugStyle()
        {
            if (debugStyle != null) return;
            // Slightly translucent black background
            debugBgTex = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.75f));
            debugStyle = new GUIStyle(GUI.skin.label);
            debugStyle.normal.background = debugBgTex;
            debugStyle.normal.textColor = Color.white;
            debugStyle.fontSize = 18; // augmenter légèrement la taille de la police
            debugStyle.padding = new RectOffset(8, 8, 6, 6);
            debugStyle.margin = new RectOffset(4, 4, 4, 4);
        }

        void OnGUI()
        {
            // Debug display with background and larger font
            EnsureDebugStyle();

            GUILayout.BeginVertical();
            GUILayout.Label($"Airspeed: {airspeed:F1} m/s", debugStyle);
            GUILayout.Label($"AoA(angle d'attaque): {angleOfAttack:F1}°", debugStyle);
            GUILayout.Label($"Stalled(callé): {isStalled}", debugStyle);// perte de portance si vrai
            GUILayout.Label($"Throttle(gas): {throttle:P0}", debugStyle);
                // AutoStab removed
            GUILayout.Label($"Inputs — Pitch: {pitch:F2}  Yaw: {yaw:F2}  Roll: {roll:F2}", debugStyle);
            // Grounded state removed
            GUILayout.EndVertical();
        }
    }
}
