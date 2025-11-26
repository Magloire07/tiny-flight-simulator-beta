using UnityEngine;
using MFlight.Demo;

/// <summary>
/// Stabilise l'avion au sol et maintient son tangage initial tant qu'il est proche du sol (simulation décollage).
/// L'avion garde son angle de départ jusqu'à dépasser une hauteur définie, puis le contrôle complet est rendu au pilote.
/// </summary>
[DefaultExecutionOrder(50)]
public class PlaneGroundStability : MonoBehaviour
{
    // Global switch to silence all OnGUI debug output from this component, regardless of instance settings.
    public static bool DisableAllOnScreenDebug = true;
    [Header("Références")]
    public MFlight.Demo.Plane plane; // assigner l'avion; auto-récup si null
    public WheelCollider[] wheelColliders; // si vide, auto-récup dans enfants

    [Header("Paramètres de Décollage (Basé sur Altitude)")] 
    [Tooltip("Hauteur max (m) au-dessus du sol pour maintenir la stabilité. Au-delà, contrôle total rendu.")]
    public float maxGroundProximityHeight = 2f;
    [Tooltip("Plage de transition (m) pour adoucir le relâchement du contrôle")] public float releaseFadeHeightBand = 1f;

    [Header("Stabilité au Sol")]
    [Tooltip("Force de stabilisation sur les roues pour éviter oscillations")] public float wheelStabilizationForce = 300f;
    [Tooltip("Damping du roulis (0-1) pour garder l'avion stable latéralement")] [Range(0f,1f)] public float rollDampingWhileGrounded = 0.4f;
    [Tooltip("Damping du lacet (0-1) pour éviter rotation excessive au sol")] [Range(0f,1f)] public float yawDampingWhileGrounded = 0.3f;

    [Header("Maintien du Tangage Initial")]
    [Tooltip("Maintenir le pitch initial de l'avion tant que proche du sol")] public bool lockInitialPitch = true;
    [Tooltip("Couple par degré d'erreur pour maintenir le pitch initial (Nm/deg)")] public float pitchHoldTorquePerDeg = 50f;
    [Tooltip("Couple max appliqué pour le maintien du pitch (Nm)")] public float maxPitchHoldTorque = 3000f;

    [Header("Détection du Sol")]
    [Tooltip("Nombre minimum de roues au sol pour considérer l'avion comme stable")] public int minGroundedWheels = 2;
    [Tooltip("Longueur du raycast pour détecter le sol (m)")] public float groundRayLength = 5.0f;
    [Tooltip("LayerMask du sol (si 0 => tout)")] public LayerMask groundMask = 0;
    [Tooltip("Afficher debug à l'écran")] public bool showDebug = true;

    private Rigidbody rb;
    private bool isStabilizing;
    private float initialPitchAngle;
    private bool initialPitchCaptured;
    private float currentGroundHeight;
    private string statusReason = "Init";

    void Awake()
    {
        if (plane == null) plane = GetComponentInParent<MFlight.Demo.Plane>();
        rb = plane != null ? plane.GetComponent<Rigidbody>() : null;
        if (wheelColliders == null || wheelColliders.Length == 0)
        {
            // Rechercher d'abord sous nous, sinon sous tout l'avion
            var local = GetComponentsInChildren<WheelCollider>();
            if (local != null && local.Length > 0) wheelColliders = local;
            else if (plane != null) wheelColliders = plane.GetComponentsInChildren<WheelCollider>();
        }
        initialPitchCaptured = false;
    }

