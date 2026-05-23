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
    public Color highlightColor = new Color(0f, 0.82f, 1f, 1f);

    // ---- Etat interne ----
    private InputDevice leftController;
    private InputDevice rightController;

    private float lastNavigationTime = -999f;
    private bool lastTriggerRight = false;
    private bool lastTriggerLeft  = false;

    // Ray Interactors XR (pour détecter le bouton survolé par le laser)
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor[] _rayInteractors;

    // (legacy) Suivi du bouton sélectionné pour le highlight direct
    private GameObject _previousSelected;
    private Dictionary<Image, Color> _originalColors = new Dictionary<Image, Color>();
    private List<GraphicRaycaster> _disabledRaycasters = new List<GraphicRaycaster>();

    // ---- Lifecycle ----

    void Start() { }

    void OnEnable()
    {
        CacheAllButtonColors();
        RefreshControllers();
        _rayInteractors = FindObjectsOfType<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
    }

    void OnDisable()
    {
    }

    /// <summary>Configure le ColorBlock de chaque bouton pour que hover (laser) et sélection (joystick) affichent highlightColor.</summary>
    void CacheAllButtonColors()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button btn in buttons)
        {
            // Supprimer MenuButtonHighlight résiduel s'il existe
            MenuButtonHighlight old = btn.GetComponent<MenuButtonHighlight>();
            if (old != null) Destroy(old);

            // Configurer les couleurs hover/select dans le ColorBlock
            ColorBlock cb = btn.colors;
            cb.highlightedColor = highlightColor;
            cb.selectedColor    = highlightColor;
            cb.pressedColor     = new Color(highlightColor.r * 0.6f, highlightColor.g * 0.6f, highlightColor.b * 0.6f, highlightColor.a);
            cb.colorMultiplier  = 1f;   // garantit que la couleur n'est pas atténuée
            cb.fadeDuration     = 0.05f; // transition quasi-instantanée
            btn.colors = cb;

            // ColorTint : Unity gère hover/select/press via CrossFadeColor nativement
            btn.transition = Selectable.Transition.ColorTint;

            // Empêcher la navigation clavier vers les éléments hors menu (ex: panneau simulateur XR)
            Canvas parentCanvas = btn.GetComponentInParent<Canvas>(true);
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Navigation nav = btn.navigation;
                nav.mode = Navigation.Mode.None;
                btn.navigation = nav;
            }
        }
    }

    /// <summary>Désactive/réactive le GraphicRaycaster des canvas overlay (simulateur XR) pendant que le menu est ouvert.</summary>
    void BlockOverlayRaycasters(bool block)
    {
        if (block)
        {
            _disabledRaycasters.Clear();
            foreach (Canvas c in FindObjectsOfType<Canvas>(true))
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    GraphicRaycaster gr = c.GetComponent<GraphicRaycaster>();
                    if (gr != null && gr.enabled)
                    {
                        gr.enabled = false;
                        _disabledRaycasters.Add(gr);
                    }
                }
            }
        }
        else
        {
            foreach (var gr in _disabledRaycasters)
                if (gr != null) gr.enabled = true;
            _disabledRaycasters.Clear();
        }
    }

    // LateUpdateSelectionHighlight() supprimé — ColorTint gère hover/select nativement via CrossFadeColor

    void Update()
    {
        // Revalide les contrôleurs si déconnectés
        if (!leftController.isValid || !rightController.isValid)
            RefreshControllers();

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

    void LateUpdate() { }

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

        // Clic souris = gâchette simulée dans XR Device Simulator
        bool mouseClick = Input.GetMouseButtonDown(0);

        bool pressed = triggerRight || triggerLeft;
        bool wasPressedBefore = lastTriggerRight || lastTriggerLeft;

        // Front montant uniquement pour les gâchettes matérielles
        if ((pressed && !wasPressedBefore) || mouseClick)
        {
            // Essayer d'abord de cliquer l'objet UI directement sous le laser
            if (!TryClickRayTarget())
                ClickSelected(); // Fallback : cliquer le bouton sélectionné au joystick
        }

        lastTriggerRight = triggerRight;
        lastTriggerLeft  = triggerLeft;
    }

    /// <summary>
    /// Clique le bouton UI actuellement survolé par un XR Ray Interactor.
    /// Retourne true si un bouton a été cliqué.
    /// </summary>
    bool TryClickRayTarget()
    {
        if (_rayInteractors == null) return false;
        foreach (var ri in _rayInteractors)
        {
            if (ri == null || !ri.isActiveAndEnabled) continue;
            if (ri.TryGetCurrentUIRaycastResult(out var result) && result.gameObject != null)
            {
                Button btn = result.gameObject.GetComponentInParent<Button>();
                if (btn != null && btn.interactable)
                {
                    btn.onClick.Invoke();
                    return true;
                }
            }
        }
        return false;
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
