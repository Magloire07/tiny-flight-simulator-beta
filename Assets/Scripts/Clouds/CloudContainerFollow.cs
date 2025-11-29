using UnityEngine;

/// <summary>
/// Fait suivre le container de nuages volumétriques à l'avion pour qu'il y ait toujours des nuages visibles.
/// Le container se repositionne autour de l'avion tout en préservant la génération procédurale des nuages.
/// </summary>
public class CloudContainerFollow : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Transform de l'avion à suivre (auto-détecté si null)")]
    public Transform target;
    
    [Header("Paramètres de Suivi")]
    [Tooltip("Distance de déplacement avant de recentrer le container")]
    public float moveThreshold = 1000f;
    
    [Tooltip("Offset vertical du container par rapport à l'avion")]
    public float verticalOffset = 500f;
    
    [Tooltip("Limiter le suivi à l'horizontale uniquement (Y fixe)")]
    public bool horizontalOnlyFollow = true;
    
    [Header("Taille du Container")]
    [Tooltip("Taille du container de nuages (X, Y, Z) - Augmenter pour couvrir l'horizon")]
    public Vector3 containerSize = new Vector3(80000f, 150f, 80000f);
    
    [Tooltip("Auto-ajuster la taille du container pour couvrir tout l'horizon visible")]
    public bool autoSizeForHorizon = true;
    
    [Tooltip("Distance de vue de la caméra (pour calculer la taille automatique)")]
    public float cameraFarClipPlane = 50000f;
    
    [Header("Optimisation Horizon")]
    [Tooltip("Offset vertical pour positionner les nuages plus haut dans le ciel")]
    public float skyCloudOffset = 2000f;
    
    [Tooltip("Ajuster automatiquement la position Y du container en fonction de l'altitude de l'avion")]
    public bool adjustHeightWithPlane = false;
    
    [Header("Debug")]
    [Tooltip("Afficher les informations de debug")]
    public bool showDebug = true;
    
    [Tooltip("Forcer le repositionnement à chaque frame (pour tester)")]
    public bool alwaysFollow = false;

    private Vector3 lastTargetPosition;
    private Vector3 containerCenter;
    private bool initialized = false;

    void Start()
    {
        // Auto-détecter l'avion si pas assigné
        if (target == null)
        {
            var plane = FindObjectOfType<MFlight.Demo.Plane>();
            if (plane != null)
            {
                target = plane.transform;
                Debug.Log($"CloudContainerFollow: Avion détecté - {plane.name}");
            }
        }

        if (target == null)
        {
            Debug.LogError("CloudContainerFollow: Aucun avion trouvé! Assignez manuellement le target.");
            enabled = false;
            return;
        }

        // Auto-ajuster la taille du container pour couvrir l'horizon
        if (autoSizeForHorizon)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                cameraFarClipPlane = cam.farClipPlane;
                // Rendre le container assez grand pour couvrir tout l'horizon visible
                float horizontalSize = cameraFarClipPlane * 2f;
                // Préserver le Y défini dans l'Inspector, modifier seulement X et Z
                containerSize = new Vector3(horizontalSize, containerSize.y, horizontalSize);
                Debug.Log($"CloudContainerFollow: Taille auto-ajustée (X,Z)={horizontalSize}m, Y={containerSize.y}m (préservé de l'Inspector)");
            }
        }

        // Appliquer la taille du container (ou utiliser le scale existant si pas d'auto-size)
        if (autoSizeForHorizon)
        {
            // Préserver le Y du scale actuel
            Vector3 currentScale = transform.localScale;
            transform.localScale = new Vector3(containerSize.x, currentScale.y, containerSize.z);
            Debug.Log($"CloudContainerFollow: Scale appliqué - X={containerSize.x}, Y={currentScale.y} (préservé), Z={containerSize.z}");
        }

        // Initialiser la position
        InitializePosition();
        initialized = true;
        
        Debug.Log($"CloudContainerFollow: Initialisé. Container à {transform.position}, Avion à {target.position}");
    }

    void InitializePosition()
    {
        if (target == null) return;

        lastTargetPosition = target.position;
        
        // Garder le Y initial du container au lieu de le recalculer
        if (horizontalOnlyFollow)
        {
            // Utiliser le Y existant du container + optionnel offset vers le ciel
            float baseY = transform.position.y;
            if (adjustHeightWithPlane)
            {
                baseY = target.position.y + skyCloudOffset;
            }
            containerCenter = new Vector3(target.position.x, baseY, target.position.z);
        }
        else
        {
            containerCenter = new Vector3(target.position.x, target.position.y + verticalOffset, target.position.z);
        }
        
        // Ne pas modifier la position si horizontalOnlyFollow - garder le Y de l'Inspector
        if (horizontalOnlyFollow && !adjustHeightWithPlane)
        {
            transform.position = new Vector3(containerCenter.x, transform.position.y, containerCenter.z);
        }
        else
        {
            transform.position = containerCenter;
        }
    }

    void Update()
    {
        if (target == null || !initialized) return;

        // Mode test : suivre en continu
        if (alwaysFollow)
        {
            RepositionContainer();
            return;
        }

        // Calculer la distance de déplacement
        Vector3 targetPos = target.position;
        Vector3 delta = targetPos - lastTargetPosition;
        
        // Ne considérer que le déplacement horizontal si l'option est activée
        if (horizontalOnlyFollow)
        {
            delta.y = 0;
        }

        float distanceMoved = delta.magnitude;

        if (showDebug && distanceMoved > 0.1f)
        {
            Debug.Log($"CloudContainer: Avion déplacé de {distanceMoved:F1}m (seuil: {moveThreshold}m)");
        }

        // Si l'avion s'est déplacé au-delà du seuil, recentrer le container
        if (distanceMoved > moveThreshold)
        {
            Debug.Log($"CloudContainer: Seuil atteint! Repositionnement...");
            RepositionContainer();
        }
    }

    void RepositionContainer()
    {
        if (target == null) return;

        lastTargetPosition = target.position;

        if (horizontalOnlyFollow)
        {
            // Suivre uniquement en X et Z - conserver le Y actuel du container
            containerCenter.x = target.position.x;
            containerCenter.y = transform.position.y; // Utiliser le Y actuel, pas recalculer
            containerCenter.z = target.position.z;
        }
        else
        {
            // Suivre en X, Y, Z avec offset vertical
            containerCenter = new Vector3(
                target.position.x,
                target.position.y + verticalOffset,
                target.position.z
            );
        }

        transform.position = containerCenter;

        if (showDebug)
        {
            Debug.Log($"CloudContainer repositionné à {containerCenter}");
        }
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        if (showDebug && target != null)
        {
            // Afficher la zone de seuil
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lastTargetPosition, moveThreshold);
            
            // Afficher la ligne vers l'avion
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, target.position);
            
            // Afficher le container
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
    }

    void OnGUI()
    {
        if (!showDebug || target == null) return;
        
        GUI.Label(new Rect(10, 135, 500, 25), $"CloudContainer: Pos={transform.position.ToString("F0")}");
        GUI.Label(new Rect(10, 160, 500, 25), $"Avion: Pos={target.position.ToString("F0")}");
        
        Vector3 delta = target.position - lastTargetPosition;
        if (horizontalOnlyFollow) delta.y = 0;
        float dist = delta.magnitude;
        
        GUI.Label(new Rect(10, 185, 500, 25), $"Distance déplacée: {dist:F1}m / {moveThreshold}m");
    }
}
