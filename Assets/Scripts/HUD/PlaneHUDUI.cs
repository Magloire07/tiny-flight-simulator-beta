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
    [Header("Vitesse")]
    [Tooltip("RectTransform du cadran de vitesse fixe.")] public RectTransform speedCadran;
    [Tooltip("RectTransform de l'aiguille de vitesse (qui tourne selon la vitesse).")] public RectTransform speedAiguille;
    public Text speedTextGauge;
    [Tooltip("Vitesse maximale sur le cadran (km/h).")] public float maxSpeedKmh = 200f;
    
    [Header("Compas")]
    [Tooltip("RectTransform du cadran de cap fixe.")] public RectTransform capCadran;
    [Tooltip("RectTransform de l'aiguille de cap (qui tourne selon le heading).")] public RectTransform capAiguille;
    
    [Header("Fuel")]
    [Tooltip("RectTransform du cadran de fuel fixe.")] public RectTransform fuelCadran;
    [Tooltip("RectTransform de l'aiguille de fuel (qui tourne selon le niveau).")] public RectTransform fuelAiguille;
    public Text fuelText;
    [Range(0f,1f)] public float fuel01 = 0.98f;

    [Header("Horizon Artificiel")]
    [Tooltip("RectTransform du cadran fixe (fond stable).")] public RectTransform horizonCadran;
    [Tooltip("RectTransform de l'aiguille horizontale (qui tourne et se déplace).")] public RectTransform horizonAiguille;
    [Tooltip("Pixels de décalage vertical par degré de pitch.")] public float pitchPixelsPerDegree = 4f;
    [Tooltip("Lisser la transition (0 = instantané).")][Range(0f,1f)] public float smoothFactor = 0.15f;

    [Header("Map Zone")]
    [Tooltip("RectTransform pour la zone de la carte (future implémentation).")] public RectTransform mapZone;
    [Tooltip("Créer automatiquement la zone de carte si non assignée.")] public bool autoCreateMapZone = true;
    [Tooltip("Taille de la zone de carte.")] public Vector2 mapZoneSize = new Vector2(400, 300);
    [Tooltip("Position ancrée de la zone de carte.")] public Vector2 mapZonePosition = new Vector2(-220, -170);
    [Tooltip("Couleur de fond de la zone de carte.")] public Color mapZoneColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

    private float displayedPitch;
    private float displayedRoll;
    private float displayedHeading;
    private float displayedSpeed;
    private Vector3 baseAiguilleWorldPosition;
    private Vector3 baseCapAiguilleWorldPosition;
    private Vector3 baseFuelAiguilleWorldPosition;
    private Vector3 baseSpeedAiguilleWorldPosition;
    private Quaternion initialFuelRotation;
    private bool isInitialized = false;
    private bool isCapInitialized = false;
    private bool isFuelInitialized = false;
    private bool isSpeedInitialized = false;

    private void Awake()
    {
        if (provider == null) provider = FindObjectOfType<PlaneHudProvider>();
        if (plane == null)
        {
            if (provider != null && provider.plane != null) plane = provider.plane;
            else plane = FindObjectOfType<DemoPlane>();
        }
        
        // Initialiser la position de base de l'aiguille pour l'aligner avec le cadran
        if (horizonCadran != null && horizonAiguille != null)
        {
            // Aligner au centre world du cadran (indépendant de la taille des images)
            baseAiguilleWorldPosition = horizonCadran.position;
            horizonAiguille.position = baseAiguilleWorldPosition;
            isInitialized = true;
        }
        
        // Initialiser la position de base de l'aiguille de cap
        if (capCadran != null && capAiguille != null)
        {
            // Aligner au centre world du cadran (indépendant de la taille des images)
            baseCapAiguilleWorldPosition = capCadran.position;
            capAiguille.position = baseCapAiguilleWorldPosition;
            isCapInitialized = true;
        }
        
        // Initialiser la position de base de l'aiguille de fuel
        if (fuelCadran != null && fuelAiguille != null)
        {
            // Aligner au centre world du cadran (indépendant de la taille des images)
            baseFuelAiguilleWorldPosition = fuelCadran.position;
            fuelAiguille.position = baseFuelAiguilleWorldPosition;
            // L'aiguille à rotation Z = 0 correspond à 100% (réservoir plein)
            fuelAiguille.localRotation = Quaternion.identity;
            initialFuelRotation = Quaternion.identity;
            isFuelInitialized = true;
        }
        
        // Initialiser la position de base de l'aiguille de vitesse
        if (speedCadran != null && speedAiguille != null)
        {
            // Aligner au centre world du cadran (indépendant de la taille des images)
            baseSpeedAiguilleWorldPosition = speedCadran.position;
            speedAiguille.position = baseSpeedAiguilleWorldPosition;
            // L'aiguille à rotation Z = 0 correspond à la vitesse maximale
            speedAiguille.localRotation = Quaternion.identity;
            isSpeedInitialized = true;
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
        displayedHeading = Mathf.LerpAngle(displayedHeading, hdg, 1f - smoothFactor);
        // Convertir la vitesse de knots en km/h pour l'affichage
        float speedKmh = speedKt * 1.852f;
        displayedSpeed = Mathf.Lerp(displayedSpeed, speedKmh, 1f - smoothFactor);

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

        // Compas (Cap)
        if (capAiguille)
        {
            // S'assurer que la position de base est initialisée
            if (!isCapInitialized && capCadran != null)
            {
                baseCapAiguilleWorldPosition = capCadran.position;
                isCapInitialized = true;
            }
            
            // Rotation de l'aiguille selon le heading (Nord en haut = 0°)
            capAiguille.localRotation = Quaternion.Euler(0f, 0f, -displayedHeading);
            capAiguille.position = baseCapAiguilleWorldPosition;
        }
        
        // Vitesse (cadran et aiguille)
        if (speedAiguille)
        {
            // S'assurer que la position de base est initialisée
            if (!isSpeedInitialized && speedCadran != null)
            {
                baseSpeedAiguilleWorldPosition = speedCadran.position;
                isSpeedInitialized = true;
            }
            
            // Rotation de l'aiguille selon la vitesse
            // 0 km/h = -120°, maxSpeedKmh = 0°
            float speedRatio = Mathf.Clamp01(displayedSpeed / maxSpeedKmh);
            float speedAngle = -120f + (speedRatio * 120f);
            speedAiguille.localRotation = Quaternion.Euler(0f, 0f, speedAngle);
            speedAiguille.position = baseSpeedAiguilleWorldPosition;
        }
        if (speedTextGauge) speedTextGauge.text = string.Format("{0:0} km/h", displayedSpeed);
        
        // Fuel gauge (cadran et aiguille)
        if (fuelAiguille)
        {
            // S'assurer que la position de base est initialisée
            if (!isFuelInitialized && fuelCadran != null)
            {
                baseFuelAiguilleWorldPosition = fuelCadran.position;
                initialFuelRotation = Quaternion.identity;
                isFuelInitialized = true;
            }
            
            // Rotation de l'aiguille selon le niveau de fuel
            // 100% = 0°, 50% = -60°, 0% = -120°
            float fuelAngle = -120f + (Mathf.Clamp01(fuel01) * 120f);
            fuelAiguille.localRotation = Quaternion.Euler(0f, 0f, fuelAngle);
            fuelAiguille.position = baseFuelAiguilleWorldPosition;
        }
        if (fuelText) fuelText.text = string.Format("{0:0}%", Mathf.Clamp01(fuel01) * 100f);

        // Horizon artificiel
        // Le cadran reste fixe (horizonCadran ne bouge pas)
        // L'aiguille tourne selon le roll ET se déplace verticalement selon le pitch
        if (horizonAiguille)
        {
            // S'assurer que la position de base est initialisée
            if (!isInitialized && horizonCadran != null)
            {
                baseAiguilleWorldPosition = horizonCadran.position;
                isInitialized = true;
            }
            
            // Rotation de l'aiguille selon le roll (vers la gauche ou la droite)
            horizonAiguille.localRotation = Quaternion.Euler(0f, 0f, -displayedRoll);
            
            // Déplacement vertical selon le pitch (haut ou bas) à partir de la position du cadran
            Vector3 worldPos = baseAiguilleWorldPosition;
            worldPos.y += -displayedPitch * pitchPixelsPerDegree; // pitch positif => aiguille descend
            horizonAiguille.position = worldPos;
        }
    }
}
