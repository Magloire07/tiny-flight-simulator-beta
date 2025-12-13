using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Génère des moulins à vent dans les zones rurales avec rotation des pales
/// </summary>
public class WindmillGenerator : MonoBehaviour
{
    [Header("Références Terrain")]
    [Tooltip("Terrain sur lequel générer les moulins")]
    public Terrain terrain;
    
    [Header("Moulins")]
    [Tooltip("Prefab de moulin à vent")]
    public GameObject windmillPrefab;
    
    [Tooltip("Nombre de moulins à générer")]
    [Range(1, 50)]
    public int windmillCount = 10;
    
    [Tooltip("Variation d'échelle des moulins (0.5 = 50% à 150%)")]
    [Range(0f, 0.5f)]
    public float scaleVariation = 0.1f;
    
    [Header("Rotation des Pales")]
    [Tooltip("Nom de l'objet enfant contenant les pales (laisser vide si moulin = 1 objet)")]
    public string bladesObjectName = "Blades";
    
    [Tooltip("Vitesse de rotation des pales (degrés par seconde)")]
    public float rotationSpeed = 30f;
    
    [Tooltip("Variation de vitesse entre moulins (0-1)")]
    [Range(0f, 1f)]
    public float speedVariation = 0.3f;
    
    [Tooltip("Axe de rotation des pales")]
    public Vector3 rotationAxis = Vector3.forward;
    
    [Header("Placement")]
    [Tooltip("Centre de la zone de génération")]
    public Vector3 generationCenter = Vector3.zero;
    
    [Tooltip("Rayon de la zone de génération")]
    public float generationRadius = 2000f;
    
    [Tooltip("Distance minimale entre deux moulins")]
    public float minWindmillSpacing = 40f;
    
    [Tooltip("Placer sur des collines (altitude minimale)")]
    public bool preferHighGround = true;
    
    [Tooltip("Altitude minimale si preferHighGround activé")]
    public float minAltitude = 50f;
    
    [Header("Accessoires")]
    [Tooltip("Prefabs de clôtures autour du moulin")]
    public GameObject fencePrefab;
    
    [Tooltip("Créer une clôture autour de chaque moulin")]
    public bool createFence = true;
    
    [Tooltip("Rayon de la clôture")]
    public float fenceRadius = 15f;
    
    [Tooltip("Nombre de segments de clôture")]
    public int fenceSegments = 8;
    
    [Header("Végétation Locale")]
    [Tooltip("Prefabs de buissons/herbes près du moulin")]
    public GameObject[] vegetationPrefabs;
    
    [Tooltip("Nombre d'objets de végétation par moulin")]
    [Range(0, 20)]
    public int vegetationPerWindmill = 5;
    
    [Tooltip("Distance de la végétation du moulin")]
    public float vegetationDistance = 10f;
    
    [Header("Optimisation")]
    [Tooltip("Parent pour organiser les objets générés")]
    public Transform windmillsParent;
    
    [Tooltip("Graine aléatoire (0 = aléatoire)")]
    public int seed = 0;
    
    // Données internes
    private List<GameObject> generatedObjects = new List<GameObject>();
    private List<WindmillData> windmills = new List<WindmillData>();
    private System.Random random;

    void Start()
    {
        if (terrain == null)
            terrain = Terrain.activeTerrain;
    }

