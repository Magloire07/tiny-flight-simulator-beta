using UnityEngine;
using UnityEngine.UI;
using MFlight.Demo;
using DemoPlane = MFlight.Demo.Plane;

/// <summary>
/// Contrôle on/off du moteur de l'avion avec un bouton UI
/// </summary>
public class EngineController : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Référence au script Plane")]
    public DemoPlane plane;
    
    [Tooltip("Transform de l'hélice pour animation visuelle (optionnel)")]
    public Transform propeller;
    
    [Header("Contrôles")]
    [Tooltip("Bouton UI pour allumer/éteindre le moteur")]
    public Button engineToggleButton;
    
    [Tooltip("Texte du bouton")]
    public Text engineButtonText;
    
    [Tooltip("Image du bouton (pour changer la couleur)")]
    public Image engineButtonImage;
    
    [Tooltip("Touche clavier alternative (optionnelle)")]
    public KeyCode toggleEngineKey = KeyCode.None;
    
    [Header("Couleurs Bouton")]
    [Tooltip("Couleur du bouton quand moteur allumé")]
    public Color buttonColorOn = Color.green;
    
    [Tooltip("Couleur du bouton quand moteur éteint")]
    public Color buttonColorOff = Color.red;
    
    [Tooltip("Couleur du texte quand moteur allumé")]
    public Color textColorOn = new Color(0f, 0.5f, 0f); // Vert foncé
    
    [Tooltip("Couleur du texte quand moteur éteint")]
    public Color textColorOff = new Color(0.8f, 0f, 0f); // Rouge foncé
    
    [Header("État")]
    [Tooltip("Le moteur est-il allumé?")]
    public bool engineOn = true;
    
    [Header("Animation Hélice")]
    [Tooltip("Vitesse de rotation de l'hélice (degrés/seconde)")]
    public float propellerSpeed = 5000f;
    
    [Tooltip("Vitesse de ralentissement quand moteur éteint")]
    public float propellerDeceleration = 2000f;
    
    [Header("Audio (Optionnel)")]
    [Tooltip("Source audio pour le son du moteur")]
    public AudioSource engineAudioSource;
    
    [Tooltip("Pitch minimum quand moteur au ralenti")]
    public float minEnginePitch = 0.5f;
    
    [Tooltip("Pitch maximum à plein régime")]
    public float maxEnginePitch = 2f;
    
    // Variables internes
    private float currentPropellerSpeed = 0f;
    private float savedThrottle = 0f;
    
    void Start()
    {
        // Trouver les références automatiquement si non assignées
        if (plane == null)
        {
            plane = GetComponent<DemoPlane>();
            if (plane == null)
            {
                Debug.LogError("EngineController: Aucune référence Plane trouvée!");
                enabled = false;
                return;
            }
        }
        
        // Chercher l'hélice automatiquement
        if (propeller == null)
        {
            Transform[] children = GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child.name.ToLower().Contains("propeller") || 
                    child.name.ToLower().Contains("helice") ||
                    child.name.ToLower().Contains("prop"))
                {
                    propeller = child;
                    Debug.Log("EngineController: Hélice trouvée: " + child.name);
                    break;
                }
            }
        }
        
        // Initialiser l'audio si présent
        if (engineAudioSource != null)
        {
            if (engineOn)
            {
                if (!engineAudioSource.isPlaying)
                    engineAudioSource.Play();
            }
            else
            {
                engineAudioSource.Stop();
            }
        }
        
        // Configurer le bouton UI
        if (engineToggleButton != null)
        {
            engineToggleButton.onClick.AddListener(ToggleEngine);
            
            // Trouver l'Image automatiquement si non assignée
            if (engineButtonImage == null)
            {
                engineButtonImage = engineToggleButton.GetComponent<Image>();
            }
            
            UpdateButtonText();
            Debug.Log("EngineController: Bouton UI configuré avec succès");
        }
        else
        {
            Debug.LogWarning("EngineController: Aucun bouton UI assigné");
        }
        
        currentPropellerSpeed = engineOn ? propellerSpeed : 0f;
    }
    
    void Update()
    {
        // Détecter l'appui sur la touche de toggle (si définie)
        if (toggleEngineKey != KeyCode.None && Input.GetKeyDown(toggleEngineKey))
        {
            ToggleEngine();
        }
        
        // Mettre à jour l'audio du moteur
        UpdateEngineAudio();
    }
    
    void LateUpdate()
    {
        // Animer l'hélice
        if (propeller != null)
        {
            AnimatePropeller();
        }
        
        // Couper le throttle si moteur éteint
        if (!engineOn && plane != null)
        {
            plane.throttle = 0f;
        }
    }
    
    /// <summary>
    /// Allume ou éteint le moteur
    /// </summary>
    public void ToggleEngine()
    {
        engineOn = !engineOn;
        
        if (engineOn)
        {
            Debug.Log("EngineController: Moteur ALLUMÉ");
            
            // Redémarrer l'audio
            if (engineAudioSource != null && !engineAudioSource.isPlaying)
            {
                engineAudioSource.Play();
            }
        }
        else
        {
            Debug.Log("EngineController: Moteur ÉTEINT");
            
            // Sauvegarder le throttle actuel (optionnel)
            if (plane != null)
            {
                savedThrottle = plane.throttle;
                plane.throttle = 0f;
            }
            
            // Couper progressivement l'audio
            // (on le laisse tourner pour effet de ralentissement)
        }
        
        // Mettre à jour le texte du bouton
        UpdateButtonText();
    }
    
    /// <summary>
    /// Met à jour le texte du bouton selon l'état du moteur
    /// </summary>
    void UpdateButtonText()
    {
        if (engineButtonText != null)
        {
            engineButtonText.text = engineOn ? "Moteur: ON" : "Moteur: OFF";
            engineButtonText.color = engineOn ? textColorOn : textColorOff;
        }
        
        if (engineButtonImage != null)
        {
            engineButtonImage.color = engineOn ? buttonColorOn : buttonColorOff;
        }
    }
    
    /// <summary>
    /// Anime la rotation de l'hélice
    /// </summary>
    void AnimatePropeller()
    {
        if (engineOn && plane != null)
        {
            // Vitesse proportionnelle au throttle
            float targetSpeed = propellerSpeed * Mathf.Lerp(0.2f, 1f, plane.throttle);
            currentPropellerSpeed = Mathf.Lerp(currentPropellerSpeed, targetSpeed, Time.deltaTime * 2f);
        }
        else
        {
            // Ralentir progressivement
            currentPropellerSpeed = Mathf.Max(0f, currentPropellerSpeed - propellerDeceleration * Time.deltaTime);
        }
        
        // Appliquer la rotation
        propeller.Rotate(Vector3.forward * currentPropellerSpeed * Time.deltaTime);
    }
    
    /// <summary>
    /// Met à jour le pitch de l'audio selon le throttle
    /// </summary>
    void UpdateEngineAudio()
    {
        if (engineAudioSource == null || plane == null) return;
        
        if (engineOn)
        {
            // Ajuster le pitch selon le throttle
            float targetPitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, plane.throttle);
            engineAudioSource.pitch = Mathf.Lerp(engineAudioSource.pitch, targetPitch, Time.deltaTime * 3f);
            
            // Ajuster le volume légèrement
            engineAudioSource.volume = Mathf.Lerp(0.5f, 1f, plane.throttle);
        }
        else
        {
            // Ralentir progressivement le son
            engineAudioSource.pitch = Mathf.Max(0.1f, engineAudioSource.pitch - Time.deltaTime * 0.5f);
            engineAudioSource.volume = Mathf.Max(0f, engineAudioSource.volume - Time.deltaTime * 0.5f);
            
            // Arrêter complètement quand très bas
            if (engineAudioSource.pitch < 0.2f && engineAudioSource.isPlaying)
            {
                engineAudioSource.Stop();
            }
        }
    }
    
    /// <summary>
    /// Affiche l'état du moteur dans le GUI (optionnel - peut être désactivé si vous utilisez uniquement le bouton UI)
    /// </summary>
    void OnGUI()
    {
        // Désactiver si vous utilisez uniquement le bouton UI
        if (engineToggleButton != null) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = engineOn ? Color.green : Color.red;
        
        string status = engineOn ? "ENGINE ON" : "ENGINE OFF";
        GUI.Label(new Rect(10, Screen.height - 40, 200, 30), status, style);
        
        // Instructions
        GUIStyle smallStyle = new GUIStyle(GUI.skin.label);
        smallStyle.fontSize = 14;
        smallStyle.normal.textColor = Color.white;
        
        if (toggleEngineKey != KeyCode.None)
        {
            GUI.Label(new Rect(10, Screen.height - 20, 300, 20), 
                $"Press {toggleEngineKey} to toggle engine", smallStyle);
        }
    }
}
