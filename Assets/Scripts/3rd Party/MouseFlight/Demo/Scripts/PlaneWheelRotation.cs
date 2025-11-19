using UnityEngine;

/// <summary>
/// Synchronise la rotation visuelle d'une roue (mesh) avec un WheelCollider.
/// Pour roues avant qui roulent seulement (pas de steering).
/// À placer sur le GameObject de la roue qui contient le WheelCollider.
/// 
/// IMPORTANT: Le WheelCollider ne doit PAS avoir de motorTorque (laisser à 0).
/// Les roues tournent automatiquement par friction quand l'avion avance.
/// </summary>
public class PlaneWheelRotation : MonoBehaviour
{
    [Header("Wheel Components")]
    [Tooltip("Le Transform du mesh de la roue à faire tourner (laisser vide pour utiliser ce GameObject)")]
    public Transform wheelMesh;

    [Header("Rotation Settings")]
    [Tooltip("Vitesse de rotation de la roue (degrés par unité de distance)")]
    public float rotationSpeed = 360f;
    [Tooltip("Invert rolling direction if the mesh spins backward (use -1 to invert)")]
    public float directionMultiplier = 1f;
    [Tooltip("Use rigidbody velocity to determine spin sign (more stable than WheelCollider.rpm sign)")]
    public bool useVelocityForSpinSign = true;
    [Tooltip("Minimum speed to rely on velocity sign (m/s)")]
    public float velocitySignMinSpeed = 0.2f;
    [Tooltip("RPM below which we consider the wheel visually stopped")]
    public float rpmStopDeadzone = 1.5f;
    [Tooltip("Brake torque above which we freeze visual spin")]
    public float brakeStopTorque = 5f;

    private WheelCollider wheelCollider;
    private Vector3 initialLocalEuler;
    private float accumulatedRotation;
    private Rigidbody rb;

    void Start()
    {
        // Récupérer le WheelCollider sur ce GameObject
        wheelCollider = GetComponent<WheelCollider>();
        
        if (wheelCollider == null)
        {
            Debug.LogError("PlaneWheelRotation: Pas de WheelCollider trouvé sur " + gameObject.name);
        }
        
        // Si pas de mesh assigné, utiliser ce GameObject
        if (wheelMesh == null)
        {
            wheelMesh = transform;
        }
        
        // Sauvegarder la rotation locale initiale
        if (wheelMesh != null)
        {
            initialLocalEuler = wheelMesh.localEulerAngles;
        }
        
        accumulatedRotation = 0f;

        // RigidBody (to read velocity sign)
        rb = GetComponentInParent<Rigidbody>();
    }

    void Update()
    {
        if (wheelCollider != null && wheelMesh != null)
        {
            // Get RPM and compute rotation with a robust sign
            float rpm = wheelCollider.rpm;
            float sign = 1f;
            if (useVelocityForSpinSign && rb != null)
            {
                float speed = rb.velocity.magnitude;
                if (speed > velocitySignMinSpeed)
                {
                    sign = Mathf.Sign(Vector3.Dot(rb.velocity, transform.forward));
                    if (sign == 0f) sign = 1f;
                }
                else
                {
                    sign = Mathf.Sign(rpm != 0f ? rpm : 1f);
                }
            }
            else
            {
                sign = Mathf.Sign(rpm != 0f ? rpm : 1f);
            }

            bool grounded = wheelCollider.isGrounded;
            bool stopVisual = grounded && ((rb != null && rb.velocity.magnitude <= velocitySignMinSpeed && Mathf.Abs(rpm) <= rpmStopDeadzone) || wheelCollider.brakeTorque >= brakeStopTorque);
            float rotationThisFrame = stopVisual ? 0f : directionMultiplier * sign * Mathf.Abs(rpm) * 6f * Time.deltaTime; // 6 = 360°/60s
            
            accumulatedRotation += rotationThisFrame;
            
            // Apply rotation: keep initial Y and Z, only rotate on X (rolling)
            Vector3 newEuler = initialLocalEuler;
            newEuler.x = accumulatedRotation;
            
            wheelMesh.localEulerAngles = newEuler;
        }
    }
}
