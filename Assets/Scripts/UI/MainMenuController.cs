using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Contrôleur du menu principal avec sélection d'avion, scénarios, paramètres et didacticiel
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Panneaux du Menu")]
    [Tooltip("Panneau principal avec les boutons principaux")]
    public GameObject mainPanel;
    
    [Tooltip("Panneau de sélection d'avion")]
    public GameObject aircraftSelectionPanel;
    
    [Tooltip("Panneau des paramètres")]
    public GameObject settingsPanel;
    
    [Tooltip("Panneau du didacticiel")]
    public GameObject tutorialPanel;
    
    [Tooltip("Panneau des missions")]
    public GameObject missionPanel;
    
    [Header("Sélection d'Avion")]
    [Tooltip("Liste des GameObjects avions à afficher (Plane1, Plane2, Plane3)")]
    public List<GameObject> aircraftDisplayObjects = new List<GameObject>();
    
    [Tooltip("Index de l'avion actuellement sélectionné")]
    private int selectedAircraftIndex = 0;
    
    [Tooltip("Code couleur hex de l'avion actuellement sélectionné")]
    private string selectedAircraftColorCode = "FFFFFF";
    
    [Header("Dialogue de Confirmation")]
    [Tooltip("Panel de confirmation de sélection")]
    public GameObject confirmationDialog;
    
    [Tooltip("Texte du message de confirmation")]
    public Text confirmationText;
    
    [Tooltip("Durée d'affichage du dialogue (secondes, 0 = manuel)")]
    public float confirmationDuration = 2f;
    
    [Header("Sélection de Mission")]
    [Tooltip("Index de la mission actuellement sélectionnée")]
    private int selectedMissionIndex = 0;
    
    [Tooltip("GameObjects des missions (Mission1, Mission2, Mission3)")]
    public List<GameObject> missionObjects = new List<GameObject>();
    
    [Header("Paramètres")]
    [Tooltip("Slider pour le volume audio")]
    public Slider volumeSlider;
    
    [Tooltip("Slider pour la qualité graphique")]
    public Slider graphicsQualitySlider;
    
    [Tooltip("Toggle pour le mode plein écran")]
    public Toggle fullscreenToggle;
    
    [Tooltip("Dropdown pour la résolution")]
    public Dropdown resolutionDropdown;
    
    [Tooltip("Texte affichant la qualité graphique")]
    public Text graphicsQualityText;
    
    [Header("Didacticiel")]
    [Tooltip("Index de la page actuelle du didacticiel")]
    private int tutorialPageIndex = 0;
    
    [Tooltip("Liste des pages du didacticiel")]
    public List<TutorialPage> tutorialPages = new List<TutorialPage>();
    
    [Tooltip("Texte du titre du didacticiel")]
    public Text tutorialTitleText;
    
    [Tooltip("Texte du contenu du didacticiel")]
    public Text tutorialContentText;
    
    [Tooltip("Image du didacticiel")]
    public Image tutorialImage;
    
    [Tooltip("Bouton page précédente")]
    public Button previousPageButton;
    
    [Tooltip("Bouton page suivante")]
    public Button nextPageButton;
    
    [Header("Audio")]
    [Tooltip("Son de clic de bouton")]
    public AudioClip buttonClickSound;
    
    private AudioSource audioSource;
    
    private Resolution[] resolutions;

    void Start()
    {
        // Créer l'AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        // Charger les paramètres sauvegardés
        LoadSettings();
        
        // Initialiser les résolutions
        InitializeResolutions();
        
        // Initialiser les données par défaut si nécessaire
        InitializeDefaultData();
        
        // Initialiser l'affichage des avions (désactiver tous sauf le premier)
        InitializeAircraftDisplay();
        
        // Initialiser l'affichage des missions (désactiver tous sauf la première)
        InitializeMissionDisplay();
        
        // Afficher le panneau principal
        ShowMainPanel();
    }
    
    /// <summary>
    /// Initialise l'affichage des avions au démarrage
    /// </summary>
    void InitializeAircraftDisplay()
    {
        // Désactiver tous les avions d'abord
        foreach (GameObject planeObj in aircraftDisplayObjects)
        {
            if (planeObj != null)
                planeObj.SetActive(false);
        }
        
        // Toujours commencer avec le premier avion (index 0)
        selectedAircraftIndex = 0;
        
        // Activer le premier avion
        if (aircraftDisplayObjects.Count > 0 && aircraftDisplayObjects[0] != null)
        {
            aircraftDisplayObjects[0].SetActive(true);
            Debug.Log($"MainMenuController: Premier avion activé: {aircraftDisplayObjects[0].name}");
        }
    }
    
    /// <summary>
    /// Initialise l'affichage des missions au démarrage
    /// </summary>
    void InitializeMissionDisplay()
    {
        // Désactiver toutes les missions d'abord
        foreach (GameObject mission in missionObjects)
        {
            if (mission != null)
                mission.SetActive(false);
        }
        
        // Toujours commencer avec la première mission (index 0)
        selectedMissionIndex = 0;
        
        // Activer la première mission
        if (missionObjects.Count > 0 && missionObjects[0] != null)
        {
            missionObjects[0].SetActive(true);
            Debug.Log($"MainMenuController: Première mission activée: {missionObjects[0].name}");
        }
    }

    #region Navigation entre Panneaux

    /// <summary>
    /// Affiche le panneau principal
    /// </summary>
    public void ShowMainPanel()
    {
        HideAllPanels();
        if (mainPanel != null)
            mainPanel.SetActive(true);
        PlayButtonSound();
    }

    /// <summary>
    /// Affiche le panneau de sélection d'avion
    /// </summary>
    public void ShowAircraftSelectionPanel()
    {
        HideAllPanels();
        if (aircraftSelectionPanel != null)
        {
            aircraftSelectionPanel.SetActive(true);
            UpdateAircraftDisplay();
        }
        PlayButtonSound();
    }

    /// <summary>
    /// Affiche le panneau des paramètres
    /// </summary>
    public void ShowSettingsPanel()
    {
        HideAllPanels();
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
        PlayButtonSound();
    }

    /// <summary>
    /// Affiche le panneau du didacticiel
    /// </summary>
    public void ShowTutorialPanel()
    {
        HideAllPanels();
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            tutorialPageIndex = 0;
            UpdateTutorialDisplay();
        }
        PlayButtonSound();
    }

    /// <summary>
    /// Affiche le panneau de sélection de missions
    /// </summary>
    public void ShowMissionPanel()
    {
        HideAllPanels();
        if (missionPanel != null)
        {
            missionPanel.SetActive(true);
            UpdateMissionDisplay();
        }
        PlayButtonSound();
    }

    /// <summary>
    /// Cache tous les panneaux
    /// </summary>
    void HideAllPanels()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (aircraftSelectionPanel != null) aircraftSelectionPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (missionPanel != null) missionPanel.SetActive(false);
    }

    #endregion

    #region Sélection d'Avion

    /// <summary>
    /// Sélectionne l'avion précédent
    /// </summary>
    public void PreviousAircraft()
    {
        if (aircraftDisplayObjects.Count == 0)
        {
            Debug.LogWarning("MainMenuController: Aucun avion dans aircraftDisplayObjects!");
            return;
        }
        
        selectedAircraftIndex--;
        if (selectedAircraftIndex < 0)
            selectedAircraftIndex = aircraftDisplayObjects.Count - 1;
        
        Debug.Log($"MainMenuController: Previous - Index maintenant: {selectedAircraftIndex}");
        UpdateAircraftDisplay();
        PlayButtonSound();
    }

    /// <summary>
    /// Sélectionne l'avion suivant
    /// </summary>
    public void NextAircraft()
    {
        if (aircraftDisplayObjects.Count == 0)
        {
            Debug.LogWarning("MainMenuController: Aucun avion dans aircraftDisplayObjects!");
            return;
        }
        
        selectedAircraftIndex++;
        if (selectedAircraftIndex >= aircraftDisplayObjects.Count)
            selectedAircraftIndex = 0;
        
        Debug.Log($"MainMenuController: Next - Index maintenant: {selectedAircraftIndex}");
        UpdateAircraftDisplay();
        PlayButtonSound();
    }

    /// <summary>
    /// Met à jour l'affichage de l'avion sélectionné
    /// </summary>
    void UpdateAircraftDisplay()
    {
        Debug.Log($"MainMenuController: UpdateAircraftDisplay - Index: {selectedAircraftIndex}, Nombre d'avions: {aircraftDisplayObjects.Count}");
        
        // Masquer tous les avions d'abord
        foreach (GameObject planeObj in aircraftDisplayObjects)
        {
            if (planeObj != null)
            {
                planeObj.SetActive(false);
                Debug.Log($"MainMenuController: Masqué {planeObj.name}");
            }
        }
        
        // Afficher l'avion sélectionné
        if (selectedAircraftIndex >= 0 && selectedAircraftIndex < aircraftDisplayObjects.Count)
        {
            GameObject selectedPlane = aircraftDisplayObjects[selectedAircraftIndex];
            if (selectedPlane != null)
            {
                selectedPlane.SetActive(true);
                
                // Récupérer le code couleur depuis le Text enfant tagué "color" de l'avion visible
                // Le Text peut être désactivé pour ne pas être affiché, mais on le récupère quand même
                Text[] allTexts = selectedPlane.GetComponentsInChildren<Text>(true);
                Text colorCodeText = null;
                
                foreach (Text txt in allTexts)
                {
                    if (txt.CompareTag("color"))
                    {
                        colorCodeText = txt;
                        Debug.Log($"MainMenuController: Text 'color' trouvé sur {selectedPlane.name}: {txt.text}");
                        break;
                    }
                }
                
                if (colorCodeText != null)
                {
                    selectedAircraftColorCode = colorCodeText.text.Trim();
                    Debug.Log($"MainMenuController: Avion {selectedPlane.name} activé - Code couleur récupéré: {selectedAircraftColorCode}");
                }
                else
                {
                    Debug.LogWarning($"MainMenuController: Pas de Text avec tag 'color' trouvé sur {selectedPlane.name}");
                }
            }
            else
            {
                Debug.LogWarning($"MainMenuController: L'avion à l'index {selectedAircraftIndex} est null!");
            }
        }
    }
    
    /// <summary>
    /// Confirme la sélection de l'avion et sauvegarde le code couleur
    /// </summary>
    public void ConfirmAircraftSelection()
    {
        // Sauvegarder l'index et le code couleur
        PlayerPrefs.SetInt("SelectedAircraft", selectedAircraftIndex);
        PlayerPrefs.SetString("AircraftColorCode", selectedAircraftColorCode);
        PlayerPrefs.Save();
        
        Debug.Log($"MainMenuController: Avion {selectedAircraftIndex} sélectionné avec couleur {selectedAircraftColorCode}");
        
        // Afficher le dialogue de confirmation
        ShowConfirmationDialog($"Avion #{selectedAircraftIndex + 1} sélectionné!\nCouleur: #{selectedAircraftColorCode}");
        
        PlayButtonSound();
    }
    
    /// <summary>
    /// Affiche le dialogue de confirmation
    /// </summary>
    void ShowConfirmationDialog(string message)
    {
        if (confirmationDialog == null)
        {
            Debug.LogWarning("MainMenuController: Dialogue de confirmation non assigné");
            return;
        }
        
        // Annuler tout Invoke précédent pour éviter les conflits
        CancelInvoke("HideConfirmationDialog");
        
        // Déplacer le dialogue à la fin de la hiérarchie pour qu'il soit rendu en dernier (au-dessus)
        confirmationDialog.transform.SetAsLastSibling();
        
        // Mettre à jour le texte
        if (confirmationText != null)
        {
            confirmationText.text = message;
            // S'assurer que le texte est visible
            confirmationText.color = Color.white;
            confirmationText.enabled = true;
            
            // Ajuster le RectTransform du texte pour qu'il soit visible
            RectTransform textRect = confirmationText.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.anchorMin = new Vector2(0, 0);
                textRect.anchorMax = new Vector2(1, 1);
                textRect.offsetMin = new Vector2(10, 10); // Padding gauche/bas
                textRect.offsetMax = new Vector2(-10, -10); // Padding droit/haut
            }
        }
        
        // Afficher le dialogue
        confirmationDialog.SetActive(true);
        
        Debug.Log($"MainMenuController: Dialogue affiché avec message: {message}");
        
        // Masquer automatiquement après un délai si configuré
        if (confirmationDuration > 0)
        {
            Invoke("HideConfirmationDialog", confirmationDuration);
        }
    }
    
    /// <summary>
    /// Masque le dialogue de confirmation
    /// </summary>
    public void HideConfirmationDialog()
    {
        if (confirmationDialog != null)
        {
            confirmationDialog.SetActive(false);
        }
    }

    #endregion

    #region Lancement du Jeu

    /// <summary>
    /// Lance le jeu avec les sélections actuelles
    /// </summary>
    public void StartGame()
    {
        PlayButtonSound();
        
        // Sauvegarder la sélection d'avion
        PlayerPrefs.SetInt("SelectedAircraft", selectedAircraftIndex);
        
        // Marquer que le jeu est lancé depuis le MainMenu
        PlayerPrefs.SetInt("FromMainMenu", 1);
        PlayerPrefs.Save();
        
        // Charger la scène Flight Demo
        SceneManager.LoadScene("Flight Demo");
    }
    
    /// <summary>
    /// Retourne au menu principal depuis la scène de jeu
    /// </summary>
    public static void ReturnToMainMenu()
    {
        Time.timeScale = 1f; // S'assurer que le temps n'est pas en pause
        
        // Nettoyer le flag FromMainMenu pour éviter des problèmes
        PlayerPrefs.DeleteKey("FromMainMenu");
        PlayerPrefs.Save();
        
        SceneManager.LoadScene("MainMenu");
    }

    #endregion

    #region Paramètres

    /// <summary>
    /// Initialise la liste des résolutions
    /// </summary>
    void InitializeResolutions()
    {
        if (resolutionDropdown == null) return;
        
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        
        List<string> options = new List<string>();
        int currentResolutionIndex = 0;
        
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height + " @ " + resolutions[i].refreshRate + "Hz";
            options.Add(option);
            
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }
        
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    /// <summary>
    /// Change le volume
    /// </summary>
    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("Volume", volume);
    }

    /// <summary>
    /// Change la qualité graphique
    /// </summary>
    public void SetGraphicsQuality(float quality)
    {
        int qualityLevel = Mathf.RoundToInt(quality);
        QualitySettings.SetQualityLevel(qualityLevel);
        PlayerPrefs.SetInt("GraphicsQuality", qualityLevel);
        
        if (graphicsQualityText != null)
        {
            string[] qualityNames = { "Très Bas", "Bas", "Moyen", "Élevé", "Très Élevé", "Ultra" };
            if (qualityLevel < qualityNames.Length)
                graphicsQualityText.text = qualityNames[qualityLevel];
        }
    }

    /// <summary>
    /// Change le mode plein écran
    /// </summary>
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    /// <summary>
    /// Change la résolution
    /// </summary>
    public void SetResolution(int resolutionIndex)
    {
        if (resolutionIndex < 0 || resolutionIndex >= resolutions.Length)
            return;
        
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
    }

    /// <summary>
    /// Charge les paramètres sauvegardés
    /// </summary>
    void LoadSettings()
    {
        // Volume
        if (volumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat("Volume", 1f);
            volumeSlider.value = volume;
            AudioListener.volume = volume;
        }
        
        // Qualité graphique
        if (graphicsQualitySlider != null)
        {
            int quality = PlayerPrefs.GetInt("GraphicsQuality", QualitySettings.GetQualityLevel());
            graphicsQualitySlider.value = quality;
            QualitySettings.SetQualityLevel(quality);
            SetGraphicsQuality(quality);
        }
        
        // Plein écran
        if (fullscreenToggle != null)
        {
            bool fullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            fullscreenToggle.isOn = fullscreen;
            Screen.fullScreen = fullscreen;
        }
        
        // Résolution
        if (resolutionDropdown != null)
        {
            int resIndex = PlayerPrefs.GetInt("ResolutionIndex", resolutions.Length - 1);
            if (resIndex < resolutions.Length)
            {
                resolutionDropdown.value = resIndex;
            }
        }
        
        // Sélections
        selectedAircraftIndex = PlayerPrefs.GetInt("SelectedAircraft", 0);
    }

    #endregion

    #region Didacticiel

    /// <summary>
    /// Affiche la page précédente du didacticiel
    /// </summary>
    public void PreviousTutorialPage()
    {
        if (tutorialPages.Count == 0) return;
        
        tutorialPageIndex--;
        if (tutorialPageIndex < 0)
            tutorialPageIndex = 0;
        
        UpdateTutorialDisplay();
        PlayButtonSound();
    }

    /// <summary>
    /// Affiche la page suivante du didacticiel
    /// </summary>
    public void NextTutorialPage()
    {
        if (tutorialPages.Count == 0) return;
        
        tutorialPageIndex++;
        if (tutorialPageIndex >= tutorialPages.Count)
            tutorialPageIndex = tutorialPages.Count - 1;
        
        UpdateTutorialDisplay();
        PlayButtonSound();
    }

    /// <summary>
    /// Met à jour l'affichage du didacticiel
    /// </summary>
    void UpdateTutorialDisplay()
    {
        if (tutorialPages.Count == 0 || tutorialPageIndex >= tutorialPages.Count)
            return;
        
        TutorialPage page = tutorialPages[tutorialPageIndex];
        
        if (tutorialTitleText != null)
            tutorialTitleText.text = page.title;
        
        if (tutorialContentText != null)
            tutorialContentText.text = page.content;
        
        if (tutorialImage != null && page.image != null)
            tutorialImage.sprite = page.image;
        
        // Activer/désactiver les boutons de navigation
        if (previousPageButton != null)
            previousPageButton.interactable = (tutorialPageIndex > 0);
        
        if (nextPageButton != null)
            nextPageButton.interactable = (tutorialPageIndex < tutorialPages.Count - 1);
    }

    #endregion

    #region Utilitaires

    /// <summary>
    /// Joue le son de clic de bouton
    /// </summary>
    void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    /// <summary>
    /// Initialise les données par défaut
    /// </summary>
    void InitializeDefaultData()
    {
        // Pages de didacticiel par défaut si la liste est vide
        if (tutorialPages.Count == 0)
        {
            tutorialPages.Add(new TutorialPage
            {
                title = "🎮 Bienvenue !",
                content = "Bienvenue dans le simulateur de vol !\n\n" +
                         "Ce didacticiel vous guidera à travers les bases du pilotage.\n\n" +
                         "Utilisez les flèches pour naviguer entre les pages."
            });
            
            tutorialPages.Add(new TutorialPage
            {
                title = "✈️ Contrôles de Base",
                content = "CLAVIER:\n" +
                         "• W/S: Pitch (monter/descendre)\n" +
                         "• A/D: Roll (incliner)\n" +
                         "• Q/E: Yaw (tourner)\n" +
                         "• Shift/Ctrl: Throttle (accélérer/ralentir)\n\n" +
                         "SOURIS:\n" +
                         "• Bouton droit maintenu: Regarder autour"
            });
            
            tutorialPages.Add(new TutorialPage
            {
                title = "🎛️ Interface",
                content = "HUD (Affichage Tête Haute):\n\n" +
                         "• Altimètre: Votre altitude actuelle\n" +
                         "• Anémomètre: Votre vitesse\n" +
                         "• Horizon artificiel: Votre orientation\n" +
                         "• Compas: Votre direction\n\n" +
                         "Menu (ESC): Accédez aux paramètres en vol"
            });
            
            tutorialPages.Add(new TutorialPage
            {
                title = "🌤️ Météo",
                content = "Le système météo dynamique affecte votre vol:\n\n" +
                         "• Vent: Pousse l'avion\n" +
                         "• Pluie: Réduit la visibilité\n" +
                         "• Brouillard: Limite la vision\n" +
                         "• Orage: Turbulences fortes\n\n" +
                         "Ajustez la météo dans le menu en vol!"
            });
            
            tutorialPages.Add(new TutorialPage
            {
                title = "🚀 Décollage",
                content = "Pour décoller:\n\n" +
                         "1. Augmentez le throttle (Shift)\n" +
                         "2. Accélérez sur la piste\n" +
                         "3. Tirez doucement sur le manche (S)\n" +
                         "4. Maintenez l'angle de montée stable\n" +
                         "5. Rétractez le train d'atterrissage si disponible"
            });
            
            tutorialPages.Add(new TutorialPage
            {
                title = "🛬 Atterrissage",
                content = "Pour atterrir:\n\n" +
                         "1. Réduisez le throttle\n" +
                         "2. Alignez-vous avec la piste\n" +
                         "3. Descendez progressivement\n" +
                         "4. Gardez une vitesse stable\n" +
                         "5. Touchez en douceur le sol\n" +
                         "6. Freinez (B)"
            });
            
            tutorialPages.Add(new TutorialPage
            {
                title = "✅ Prêt à Voler !",
                content = "Vous connaissez maintenant les bases !\n\n" +
                         "Conseils:\n" +
                         "• Commencez par le Vol Libre\n" +
                         "• Pratiquez les virages\n" +
                         "• Ne volez pas trop vite près du sol\n" +
                         "• Gardez toujours un œil sur l'altitude\n\n" +
                         "Bon vol ! 🛫"
            });
        }
    }

    /// <summary>
    /// Quitte le jeu
    /// </summary>
    public void QuitGame()
    {
        PlayButtonSound();
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    #endregion

    #region Sélection de Mission

    /// <summary>
    /// Met à jour l'affichage des missions
    /// </summary>
    void UpdateMissionDisplay()
    {
        if (missionObjects == null || missionObjects.Count == 0)
        {
            Debug.LogWarning("MainMenuController: Aucune mission dans missionObjects!");
            return;
        }

        // Désactiver toutes les missions
        foreach (GameObject mission in missionObjects)
        {
            if (mission != null)
            {
                mission.SetActive(false);
            }
        }

        // Activer la mission sélectionnée
        if (selectedMissionIndex >= 0 && selectedMissionIndex < missionObjects.Count)
        {
            GameObject selectedMission = missionObjects[selectedMissionIndex];
            if (selectedMission != null)
            {
                selectedMission.SetActive(true);
                Debug.Log($"MainMenuController: Mission {selectedMissionIndex + 1} affichée");
            }
        }
    }

    /// <summary>
    /// Sélectionne la mission précédente
    /// </summary>
    public void PreviousMission()
    {
        if (missionObjects.Count == 0) return;
        
        selectedMissionIndex--;
        if (selectedMissionIndex < 0)
            selectedMissionIndex = missionObjects.Count - 1;
        
        UpdateMissionDisplay();
        PlayButtonSound();
        Debug.Log($"MainMenuController: Mission précédente - Index: {selectedMissionIndex}");
    }

    /// <summary>
    /// Sélectionne la mission suivante
    /// </summary>
    public void NextMission()
    {
        if (missionObjects.Count == 0) return;
        
        selectedMissionIndex++;
        if (selectedMissionIndex >= missionObjects.Count)
            selectedMissionIndex = 0;
        
        UpdateMissionDisplay();
        PlayButtonSound();
        Debug.Log($"MainMenuController: Mission suivante - Index: {selectedMissionIndex}");
    }

    /// <summary>
    /// Sélectionne une mission spécifique par index
    /// </summary>
    public void SelectMission(int missionIndex)
    {
        if (missionIndex >= 0 && missionIndex < missionObjects.Count)
        {
            selectedMissionIndex = missionIndex;
            UpdateMissionDisplay();
            PlayButtonSound();
            Debug.Log($"MainMenuController: Mission {missionIndex + 1} sélectionnée");
        }
    }

    /// <summary>
    /// Confirme la sélection de la mission actuelle
    /// </summary>
    public void ConfirmMissionSelection()
    {
        // Obtenir le nom de la mission
        string missionName = "Mission inconnue";
        if (selectedMissionIndex >= 0 && selectedMissionIndex < missionObjects.Count && missionObjects[selectedMissionIndex] != null)
        {
            missionName = missionObjects[selectedMissionIndex].name;
        }
        
        // Sauvegarder l'index ET le nom de la mission sélectionnée
        PlayerPrefs.SetInt("SelectedMission", selectedMissionIndex);
        PlayerPrefs.SetString("SelectedMissionName", missionName);
        PlayerPrefs.Save();
        
        Debug.Log($"MainMenuController: {missionName} confirmée et sauvegardée (index: {selectedMissionIndex})");
        
        // Afficher un message de confirmation avec le nom réel
        ShowConfirmationDialog($"{missionName} sélectionnée!");
        
        PlayButtonSound();
    }

    #endregion
}

/// <summary>
/// Données d'un avion
/// </summary>
[System.Serializable]
public class AircraftData
{
    public string aircraftName;
    public string description;
    public Sprite previewSprite;
    public string prefabName;
}

/// <summary>
/// Page de didacticiel
/// </summary>
[System.Serializable]
public class TutorialPage
{
    public string title;
    [TextArea(5, 15)]
    public string content;
    public Sprite image;
}
