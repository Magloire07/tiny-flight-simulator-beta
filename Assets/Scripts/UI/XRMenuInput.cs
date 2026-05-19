using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections.Generic;

/// <summary>
/// Permet de naviguer dans le menu principal avec les manettes Meta Quest 3S.
/// - Joystick gauche ou droit : naviguer entre les boutons
/// - Gâchette droite ou gauche : cliquer le bouton sélectionné
/// 
/// SETUP UNITY :
/// 1. Attacher ce script à un GameObject dans la scène du menu (ex: "XRMenuInput")
/// 2. Dans l'EventSystem de la scène, assigner le premier bouton dans "First Selected"
/// 3. Sur chaque Button, vérifier que "Navigation" est sur "Automatic" (Inspector > Button)
/// 4. Optionnel : assigner une couleur de highlight dans ColorBlock des boutons
/// </summary>
public class XRMenuInput : MonoBehaviour
{
    [Header("Navigation")]
    [Tooltip("Temps minimum entre deux déplacements de sélection (secondes)")]
    public float navigationCooldown = 0.25f;

    [Tooltip("Seuil minimum du joystick pour déclencher une navigation")]
    [Range(0.1f, 0.9f)]
    public float joystickDeadzone = 0.4f;

    [Header("Bouton initial sélectionné (optionnel)")]
    [Tooltip("Bouton sélectionné au démarrage. Si vide, prend le 1er bouton trouvé.")]
    public Button firstSelectedButton;

    [Header("Highlight")]
    [Tooltip("Couleur du bouton actuellement sélectionné / survolé")]
    public Color highlightColor = new Color(0.18f, 0.55f, 1f, 1f);

    // ---- Etat interne ----
    private InputDevice leftController;
    private InputDevice rightController;

    private float lastNavigationTime = -999f;
    private bool lastTriggerRight = false;
    private bool lastTriggerLeft  = false;

    // Suivi du bouton sélectionné pour le highlight direct
    private GameObject _previousSelected;
    private Dictionary<Image, Color> _originalColors = new Dictionary<Image, Color>();

    // ---- Lifecycle ----

    void Start()
    {
        CacheAllButtonColors();
    }

    void OnEnable()
    {
        RefreshControllers();
        SelectFirstButton();
    }

