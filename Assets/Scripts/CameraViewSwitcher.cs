using UnityEngine;
using MFlight;

/// <summary>
/// Permet de basculer entre vue cockpit et vue extérieure avec la touche V.
/// </summary>
public class CameraViewSwitcher : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Transform de l'avion")]
    public Transform aircraft;
    
    [Tooltip("Caméra à déplacer")]
    public Camera viewCamera;
    
    [Tooltip("Référence au MouseFlightController pour désactiver en vue cockpit")]
    public MouseFlightController mouseFlightController;
    
    [Header("Vue Extérieure (actuelle)")]
    [Tooltip("Position de la caméra en vue extérieure (relative à l'avion)")]
    public Vector3 externalViewOffset = new Vector3(0f, 2f, -8f);
    
    [Tooltip("Distance de la caméra en vue extérieure")]
    public float externalViewDistance = 8f;
    
    [Header("Vue Cockpit")]
    [Tooltip("Utiliser directement l'offset par rapport à l'avion (ignore la recherche de siège)")]
    public bool useOffsetOnly = true;
    
    [Tooltip("Transform du siège pilote (utilisé seulement si useOffsetOnly = false)")]
    public Transform pilotSeatTransform;
    
    [Tooltip("Position de la caméra en vue cockpit (relative à l'avion)")]
    public Vector3 cockpitViewOffset = new Vector3(0.03f, 0.41f, 0.8f);
    
    [Tooltip("Rotation de la caméra en vue cockpit (Euler angles)")]
    public Vector3 cockpitViewRotation = new Vector3(3f, -1.13f, 0f);
    
    [Tooltip("Field of View (FOV) en vue cockpit")]
    [Range(30f, 120f)]
    public float cockpitFOV = 30f;
    
    [Header("Contrôle Vue Cockpit")]
    [Tooltip("Permettre de regarder autour avec la souris en vue cockpit")]
    public bool enableCockpitFreeLook = true;
    
    [Tooltip("Sensibilité de rotation de la caméra cockpit")]
    public float cockpitLookSensitivity = 2f;
    
    [Tooltip("Angle maximum de rotation horizontale (gauche/droite)")]
    [Range(0f, 180f)]
    public float maxYawAngle = 90f;
    
    [Tooltip("Angle maximum de rotation verticale (haut/bas)")]
    [Range(0f, 90f)]
    public float maxPitchAngle = 60f;
    
    [Tooltip("Touche pour réinitialiser la vue (regarder devant)")]
    public KeyCode recenterViewKey = KeyCode.C;
    
    [Header("Transition")]
    [Tooltip("Vitesse de transition entre les vues (0 = instantané)")]
    [Range(0f, 20f)]
    public float transitionSpeed = 10f;
    
    [Header("Contrôles")]
    [Tooltip("Touche pour changer de vue")]
    public KeyCode switchViewKey = KeyCode.V;
    
    [Header("État")]
    [Tooltip("Vue actuelle (false = extérieure, true = cockpit)")]
    public bool isCockpitView = false;
    
    [Tooltip("Vue par défaut au démarrage (false = extérieure, true = cockpit)")]
    public bool startInCockpitView = false;
    
    [Header("Audio")]
    [Tooltip("Son du beep lors du changement de vue")]
    public AudioClip switchViewBeep;
    
    [Tooltip("Volume du beep (0-1)")]
    [Range(0f, 1f)]
    public float beepVolume = 0.5f;
    
    [Tooltip("Pitch du beep pour vue cockpit")]
    [Range(0.5f, 2f)]
    public float cockpitBeepPitch = 1.2f;
    
    [Tooltip("Pitch du beep pour vue extérieure")]
    [Range(0.5f, 2f)]
    public float externalBeepPitch = 0.8f;
    
    // Variables internes
    private AudioSource audioSource;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float externalViewFOV; // Sauvegarde du FOV de la vue externe
    private float cockpitYaw = 0f; // Rotation horizontale de la vue cockpit
    private float cockpitPitch = 0f; // Rotation verticale de la vue cockpit
    
    void Start()
    {
        Debug.Log("CameraViewSwitcher: Initialisation...");
        
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
            Debug.Log("CameraViewSwitcher: Caméra trouvée automatiquement: " + (viewCamera != null ? viewCamera.name : "null"));
        }
        
        if (aircraft == null)
        {
            Debug.LogError("CameraViewSwitcher: Aucune référence aircraft assignée!");
            enabled = false;
            return;
        }
        
        // Chercher le siège pilote dans l'avion si non assigné ET si useOffsetOnly est false
        if (!useOffsetOnly && pilotSeatTransform == null)
        {
            // Chercher un Transform nommé "pilot", "seat", "cockpit" (mais pas "camera" pour éviter confusion)
            Transform[] children = aircraft.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                string nameLower = child.name.ToLower();
                if (nameLower.Contains("pilot") || nameLower.Contains("seat") || nameLower.Contains("cockpit"))
                {
                    pilotSeatTransform = child;
                    Debug.Log("CameraViewSwitcher: Siège pilote trouvé automatiquement: " + child.name);
                    break;
                }
            }
            
            if (pilotSeatTransform == null)
            {
                Debug.LogWarning("CameraViewSwitcher: Aucun siège pilote trouvé. Utilisation de cockpitViewOffset par rapport à l'avion.");
            }
        }
        else if (useOffsetOnly)
        {
            Debug.Log("CameraViewSwitcher: Mode Offset Only activé - utilisation de cockpitViewOffset par rapport à l'avion.");
        }
        else if (pilotSeatTransform != null)
        {
            Debug.Log("CameraViewSwitcher: Siège pilote assigné: " + pilotSeatTransform.name);
        }
        
        if (mouseFlightController == null)
        {
            mouseFlightController = FindObjectOfType<MouseFlightController>();
            if (mouseFlightController != null)
            {
                Debug.Log("CameraViewSwitcher: MouseFlightController trouvé: " + mouseFlightController.name);
            }
        }
        
        // Créer l'AudioSource pour le beep
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // Son 2D
            audioSource.volume = beepVolume;
        }
        
        // Sauvegarder le FOV initial de la vue externe
        if (viewCamera != null)
        {
            externalViewFOV = viewCamera.fieldOfView;
            Debug.Log("CameraViewSwitcher: FOV vue externe sauvegardé: " + externalViewFOV);
        }
        
        // Appliquer la vue par défaut au démarrage
        isCockpitView = startInCockpitView;
        
        // Initialiser avec la vue actuelle
        if (isCockpitView)
        {
            SetCockpitView(true);
        }
        else
        {
            SetExternalView(true);
        }
        
        Debug.Log("CameraViewSwitcher: Initialisation terminée. Vue par défaut: " + (isCockpitView ? "Cockpit" : "Extérieure") + ". Appuyez sur " + switchViewKey + " pour changer de vue.");
    }
    
    void Update()
    {
        // Détecter l'appui sur la touche V
        if (Input.GetKeyDown(switchViewKey))
        {
            Debug.Log("CameraViewSwitcher: Touche " + switchViewKey + " appuyée!");
            ToggleView();
        }
        
        // Gestion du free look en vue cockpit
        if (isCockpitView && enableCockpitFreeLook)
        {
            HandleCockpitFreeLook();
        }
    }
    
    void LateUpdate()
    {
        // Mise à jour de la position/rotation de la caméra APRÈS le physics update
        UpdateCameraTransform();
    }
    
    /// <summary>
    /// Gère le free look avec la souris en vue cockpit
    /// </summary>
    void HandleCockpitFreeLook()
    {
        // Récupérer le mouvement de la souris
        float mouseX = Input.GetAxis("Mouse X") * cockpitLookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * cockpitLookSensitivity;
        
        // Appliquer la rotation
        cockpitYaw += mouseX;
        cockpitPitch -= mouseY; // Inverser pour que haut = regarder haut
        
        // Limiter les angles
        cockpitYaw = Mathf.Clamp(cockpitYaw, -maxYawAngle, maxYawAngle);
        cockpitPitch = Mathf.Clamp(cockpitPitch, -maxPitchAngle, maxPitchAngle);
        
        // Réinitialiser la vue avec la touche C
        if (Input.GetKeyDown(recenterViewKey))
        {
            cockpitYaw = 0f;
            cockpitPitch = 0f;
        }
    }
    
    /// <summary>
    /// Bascule entre vue cockpit et vue extérieure
    /// </summary>
    public void ToggleView()
    {
        Debug.Log($">>> ToggleView() appelé - isCockpitView AVANT: {isCockpitView}");
        isCockpitView = !isCockpitView;
        Debug.Log($">>> ToggleView() - isCockpitView APRÈS toggle: {isCockpitView}");
        
        if (isCockpitView)
        {
            Debug.Log(">>> Appel SetCockpitView(true)");
            SetCockpitView(true); // Changement instantané
            PlaySwitchBeep(true);
            Debug.Log(">>> Vue Cockpit activée (instant)");
        }
        else
        {
            Debug.Log(">>> Appel SetExternalView(true)");
            SetExternalView(true); // Changement instantané
            PlaySwitchBeep(false);
            Debug.Log(">>> Vue Extérieure activée (instant)");
        }
        
        // Forcer une mise à jour immédiate
        if (viewCamera != null)
        {
            Debug.Log($">>> Caméra position: {viewCamera.transform.position}, rotation: {viewCamera.transform.rotation.eulerAngles}");
        }
    }
    
    /// <summary>
    /// Joue le son de changement de vue
    /// </summary>
    void PlaySwitchBeep(bool toCockpit)
    {
        Debug.Log($"*** PlaySwitchBeep appelé! toCockpit={toCockpit}, Time={Time.time}, StackTrace={System.Environment.StackTrace}");
        
        if (audioSource == null) return;
        
        if (switchViewBeep != null)
        {
            // Pitch différent selon la vue
            audioSource.pitch = toCockpit ? cockpitBeepPitch : externalBeepPitch;
            audioSource.PlayOneShot(switchViewBeep, beepVolume);
        }
        else
        {
            // Beep synthétique si aucun AudioClip n'est assigné
            audioSource.pitch = toCockpit ? cockpitBeepPitch : externalBeepPitch;
            audioSource.PlayOneShot(GenerateBeep(), beepVolume);
        }
    }
    
    /// <summary>
    /// Génère un beep synthétique simple
    /// </summary>
    AudioClip GenerateBeep()
    {
        int sampleRate = 44100;
        int samples = sampleRate / 10; // 0.1 seconde
        AudioClip beep = AudioClip.Create("Beep", samples, 1, sampleRate, false);
        
        float[] data = new float[samples];
        float frequency = 800f; // Hz
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            // Onde sinusoïdale avec enveloppe
            float envelope = 1f - (float)i / samples; // Décroissance
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.5f;
        }
        
        beep.SetData(data, 0);
        return beep;
    }
    
    /// <summary>
    /// Configure la vue cockpit
    /// </summary>
    void SetCockpitView(bool instant)
    {
        // Réinitialiser le free look
        cockpitYaw = 0f;
        cockpitPitch = 0f;
        
        // Utiliser l'offset relatif à l'avion (suit toujours le Rigidbody)
        if (useOffsetOnly || pilotSeatTransform == null)
        {
            targetPosition = aircraft.TransformPoint(cockpitViewOffset);
            targetRotation = aircraft.rotation * Quaternion.Euler(cockpitViewRotation);
            Debug.Log("CameraViewSwitcher: Position cockpit depuis offset (suit l'avion): " + cockpitViewOffset);
        }
        else
        {
            // Utiliser le siège pilote + offset
            targetPosition = pilotSeatTransform.position + pilotSeatTransform.TransformDirection(cockpitViewOffset);
            targetRotation = pilotSeatTransform.rotation * Quaternion.Euler(cockpitViewRotation);
            Debug.Log("CameraViewSwitcher: Position cockpit depuis siège pilote + offset: " + targetPosition);
        }
        
        // Désactiver MouseFlightController en vue cockpit
        if (mouseFlightController != null)
        {
            mouseFlightController.enabled = false;
            Debug.Log("CameraViewSwitcher: MouseFlightController désactivé");
        }
        
        // Appliquer le FOV cockpit
        if (viewCamera != null)
        {
            viewCamera.fieldOfView = cockpitFOV;
        }
        
        if (instant && viewCamera != null)
        {
            viewCamera.transform.position = targetPosition;
            viewCamera.transform.rotation = targetRotation;
        }
    }
    
    /// <summary>
    /// Configure la vue extérieure
    /// </summary>
    void SetExternalView(bool instant)
    {
        // Réactiver MouseFlightController en vue extérieure
        if (mouseFlightController != null)
        {
            mouseFlightController.enabled = true;
        }
        
        // Restaurer le FOV de la vue externe
        if (viewCamera != null)
        {
            viewCamera.fieldOfView = externalViewFOV;
        }
        
        // Si MouseFlightController gère la caméra, ne rien faire
        // Sinon, positionner manuellement
        if (mouseFlightController == null)
        {
            targetPosition = aircraft.position + aircraft.TransformDirection(externalViewOffset);
            targetRotation = Quaternion.LookRotation(aircraft.position - targetPosition);
            
            if (instant && viewCamera != null)
            {
                viewCamera.transform.position = targetPosition;
                viewCamera.transform.rotation = targetRotation;
            }
        }
    }
    
    /// <summary>
    /// Met à jour la position et rotation de la caméra avec interpolation
    /// </summary>
    void UpdateCameraTransform()
    {
        if (viewCamera == null || aircraft == null) return;
        
        if (isCockpitView)
        {
            // Vue cockpit: la caméra suit rigidement l'avion (pas d'interpolation pour éviter décalage)
            if (useOffsetOnly || pilotSeatTransform == null)
            {
                targetPosition = aircraft.TransformPoint(cockpitViewOffset);
                // Appliquer la rotation de base + le free look
                Quaternion baseRotation = aircraft.rotation * Quaternion.Euler(cockpitViewRotation);
                Quaternion freeLookRotation = Quaternion.Euler(cockpitPitch, cockpitYaw, 0f);
                targetRotation = baseRotation * freeLookRotation;
            }
            else
            {
                // Utiliser le siège pilote + offset
                targetPosition = pilotSeatTransform.position + pilotSeatTransform.TransformDirection(cockpitViewOffset);
                // Appliquer la rotation de base + le free look
                Quaternion baseRotation = pilotSeatTransform.rotation * Quaternion.Euler(cockpitViewRotation);
                Quaternion freeLookRotation = Quaternion.Euler(cockpitPitch, cockpitYaw, 0f);
                targetRotation = baseRotation * freeLookRotation;
            }
            
            // Application directe sans Lerp pour éviter l'effet d'inertie
            viewCamera.transform.position = targetPosition;
            viewCamera.transform.rotation = targetRotation;
            
            // Maintenir le FOV cockpit
            viewCamera.fieldOfView = cockpitFOV;
        }
        else
        {
            // Vue extérieure: MouseFlightController gère la caméra
            // Ne rien faire ici
        }
    }
    
    /// <summary>
    /// Debug: dessiner les positions de caméra dans l'éditeur
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (aircraft == null) return;
        
        // Vue cockpit (vert)
        Gizmos.color = Color.green;
        Vector3 cockpitPos = aircraft.TransformPoint(cockpitViewOffset);
        Gizmos.DrawWireSphere(cockpitPos, 0.2f);
        Gizmos.DrawLine(aircraft.position, cockpitPos);
        
        // Vue extérieure (bleu)
        Gizmos.color = Color.blue;
        Vector3 externalPos = aircraft.position + aircraft.TransformDirection(externalViewOffset);
        Gizmos.DrawWireSphere(externalPos, 0.3f);
        Gizmos.DrawLine(aircraft.position, externalPos);
    }
}
