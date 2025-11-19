using UnityEngine;
using MFlight.Demo;

/// <summary>
/// Maintient l'avion plaqué au sol (3 roues) jusqu'à la vitesse de décollage.
/// Applique une force vers le bas et amortit le tangage/roulis pour éviter l'instabilité.
/// Se désactive automatiquement quand la vitesse de décollage est atteinte.
/// </summary>
[DefaultExecutionOrder(50)]
public class PlaneGroundStability : MonoBehaviour
{
    // Global switch to silence all OnGUI debug output from this component, regardless of instance settings.
    public static bool DisableAllOnScreenDebug = true;
    [Header("Références")]
    public MFlight.Demo.Plane plane; // assigner l'avion; auto-récup si null
    public WheelCollider[] wheelColliders; // si vide, auto-récup dans enfants

    [Header("Paramètres de Décollage")] 
    [Tooltip("Utiliser la vitesse de décollage du Plane (takeoffMinSpeed). Si désactivé, utiliser customSpeed.")]
    public bool usePlaneTakeoffSpeed = true;
    [Tooltip("Vitesse de libération (m/s) si non liée au Plane")] public float customReleaseSpeed = 100f;
    [Tooltip("Plage de transition (m/s) autour de la vitesse de libération pour adoucir le relâchement")] public float releaseFadeBandwidth = 8f;

    [Header("Stabilité au Sol")]
    [Tooltip("Multiplier par le poids (m*g) pour la force verticale. 1 = poids équivalent")]
    public float holdDownWeightFactor = 1.0f;
    [Tooltip("Force verticale supplémentaire minimale (Newtons), s'ajoute au facteur de poids")] public float minExtraHoldForce = 500f;
    [Tooltip("Force supplémentaire appliquée vers le bas sur l'avant (Newtons)")] public float noseDownForce = 800f;
    [Tooltip("Offset local pour la force de nez (point d'application)")] public Vector3 noseForceLocalOffset = new Vector3(0f, 0f, 2f);
    [Tooltip("Angle de tangage max autorisé tant que pas libéré (degrés)")] public float maxPitchWhileHeld = 6f;
    [Tooltip("Damping multiplicateur pour la vitesse angulaire locale (0-1)")] [Range(0f,1f)] public float angularDampingWhileHeld = 0.6f;
    [Tooltip("Réduction du roulis (0-1) appliquée en plus du damping global")] [Range(0f,1f)] public float rollDampingExtra = 0.5f;
    [Tooltip("Damping vertical linéaire (0-1) pour coller au sol")] [Range(0f,1f)] public float verticalVelocityDamping = 0.75f;
    [Tooltip("Damping de lacet (Y) local (0-1)")] [Range(0f,1f)] public float yawDampingWhileHeld = 0.4f;
    [Tooltip("Appliquer la force vers le bas répartie à chaque roue")] public bool distributeForcePerWheel = true;
    [Tooltip("Afficher un label debug à l'écran")] public bool showDebug = false;

    [Header("Renforcement (Anti-Décollage Prématuré)")]
    [Tooltip("Supprimer quasi totalement la portance aérodynamique tant que hold actif")] public bool suppressLiftWhileHeld = true;
    [Tooltip("Facteur additionnel * poids appliqué vers le bas en plus du hold normal")] public float extraDownwardWeightFactor = 0.8f;
    [Tooltip("Vitesse verticale ascendante max autorisée (m/s) tant que hold actif")] public float maxUpVelocityWhileHeld = 0.2f;
    [Tooltip("Bloquer le pitch positif au-dessus de ce seuil (degrés) tant que hold actif")] public float clampPositivePitchDegrees = 2f;
    [Tooltip("Appliquer couple instantané pour écraser pitch > clampPositivePitchDegrees")] public bool applyHardNoseClamp = true;
    [Tooltip("Aligner activement le nez vers un pitch cible tant que hold actif")] public bool enforceTargetPitchWhileHeld = true;
    [Tooltip("Pitch cible à maintenir (°) tant que hold actif")] public float targetPitchWhileHeldDeg = 0f;
    [Tooltip("Couple par degré d'erreur pour aligner le pitch (Nm/deg)")] public float pitchAlignTorquePerDeg = 40f;

