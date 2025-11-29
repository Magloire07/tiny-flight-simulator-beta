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
    [Tooltip("Transform du siège pilote (si null, cherche automatiquement 'pilot' ou 'seat' dans l'avion)")]
    public Transform pilotSeatTransform;
    
    [Tooltip("Position de la caméra en vue cockpit (relative à l'avion si pas de siège trouvé)")]
    public Vector3 cockpitViewOffset = new Vector3(0f, 0.77f, 0.80f);
    
    [Tooltip("Rotation de la caméra en vue cockpit (Euler angles)")]
    public Vector3 cockpitViewRotation = new Vector3(90f, 0f, 0f);
    
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
    
    // Variables internes
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool useMouseFlightInExternalView = true;
    
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
        
        // Chercher le siège pilote dans l'avion si non assigné
        if (pilotSeatTransform == null)
        {
            // Chercher un Transform nommé "pilot", "seat", "cockpit", etc.
            Transform[] children = aircraft.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                string nameLower = child.name.ToLower();
                if (nameLower.Contains("pilot") || nameLower.Contains("seat") || 
                    nameLower.Contains("cockpit") || nameLower.Contains("camera"))
                {
                    pilotSeatTransform = child;
                    Debug.Log("CameraViewSwitcher: Siège pilote trouvé automatiquement: " + child.name);
                    break;
                }
            }
            
            if (pilotSeatTransform == null)
            {
                Debug.LogWarning("CameraViewSwitcher: Aucun siège pilote trouvé. Utilisation de cockpitViewOffset.");
            }
        }
        else
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
        
        // Initialiser avec la vue actuelle
        if (isCockpitView)
        {
            SetCockpitView(true);
        }
        else
        {
            SetExternalView(true);
        }
        
        Debug.Log("CameraViewSwitcher: Initialisation terminée. Appuyez sur " + switchViewKey + " pour changer de vue.");
    }
    
    void Update()
    {
        // Détecter l'appui sur la touche V
        if (Input.GetKeyDown(switchViewKey))
        {
            Debug.Log("CameraViewSwitcher: Touche " + switchViewKey + " appuyée!");
            ToggleView();
        }
        
        // Mise à jour de la position/rotation de la caméra
        UpdateCameraTransform();
    }
    
    /// <summary>
    /// Bascule entre vue cockpit et vue extérieure
    /// </summary>
    void ToggleView()
    {
        isCockpitView = !isCockpitView;
        
        if (isCockpitView)
        {
            SetCockpitView(false);
            Debug.Log("Vue Cockpit activée");
        }
        else
        {
            SetExternalView(false);
            Debug.Log("Vue Extérieure activée");
        }
    }
    
    /// <summary>
    /// Configure la vue cockpit
    /// </summary>
    void SetCockpitView(bool instant)
    {
        // Si un siège pilote existe, utiliser sa position + offset
        if (pilotSeatTransform != null)
        {
            targetPosition = pilotSeatTransform.position + pilotSeatTransform.TransformDirection(cockpitViewOffset);
            targetRotation = pilotSeatTransform.rotation * Quaternion.Euler(cockpitViewRotation);
            Debug.Log("CameraViewSwitcher: Position cockpit depuis siège pilote + offset: " + targetPosition);
        }
        else
        {
            // Sinon, utiliser l'offset relatif à l'avion
            targetPosition = aircraft.TransformPoint(cockpitViewOffset);
            targetRotation = aircraft.rotation * Quaternion.Euler(cockpitViewRotation);
            Debug.Log("CameraViewSwitcher: Position cockpit depuis offset: " + targetPosition);
        }
        
        // Désactiver MouseFlightController en vue cockpit
        if (mouseFlightController != null)
        {
            mouseFlightController.enabled = false;
            Debug.Log("CameraViewSwitcher: MouseFlightController désactivé");
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
            // Vue cockpit: la caméra suit rigidement l'avion ou le siège
            if (pilotSeatTransform != null)
            {
                targetPosition = pilotSeatTransform.position + pilotSeatTransform.TransformDirection(cockpitViewOffset);
                targetRotation = pilotSeatTransform.rotation * Quaternion.Euler(cockpitViewRotation);
            }
            else
            {
                targetPosition = aircraft.TransformPoint(cockpitViewOffset);
                targetRotation = aircraft.rotation * Quaternion.Euler(cockpitViewRotation);
            }
            
            if (transitionSpeed > 0f)
            {
                viewCamera.transform.position = Vector3.Lerp(viewCamera.transform.position, targetPosition, Time.deltaTime * transitionSpeed);
                viewCamera.transform.rotation = Quaternion.Slerp(viewCamera.transform.rotation, targetRotation, Time.deltaTime * transitionSpeed);
            }
            else
            {
                viewCamera.transform.position = targetPosition;
                viewCamera.transform.rotation = targetRotation;
            }
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
