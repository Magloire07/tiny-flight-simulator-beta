using UnityEngine;

/// <summary>
/// Système de sol infini qui suit l'avion en repositionnant le ground en X et Z
/// </summary>
public class InfiniteGroundSystem : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Transform de l'avion à suivre")]
    public Transform playerPlane;
    
    [Tooltip("GameObject du ground à repositionner (ne crée pas de nouveaux objets)")]
    public GameObject groundPrefab;
    
    [Header("Configuration")]
    [Tooltip("Distance de suivi (en mètres) - le ground suit l'avion avec ce décalage")]
    public float followDistance = 0f;
    
    [Header("Optimisation")]
    [Tooltip("Intervalle de mise à jour (secondes)")]
    public float updateInterval = 0.5f;
    
    // Données internes
    private float originalGroundY; // Position Y d'origine du ground
    private float nextUpdateTime;
    
    void Start()
    {
        // Trouver l'avion si non assigné
        if (playerPlane == null)
        {
            GameObject plane = GameObject.FindGameObjectWithTag("Player");
            if (plane != null)
                playerPlane = plane.transform;
            else
                Debug.LogError("InfiniteGroundSystem: Aucun avion trouvé!");
        }
        
        // Récupérer et sauvegarder la position Y d'origine du ground
        if (groundPrefab != null)
        {
            originalGroundY = groundPrefab.transform.position.y;
            Debug.Log($"InfiniteGroundSystem: Position Y d'origine du ground = {originalGroundY}");
        }
        else
        {
            Debug.LogError("InfiniteGroundSystem: Aucun ground prefab assigné!");
        }
    }
    
    void Update()
    {
        if (playerPlane == null || groundPrefab == null) return;
        
        // Vérification périodique
        if (Time.time >= nextUpdateTime)
        {
            nextUpdateTime = Time.time + updateInterval;
            UpdateGroundPosition();
        }
    }
    
    /// <summary>
    /// Met à jour la position du ground pour suivre l'avion en X et Z seulement
    /// </summary>
    void UpdateGroundPosition()
    {
        Vector3 planePos = playerPlane.position;
        
        // Nouvelle position: X et Z de l'avion, Y d'origine du ground
        Vector3 newPosition = new Vector3(
            planePos.x + followDistance,
            originalGroundY,
            planePos.z + followDistance
        );
        
        groundPrefab.transform.position = newPosition;
    }
}
