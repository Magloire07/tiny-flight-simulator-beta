using UnityEngine;
using MFlight.Demo;

/// <summary>
/// Maintient temporairement le tangage (pitch) ou l'angle d'attaque (AoA) de l'avion
/// à une valeur cible pendant une durée donnée au démarrage. Utile pour test / mise en scène.
/// N'interfère qu'en ajoutant un couple correcteur (n'écrase pas vos entrées).
/// </summary>
[DefaultExecutionOrder(60)]
public class PlaneStartupPitchHold : MonoBehaviour
{
    public enum HoldMode { Pitch, AngleOfAttack }

    [Header("Références")]
    public MFlight.Demo.Plane plane; // auto assign si null
    private Rigidbody rb;

    [Header("Configuration Hold")]
    [Tooltip("Pitch ou AoA à maintenir (degrés)")] public float targetDegrees = 15f;
    [Tooltip("Durée totale du maintien (secondes)")] public float holdDuration = 10f;
    [Tooltip("Mode de maintien: Pitch absolu horizon ou AoA par rapport au flux d'air")]
    public HoldMode mode = HoldMode.Pitch;
    [Tooltip("Force du couple correcteur (Nm par degré d'erreur)")] public float torquePerDegree = 40f;
    [Tooltip("Couple max appliqué (Nm)")] public float maxTorque = 2500f;
    [Tooltip("Réduire la force si l'utilisateur applique un pitch en sens inverse")] public bool reduceWhenPilotOpposes = true;
    [Tooltip("Se désactive si airspeed dépasse ce seuil (0 = ignore)")] public float cancelAboveAirspeed = 0f;
    [Tooltip("Se désactive si l'utilisateur dépasse ce pitch input (0 = ignore)")] public float cancelPilotPitchInputThreshold = 0.9f;
    [Tooltip("Affichage debug GUI")] public bool showDebug = true;

    private float startTime;
    private bool active;
    private string lastReason = "Init";

    void Awake()
    {
        if (plane == null) plane = GetComponentInParent<MFlight.Demo.Plane>();
        rb = plane != null ? plane.GetComponent<Rigidbody>() : null;
    }

    void OnEnable()
    {
        startTime = Time.time;
        active = true;
        lastReason = "HoldStarted";
    }

    void FixedUpdate()
    {
        if (!active || plane == null || rb == null) return;

        float elapsed = Time.time - startTime;
        if (elapsed > holdDuration)
        {
            active = false; lastReason = "DurationElapsed"; return;
        }
        if (cancelAboveAirspeed > 0f && plane.Airspeed >= cancelAboveAirspeed)
        {
            active = false; lastReason = "AirspeedCancel"; return;
        }
        // We don't have direct pilot pitch input property exposed separately (pitch stored private),
        // so cancellation via input threshold is skipped unless implemented later.

        // Mesurer l'état actuel
        float currentDeg = 0f;
        if (mode == HoldMode.Pitch)
        {
            Vector3 flatFwd = Vector3.ProjectOnPlane(rb.transform.forward, Vector3.up);
            if (flatFwd.sqrMagnitude < 1e-6f) flatFwd = rb.transform.forward; // fallback
            currentDeg = Vector3.SignedAngle(flatFwd, rb.transform.forward, rb.transform.right);
        }
        else // AoA
        {
            currentDeg = plane.AngleOfAttack; // déjà en degrés
        }

        float error = targetDegrees - currentDeg; // positif => on veut cabrer

        // Calculer couple correcteur
        float torqueMagnitude = Mathf.Clamp(Mathf.Abs(error) * torquePerDegree, 0f, maxTorque);
        // Appliquer un lissage pour éviter à-coups si proche de la cible
        float proximity = Mathf.InverseLerp(0f, 5f, Mathf.Abs(error)); // 0 près de cible, 1 loin
        torqueMagnitude *= Mathf.Clamp01(proximity + 0.2f); // garder un peu de force près de cible

        // Réduction si pilote oppose (approximation via angular velocity signe)
        if (reduceWhenPilotOpposes)
        {
            Vector3 localAngVel = rb.transform.InverseTransformDirection(rb.angularVelocity);
            float pitchRateDegPerSec = localAngVel.x * Mathf.Rad2Deg;
            // Si le signe du pitchRate s'éloigne de la cible, réduire
            if (Mathf.Sign(pitchRateDegPerSec) == Mathf.Sign(error))
            {
                // on va vers la cible, OK
            }
            else
            {
                torqueMagnitude *= 0.4f; // réduire
            }
        }

        // Appliquer couple sur l'axe X local (pitch)
        float torqueSign = Mathf.Sign(error); // positif => cabrer (axe X positif)
        Vector3 correctiveTorqueLocal = new Vector3(torqueMagnitude * torqueSign, 0f, 0f);
        rb.AddRelativeTorque(correctiveTorqueLocal, ForceMode.Force);
    }

    void OnGUI()
    {
        if (!showDebug) return;
        string state = active ? "ACTIVE" : "INACTIVE";
        GUI.Label(new Rect(10, 10, 420, 25), $"StartupHold: {state} Mode: {mode} Target: {targetDegrees:F1}° Reason: {lastReason}");
        if (active && plane != null)
        {
            GUI.Label(new Rect(10, 35, 420, 25), $"Airspeed: {plane.Airspeed:F1}  AoA: {plane.AngleOfAttack:F1}°");
        }
    }
}
