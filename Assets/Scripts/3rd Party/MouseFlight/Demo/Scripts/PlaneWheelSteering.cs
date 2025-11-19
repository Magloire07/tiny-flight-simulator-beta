using UnityEngine;

/// <summary>
/// Contrôle la direction de la roue arrière (tail wheel) pour les avions "tail-dragger".
/// Applique le steering au WheelCollider arrière et synchronise la rotation visuelle du mesh.
/// À placer sur le GameObject de la roue arrière qui contient le WheelCollider.
/// Note: La roue arrière tourne sur l'axe Z pour le steering.
/// 
/// IMPORTANT: Le WheelCollider ne doit PAS avoir de motorTorque (laisser à 0).
/// Les roues tournent automatiquement par friction quand l'avion avance avec la poussée des moteurs.
/// </summary>
public class PlaneWheelSteering : MonoBehaviour
{
    [Header("Wheel Components")]
    [Tooltip("Le Transform du mesh de la roue (laisser vide pour utiliser ce GameObject)")]
    public Transform wheelMesh;

    [Header("Steering Settings")]
    [Tooltip("Angle maximum de braquage (degrés)")]
    public float maxSteerAngle = 30f;
    
    [Tooltip("Vitesse max pour autoriser le braquage")]
    public float maxSteerSpeed = 30f;
    
    [Tooltip("Axe de roulement (X, Y ou Z selon l'orientation de la roue)")]
    public enum RollAxis { X, Y, Z }
    public RollAxis rollAxis = RollAxis.Y;
    
    [Tooltip("Axe de steering (X, Y ou Z selon l'orientation de la roue)")]
    public enum SteerAxis { X, Y, Z }
    public SteerAxis steerAxis = SteerAxis.Z;
    
    [Tooltip("Inverser le sens de rotation du roulement (-1 pour inverser, 1 pour normal)")]
    public float rollDirectionMultiplier = 1f;
    [Tooltip("Inverser le sens de braquage visuel/physique (-1 pour inverser, 1 pour normal)")]
    public float steerDirectionMultiplier = 1f;
    [Tooltip("Utiliser la vitesse pour le signe du roulage (plus stable que le signe des RPM)")]
    public bool useVelocityForSpinSign = true;
    [Tooltip("Vitesse minimale pour utiliser le signe de la vitesse (m/s)")]
    public float velocitySignMinSpeed = 0.2f;
    [Tooltip("Sous ce RPM, on considère la roue comme à l'arrêt pour l'affichage")]
    public float rpmStopDeadzone = 1.5f;
    [Tooltip("Couple de frein au-dessus duquel on fige visuellement la rotation")]
    public float brakeStopTorque = 5f;

    private WheelCollider wheelCollider;
    private Rigidbody rb;
    private Vector3 initialLocalEuler;
    private float accumulatedRotation;

    void Start()
    {
        // Récupérer le WheelCollider sur ce GameObject
        wheelCollider = GetComponent<WheelCollider>();
        
        if (wheelCollider == null)
        {
            Debug.LogError("PlaneWheelSteering: Pas de WheelCollider trouvé sur " + gameObject.name);
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
        
        // Trouver le Rigidbody de l'avion (sur le parent ou ancêtre)
        rb = GetComponentInParent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("PlaneWheelSteering: Pas de Rigidbody trouvé sur les parents");
        }
        
        accumulatedRotation = 0f;
    }

    void Update()
    {
        if (wheelCollider == null || wheelMesh == null) return;
        
        // Calculate steering angle
        float steerInput = Input.GetAxis("Horizontal");
        float currentSteerAngle = steerInput * maxSteerAngle * steerDirectionMultiplier;
        
        // Limit steering at high speed
        if (rb != null && rb.velocity.magnitude > maxSteerSpeed)
            currentSteerAngle = 0f;
        
        // Get RPM from WheelCollider and calculate roll rotation
        float rpm = wheelCollider.rpm;
        float sign = 1f;
        if (useVelocityForSpinSign && rb != null)
        {
            float speed = rb.velocity.magnitude;
            if (speed > velocitySignMinSpeed)
                sign = Mathf.Sign(Vector3.Dot(rb.velocity, transform.forward));
            else
                sign = Mathf.Sign(rpm != 0f ? rpm : 1f);
        }
        else
        {
            sign = Mathf.Sign(rpm != 0f ? rpm : 1f);
        }

        // Stop visual spin when essentially idle or braking hard
        bool grounded = wheelCollider.isGrounded;
        bool stopVisual = grounded && ( (rb != null && rb.velocity.magnitude <= velocitySignMinSpeed && Mathf.Abs(rpm) <= rpmStopDeadzone) || wheelCollider.brakeTorque >= brakeStopTorque );
        float rollRotationThisFrame = stopVisual ? 0f : Mathf.Abs(rpm) * 6f * Time.deltaTime * rollDirectionMultiplier * sign; // 6 = 360°/60s
        
        accumulatedRotation += rollRotationThisFrame;
        
        // Apply rotation based on selected axes
        Vector3 newEuler = initialLocalEuler;
        
        // Check if roll and steer are on same axis - if so, combine them
        bool sameAxis = (rollAxis == RollAxis.X && steerAxis == SteerAxis.X) ||
                        (rollAxis == RollAxis.Y && steerAxis == SteerAxis.Y) ||
                        (rollAxis == RollAxis.Z && steerAxis == SteerAxis.Z);
        
        if (sameAxis)
        {
            // Both on same axis - ADD them together
            float combinedRotation = accumulatedRotation + currentSteerAngle;
            
            switch (rollAxis)
            {
                case RollAxis.X:
                    newEuler.x = combinedRotation;
                    break;
                case RollAxis.Y:
                    newEuler.y = combinedRotation;
                    break;
                case RollAxis.Z:
                    newEuler.z = combinedRotation;
                    break;
            }
        }
        else
        {
            // Different axes - apply separately
            // Apply roll rotation on selected axis
            switch (rollAxis)
            {
                case RollAxis.X:
                    newEuler.x = accumulatedRotation;
                    break;
                case RollAxis.Y:
                    newEuler.y = accumulatedRotation;
                    break;
                case RollAxis.Z:
                    newEuler.z = accumulatedRotation;
                    break;
            }
            
            // Apply steering on selected axis
            switch (steerAxis)
            {
                case SteerAxis.X:
                    newEuler.x = currentSteerAngle;
                    break;
                case SteerAxis.Y:
                    newEuler.y = currentSteerAngle;
                    break;
                case SteerAxis.Z:
                    newEuler.z = currentSteerAngle;
                    break;
            }
        }
        
        wheelMesh.localEulerAngles = newEuler;
    }

    void FixedUpdate()
    {
        if (wheelCollider == null) return;
        
        // Appliquer le steering au WheelCollider pour la physique
        float steerInput = Input.GetAxis("Horizontal");
        float steerAngle = steerInput * maxSteerAngle * steerDirectionMultiplier;

        // Limiter le braquage à haute vitesse
        if (rb != null && rb.velocity.magnitude > maxSteerSpeed)
            steerAngle = 0f;

        wheelCollider.steerAngle = steerAngle;
    }
}