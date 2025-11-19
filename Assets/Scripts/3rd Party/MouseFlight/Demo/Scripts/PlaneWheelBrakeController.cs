using UnityEngine;

/// <summary>
/// Applies brake torque to wheel colliders when throttle is low and the aircraft is grounded,
/// to stop wheels quickly and prevent visual backward spin after cutting throttle.
/// Optionally supports a manual brake key.
/// </summary>
public class PlaneWheelBrakeController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider[] wheels;

    [Header("Ground Check")]
    [Tooltip("Ray length to check for ground contact (meters)")]
    public float groundRayLength = 3f;

    [Header("Brake Settings")] 
    [Tooltip("Brake torque applied when throttled off and grounded")] 
    public float idleBrakeTorque = 150f;
    [Tooltip("Extra brake torque when manual brake key held")] 
    public float manualBrakeTorque = 300f;
    [Tooltip("Throttle below which idle braking is enabled")] 
    public float throttleBrakeThreshold = 0.05f;
    [Tooltip("Speed below which idle braking is enabled (m/s)")] 
    public float speedBrakeThreshold = 2f;
    [Tooltip("Manual brake key")] 
    public KeyCode brakeKey = KeyCode.Space;

    [Header("Parking Brake")]
    [Tooltip("Enable latched parking brake logic")] 
    public bool enableParkingBrake = true;
    [Tooltip("Parking brake torque when engaged")] 
    public float parkingBrakeTorque = 800f;
    [Tooltip("Toggle key for parking brake latch")] 
    public KeyCode parkingBrakeToggleKey = KeyCode.B;
    [Tooltip("Throttle level to auto-release parking brake")] 
    public float parkingThrottleRelease = 0.15f;
    [Tooltip("Auto-engage parking brake when throttle is ~0, slow and grounded")] 
    public bool autoEngageParkingAtIdle = true;

    private bool parkingLatched = false;

    private MFlight.Demo.Plane plane;

    void Awake()
    {
        plane = GetComponentInParent<MFlight.Demo.Plane>();
        if (wheels == null || wheels.Length == 0)
        {
            wheels = GetComponentsInChildren<WheelCollider>();
        }
    }

    void FixedUpdate()
    {
        if (wheels == null || wheels.Length == 0 || plane == null) return;

        // Handle parking brake toggle
        if (enableParkingBrake && Input.GetKeyDown(parkingBrakeToggleKey))
        {
            parkingLatched = !parkingLatched;
        }

        // Auto-release parking brake if throttle raised
        if (enableParkingBrake && parkingLatched && planeThrottle() >= parkingThrottleRelease)
        {
            parkingLatched = false;
        }

        bool grounded = isPlaneGrounded();
        bool manualBrake = Input.GetKey(brakeKey);
        bool idleBrake = plane != null && plane.enabled && planeThrottle() < throttleBrakeThreshold && planeAirspeed() < speedBrakeThreshold && grounded;
        bool autoParkingReady = enableParkingBrake && autoEngageParkingAtIdle && planeThrottle() < throttleBrakeThreshold && planeAirspeed() < speedBrakeThreshold && grounded;

        float targetBrake = 0f;
        if (manualBrake)
            targetBrake = manualBrakeTorque;
        else if (enableParkingBrake && (parkingLatched || autoParkingReady))
            targetBrake = parkingBrakeTorque;
        else if (idleBrake)
            targetBrake = idleBrakeTorque;

        foreach (var wc in wheels)
        {
            if (wc == null) continue;
            wc.brakeTorque = targetBrake;
        }
    }

    private float planeThrottle()
    {
        return plane != null ? plane.throttle : 0f;
    }

    private float planeAirspeed()
    {
        // Using Rigidbody speed is consistent with Plane's airspeed when on ground
        var rb = plane != null ? plane.GetComponent<Rigidbody>() : null;
        return rb != null ? rb.velocity.magnitude : 0f;
    }

    private bool isPlaneGrounded()
    {
        // Simple ray check mirroring Plane's logic
        if (plane == null) return false;
        RaycastHit hit;
        return Physics.Raycast(plane.transform.position, -plane.transform.up, out hit, groundRayLength);
    }
}
