using UnityEngine;
using UnityEngine.UI;
using DemoPlane = MFlight.Demo.Plane;

/// <summary>
/// Met à jour les éléments UI du HUD à partir d'un <see cref="PlaneHudProvider"/>.
/// Prévu pour être placé sur un GameObject Canvas.
/// </summary>
public class PlaneHUDUI : MonoBehaviour
{
    [Header("Source")] public PlaneHudProvider provider;
    [Tooltip("Référence Plane pour throttle, stall, etc.")] public DemoPlane plane;

    [Header("Texts (UnityEngine.UI)")] 
    public Text speedText; 
    public Text altitudeText; 
    public Text headingText; 
    public Text pitchText; 
    public Text rollText; 
    public Text verticalSpeedText; 
    public Text gpsText;

    [Header("Gauges")]
    [Tooltip("Image type=Filled pour le throttle (0..1)")] public Image throttleArc;
    public Text throttleText;
    [Tooltip("Image type=Filled pour le fuel (0..1)")] public Image fuelArc;
    public Text fuelText;
    [Range(0f,1f)] public float fuel01 = 0.98f;

    [Header("Vertical Speed Indicator")]
    [Tooltip("Image type=Filled vertical (fillAmount 0..1)")] public Image vsiBar;
    public Text vsiText;
    [Tooltip("± plage m/s pour l'indicateur vertical")] public float vsiRange = 20f;

    [Header("Horizon Artificiel")]
    [Tooltip("RectTransform à faire pivoter pour le roulis.")] public RectTransform horizonRollLayer;
    [Tooltip("RectTransform à déplacer verticalement pour le tangage (enfants du layer roulis de préférence).")]
    public RectTransform horizonPitchLayer;
    [Tooltip("Pixels de décalage vertical par degré de pitch.")] public float pitchPixelsPerDegree = 4f;
    [Tooltip("Lisser la transition (0 = instantané).")][Range(0f,1f)] public float smoothFactor = 0.15f;

    private float displayedPitch;
    private float displayedRoll;

    private void Awake()
    {
        if (provider == null) provider = FindObjectOfType<PlaneHudProvider>();
        if (plane == null)
        {
            if (provider != null && provider.plane != null) plane = provider.plane;
            else plane = FindObjectOfType<DemoPlane>();
        }
    }

    private void Update()
    {
        if (provider == null) return;

        // Récupération données
        float speedKt = provider.SpeedKnots;
        float alt = provider.AltitudeMeters;
        float hdg = provider.HeadingDegrees;
        float pitch = provider.PitchDegrees;
        float roll = provider.RollDegrees;
        float vs = provider.VerticalSpeed;

        // Lissage simple
        displayedPitch = Mathf.Lerp(displayedPitch, pitch, 1f - smoothFactor);
        displayedRoll = Mathf.Lerp(displayedRoll, roll, 1f - smoothFactor);

        // Textes
        if (speedText) speedText.text = string.Format("SPD {0:F0} kt", speedKt);
        if (altitudeText) altitudeText.text = string.Format("ALT {0:F0} m", alt);
        if (headingText) headingText.text = string.Format("HDG {0:000}°", Mathf.RoundToInt(hdg) % 360);
        if (pitchText) pitchText.text = string.Format("PITCH {0:+0.0;-0.0;0.0}°", pitch);
        if (rollText) rollText.text = string.Format("ROLL {0:+0.0;-0.0;0.0}°", roll);
        if (verticalSpeedText) verticalSpeedText.text = string.Format("VS {0:+0.0;-0.0;0.0} m/s", vs);
        if (gpsText)
        {
            if (provider.enableGeographicMapping)
                gpsText.text = string.Format("LAT {0:F5}\nLON {1:F5}", provider.Latitude, provider.Longitude);
            else
                gpsText.text = string.Format("POS X:{0:F0} Y:{1:F0} Z:{2:F0}", provider.WorldPosition.x, provider.WorldPosition.y, provider.WorldPosition.z);
        }

        // Throttle / Fuel gauges
        if (plane != null)
        {
            float thr = Mathf.Clamp01(plane.throttle);
            if (throttleArc) throttleArc.fillAmount = thr;
            if (throttleText) throttleText.text = string.Format("{0:0}%", thr * 100f);
        }
        if (fuelArc) fuelArc.fillAmount = Mathf.Clamp01(fuel01);
        if (fuelText) fuelText.text = string.Format("{0:0}%", Mathf.Clamp01(fuel01) * 100f);

        // VSI (vertical speed) as bar 0..1 where 0.5 = 0 m/s
        if (vsiBar)
        {
            float t = Mathf.InverseLerp(-vsiRange, vsiRange, Mathf.Clamp(vs, -vsiRange, vsiRange));
            vsiBar.fillAmount = t; // requires Filled vertical
        }
        if (vsiText) vsiText.text = string.Format("{0:+0.0;-0.0;0.0}", vs);

        // Horizon artificiel
        if (horizonRollLayer)
        {
            horizonRollLayer.localRotation = Quaternion.Euler(0f, 0f, -displayedRoll); // roulis inverse pour aspect standard
        }
        if (horizonPitchLayer)
        {
            var ap = horizonPitchLayer.anchoredPosition;
            ap.y = -displayedPitch * pitchPixelsPerDegree; // pitch haut => déplacer layer vers le bas
            horizonPitchLayer.anchoredPosition = ap;
        }
    }
}
