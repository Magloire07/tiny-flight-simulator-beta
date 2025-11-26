using UnityEngine;
using MFlight.Demo;

/// <summary>
/// Maintient le tangage de l'avion proche du sol pour simuler un décollage réaliste.
/// Capture automatiquement l'angle de tangage initial au contact du sol et le maintient
/// tant que l'altitude est inférieure à un seuil défini. Libère progressivement le contrôle en altitude.
/// </summary>
[DefaultExecutionOrder(60)]
public class PlaneStartupPitchHold : MonoBehaviour
{
    public enum HoldMode { InitialPitch, FixedPitch, AngleOfAttack }

    [Header("Références")]
    public MFlight.Demo.Plane plane; // auto assign si null
    public WheelCollider[] wheelColliders; // pour détecter contact au sol
    private Rigidbody rb;

    [Header("Mode de Maintien")]
    [Tooltip("InitialPitch: capture et maintient l'angle au premier contact sol | FixedPitch: maintient un angle fixe | AngleOfAttack: maintient un AoA")]
    public HoldMode mode = HoldMode.InitialPitch;
    [Tooltip("Pitch fixe à maintenir (degrés) - utilisé en mode FixedPitch")] public float fixedTargetDegrees = 0f;

    [Header("Paramètres Altitude (InitialPitch)")]
    [Tooltip("Hauteur max (m) au-dessus du sol pour maintenir le pitch. Au-delà, contrôle total rendu.")]
    public float maxGroundProximityHeight = 2f;
    [Tooltip("Plage de transition (m) pour adoucir le relâchement du contrôle")] public float releaseFadeHeightBand = 1f;
    [Tooltip("Longueur du raycast pour détecter le sol (m)")] public float groundRayLength = 5.0f;
    [Tooltip("LayerMask du sol (si 0 => tout)")] public LayerMask groundMask = 0;

    [Header("Paramètres Temporels (FixedPitch/AoA)")]
    [Tooltip("Durée totale du maintien en secondes - utilisé en mode FixedPitch/AoA")] public float holdDuration = 10f;
    [Tooltip("Se désactive si airspeed dépasse ce seuil (0 = ignore)")] public float cancelAboveAirspeed = 0f;

    [Header("Couple Correcteur")]
    [Tooltip("Force du couple correcteur (Nm par degré d'erreur)")] public float torquePerDegree = 200f;
    [Tooltip("Couple max appliqué (Nm)")] public float maxTorque = 10000f;
    [Tooltip("Réduire la force si l'utilisateur applique un pitch en sens inverse")] public bool reduceWhenPilotOpposes = true;
    [Tooltip("Damping direct sur la vitesse angulaire de pitch (0-1)")] [Range(0f,1f)] public float pitchAngularDamping = 0.95f;

    [Header("Stabilité Latérale")]
    [Tooltip("Damping du roulis (0-1) pour garder l'avion stable latéralement")] [Range(0f,1f)] public float rollDampingWhileActive = 0.4f;
    [Tooltip("Damping du lacet (0-1) pour éviter rotation excessive")] [Range(0f,1f)] public float yawDampingWhileActive = 0.3f;
    [Tooltip("Force de stabilisation sur les roues (N) pour éviter rebonds")] public float wheelStabilizationForce = 300f;

    [Header("Critères d'Activation")]
    [Tooltip("Nombre minimum de roues au sol pour capturer le pitch initial")] public int minGroundedWheels = 2;

    [Header("Debug")]
    [Tooltip("Affichage debug GUI")] public bool showDebug = true;