    [Header("Critères Roues au Sol")]
    [Tooltip("Nombre minimum de roues au sol pour activer le maintien")] public int minGroundedWheels = 3;
    [Tooltip("Longueur du raycast (fallback si WheelCollider non fiable)")] public float wheelRayLength = 1.0f;
    [Tooltip("LayerMask du sol (si 0 => tout)")] public LayerMask groundMask = 0;

    [Header("Démarrage / Stabilisation Initiale")]
    [Tooltip("Durée (s) de tolérance au démarrage (autorise 2 roues) et applique une force d'assise")] public float startupSettleTime = 0.6f;
    [Tooltip("Facteur du poids appliqué vers le bas pendant la phase de settling")] public float startupSeatForceWeightFactor = 0.5f;

    [Header("Robustesse / Debug")]
    [Tooltip("Maintenir le hold tant que la vitesse < releaseSpeed même si moins de roues que minGroundedWheels (≥1)")] public bool relaxWheelRequirementAtLowSpeed = true;
    [Tooltip("Afficher un tableau par roue")] public bool showPerWheelStatus = false;

    private Rigidbody rb;
    private bool isHeld;
    private float settleTimer;
    private string releaseReason = "Init";

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
        settleTimer = startupSettleTime;
    }

    void FixedUpdate()
    {
        if (plane == null || rb == null) return;

        float releaseSpeed = usePlaneTakeoffSpeed ? plane.takeoffMinSpeed : customReleaseSpeed;
        int groundedCount = CountGroundedWheels();
        bool criteriaGround = groundedCount >= minGroundedWheels;
        bool criteriaSpeed = plane.Airspeed < releaseSpeed;

        // Fenêtre de settling au démarrage: tolérer 2 roues et pousser l'avion à s'asseoir
        if (settleTimer > 0f)
        {
            settleTimer -= Time.fixedDeltaTime;
            if (!criteriaGround && groundedCount >= Mathf.Max(1, minGroundedWheels - 1))
            {
                criteriaGround = true; // tolérance temporaire
            }
            // appliquer une force d'assise
            float weight = rb.mass * Physics.gravity.magnitude;
            rb.AddForce(Vector3.down * (weight * startupSeatForceWeightFactor), ForceMode.Force);
        }

        // Relax wheel requirement if opted and still below speed
        if (relaxWheelRequirementAtLowSpeed && !criteriaGround && criteriaSpeed && groundedCount >= 1 && settleTimer <= 0f)
        {
            criteriaGround = true;
        }

        // Facteur de fade (1 = maintien fort, 0 = relâché)
        float fade = 0f;
        if (criteriaGround)
        {
            float a = releaseSpeed - releaseFadeBandwidth; // début de fade
            float b = releaseSpeed; // fin de fade
            fade = Mathf.Clamp01((b - plane.Airspeed) / Mathf.Max(0.1f, (b - a)));
        }
        bool holdPrev = isHeld;
        isHeld = criteriaGround && fade > 0f;
        if (!isHeld)
        {
            if (!criteriaGround && criteriaSpeed)
                releaseReason = $"WheelCount<{minGroundedWheels} ({groundedCount})";
            else if (!criteriaSpeed)
                releaseReason = $"Speed>=Release ({plane.Airspeed:F1})";
            else if (fade <= 0f)
                releaseReason = "Fade=0";
        }
        else if (!holdPrev && isHeld)
        {
            releaseReason = "Holding";
        }

        if (isHeld)
        {
            // Calculer force de maintien basée sur le poids
            float weight = rb.mass * Physics.gravity.magnitude;
            float holdForceTotal = weight * holdDownWeightFactor + minExtraHoldForce;
            holdForceTotal *= fade; // appliquer le fade proche de la vitesse de libération

            // Portance supprimée: appliquer une force supplémentaire pour contre-balancer toute montée
            if (suppressLiftWhileHeld)
            {
                holdForceTotal += weight * extraDownwardWeightFactor * fade;
            }

            if (distributeForcePerWheel && groundedCount > 0)
            {
                float perWheel = holdForceTotal / groundedCount;
                foreach (var wc in wheelColliders)
                {
                    if (wc == null) continue;
                    if (!WheelIsGrounded(wc)) continue;
                    rb.AddForceAtPosition(Vector3.down * perWheel, wc.transform.position, ForceMode.Force);
                }
            }
            else
            {
                // Force vers le bas au centre
                rb.AddForce(Vector3.down * holdForceTotal, ForceMode.Force);
            }

            // Force de nez pour empêcher cabrage prématuré
            Vector3 noseWorldPos = rb.transform.TransformPoint(noseForceLocalOffset);
            rb.AddForceAtPosition(Vector3.down * (noseDownForce * fade), noseWorldPos, ForceMode.Force);

            // Limiter tangage: si angle dépasse maxPitchWhileHeld, appliquer contre-couple
            float pitchAngle = Vector3.SignedAngle(Vector3.ProjectOnPlane(rb.transform.forward, Vector3.up), rb.transform.forward, rb.transform.right);
            if (pitchAngle > maxPitchWhileHeld)
            {
                // Couple négatif autour de l'axe X pour abaisser le nez
                rb.AddTorque(-rb.transform.right * (pitchAngle - maxPitchWhileHeld) * 50f * fade, ForceMode.Force);
            }

            // Alignement actif vers pitch cible (ex: 0°) pour éviter nez vers le haut
            if (enforceTargetPitchWhileHeld)
            {
                float alignError = pitchAngle - targetPitchWhileHeldDeg; // positif => trop haut
                float alignTorqueMag = Mathf.Min(Mathf.Abs(alignError) * pitchAlignTorquePerDeg, 2000f) * fade;
                // appliquer seulement si erreur significative (>0.2°)
                if (Mathf.Abs(alignError) > 0.2f)
                {
                    rb.AddTorque(-rb.transform.right * alignError / Mathf.Max(0.01f, Mathf.Abs(alignError)) * alignTorqueMag, ForceMode.Force);
                }
            }

            // Clamp dur sur pitch positif trop élevé (empêche cabrage pour générer portance)
            if (applyHardNoseClamp && pitchAngle > clampPositivePitchDegrees)
            {
                rb.AddTorque(-rb.transform.right * (pitchAngle - clampPositivePitchDegrees) * 120f * fade, ForceMode.Force);
            }

            // Damping angulaire local (appliqué sur X/Z et un peu Y) pour réduire oscillations
            Vector3 localAngular = rb.transform.InverseTransformDirection(rb.angularVelocity);
            localAngular.x *= Mathf.Clamp01(1f - angularDampingWhileHeld * fade);
            localAngular.z *= Mathf.Clamp01(1f - (angularDampingWhileHeld + rollDampingExtra * (1f - angularDampingWhileHeld)) * fade);
            localAngular.y *= Mathf.Clamp01(1f - yawDampingWhileHeld * fade);
            rb.angularVelocity = rb.transform.TransformDirection(localAngular);

            // Damping vertical linéaire pour bien coller au sol
            Vector3 v = rb.velocity;
            float newVy = Mathf.Lerp(v.y, 0f, verticalVelocityDamping * fade);
            // Empêcher montée (si v.y > max autorisé)
            if (newVy > maxUpVelocityWhileHeld) newVy = maxUpVelocityWhileHeld * 0.5f; // réduire encore
            rb.velocity = new Vector3(v.x, newVy, v.z);
        }
    }

    int CountGroundedWheels()
    {
        int count = 0;
        foreach (var wc in wheelColliders)
        {
            if (wc == null) continue;
            bool grounded = WheelIsGrounded(wc);
            if (grounded) count++;
        }
        return count;
    }

    bool WheelIsGrounded(WheelCollider wc)
    {
        if (wc.isGrounded) return true;
        WheelHit hit;
        if (wc.GetGroundHit(out hit)) return true;
        // Fallback raycast du centre de la roue
        Ray ray = new Ray(wc.transform.position, -wc.transform.up);
        return Physics.Raycast(ray, wheelRayLength, groundMask.value == 0 ? ~0 : groundMask);
    }

    void OnGUI()
    {
        return;
    }
}
