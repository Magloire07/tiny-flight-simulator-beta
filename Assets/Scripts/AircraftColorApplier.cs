using UnityEngine;

/// <summary>
/// Applique la couleur sélectionnée au material de l'avion dans la scène de jeu
/// </summary>
public class AircraftColorApplier : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Liste des Renderers des parties de l'avion à colorier")]
    public Renderer[] aircraftRenderers;
    
    [Tooltip("Nom de la propriété de couleur du shader (ex: _Color, _BaseColor)")]
    public string colorPropertyName = "_Color";
    
    [Tooltip("Appliquer la couleur automatiquement au Start")]
    public bool applyOnStart = true;
    
    [Tooltip("Index du material à modifier sur chaque Renderer (0 par défaut)")]
    public int materialIndex = 0;
    
    void Start()
    {
        // Trouver tous les Renderers automatiquement si la liste est vide
        if (aircraftRenderers == null || aircraftRenderers.Length == 0)
        {
            aircraftRenderers = GetComponentsInChildren<Renderer>();
            if (aircraftRenderers.Length == 0)
            {
                Debug.LogError("AircraftColorApplier: Aucun Renderer trouvé sur l'avion!");
                return;
            }
            Debug.Log($"AircraftColorApplier: {aircraftRenderers.Length} Renderers trouvés automatiquement");
        }
        
        if (applyOnStart)
        {
            ApplySavedColor();
        }
    }
    
    /// <summary>
    /// Applique la couleur sauvegardée depuis le menu
    /// </summary>
    public void ApplySavedColor()
    {
        // Récupérer le code couleur sauvegardé
        string colorCode = PlayerPrefs.GetString("AircraftColorCode", "FFFFFF");
        
        // Convertir le code hex en Color
        Color newColor = HexToColor(colorCode);
        
        // Appliquer la couleur au material
        ApplyColor(newColor);
        
        Debug.Log($"AircraftColorApplier: Couleur appliquée: #{colorCode} = {newColor}");
    }
    
    /// <summary>
    /// Applique une couleur au material de toutes les parties de l'avion
    /// </summary>
    public void ApplyColor(Color color)
    {
        if (aircraftRenderers == null || aircraftRenderers.Length == 0)
        {
            Debug.LogError("AircraftColorApplier: Aucun Renderer assigné!");
            return;
        }
        
        int partsColored = 0;
        
        // Appliquer la couleur à tous les Renderers de la liste
        foreach (Renderer renderer in aircraftRenderers)
        {
            if (renderer == null)
            {
                Debug.LogWarning("AircraftColorApplier: Un Renderer de la liste est null, ignoré");
                continue;
            }
            
            // Obtenir le material (créer une instance si nécessaire)
            Material mat;
            if (Application.isPlaying)
            {
                // En mode jeu, créer une instance du material
                if (materialIndex < renderer.materials.Length)
                {
                    mat = renderer.materials[materialIndex];
                }
                else
                {
                    Debug.LogWarning($"AircraftColorApplier: Index material {materialIndex} invalide pour {renderer.name}");
                    continue;
                }
            }
            else
            {
                // En mode éditeur, modifier le shared material
                if (materialIndex < renderer.sharedMaterials.Length)
                {
                    mat = renderer.sharedMaterials[materialIndex];
                }
                else
                {
                    Debug.LogWarning($"AircraftColorApplier: Index material {materialIndex} invalide pour {renderer.name}");
                    continue;
                }
            }
            
            // Vérifier si le material a la propriété de couleur
            if (mat.HasProperty(colorPropertyName))
            {
                mat.SetColor(colorPropertyName, color);
                partsColored++;
            }
            else
            {
                Debug.LogWarning($"AircraftColorApplier: Le material de {renderer.name} n'a pas de propriété '{colorPropertyName}'");
            }
        }
        
        Debug.Log($"AircraftColorApplier: Couleur appliquée à {partsColored}/{aircraftRenderers.Length} parties");
    }
    
    /// <summary>
    /// Convertit un code couleur hex (ex: "FF00D7") en Color Unity
    /// </summary>
    Color HexToColor(string hex)
    {
        // Retirer le # si présent
        hex = hex.Replace("#", "");
        
        // S'assurer que le code a 6 caractères
        if (hex.Length != 6)
        {
            Debug.LogWarning($"AircraftColorApplier: Code couleur invalide '{hex}', utilisation du blanc");
            return Color.white;
        }
        
        try
        {
            // Convertir les composantes RGB
            byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            
            // Convertir en Color Unity (0-1)
            return new Color32(r, g, b, 255);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"AircraftColorApplier: Erreur de conversion du code '{hex}': {e.Message}");
            return Color.white;
        }
    }
    
    /// <summary>
    /// Applique une couleur depuis un code hex
    /// </summary>
    public void ApplyColorFromHex(string hexCode)
    {
        Color color = HexToColor(hexCode);
        ApplyColor(color);
    }
}