    /// <summary>Mémorise les couleurs d'origine et désactive les transitions ColorBlock sur tous les boutons.</summary>
    void CacheAllButtonColors()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button btn in buttons)
        {
            // Supprimer MenuButtonHighlight résiduel s'il existe
            MenuButtonHighlight old = btn.GetComponent<MenuButtonHighlight>();
            if (old != null) Destroy(old);

            Image img = btn.GetComponent<Image>();
            if (img != null && !_originalColors.ContainsKey(img))
            {
                // Utiliser normalColor du ColorBlock, pas img.color :
                // au moment de Start(), le bouton initialement sélectionné a déjà
                // sa couleur selectedColor appliquée sur l'image par Unity,
                // ce qui fausserait le cache.
                Color normal = btn.colors.normalColor;
                _originalColors[img] = normal;
                img.color = normal; // remettre immédiatement à la couleur normale
            }

            // Transition.None : empêche CrossFadeColor d'écraser notre couleur
            btn.transition = Selectable.Transition.None;
        }
    }

    /// <summary>
    /// Appelé dans LateUpdate : force la couleur du bouton sélectionné APRÈS que
    /// tous les autres systèmes (Button.DoStateTransition, CrossFadeColor) ont tourné.
    /// Restaure la couleur normale sur l'ancien bouton quand la sélection change.
    /// </summary>
    void LateUpdateSelectionHighlight()
    {
        if (EventSystem.current == null) return;
        GameObject current = EventSystem.current.currentSelectedGameObject;

        // Restaurer l'ancien bouton si la sélection a changé
        if (current != _previousSelected)
        {
            if (_previousSelected != null)
            {
                Image img = _previousSelected.GetComponent<Image>();
                if (img != null && _originalColors.ContainsKey(img))
                    img.color = _originalColors[img];
            }
            _previousSelected = current;
        }

        // Forcer la couleur chaque frame sur le bouton courant
        // (écrase CrossFadeColor qui pourrait la réinitialiser après Update)
        if (current != null)
        {
            Image img = current.GetComponent<Image>();
            if (img != null)
            {
                if (!_originalColors.ContainsKey(img))
                    _originalColors[img] = img.color;
                img.color = highlightColor;
            }
        }
    }

    void Update()
    {
        // Revalide les contrôleurs si déconnectés
        if (!leftController.isValid || !rightController.isValid)
            RefreshControllers();

        // Si rien n'est sélectionné (changement de panneau), re-sélectionner auto
        EnsureSelection();

        HandleNavigation();
        HandleClick();
    }

    // ---- Initialisation ----

    /// <summary>Cherche les contrôleurs XR connectés.</summary>
    void RefreshControllers()
    {
        var devices = new List<InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, devices);
        if (devices.Count > 0)
            leftController = devices[0];

        devices.Clear();

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
        if (devices.Count > 0)
            rightController = devices[0];
    }

    void LateUpdate()
    {
        LateUpdateSelectionHighlight();
    }

    /// <summary>Sélectionne le premier bouton au démarrage.</summary>
    void SelectFirstButton()
    {
        if (EventSystem.current == null) return;

        if (firstSelectedButton != null && firstSelectedButton.gameObject.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
            return;
        }

        SelectFirstActiveButton();
    }

    /// <summary>
    /// Si le GameObject sélectionné est null ou inactif (ex: changement de panneau),
    /// cherche et sélectionne automatiquement le premier bouton interactable visible.
    /// </summary>
    void EnsureSelection()
    {
        if (EventSystem.current == null) return;

        GameObject sel = EventSystem.current.currentSelectedGameObject;

        // Sélection valide : on ne fait rien
        if (sel != null && sel.activeInHierarchy)
            return;

        SelectFirstActiveButton();
    }

    /// <summary>Sélectionne le premier Button actif et interactable dans la scène.</summary>
    void SelectFirstActiveButton()
    {
        if (EventSystem.current == null) return;

        Button[] buttons = FindObjectsOfType<Button>(false); // false = actifs uniquement
        foreach (Button btn in buttons)
        {
            if (btn.interactable && btn.gameObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(btn.gameObject);
                return;
            }
        }
    }

    // ---- Navigation au joystick ----

    void HandleNavigation()
    {
        if (Time.unscaledTime - lastNavigationTime < navigationCooldown)
            return;

        Vector2 axis = GetJoystickAxis();

        if (axis.magnitude < joystickDeadzone)
            return;

        // Déterminer la direction dominante
        MoveDirection dir;
        if (Mathf.Abs(axis.x) >= Mathf.Abs(axis.y))
            dir = axis.x > 0 ? MoveDirection.Right : MoveDirection.Left;
        else
            dir = axis.y > 0 ? MoveDirection.Up : MoveDirection.Down;

        // Envoyer l'événement de navigation à l'EventSystem
        if (EventSystem.current != null)
        {
            var axisEvent = new AxisEventData(EventSystem.current) { moveDir = dir };
            ExecuteEvents.Execute(
                EventSystem.current.currentSelectedGameObject,
                axisEvent,
                ExecuteEvents.moveHandler);
        }

        lastNavigationTime = Time.unscaledTime;
    }

    /// <summary>Lit l'axe du joystick (manette droite en priorité, sinon gauche).</summary>
    Vector2 GetJoystickAxis()
    {
        Vector2 axis = Vector2.zero;

        if (rightController.isValid)
            rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis);

        if (axis.magnitude < joystickDeadzone && leftController.isValid)
            leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis);

        return axis;
    }

    // ---- Clic à la gâchette ----

    void HandleClick()
    {
        bool triggerRight = false;
        bool triggerLeft  = false;

        if (rightController.isValid)
            rightController.TryGetFeatureValue(CommonUsages.triggerButton, out triggerRight);

        if (leftController.isValid)
            leftController.TryGetFeatureValue(CommonUsages.triggerButton, out triggerLeft);

        bool pressed = triggerRight || triggerLeft;
        bool wasPressedBefore = lastTriggerRight || lastTriggerLeft;

        // Front montant uniquement (évite le maintien répété)
        if (pressed && !wasPressedBefore)
            ClickSelected();

        lastTriggerRight = triggerRight;
        lastTriggerLeft  = triggerLeft;
    }

    /// <summary>Exécute le clic sur le GameObject actuellement sélectionné.</summary>
    void ClickSelected()
    {
        if (EventSystem.current == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null) return;

        // Déclenche Submit (bouton, toggle, slider, etc.)
        ExecuteEvents.Execute(selected, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);

        // Déclenche aussi onClick explicitement pour les Button
        Button btn = selected.GetComponent<Button>();
        if (btn != null && btn.interactable)
            btn.onClick.Invoke();

        // Toggle
        Toggle toggle = selected.GetComponent<Toggle>();
        if (toggle != null && toggle.interactable)
            toggle.isOn = !toggle.isOn;
    }
}