    void FixedUpdate()
    {
        if (plane == null || rb == null) return;

        // Capturer le pitch initial au premier contact au sol
        if (!initialPitchCaptured && CountGroundedWheels() >= minGroundedWheels)
        {
            initialPitchAngle = GetCurrentPitchAngle();
            initialPitchCaptured = true;
            statusReason = "Initial Pitch Captured: " + initialPitchAngle.ToString("F1") + "°";
        }

        // Calculer hauteur au-dessus du sol
        currentGroundHeight = GetHeightAboveGround();

        // Déterminer si la stabilisation est active (basée sur hauteur)
        float fadeStart = maxGroundProximityHeight;
        float fadeEnd = maxGroundProximityHeight + releaseFadeHeightBand;
        float fade = 0f;
        
        if (currentGroundHeight < fadeStart)
        {
            fade = 1f; // Stabilisation complète
            statusReason = "Ground Proximity Active (h=" + currentGroundHeight.ToString("F1") + "m)";
        }
        else if (currentGroundHeight < fadeEnd)
        {
            // Transition progressive
            fade = 1f - ((currentGroundHeight - fadeStart) / releaseFadeHeightBand);
            statusReason = "Releasing (fade=" + (fade * 100f).ToString("F0") + "%)";
        }
        else
        {
            fade = 0f;
            statusReason = "Released - Full Control (h=" + currentGroundHeight.ToString("F1") + "m)";
        }

        isStabilizing = fade > 0f;

        if (isStabilizing && initialPitchCaptured)
        {
            // Maintenir le pitch initial
            if (lockInitialPitch)
            {
                float currentPitch = GetCurrentPitchAngle();
                float pitchError = initialPitchAngle - currentPitch;
                
                // Appliquer un couple correcteur proportionnel à l'erreur
                float torqueMag = Mathf.Clamp(Mathf.Abs(pitchError) * pitchHoldTorquePerDeg, 0f, maxPitchHoldTorque);
                torqueMag *= fade; // Appliquer le facteur de transition
                
                float torqueSign = Mathf.Sign(pitchError);
                Vector3 pitchTorque = rb.transform.right * (torqueSign * torqueMag);
                rb.AddTorque(pitchTorque, ForceMode.Force);
            }

            // Damping du roulis et lacet pour stabilité latérale
            Vector3 localAngular = rb.transform.InverseTransformDirection(rb.angularVelocity);
            localAngular.z *= Mathf.Clamp01(1f - rollDampingWhileGrounded * fade); // Roll
            localAngular.y *= Mathf.Clamp01(1f - yawDampingWhileGrounded * fade); // Yaw
            rb.angularVelocity = rb.transform.TransformDirection(localAngular);

            // Force de stabilisation légère sur les roues pour éviter rebonds
            int groundedCount = CountGroundedWheels();
            if (groundedCount > 0 && wheelStabilizationForce > 0f)
            {
                float forcePerWheel = (wheelStabilizationForce * fade) / groundedCount;
                foreach (var wc in wheelColliders)
                {
                    if (wc == null || !WheelIsGrounded(wc)) continue;
                    rb.AddForceAtPosition(Vector3.down * forcePerWheel, wc.transform.position, ForceMode.Force);
                }
            }
        }
    }

    float GetCurrentPitchAngle()
    {
        // Calcul de l'angle de tangage par rapport à l'horizon
        Vector3 flatFwd = Vector3.ProjectOnPlane(rb.transform.forward, Vector3.up);
        if (flatFwd.sqrMagnitude < 1e-6f) flatFwd = rb.transform.forward;
        return Vector3.SignedAngle(flatFwd, rb.transform.forward, rb.transform.right);
    }

    float GetHeightAboveGround()
    {
        // Raycast depuis le centre de l'avion vers le bas
        Ray ray = new Ray(rb.transform.position, Vector3.down);
        RaycastHit hit;
        int mask = groundMask.value == 0 ? ~0 : groundMask.value;
        
        if (Physics.Raycast(ray, out hit, groundRayLength, mask))
        {
            return hit.distance;
        }
        
        // Si pas de hit, considérer comme très haut
        return groundRayLength;
    }

    int CountGroundedWheels()
    {
        int count = 0;
        if (wheelColliders == null) return 0;
        foreach (var wc in wheelColliders)
        {
            if (wc == null) continue;
            if (WheelIsGrounded(wc)) count++;
        }
        return count;
    }

    bool WheelIsGrounded(WheelCollider wc)
    {
        if (wc.isGrounded) return true;
        WheelHit hit;
        return wc.GetGroundHit(out hit);
    }

    void OnGUI()
    {
        if (!showDebug || DisableAllOnScreenDebug) return;
        
        GUI.Label(new Rect(10, 60, 500, 25), $"Ground Stability: {(isStabilizing ? "ACTIVE" : "INACTIVE")} - {statusReason}");
        if (initialPitchCaptured)
        {
            float currentPitch = GetCurrentPitchAngle();
            GUI.Label(new Rect(10, 85, 500, 25), 
                $"Pitch: Current={currentPitch:F1}° Target={initialPitchAngle:F1}° Height={currentGroundHeight:F1}m");
        }
        GUI.Label(new Rect(10, 110, 500, 25), $"Wheels Grounded: {CountGroundedWheels()}/{(wheelColliders != null ? wheelColliders.Length : 0)}");
    }
}
