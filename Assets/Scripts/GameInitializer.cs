using UnityEngine;

/// <summary>
/// Charge les paramètres sauvegardés depuis le menu principal au démarrage de la scène Flight Demo
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("GameMenuController pour appliquer les paramètres")]
    public GameMenuController gameMenuController;
    
    [Tooltip("DynamicWeatherSystem pour appliquer la météo du scénario")]
    public DynamicWeatherSystem dynamicWeather;
    
    void Start()
    {
        // Trouver les références si non assignées
        if (gameMenuController == null)
            gameMenuController = FindObjectOfType<GameMenuController>();
        
        if (dynamicWeather == null)
            dynamicWeather = FindObjectOfType<DynamicWeatherSystem>();
        
        // Charger les sélections du menu principal
        int selectedAircraft = PlayerPrefs.GetInt("SelectedAircraft", 0);
        int selectedScenario = PlayerPrefs.GetInt("SelectedScenario", 0);
        
        Debug.Log($"GameInitializer: Avion sélectionné = {selectedAircraft}, Scénario = {selectedScenario}");
        
        // Appliquer la configuration du scénario
        ApplyScenarioSettings(selectedScenario);
        
        // TODO: Charger le modèle d'avion correspondant à selectedAircraft
        // Par exemple: Instantiate(aircraftPrefabs[selectedAircraft]);
    }
    
    /// <summary>
    /// Applique les paramètres du scénario sélectionné
    /// </summary>
    void ApplyScenarioSettings(int scenarioIndex)
    {
        switch (scenarioIndex)
        {
            case 0: // Vol Libre
                // Météo agréable par défaut
                if (gameMenuController != null && gameMenuController.weatherSlider != null)
                    gameMenuController.weatherSlider.value = 0.3f;
                
                if (gameMenuController != null && gameMenuController.timeSlider != null)
                    gameMenuController.timeSlider.value = 12f; // Midi
                
                Debug.Log("Scénario Vol Libre chargé");
                break;
                
            case 1: // Vol dans la Tempête
                // Météo orageuse
                if (gameMenuController != null && gameMenuController.weatherSlider != null)
                    gameMenuController.weatherSlider.value = 0.9f;
                
                if (gameMenuController != null && gameMenuController.timeSlider != null)
                    gameMenuController.timeSlider.value = 15f; // 15h00
                
                if (dynamicWeather != null)
                    dynamicWeather.SetWeatherIntensity(0.9f);
                
                Debug.Log("Scénario Tempête chargé");
                break;
                
            default:
                Debug.LogWarning($"Scénario inconnu: {scenarioIndex}");
                break;
        }
    }
}
