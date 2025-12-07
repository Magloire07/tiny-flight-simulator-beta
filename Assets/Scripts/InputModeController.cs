using UnityEngine;
using UnityEngine.UI;
using MFlight.Demo;
using DemoPlane = MFlight.Demo.Plane;

/// <summary>
/// Gère le basculement entre contrôle clavier et joystick
/// </summary>
public class InputModeController : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Référence au script Plane")]
    public DemoPlane plane;
    
    [Tooltip("Texte UI pour afficher les messages (optionnel)")]
    public Text messageText;
    
    [Header("Mode de Contrôle")]
    [Tooltip("Mode actuel: true = Clavier, false = Joystick")]
    public bool useKeyboard = true;
    
    [Header("Sensibilités Clavier")]
    [Tooltip("Sensibilité souris Yaw (clavier)")]
    public float keyboardMouseYawSensitivity = 0.75f;
    
    [Tooltip("Sensibilité souris Pitch (clavier)")]
    public float keyboardMousePitchSensitivity = 0.75f;
    
    [Tooltip("Sensibilité roll clavier")]
    public float keyboardRollSensitivity = 1.0f;
    
    [Header("Sensibilités Joystick")]
    [Tooltip("Sensibilité joystick Yaw")]
    public float joystickYawSensitivity = 1.5f;
    
    [Tooltip("Sensibilité joystick Pitch")]
    public float joystickPitchSensitivity = 1.5f;
    
    [Tooltip("Sensibilité joystick Roll")]
    public float joystickRollSensitivity = 2.0f;
    
    [Header("Détection Joystick")]
    [Tooltip("Durée d'affichage du message d'alerte (secondes)")]
    public float messageDisplayTime = 3f;
    
    [Tooltip("Vérifier la connexion du joystick")]
    public bool checkJoystickConnection = true;
    
    // Variables internes
    private float messageTimer = 0f;
    private bool isShowingMessage = false;
    
    void Start()
    {
        if (plane == null)
        {
            plane = GetComponent<DemoPlane>();
            if (plane == null)
            {
                Debug.LogError("InputModeController: Aucune référence Plane trouvée!");
                enabled = false;
                return;
            }
        }
        
        // Appliquer le mode par défaut
        ApplyInputMode(useKeyboard);
        
        // Cacher le message au démarrage
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
    }
    
    void Update()
    {
        // Gérer le timer du message
        if (isShowingMessage)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f)
            {
                HideMessage();
            }
        }
    }
    
    /// <summary>
    /// Bascule entre clavier et joystick
    /// </summary>
    public void ToggleInputMode()
    {
        useKeyboard = !useKeyboard;
        
        // Vérifier si un joystick est connecté lors du passage en mode joystick
        if (!useKeyboard && checkJoystickConnection)
        {
            if (!IsJoystickConnected())
            {
                ShowMessage("⚠ AUCUN JOYSTICK DÉTECTÉ ⚠\nVérifiez la connexion de votre manette");
                // Revenir en mode clavier
                useKeyboard = true;
                ApplyInputMode(true);
                return;
            }
        }
        
        ApplyInputMode(useKeyboard);
    }
    
    /// <summary>
    /// Définit le mode de contrôle
    /// </summary>
    public void SetInputMode(bool keyboard)
    {
        useKeyboard = keyboard;
        ApplyInputMode(useKeyboard);
    }
    
    /// <summary>
    /// Applique les paramètres selon le mode
    /// </summary>
    void ApplyInputMode(bool keyboard)
    {
        if (plane == null) return;
        
        if (keyboard)
        {
            // Mode Clavier: contrôle direct avec souris + clavier
            plane.useDirectInputMapping = true;
            plane.mouseYawSensitivity = keyboardMouseYawSensitivity;
            plane.mousePitchSensitivity = keyboardMousePitchSensitivity;
            plane.keyboardRollSensitivity = keyboardRollSensitivity;
            plane.keyboardPitchSensitivity = 1.0f;
            plane.keyboardYawSensitivity = 1.0f;
            
            Debug.Log("InputModeController: Mode CLAVIER activé");
        }
        else
        {
            // Mode Joystick: sensibilités augmentées pour joystick
            plane.useDirectInputMapping = true; // Garder le mapping direct
            plane.mouseYawSensitivity = joystickYawSensitivity;
            plane.mousePitchSensitivity = joystickPitchSensitivity;
            plane.keyboardRollSensitivity = joystickRollSensitivity;
            plane.keyboardPitchSensitivity = joystickPitchSensitivity;
            plane.keyboardYawSensitivity = joystickYawSensitivity;
            
            Debug.Log("InputModeController: Mode JOYSTICK activé");
        }
    }
    
    /// <summary>
    /// Retourne le mode actuel sous forme de texte
    /// </summary>
    public string GetCurrentModeName()
    {
        return useKeyboard ? "Clavier" : "Joystick";
    }
    
    /// <summary>
    /// Vérifie si un joystick est connecté
    /// </summary>
    bool IsJoystickConnected()
    {
        string[] joystickNames = Input.GetJoystickNames();
        
        // Filtrer les entrées vides
        foreach (string name in joystickNames)
        {
            if (!string.IsNullOrEmpty(name))
            {
                Debug.Log("InputModeController: Joystick détecté: " + name);
                return true;
            }
        }
        
        Debug.LogWarning("InputModeController: Aucun joystick détecté");
        return false;
    }
    
    /// <summary>
    /// Affiche un message à l'utilisateur
    /// </summary>
    void ShowMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(true);
            messageTimer = messageDisplayTime;
            isShowingMessage = true;
        }
        
        Debug.LogWarning("InputModeController: " + message);
    }
    
    /// <summary>
    /// Cache le message
    /// </summary>
    void HideMessage()
    {
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
        isShowingMessage = false;
    }
}
