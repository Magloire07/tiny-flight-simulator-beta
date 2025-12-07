using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Contrôle les freins de l'avion avec un bouton UI
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BrakeController : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Rigidbody de l'avion")]
    public Rigidbody rb;
    
    [Header("Contrôles")]
    [Tooltip("Bouton UI pour activer/désactiver les freins")]
    public Button brakeToggleButton;
    
    [Tooltip("Texte du bouton")]
    public Text brakeButtonText;
    
    [Tooltip("Image du bouton (pour changer la couleur)")]
    public Image brakeButtonImage;
    
    [Tooltip("Touche clavier alternative pour maintenir les freins")]
    public KeyCode brakeKey = KeyCode.Space;
    
    [Header("Paramètres Freinage")]
    [Tooltip("Force de freinage appliquée (Newtons)")]
    [Range(0f, 50000f)]
    public float brakeForce = 15000f;
    
    [Tooltip("Freinage uniquement au sol")]
    public bool onlyBrakeOnGround = true;
    
    [Tooltip("Distance maximale du sol pour considérer l'avion au sol")]
    public float groundCheckDistance = 2f;
    
    [Tooltip("Layer du sol")]
    public LayerMask groundLayer = -1; // Tous les layers
    
    [Header("Audio")]
    [Tooltip("Source audio pour le son de freinage")]
    public AudioSource brakeAudioSource;
    
    [Tooltip("Clip audio joué quand les freins sont activés")]
    public AudioClip brakeActivateSound;
    
    [Tooltip("Clip audio joué quand les freins sont désactivés")]
    public AudioClip brakeReleaseSound;
    
    [Tooltip("Clip audio en boucle pendant le freinage")]
    public AudioClip brakeLoopSound;
    
    [Tooltip("Volume du son de freinage")]
    [Range(0f, 1f)]
    public float brakeVolume = 0.7f;
    
    [Header("Couleurs Bouton")]
    [Tooltip("Couleur du bouton quand freins activés")]
    public Color buttonColorOn = new Color(1f, 0.5f, 0f); // Orange
    
    [Tooltip("Couleur du bouton quand freins désactivés")]
    public Color buttonColorOff = new Color(0.3f, 0.3f, 0.3f); // Gris
    
    [Tooltip("Couleur du texte quand freins activés")]
    public Color textColorOn = new Color(0.8f, 0.3f, 0f); // Orange foncé
    
    [Tooltip("Couleur du texte quand freins désactivés")]
    public Color textColorOff = Color.white;
    
    [Header("État")]
    [Tooltip("Les freins sont-ils activés?")]
    public bool brakesOn = false;
    
    // Variables internes
    private bool isGrounded = false;
    
    void Start()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
        
        // Trouver l'AudioSource automatiquement si non assigné
        if (brakeAudioSource == null)
        {
            brakeAudioSource = GetComponent<AudioSource>();
            
            // Créer un AudioSource si aucun n'existe
            if (brakeAudioSource == null)
            {
                brakeAudioSource = gameObject.AddComponent<AudioSource>();
                brakeAudioSource.playOnAwake = false;
                brakeAudioSource.loop = false;
                brakeAudioSource.spatialBlend = 1f; // Son 3D
                brakeAudioSource.volume = brakeVolume;
            }
        }
        
        // Configurer le bouton UI
        if (brakeToggleButton != null)
        {
            brakeToggleButton.onClick.AddListener(ToggleBrakes);
            
            // Trouver l'Image automatiquement si non assignée
            if (brakeButtonImage == null)
            {
                brakeButtonImage = brakeToggleButton.GetComponent<Image>();
            }
            
            UpdateButtonAppearance();
        }
    }
    
    void Update()
    {
        // Détecter l'appui sur la touche de freinage (maintenir pour freiner)
        if (brakeKey != KeyCode.None && Input.GetKey(brakeKey))
        {
            if (!brakesOn)
            {
                brakesOn = true;
                UpdateButtonAppearance();
            }
        }
        else if (brakeKey != KeyCode.None && Input.GetKeyUp(brakeKey))
        {
            if (brakesOn)
            {
                brakesOn = false;
                UpdateButtonAppearance();
            }
        }
    }
    
    void FixedUpdate()
    {
        // Vérifier si l'avion est au sol
        CheckGrounded();
        
        // Appliquer le freinage si activé
        if (brakesOn && rb != null)
        {
            ApplyBrakes();
        }
    }
    
    /// <summary>
    /// Vérifie si l'avion est au sol
    /// </summary>
    void CheckGrounded()
    {
        if (onlyBrakeOnGround)
        {
            // Raycast vers le bas pour détecter le sol
            isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
        }
        else
        {
            isGrounded = true; // Toujours considéré au sol si l'option est désactivée
        }
    }
    
    /// <summary>
    /// Applique la force de freinage
    /// </summary>
    void ApplyBrakes()
    {
        if (!isGrounded) return;
        
        // Calculer la force de freinage opposée à la vélocité horizontale
        Vector3 horizontalVelocity = rb.velocity;
        horizontalVelocity.y = 0f; // Ignorer la vélocité verticale
        
        if (horizontalVelocity.magnitude > 0.1f)
        {
            // Appliquer une force opposée à la direction du mouvement
            Vector3 brakeForceVector = -horizontalVelocity.normalized * brakeForce;
            rb.AddForce(brakeForceVector, ForceMode.Force);
            
            // Augmenter le drag pour un freinage plus efficace
            rb.drag = 5f;
            
            // Jouer le son de freinage en boucle si disponible
            if (brakeAudioSource != null && brakeLoopSound != null && !brakeAudioSource.isPlaying)
            {
                brakeAudioSource.clip = brakeLoopSound;
                brakeAudioSource.loop = true;
                brakeAudioSource.Play();
            }
        }
        else
        {
            // Arrêter complètement si vitesse très faible
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
            rb.angularVelocity *= 0.9f;
            
            // Arrêter le son de boucle
            if (brakeAudioSource != null && brakeAudioSource.isPlaying && brakeAudioSource.clip == brakeLoopSound)
            {
                brakeAudioSource.Stop();
            }
        }
    }
    
    /// <summary>
    /// Active ou désactive les freins (toggle)
    /// </summary>
    public void ToggleBrakes()
    {
        brakesOn = !brakesOn;
        UpdateButtonAppearance();
        
        // Jouer le son approprié
        PlayBrakeSound(brakesOn);
        
        Debug.Log("BrakeController: Freins " + (brakesOn ? "ACTIVÉS" : "DÉSACTIVÉS"));
    }
    
    /// <summary>
    /// Active les freins
    /// </summary>
    public void SetBrakes(bool enabled)
    {
        bool wasOn = brakesOn;
        brakesOn = enabled;
        UpdateButtonAppearance();
        
        // Jouer le son seulement si l'état a changé
        if (wasOn != brakesOn)
        {
            PlayBrakeSound(brakesOn);
        }
    }
    
    /// <summary>
    /// Joue le son de freinage approprié
    /// </summary>
    void PlayBrakeSound(bool activated)
    {
        if (brakeAudioSource == null) return;
        
        if (activated && brakeActivateSound != null)
        {
            // Son d'activation (bref)
            brakeAudioSource.loop = false;
            brakeAudioSource.PlayOneShot(brakeActivateSound, brakeVolume);
        }
        else if (!activated)
        {
            // Arrêter le son de boucle
            if (brakeAudioSource.isPlaying && brakeAudioSource.clip == brakeLoopSound)
            {
                brakeAudioSource.Stop();
            }
            
            // Son de désactivation (bref)
            if (brakeReleaseSound != null)
            {
                brakeAudioSource.PlayOneShot(brakeReleaseSound, brakeVolume);
            }
        }
    }
    
    /// <summary>
    /// Met à jour l'apparence du bouton
    /// </summary>
    void UpdateButtonAppearance()
    {
        if (brakeButtonText != null)
        {
            brakeButtonText.text = brakesOn ? "Freins: ON" : "Freins: OFF";
            brakeButtonText.color = brakesOn ? textColorOn : textColorOff;
        }
        
        if (brakeButtonImage != null)
        {
            brakeButtonImage.color = brakesOn ? buttonColorOn : buttonColorOff;
        }
        
        // Réinitialiser le drag si les freins sont désactivés
        if (!brakesOn && rb != null)
        {
            rb.drag = 0f;
        }
    }
    
    void OnDrawGizmos()
    {
        // Visualiser le raycast de détection du sol
        if (onlyBrakeOnGround)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
        }
    }
}
