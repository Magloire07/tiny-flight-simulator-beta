using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applique le shader "UI/RoundedPanel" au composant Image du GameObject.
/// À attacher sur l'objet Image qui sert de fond au panneau de menu.
///
/// SETUP UNITY :
/// 1. Sélectionner l'Image de fond du Canvas (ex: "Background" ou "Panel")
/// 2. Attacher ce script dessus
/// 3. Ajuster les couleurs et paramètres dans l'Inspector
/// 4. L'image de fond existante sera remplacée par le fond procédural
/// </summary>
[RequireComponent(typeof(Image))]
public class RoundedPanelBackground : MonoBehaviour
{
    [Header("Dégradé de fond")]
    [Tooltip("Couleur en haut du panneau")]
    public Color colorTop    = new Color(0.10f, 0.11f, 0.16f, 0.97f);

    [Tooltip("Couleur en bas du panneau")]
    public Color colorBottom = new Color(0.05f, 0.06f, 0.10f, 0.97f);

    [Header("Bordure")]
    [Tooltip("Couleur de la bordure intérieure lumineuse")]
    public Color borderColor = new Color(0.25f, 0.50f, 0.90f, 0.60f);

    [Tooltip("Epaisseur de la bordure (unité arbitraire, ex: 1.5)")]
    [Range(0f, 6f)]
    public float borderWidth = 1.5f;

    [Header("Forme")]
    [Tooltip("Rayon des coins (0 = carré, 0.5 = cercle parfait)")]
    [Range(0f, 0.5f)]
    public float cornerRadius = 0.06f;

    [Header("Effets")]
    [Tooltip("Intensité de la lueur centrale subtile")]
    [Range(0f, 1f)]
    public float glowStrength = 0.12f;

    // -----------------------------------------------------------------------

    private Image      _image;
    private Material   _material;

    static readonly int ID_ColorTop     = Shader.PropertyToID("_ColorTop");
    static readonly int ID_ColorBottom  = Shader.PropertyToID("_ColorBottom");
    static readonly int ID_BorderColor  = Shader.PropertyToID("_BorderColor");
    static readonly int ID_BorderWidth  = Shader.PropertyToID("_BorderWidth");
    static readonly int ID_Radius       = Shader.PropertyToID("_Radius");
    static readonly int ID_GlowStrength = Shader.PropertyToID("_GlowStrength");

    void Awake()
    {
        _image = GetComponent<Image>();

        Shader shader = Shader.Find("UI/RoundedPanel");
        if (shader == null)
        {
            Debug.LogError("[RoundedPanelBackground] Shader 'UI/RoundedPanel' introuvable. " +
                           "Vérifiez que Assets/Shaders/UI/RoundedPanel.shader est bien présent.");
            return;
        }

        _material = new Material(shader);
        _material.name = "RoundedPanel_Instance";

        // Retirer l'image de fond existante (on laisse le sprite vide)
        _image.sprite = null;
        _image.material = _material;
        _image.color = Color.white; // le shader gère lui-même les couleurs

        ApplyProperties();
    }

    void OnValidate()
    {
        // Mise à jour en temps réel dans l'éditeur
        if (_material != null)
            ApplyProperties();
    }

    /// <summary>Pousse tous les paramètres vers le matériau du shader.</summary>
    void ApplyProperties()
    {
        _material.SetColor(ID_ColorTop,     colorTop);
        _material.SetColor(ID_ColorBottom,  colorBottom);
        _material.SetColor(ID_BorderColor,  borderColor);
        _material.SetFloat(ID_BorderWidth,  borderWidth);
        _material.SetFloat(ID_Radius,       cornerRadius);
        _material.SetFloat(ID_GlowStrength, glowStrength);
    }

    void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);
    }
}