    void Update()
    {
        // Faire tourner les pales de tous les moulins
        foreach (WindmillData windmill in windmills)
        {
            if (windmill.bladesTransform != null)
            {
                windmill.bladesTransform.Rotate(rotationAxis, windmill.rotationSpeed * Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// Génère tous les moulins
    /// </summary>
    [ContextMenu("Generate Windmills")]
    public void GenerateWindmills()
    {
        Debug.Log("WindmillGenerator: Début de la génération des moulins...");
        
        // Initialiser le générateur aléatoire
        if (seed == 0)
            seed = Random.Range(1, 999999);
        
        random = new System.Random(seed);
        Debug.Log($"WindmillGenerator: Seed = {seed}");
        
        // Nettoyer les moulins existants
        ClearWindmills();
        
        // Créer le parent si nécessaire
        if (windmillsParent == null)
        {
            GameObject parentObj = new GameObject("Windmills");
            windmillsParent = parentObj.transform;
        }
        
        // Générer les moulins
        for (int i = 0; i < windmillCount; i++)
        {
            GenerateSingleWindmill(i);
        }
        
        Debug.Log($"WindmillGenerator: {windmills.Count} moulins générés avec {generatedObjects.Count} objets au total.");
    }

    /// <summary>
    /// Génère un moulin individuel
    /// </summary>
    void GenerateSingleWindmill(int windmillIndex)
    {
        if (windmillPrefab == null)
        {
            Debug.LogWarning("WindmillGenerator: Aucun prefab de moulin assigné!");
            return;
        }
        
        // Trouver une position valide
        Vector3 position = FindValidWindmillPosition();
        if (position == Vector3.zero)
        {
            Debug.LogWarning($"WindmillGenerator: Impossible de trouver une position valide pour le moulin {windmillIndex}");
            return;
        }
        
        // Créer le parent du moulin
        GameObject windmillParentObj = new GameObject($"Windmill_{windmillIndex}");
        windmillParentObj.transform.SetParent(windmillsParent);
        windmillParentObj.transform.position = position;
        
        // Rotation vers une direction aléatoire
        float rotation = (float)random.NextDouble() * 360f;
        windmillParentObj.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        
        // Créer le moulin
        GameObject windmill = Instantiate(windmillPrefab, position, Quaternion.Euler(0f, rotation, 0f));
        windmill.transform.SetParent(windmillParentObj.transform);
        windmill.name = "WindmillModel";
        
        // Variation d'échelle
        float scale = 1f + ((float)random.NextDouble() - 0.5f) * 2f * scaleVariation;
        windmill.transform.localScale *= scale;
        
        generatedObjects.Add(windmill);
        
        // Trouver les pales pour la rotation
        Transform bladesTransform = null;
        if (!string.IsNullOrEmpty(bladesObjectName))
        {
            bladesTransform = windmill.transform.Find(bladesObjectName);
            if (bladesTransform == null)
            {
                // Chercher récursivement
                bladesTransform = FindChildRecursive(windmill.transform, bladesObjectName);
            }
        }
        else
        {
            // Si pas de nom spécifié, utiliser le moulin lui-même
            bladesTransform = windmill.transform;
        }
        
        // Calculer la vitesse de rotation avec variation
        float speed = rotationSpeed * (1f + ((float)random.NextDouble() - 0.5f) * 2f * speedVariation);
        
        // Enregistrer les données du moulin
        WindmillData windmillData = new WindmillData
        {
            position = position,
            parentTransform = windmillParentObj.transform,
            bladesTransform = bladesTransform,
            rotationSpeed = speed
        };
        windmills.Add(windmillData);
        
        // Créer la clôture
        if (createFence && fencePrefab != null)
        {
            CreateFence(position, windmillParentObj.transform);
        }
        
        // Ajouter de la végétation locale
        if (vegetationPrefabs != null && vegetationPrefabs.Length > 0)
        {
            GenerateVegetationAroundWindmill(position, windmillParentObj.transform);
        }
        
        Debug.Log($"WindmillGenerator: Moulin {windmillIndex} créé à {position}");
    }

    /// <summary>
    /// Trouve une position valide pour un moulin
    /// </summary>
    Vector3 FindValidWindmillPosition()
    {
        int maxAttempts = 100;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Position aléatoire dans la zone
            float angle = (float)random.NextDouble() * 360f;
            float distance = (float)random.NextDouble() * generationRadius;
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;
            Vector3 position = generationCenter + offset;
            
            // Ajuster la hauteur selon le terrain
            if (terrain != null)
            {
                position.y = terrain.SampleHeight(position);
                
                // Vérifier l'altitude si preferHighGround
                if (preferHighGround && position.y < minAltitude)
                {
                    continue;
                }
            }
            
            // Vérifier la distance avec les moulins existants
            bool validPosition = true;
            foreach (WindmillData existingWindmill in windmills)
            {
                float distance2D = Vector2.Distance(
                    new Vector2(position.x, position.z),
                    new Vector2(existingWindmill.position.x, existingWindmill.position.z)
                );
                
                if (distance2D < minWindmillSpacing)
                {
                    validPosition = false;
                    break;
                }
            }
            
            if (validPosition)
                return position;
        }
        
        return Vector3.zero;
    }

    /// <summary>
    /// Crée une clôture circulaire autour du moulin
    /// </summary>
    void CreateFence(Vector3 center, Transform parent)
    {
        Transform fenceParent = new GameObject("Fence").transform;
        fenceParent.SetParent(parent);
        
        float angleStep = 360f / fenceSegments;
        
        for (int i = 0; i < fenceSegments; i++)
        {
            float angle = i * angleStep;
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * fenceRadius;
            Vector3 position = center + offset;
            
            // Ajuster la hauteur
            if (terrain != null)
            {
                position.y = terrain.SampleHeight(position);
            }
            
            // Créer le segment de clôture - rotation tangente au cercle (pas +90, juste angle)
            GameObject fenceSegment = Instantiate(fencePrefab, position, Quaternion.Euler(0f, angle, 0f));
            fenceSegment.transform.SetParent(fenceParent);
            
            generatedObjects.Add(fenceSegment);
        }
    }

    /// <summary>
    /// Génère de la végétation autour du moulin
    /// </summary>
    void GenerateVegetationAroundWindmill(Vector3 center, Transform parent)
    {
        Transform vegetationParent = new GameObject("Vegetation").transform;
        vegetationParent.SetParent(parent);
        
        for (int i = 0; i < vegetationPerWindmill; i++)
        {
            // Position aléatoire autour du moulin
            float angle = (float)random.NextDouble() * 360f;
            float distance = (float)random.NextDouble() * vegetationDistance;
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;
            Vector3 position = center + offset;
            
            // Ajuster la hauteur
            if (terrain != null)
            {
                position.y = terrain.SampleHeight(position);
            }
            
            // Choisir un prefab aléatoire
            GameObject vegPrefab = vegetationPrefabs[random.Next(vegetationPrefabs.Length)];
            
            // Créer l'objet
            GameObject veg = Instantiate(vegPrefab, position, Quaternion.identity);
            veg.transform.SetParent(vegetationParent);
            
            // Rotation aléatoire
            float rotation = (float)random.NextDouble() * 360f;
            veg.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            
            // Variation d'échelle
            float scale = 0.8f + (float)random.NextDouble() * 0.4f; // 0.8 à 1.2
            veg.transform.localScale *= scale;
            
            generatedObjects.Add(veg);
        }
    }

    /// <summary>
    /// Trouve un enfant récursivement par nom
    /// </summary>
    Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            
            Transform result = FindChildRecursive(child, name);
            if (result != null)
                return result;
        }
        
        return null;
    }

    /// <summary>
    /// Nettoie tous les moulins générés
    /// </summary>
    [ContextMenu("Clear Windmills")]
    public void ClearWindmills()
    {
        foreach (GameObject obj in generatedObjects)
        {
            if (obj != null)
                DestroyImmediate(obj);
        }
        
        generatedObjects.Clear();
        windmills.Clear();
        
        if (windmillsParent != null && windmillsParent.childCount > 0)
        {
            for (int i = windmillsParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(windmillsParent.GetChild(i).gameObject);
            }
        }
        
        Debug.Log("WindmillGenerator: Moulins nettoyés");
    }

    /// <summary>
    /// Affiche les zones en mode édition
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Zone de génération
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(generationCenter, generationRadius);
        
        // Moulins générés
        foreach (WindmillData windmill in windmills)
        {
            // Position du moulin
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(windmill.position, 5f);
            
            // Zone d'espacement
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(windmill.position, minWindmillSpacing / 2f);
            
            // Clôture
            if (createFence)
            {
                Gizmos.color = new Color(0.5f, 0.3f, 0f, 0.5f);
                Gizmos.DrawWireSphere(windmill.position, fenceRadius);
            }
        }
    }

    /// <summary>
    /// Données d'un moulin généré
    /// </summary>
    [System.Serializable]
    public class WindmillData
    {
        public Vector3 position;
        public Transform parentTransform;
        public Transform bladesTransform;
        public float rotationSpeed;
    }
}
