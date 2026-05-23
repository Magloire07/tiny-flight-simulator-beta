using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Applique automatiquement un thème de couleurs sombres à tous les boutons
/// de la scène (ou uniquement ceux d'un panneau ciblé).
///
/// SETUP UNITY :
/// 1. Attacher ce script à un GameObject dans chaque scène qui a des boutons
///    (ex: le même "XRMenuInput" ou un objet "UITheme")
/// 2. Ajuster les couleurs dans l'Inspector selon votre charte graphique
/// 3. "Apply On Start" applique le thème dès le démarrage
/// 4. "Target Panel" (optionnel) : si assigné, seuls les boutons enfants de ce
///    panneau sont affectés. Sinon, TOUS les boutons de la scène sont colorés.
/// </summary>
public class UIButtonTheme : MonoBehaviour
{
    [Header("Panneau cible (optionnel)")]
    [Tooltip("Si assigné, seuls les boutons enfants de ce panneau sont affectés.\n" +
             "Laisser vide pour affecter TOUS les boutons de la scène.")]
    public GameObject targetPanel;

    [Header("Couleurs du thème")]
    [Tooltip("Couleur de repos (état normal)")]
    public Color normalColor       = new Color(0.12f, 0.12f, 0.14f, 1f);   // gris très sombre

    [Tooltip("Couleur quand le bouton est survolé ou sélectionné (manette / souris)")]
    public Color highlightedColor  = new Color(0.25f, 0.45f, 0.70f, 1f);   // bleu nuit

    [Tooltip("Couleur quand le bouton est pressé")]
    public Color pressedColor      = new Color(0.10f, 0.30f, 0.55f, 1f);   // bleu foncé

    [Tooltip("Couleur quand le bouton est sélectionné par l'EventSystem (XR / clavier)")]
    public Color selectedColor     = new Color(0.20f, 0.55f, 0.85f, 1f);   // bleu clair

    [Tooltip("Couleur quand le bouton est désactivé (interactable = false)")]
    public Color disabledColor     = new Color(0.20f, 0.20f, 0.22f, 0.5f); // gris transparent

    [Header("Texte des boutons")]
    [Tooltip("Couleur du texte dans les boutons")]
    public Color textColor         = new Color(0.92f, 0.92f, 0.92f, 1f);   // blanc cassé

    [Header("Options")]
    [Tooltip("Appliquer le thème automatiquement au démarrage")]
    public bool applyOnStart = true;

    [Tooltip("Durée de la transition de couleur (secondes)")]
    [Range(0f, 0.5f)]
    public float fadeDuration = 0.12f;

    // -----------------------------------------------------------------------

    void Start()
    {
        if (applyOnStart)
            ApplyTheme();
    }

    /// <summary>
    /// Applique le thème à tous les boutons cibles.
    /// Peut être appelé depuis l'Inspector ou depuis un autre script.
    /// </summary>
    [ContextMenu("Appliquer le thème maintenant")]
    public void ApplyTheme()
    {
        Button[] buttons = GetTargetButtons();
        int count = 0;

        foreach (Button btn in buttons)
        {
            ApplyToButton(btn);
            count++;
        }

        Debug.Log($"[UIButtonTheme] Thème appliqué à {count} bouton(s).");
    }

    // -----------------------------------------------------------------------

    Button[] GetTargetButtons()
    {
        if (targetPanel != null)
            return targetPanel.GetComponentsInChildren<Button>(true);

        return FindObjectsOfType<Button>(true);
    }

    void ApplyToButton(Button btn)
    {
        // --- ColorBlock (fond normal et désactivé uniquement) ---
        // highlightedColor / selectedColor / pressedColor / fadeDuration
        // sont gérés par XRMenuInput.CacheAllButtonColors() — on ne les écrase pas.
        ColorBlock cb = btn.colors;
        cb.normalColor   = normalColor;
        cb.disabledColor = disabledColor;
        cb.colorMultiplier = 1f;
        btn.colors = cb;

        // --- Couleur de l'image de fond du bouton ---
        Image img = btn.GetComponent<Image>();
        if (img != null)
            img.color = normalColor;

        // --- Couleur du texte enfant (Text legacy ou TextMeshPro) ---
        ApplyTextColor(btn);
    }

    void ApplyTextColor(Button btn)
    {
        // Text legacy (UnityEngine.UI)
        Text[] texts = btn.GetComponentsInChildren<Text>(true);
        foreach (Text t in texts)
            t.color = textColor;

        // TextMeshPro (si le package est présent)
        // On utilise la réflexion pour éviter une dépendance directe à TMP
        foreach (Component comp in btn.GetComponentsInChildren<Component>(true))
        {
            if (comp == null) continue;
            System.Type type = comp.GetType();
            if (type.Name == "TextMeshProUGUI" || type.Name == "TMP_Text")
            {
                var colorProp = type.GetProperty("color");
                if (colorProp != null)
                    colorProp.SetValue(comp, textColor);
            }
        }
    }
}
