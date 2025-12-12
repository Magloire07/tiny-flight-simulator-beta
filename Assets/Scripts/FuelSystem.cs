using UnityEngine;
using UnityEngine.UI;
using MFlight.Demo;
using DemoPlane = MFlight.Demo.Plane;

/// <summary>
/// Système de gestion du carburant avec jauge visuelle
/// </summary>
public class FuelSystem : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Référence au script Plane")]
    public DemoPlane plane;
    
    [Tooltip("EngineController pour détecter si le moteur est allumé")]
    public EngineController engineController;
    
    [Header("Paramètres Carburant")]
    [Tooltip("Niveau de carburant initial (0-1)")]
    [Range(0f, 1f)]
    public float initialFuelLevel = 0.98f;
    
    [Tooltip("Temps pour vider le réservoir complet (en secondes) - 3600s = 1 heure")]
    public float fullTankDuration = 3600f; // 1 heure
    
    [Tooltip("Multiplicateur de consommation selon le throttle (1.0 = normal)")]
    public float throttleConsumptionMultiplier = 1.5f;
    
    [Tooltip("Consommation minimale au ralenti (pourcentage de la consommation max)")]
    [Range(0f, 1f)]
    public float idleConsumptionRatio = 0.2f;
    
    [Header("Aiguille de Jauge")]
    [Tooltip("Transform de l'aiguille du cadran de carburant")]
    public Transform fuelNeedleTransform;
    
    [Tooltip("Angle quand réservoir plein (en degrés)")]
    public float fullTankAngle = -45f;
    
    [Tooltip("Angle quand réservoir vide (en degrés)")]
    public float emptyTankAngle = 225f;
    
    [Tooltip("Axe de rotation de l'aiguille")]
    public Vector3 needleRotationAxis = Vector3.forward;
    
    [Header("UI Texte (Optionnel)")]
    [Tooltip("Texte affichant le pourcentage de carburant")]
    public Text fuelPercentageText;
    
    [Tooltip("Texte affichant le temps restant")]
    public Text fuelTimeRemainingText;
    
    [Header("Image Fill (Optionnel)")]
    [Tooltip("Image UI avec Fill Amount pour barre de carburant")]
    public Image fuelFillImage;
    
    [Header("Alertes")]
    [Tooltip("Niveau de carburant pour alerte basse (0-1)")]
    [Range(0f, 1f)]
    public float lowFuelWarningLevel = 0.15f;
    
    [Tooltip("Niveau critique pour arrêt moteur (0-1)")]
    [Range(0f, 1f)]
    public float criticalFuelLevel = 0.05f;
    
    [Tooltip("Texte d'alerte carburant")]
    public Text warningText;
    
    [Tooltip("Couleur normale de la jauge")]
    public Color normalFuelColor = Color.green;
    
    [Tooltip("Couleur d'alerte basse")]
    public Color lowFuelColor = Color.yellow;
    
    [Tooltip("Couleur critique")]
    public Color criticalFuelColor = Color.red;
    
    [Header("Audio (Optionnel)")]
    [Tooltip("Son d'alerte carburant faible")]
    public AudioClip lowFuelAlertSound;
    
    [Tooltip("AudioSource pour les alertes")]
    public AudioSource alertAudioSource;
    
    // État
    private float currentFuelLevel;
    private float baseConsumptionRate; // Pourcentage par seconde
    private bool lowFuelAlertTriggered = false;
    private bool engineStoppedDueToFuel = false;
    private float alertBlinkTimer = 0f;

    void Start()
    {
        // Trouver les références automatiquement
        if (plane == null)
            plane = FindObjectOfType<DemoPlane>();
        
        if (engineController == null)
            engineController = FindObjectOfType<EngineController>();
        
        if (alertAudioSource == null)
            alertAudioSource = GetComponent<AudioSource>();
        
        // Initialiser le niveau de carburant
        currentFuelLevel = initialFuelLevel;
        
        // Calculer le taux de consommation de base (pourcentage par seconde)
        baseConsumptionRate = 1f / fullTankDuration;
        
        // Initialiser l'affichage
        UpdateFuelDisplay();
        
        Debug.Log($"FuelSystem: Initialisé avec {currentFuelLevel * 100:F1}% de carburant. Consommation: {baseConsumptionRate * 100:F5}%/s");
    }

    void Update()
    {
        // Consommer le carburant si le moteur est allumé
        if (engineController != null && engineController.engineOn && currentFuelLevel > 0f)
        {
            ConsumeFuel();
        }
        
        // Mettre à jour l'affichage
        UpdateFuelDisplay();
        
        // Vérifier les alertes
        CheckFuelAlerts();
        
        // Arrêter le moteur si plus de carburant
        if (currentFuelLevel <= criticalFuelLevel && !engineStoppedDueToFuel)
        {
            HandleFuelDepletion();
        }
    }

    /// <summary>
    /// Consomme le carburant selon le throttle
    /// </summary>
    void ConsumeFuel()
    {
        if (plane == null) return;
        
        // Calculer la consommation selon le throttle
        float throttleNormalized = plane.throttle; // 0 à 1
        float consumptionMultiplier = Mathf.Lerp(idleConsumptionRatio, 1f, throttleNormalized);
        
        // Appliquer le multiplicateur de throttle
        consumptionMultiplier *= throttleConsumptionMultiplier;
        
        // Consommer le carburant
        float consumptionThisFrame = baseConsumptionRate * consumptionMultiplier * Time.deltaTime;
        currentFuelLevel = Mathf.Max(0f, currentFuelLevel - consumptionThisFrame);
    }

    /// <summary>
    /// Met à jour l'affichage de la jauge
    /// </summary>
    void UpdateFuelDisplay()
    {
        // Mettre à jour l'aiguille
        if (fuelNeedleTransform != null)
        {
            float targetAngle = Mathf.Lerp(emptyTankAngle, fullTankAngle, currentFuelLevel);
            
            // Appliquer la rotation selon l'axe défini
            if (needleRotationAxis == Vector3.forward)
            {
                fuelNeedleTransform.localRotation = Quaternion.Euler(0f, 0f, targetAngle);
            }
            else if (needleRotationAxis == Vector3.right)
            {
                fuelNeedleTransform.localRotation = Quaternion.Euler(targetAngle, 0f, 0f);
            }
            else if (needleRotationAxis == Vector3.up)
            {
                fuelNeedleTransform.localRotation = Quaternion.Euler(0f, targetAngle, 0f);
            }
        }
        
        // Mettre à jour le texte de pourcentage
        if (fuelPercentageText != null)
        {
            fuelPercentageText.text = $"{currentFuelLevel * 100:F1}%";
        }
        
        // Mettre à jour le temps restant
        if (fuelTimeRemainingText != null)
        {
            float timeRemaining = GetEstimatedTimeRemaining();
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            fuelTimeRemainingText.text = $"{minutes:D2}:{seconds:D2}";
        }
        
        // Mettre à jour la barre de remplissage
        if (fuelFillImage != null)
        {
            fuelFillImage.fillAmount = currentFuelLevel;
            
            // Changer la couleur selon le niveau
            if (currentFuelLevel <= criticalFuelLevel)
                fuelFillImage.color = criticalFuelColor;
            else if (currentFuelLevel <= lowFuelWarningLevel)
                fuelFillImage.color = lowFuelColor;
            else
                fuelFillImage.color = normalFuelColor;
        }
    }

    /// <summary>
    /// Vérifie et affiche les alertes de carburant
    /// </summary>
    void CheckFuelAlerts()
    {
        if (warningText == null) return;
        
        if (currentFuelLevel <= criticalFuelLevel)
        {
            // Alerte critique - clignotement rapide
            alertBlinkTimer += Time.deltaTime * 5f;
            warningText.enabled = Mathf.Sin(alertBlinkTimer) > 0f;
            warningText.text = "⚠ CARBURANT CRITIQUE ⚠";
            warningText.color = criticalFuelColor;
        }
        else if (currentFuelLevel <= lowFuelWarningLevel)
        {
            // Alerte basse - clignotement lent
            alertBlinkTimer += Time.deltaTime * 2f;
            warningText.enabled = Mathf.Sin(alertBlinkTimer) > 0f;
            warningText.text = "⚠ Carburant Faible";
            warningText.color = lowFuelColor;
            
            // Jouer le son d'alerte une fois
            if (!lowFuelAlertTriggered && alertAudioSource != null && lowFuelAlertSound != null)
            {
                alertAudioSource.PlayOneShot(lowFuelAlertSound);
                lowFuelAlertTriggered = true;
            }
        }
        else
        {
            warningText.enabled = false;
            lowFuelAlertTriggered = false;
        }
    }

    /// <summary>
    /// Gère l'épuisement du carburant
    /// </summary>
    void HandleFuelDepletion()
    {
        engineStoppedDueToFuel = true;
        
        if (engineController != null && engineController.engineOn)
        {
            engineController.ToggleEngine();
            Debug.LogWarning("FuelSystem: Moteur arrêté - Plus de carburant!");
        }
    }

    /// <summary>
    /// Calcule le temps restant estimé en secondes
    /// </summary>
    float GetEstimatedTimeRemaining()
    {
        if (plane == null || baseConsumptionRate <= 0f) return 0f;
        
        // Estimation basée sur le throttle actuel
        float throttleNormalized = engineController != null && engineController.engineOn ? plane.throttle : 0f;
        float consumptionMultiplier = Mathf.Lerp(idleConsumptionRatio, 1f, throttleNormalized) * throttleConsumptionMultiplier;
        
        float currentConsumptionRate = baseConsumptionRate * consumptionMultiplier;
        
        if (currentConsumptionRate <= 0f) return float.MaxValue;
        
        return currentFuelLevel / currentConsumptionRate;
    }

    /// <summary>
    /// Ajoute du carburant (pour ravitaillement)
    /// </summary>
    public void Refuel(float amount)
    {
        currentFuelLevel = Mathf.Clamp01(currentFuelLevel + amount);
        engineStoppedDueToFuel = false;
        Debug.Log($"FuelSystem: Ravitaillement de {amount * 100:F1}%. Niveau actuel: {currentFuelLevel * 100:F1}%");
    }

    /// <summary>
    /// Remplit complètement le réservoir
    /// </summary>
    public void RefuelFull()
    {
        Refuel(1f);
    }

    /// <summary>
    /// Obtient le niveau de carburant actuel
    /// </summary>
    public float GetFuelLevel()
    {
        return currentFuelLevel;
    }

    /// <summary>
    /// Définit le niveau de carburant
    /// </summary>
    public void SetFuelLevel(float level)
    {
        currentFuelLevel = Mathf.Clamp01(level);
        engineStoppedDueToFuel = false;
    }

    /// <summary>
    /// Affichage debug optionnel
    /// </summary>
    void OnGUI()
    {
        // Désactiver si vous utilisez uniquement l'UI
        if (fuelPercentageText != null) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 18;
        style.fontStyle = FontStyle.Bold;
        
        // Couleur selon le niveau
        if (currentFuelLevel <= criticalFuelLevel)
            style.normal.textColor = criticalFuelColor;
        else if (currentFuelLevel <= lowFuelWarningLevel)
            style.normal.textColor = lowFuelColor;
        else
            style.normal.textColor = normalFuelColor;
        
        GUI.Label(new Rect(10, 80, 200, 25), $"Fuel: {currentFuelLevel * 100:F1}%", style);
        
        // Temps restant
        float timeRemaining = GetEstimatedTimeRemaining();
        if (timeRemaining < 3600f)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            GUI.Label(new Rect(10, 105, 200, 25), $"Time: {minutes:D2}:{seconds:D2}", style);
        }
    }
}
