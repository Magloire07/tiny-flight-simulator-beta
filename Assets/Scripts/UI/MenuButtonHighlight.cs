using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Composant de surbrillance pour les boutons du menu.
/// Remplace directement la couleur de l'Image (pas le ColorBlock) pour un effet visible
/// sur fond sombre, que ce soit via le ray XR, la souris ou le joystick/clavier.
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class MenuButtonHighlight : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler,       IDeselectHandler
{
    [Tooltip("Couleur du bouton quand il est survolé / sélectionné")]
    public Color highlightColor = new Color(0.18f, 0.55f, 1f, 1f);   // bleu vif

    [Tooltip("Couleur du bouton quand il est pressé")]
    public Color pressedColor   = new Color(0.12f, 0.38f, 0.70f, 1f);

    // Couleur d'origine, sauvegardée automatiquement au démarrage
    private Color   _normalColor;
    private Image   _image;
    private Button  _button;
    private bool    _isHovered   = false;
    private bool    _isSelected  = false;

    void Awake()
    {
        _image  = GetComponent<Image>();
        _button = GetComponent<Button>();
        _normalColor = _image.color;

        // Désactive la transition ColorBlock pour éviter tout conflit
        _button.transition = Selectable.Transition.None;
    }

    // ---- Hover souris / ray XR ----

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        Refresh();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        Refresh();
    }

    // ---- Sélection joystick / clavier ----

    public void OnSelect(BaseEventData eventData)
    {
        _isSelected = true;
        Refresh();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _isSelected = false;
        Refresh();
    }

    // ---- Application de la couleur ----

    void Refresh()
    {
        if (!_button.interactable)
        {
            _image.color = _normalColor;
            return;
        }

        if (_isHovered || _isSelected)
            _image.color = highlightColor;
        else
            _image.color = _normalColor;
    }

    /// <summary>
    /// Appelé par XRMenuInput pour mettre à jour la couleur normale
    /// après avoir été ajouté dynamiquement à un bouton existant.
    /// </summary>
    public void SetNormalColor(Color c)
    {
        _normalColor = c;
        if (!_isHovered && !_isSelected)
            _image.color = c;
    }
}
