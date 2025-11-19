using UnityEngine;
using MFlight.Demo;
using DemoPlane = MFlight.Demo.Plane;

/// <summary>
/// Fournit les données HUD pour un avion contrôlé par le script <see cref="Plane"/>.
/// </summary>
public class PlaneHudProvider : MonoBehaviour, IHudProvider
{
    [Header("Références")]
    [Tooltip("Référence vers le script Plane.")] public DemoPlane plane;
    [Tooltip("RigidBody associé. Auto-récup si null.")] public Rigidbody rb;

    [Header("GPS Approximation")] 
    [Tooltip("Activer conversion position monde -> lat/long approximatif.")] public bool enableGeographicMapping = false;
    [Tooltip("Latitude origine.")] public double originLatitude = 48.8566; // Paris par défaut
    [Tooltip("Longitude origine.")] public double originLongitude = 2.3522;
    [Tooltip("1 unité monde = mètres ? (scale).")][Min(0.0001f)] public float worldUnitToMeters = 1f;

    [Tooltip("Recalcule dynamique du facteur mètres/degré longitude (sinus latitude).")] public bool autoUpdateLonMeters = true;
    [Tooltip("Facteur mètres par degré de latitude (~111320m).")][Min(1f)] public double metersPerDegreeLat = 111_320.0;
    [Tooltip("Facteur mètres par degré de longitude (dépend cos(lat)).")][Min(1f)] public double metersPerDegreeLon = 75_000.0; // approximatif à ~48°

    private void Awake()
    {
        if (plane == null) plane = GetComponentInParent<DemoPlane>();
        if (plane != null && rb == null) rb = plane.GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (autoUpdateLonMeters && enableGeographicMapping)
        {
            metersPerDegreeLon = metersPerDegreeLat * Mathf.Cos((float)originLatitude * Mathf.Deg2Rad);
        }
    }

    public float SpeedMs => plane != null ? plane.Airspeed : (rb != null ? rb.velocity.magnitude : 0f);
    public float SpeedKnots => SpeedMs * 1.943844f;
    public float AltitudeMeters => transform.position.y * worldUnitToMeters;
    public float HeadingDegrees
    {
        get
        {
            float h = transform.eulerAngles.y; // 0-360
            return h;
        }
    }
    public float PitchDegrees
    {
        get
        {
            // Angle entre forward et sa projection sur le plan horizontal (axe transform.right)
            Vector3 fwd = transform.forward;
            Vector3 fwdProj = Vector3.ProjectOnPlane(fwd, Vector3.up).normalized;
            if (fwdProj.sqrMagnitude < 0.0001f) return 0f;
            float angle = Vector3.SignedAngle(fwdProj, fwd, transform.right);
            return angle;
        }
    }
    public float RollDegrees
    {
        get
        {
            // Angle de roulis: comparer up projeté au plan vertical défini par forward.
            Vector3 up = transform.up;
            Vector3 upProj = Vector3.ProjectOnPlane(up, transform.forward).normalized;
            if (upProj.sqrMagnitude < 0.0001f) return 0f;
            float angle = Vector3.SignedAngle(Vector3.up, upProj, transform.forward);
            return angle;
        }
    }
    public float VerticalSpeed => rb != null ? rb.velocity.y : 0f;
    public double Latitude
    {
        get
        {
            if (!enableGeographicMapping) return 0d;
            double metersNorth = transform.position.z * worldUnitToMeters;
            return originLatitude + metersNorth / metersPerDegreeLat;
        }
    }
    public double Longitude
    {
        get
        {
            if (!enableGeographicMapping) return 0d;
            double metersEast = transform.position.x * worldUnitToMeters;
            return originLongitude + metersEast / metersPerDegreeLon;
        }
    }
    public Vector3 WorldPosition => transform.position;
}