    private float startTime;
    private bool active;
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
            var local = GetComponentsInChildren<WheelCollider>();
            if (local != null && local.Length > 0) wheelColliders = local;
            else if (plane != null) wheelColliders = plane.GetComponentsInChildren<WheelCollider>();
        }
        
        initialPitchCaptured = false;
    }

    void OnEnable()
    {
        startTime = Time.time;
        active = true;
        initialPitchCaptured = false;
        statusReason = "Enabled";
    }

    void FixedUpdate()
    {
        if (!active || plane == null || rb == null) return;

        float targetDegrees = 0f;
        float fade = 0f;

        // Mode InitialPitch: basé sur l'altitude au-dessus du sol
        if (mode == HoldMode.InitialPitch)
        {
            // Capturer le pitch initial au premier contact au sol
            if (!initialPitchCaptured && CountGroundedWheels() >= minGroundedWheels)
            {
                initialPitchAngle = GetCurrentPitchAngle();
                initialPitchCaptured = true;
                statusReason = $"Pitch Captured: {initialPitchAngle:F1}°";
            }

            if (!initialPitchCaptured)
            {
                statusReason = "Waiting for ground contact...";
                return;
            }

            // Calculer hauteur au-dessus du sol
            currentGroundHeight = GetHeightAboveGround();

            // Déterminer le facteur de fade basé sur l'altitude
            float fadeStart = maxGroundProximityHeight;
            float fadeEnd = maxGroundProximityHeight + releaseFadeHeightBand;
            
            if (currentGroundHeight < fadeStart)
            {
                fade = 1f;
                statusReason = $"Active (h={currentGroundHeight:F1}m)";
            }
            else if (currentGroundHeight < fadeEnd)
            {
                fade = 1f - ((currentGroundHeight - fadeStart) / releaseFadeHeightBand);
                statusReason = $"Releasing (fade={fade*100f:F0}%)";
            }
            else
            {
                fade = 0f;
                active = false;
                statusReason = $"Released (h={currentGroundHeight:F1}m)";
                return;
            }

            targetDegrees = initialPitchAngle;
        }
        // Mode FixedPitch ou AngleOfAttack: basé sur durée et vitesse
        else
        {
            float elapsed = Time.time - startTime;
            if (elapsed > holdDuration)
            {
                active = false;
                statusReason = "Duration Elapsed";
                return;
            }
            if (cancelAboveAirspeed > 0f && plane.Airspeed >= cancelAboveAirspeed)
            {
                active = false;
                statusReason = "Airspeed Cancel";
                return;
            }

            fade = 1f;

            if (mode == HoldMode.FixedPitch)
            {
                targetDegrees = fixedTargetDegrees;
                statusReason = $"Fixed Pitch Mode ({elapsed:F1}s)";
            }
            else // AngleOfAttack
            {
                targetDegrees = plane.AngleOfAttack;
                statusReason = $"AoA Mode ({elapsed:F1}s)";
            }
        }

        // Mesurer l'état actuel
        float currentDeg = (mode == HoldMode.AngleOfAttack) ? plane.AngleOfAttack : GetCurrentPitchAngle();
        float error = targetDegrees - currentDeg;

        // Calculer couple correcteur
        float torqueMagnitude = Mathf.Clamp(Mathf.Abs(error) * torquePerDegree, 0f, maxTorque);
        float proximity = Mathf.InverseLerp(0f, 5f, Mathf.Abs(error));
        torqueMagnitude *= Mathf.Clamp01(proximity + 0.2f);
        torqueMagnitude *= fade;

        // Réduction si pilote oppose
        if (reduceWhenPilotOpposes)
        {
            Vector3 localAngVel = rb.transform.InverseTransformDirection(rb.angularVelocity);
            float pitchRateDegPerSec = localAngVel.x * Mathf.Rad2Deg;
            if (Mathf.Sign(pitchRateDegPerSec) != Mathf.Sign(error))
            {
                torqueMagnitude *= 0.4f;
            }
        }

        // Appliquer couple sur l'axe X local (pitch)
        float torqueSign = Mathf.Sign(error);
        Vector3 pitchTorque = rb.transform.right * (torqueSign * torqueMagnitude);
        rb.AddTorque(pitchTorque, ForceMode.Force);

        // Damping latéral pour stabilité
        if (mode == HoldMode.InitialPitch && fade > 0f)
        {
            Vector3 localAngular = rb.transform.InverseTransformDirection(rb.angularVelocity);
            
            // Damping très fort sur le pitch (X) pour empêcher oscillations des roues
            localAngular.x *= Mathf.Clamp01(1f - pitchAngularDamping * fade);
            
            // Damping normal sur roll et yaw
            localAngular.z *= Mathf.Clamp01(1f - rollDampingWhileActive * fade);
            localAngular.y *= Mathf.Clamp01(1f - yawDampingWhileActive * fade);
            rb.angularVelocity = rb.transform.TransformDirection(localAngular);

            // Force de stabilisation sur les roues
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
        Vector3 flatFwd = Vector3.ProjectOnPlane(rb.transform.forward, Vector3.up);
        if (flatFwd.sqrMagnitude < 1e-6f) flatFwd = rb.transform.forward;
        // Inverser le signe: nez vers le haut = pitch positif
        return -Vector3.SignedAngle(flatFwd, rb.transform.forward, rb.transform.right);
    }

    float GetHeightAboveGround()
    {
        // Utiliser la roue la plus basse pour calculer la hauteur
        float lowestWheelHeight = float.MaxValue;
        bool foundGroundedWheel = false;
        
        if (wheelColliders != null && wheelColliders.Length > 0)
        {
            foreach (var wc in wheelColliders)
            {
                if (wc == null) continue;
                
                // Vérifier si la roue touche le sol
                WheelHit hit;
                if (wc.GetGroundHit(out hit))
                {
                    // Distance entre la roue et le sol
                    float wheelToGround = Vector3.Distance(wc.transform.position, hit.point);
                    if (wheelToGround < lowestWheelHeight)
                    {
                        lowestWheelHeight = wheelToGround;
                        foundGroundedWheel = true;
                    }
                }
            }
        }
        
        // Si au moins une roue touche le sol, retourner la hauteur la plus basse
        if (foundGroundedWheel && lowestWheelHeight < 0.5f)
        {
            return lowestWheelHeight;
        }
        
        // Sinon, fallback sur raycast depuis le centre (mais ajuster pour offset)
        Ray ray = new Ray(rb.transform.position, Vector3.down);
        RaycastHit rayHit;
        int mask = groundMask.value == 0 ? ~0 : groundMask.value;
        
        if (Physics.Raycast(ray, out rayHit, groundRayLength, mask))
        {
            // Soustraire une estimation de hauteur du centre au sol (~1m pour un avion typique)
            float estimatedCenterOffset = 1.0f;
            return Mathf.Max(0f, rayHit.distance - estimatedCenterOffset);
        }
        
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
        if (!showDebug) return;
        
        string state = active ? "ACTIVE" : "INACTIVE";
        GUI.Label(new Rect(10, 10, 500, 25), $"Pitch Hold: {state} - Mode: {mode} - {statusReason}");
        
        if (mode == HoldMode.InitialPitch && initialPitchCaptured)
        {
            float currentPitch = GetCurrentPitchAngle();
            GUI.Label(new Rect(10, 35, 500, 25), 
                $"Pitch: Current={currentPitch:F1}° Target={initialPitchAngle:F1}° Height={currentGroundHeight:F1}m");
            GUI.Label(new Rect(10, 60, 500, 25), 
                $"Wheels: {CountGroundedWheels()}/{(wheelColliders != null ? wheelColliders.Length : 0)} grounded");
        }
        else if (active && plane != null)
        {
            float currentPitch = mode == HoldMode.AngleOfAttack ? plane.AngleOfAttack : GetCurrentPitchAngle();
            float target = mode == HoldMode.AngleOfAttack ? plane.AngleOfAttack : fixedTargetDegrees;
            GUI.Label(new Rect(10, 35, 500, 25), 
                $"Current: {currentPitch:F1}° Target: {target:F1}° Airspeed: {plane.Airspeed:F1} m/s");
        }
    }
}
