using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Menu world-space positionné à côté de l'avion sur la piste.
/// Construit automatiquement le Canvas et les boutons au démarrage.
///
/// SETUP :
/// 1. Créer un GameObject vide dans la scène Flight Demo ("RunwayMenu")
/// 2. Attacher ce script dessus
/// 3. Positionner le GameObject à côté de l'avion (ex: légèrement à gauche, face au joueur)
/// 4. Assigner flightObject (l'avion) dans l'Inspector
/// 5. Assigner startMenuController si présent dans la scène (optionnel)
/// </summary>
public class RunwayMenuController : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("L'objet avion dont on doit activer les contrôles au démarrage")]
    public GameObject flightObject;

    [Tooltip("StartMenuController présent dans la scène (optionnel - pour transmettre les paramètres)")]
    public StartMenuController startMenuController;

    [Tooltip("CameraViewSwitcher pour activer la vue cockpit au démarrage du vol")]
    public CameraViewSwitcher cameraViewSwitcher;

    [Header("Apparence du panneau")]
    [Tooltip("Taille du panneau en unités monde (largeur, hauteur)")]
    public Vector2 panelSize = new Vector2(1.2f, 0.8f);

    [Header("Navigation manette")]
    [Tooltip("Délai minimum entre deux mouvements de navigation (secondes)")]
    public float navigationCooldown = 0.25f;

    [Range(0.1f, 0.9f)]
    public float joystickDeadzone = 0.4f;

    // ── Etat interne ──────────────────────────────────────────────
    private InputDevice _leftCtrl;
    private InputDevice _rightCtrl;
    private float       _lastNavTime  = -999f;
    private bool        _lastTriggerR = false;
    private bool        _lastTriggerL = false;

    private GameObject _previousSelected;

    // Matériaux des éléments UI (instances séparées pour modifier les propriétés)
    private Material _panelBgMat;
    private Material _btnBoardMat;
    private Material _btnQuitMat;

    // Couleurs de bordure par défaut de chaque bouton (restauration au dé-highlight)
    private static readonly Color _boardBorderNormal = new Color(0.20f, 0.75f, 0.30f, 0.50f);
    private static readonly Color _quitBorderNormal  = new Color(0.90f, 0.20f, 0.20f, 0.50f);
    private static readonly Color _highlightBorder   = new Color(0.30f, 0.65f, 1.00f, 1.00f);
    private const float _borderNormal    = 1.5f;
    private const float _borderHighlight = 5.0f;

    // Boutons construits
    private Button _btnBoard;
    private Button _btnQuit;

    // ── Lifecycle ─────────────────────────────────────────────────

    void Awake()
    {
        BuildCanvas();
    }

    void Start()
    {
        RefreshControllers();
        CacheButtonColors();
        SelectFirst();

        // Ne pas mettre en pause : l'avion est déjà immobile (contrôles désactivés par défaut)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    void Update()
    {
        if (!_leftCtrl.isValid || !_rightCtrl.isValid)
            RefreshControllers();

        HandleNavigation();
        HandleClick();
    }

    void LateUpdate()
    {
        LateUpdateHighlight();
    }

    // ── Construction du canvas ───────────────────────────────────

    void BuildCanvas()
    {
        // Canvas world-space
        GameObject canvasGO = new GameObject("RunwayMenuCanvas");
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = Vector3.zero;
        canvasGO.transform.localRotation = Quaternion.identity;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        // 1 pixel = 1 mm → panneau de panelSize mètres
        canvasRect.sizeDelta = panelSize * 1000f;
        canvasGO.transform.localScale = Vector3.one * 0.001f;

        // GraphicRaycaster standard (fallback desktop)
        if (canvasGO.GetComponent<GraphicRaycaster>() == null)
            canvasGO.AddComponent<GraphicRaycaster>();

        // TrackedDeviceGraphicRaycaster pour XR — ajouté sans condition :
        // XRUISetup peut s'exécuter avant ce canvas (ordre Awake non garanti),
        // et TrackedDeviceGraphicRaycaster fonctionne aussi en mode desktop.
        if (canvasGO.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();

        // EventSystem : créer si absent, et s'assurer que XRUIInputModule est présent
        EventSystem es = FindObjectOfType<EventSystem>();
        if (es == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            es = esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }
        // XRUIInputModule requis pour que les rays XR envoient des pointer events à l'UI
        if (es.GetComponent<XRUIInputModule>() == null)
            es.gameObject.AddComponent<XRUIInputModule>();

        // ── Fond du panneau ──────────────────────────────────────
        Shader roundedShader = Shader.Find("UI/RoundedPanel");
        if (roundedShader == null)
            Debug.LogWarning("[RunwayMenu] Shader 'UI/RoundedPanel' introuvable.");

        _panelBgMat = MakeMat(roundedShader,
            new Color(0.10f, 0.11f, 0.16f, 0.97f),
            new Color(0.05f, 0.06f, 0.10f, 0.97f),
            new Color(0.25f, 0.50f, 0.90f, 0.60f),
            1.5f, 0.06f, 0.12f);

        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(canvasGO.transform, false);
        RectTransform bgRect = bg.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color    = Color.white;
        bgImg.material = _panelBgMat;

        // ── Layout vertical ──────────────────────────────────────
        VerticalLayoutGroup layout = bg.AddComponent<VerticalLayoutGroup>();
        layout.padding              = new RectOffset(60, 60, 70, 70);
        layout.spacing              = 50f;
        layout.childAlignment       = TextAnchor.MiddleCenter;
        layout.childControlWidth    = true;
        layout.childControlHeight   = false;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;

        // ── Titre cyan ───────────────────────────────────────────
        CreateLabel("TitleLabel", "SIMULATEUR DE VOL", bg.transform, 80,
                    FontStyle.Bold, new Color(0.35f, 0.82f, 1.00f));

        // ── Séparateur bleu ──────────────────────────────────────
        CreateSeparator(bg.transform);

        // ── Bouton Monter à bord ─────────────────────────────────
        _btnBoardMat = MakeMat(roundedShader,
            new Color(0.08f, 0.28f, 0.12f, 0.97f),
            new Color(0.04f, 0.16f, 0.06f, 0.97f),
            _boardBorderNormal, _borderNormal, 0.10f, 0.08f);
        _btnBoard = CreateButton("BtnBoard", "MONTER A BORD", bg.transform, _btnBoardMat, 140f);
        _btnBoard.onClick.AddListener(EnterPlane);

        // ── Bouton Quitter ───────────────────────────────────────
        _btnQuitMat = MakeMat(roundedShader,
            new Color(0.35f, 0.08f, 0.08f, 0.97f),
            new Color(0.20f, 0.04f, 0.04f, 0.97f),
            _quitBorderNormal, _borderNormal, 0.10f, 0.08f);
        _btnQuit = CreateButton("BtnQuit", "QUITTER", bg.transform, _btnQuitMat, 140f);
        _btnQuit.onClick.AddListener(QuitGame);
    }

    /// <summary>Crée un matériau UI/RoundedPanel avec les paramètres donnés.</summary>
    Material MakeMat(Shader sh, Color top, Color bottom, Color border,
                     float borderWidth, float radius, float glow)
    {
        Material mat = sh != null
            ? new Material(sh)
            : new Material(Shader.Find("UI/Default"));
        mat.SetColor("_ColorTop",     top);
        mat.SetColor("_ColorBottom",  bottom);
        mat.SetColor("_BorderColor",  border);
        mat.SetFloat("_BorderWidth",  borderWidth);
        mat.SetFloat("_Radius",       radius);
        mat.SetFloat("_GlowStrength", glow);
        return mat;
    }

    // ── Helpers de construction UI ───────────────────────────────

    Button CreateButton(string name, string label, Transform parent, Material mat, float height)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight       = height;

        Image img = go.AddComponent<Image>();
        img.color    = Color.white;
        img.material = mat;

        Button btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;

        GameObject textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        RectTransform tr = textGO.AddComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.sizeDelta = Vector2.zero;

        Text txt = textGO.AddComponent<Text>();
        txt.text      = label;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize  = 62;
        txt.fontStyle = FontStyle.Bold;
        txt.color     = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;

        return btn;
    }

    void CreateLabel(string name, string content, Transform parent,
                     int size, FontStyle style, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = size + 20f;

        Text txt = go.AddComponent<Text>();
        txt.text      = content;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize  = size;
        txt.fontStyle = style;
        txt.color     = color;
        txt.alignment = TextAnchor.MiddleCenter;
    }

    void CreateSeparator(Transform parent)
    {
        GameObject go = new GameObject("Separator");
        go.transform.SetParent(parent, false);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 3f;
        le.minHeight       = 3f;
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.50f, 0.90f, 0.50f);
    }

    // ── Actions boutons ──────────────────────────────────────────

    /// <summary>Lance le vol : cache le menu, active la vue cockpit, réactive les contrôles.</summary>
    public void EnterPlane()
    {
        // Activer la vue cockpit avant tout
        if (cameraViewSwitcher == null)
            cameraViewSwitcher = FindObjectOfType<CameraViewSwitcher>();
        if (cameraViewSwitcher != null)
            cameraViewSwitcher.ActivateCockpitView();

        // Déléguer à StartMenuController si présent
        if (startMenuController != null)
        {
            Time.timeScale = 1f;
            startMenuController.StartGame();
            gameObject.SetActive(false);
            return;
        }

        // Sinon faire la logique directement
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible   = true;

        if (flightObject != null)
        {
            var plane = flightObject.GetComponent<MFlight.Demo.Plane>();
            if (plane != null) plane.enabled = true;
        }

        gameObject.SetActive(false);
    }

    /// <summary>Quitte l'application.</summary>
    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ── Highlight via propriétés matériau ────────────────────────

    void CacheButtonColors() { /* Matériaux initialisés dans BuildCanvas, rien à faire ici. */ }

    void LateUpdateHighlight()
    {
        if (EventSystem.current == null) return;
        GameObject current = EventSystem.current.currentSelectedGameObject;
        if (current == _previousSelected) return;

        // Restaurer la bordure de l'ancien bouton
        if (_previousSelected == _btnBoard?.gameObject)
            SetBorder(_btnBoardMat, _boardBorderNormal, _borderNormal);
        else if (_previousSelected == _btnQuit?.gameObject)
            SetBorder(_btnQuitMat, _quitBorderNormal, _borderNormal);

        // Appliquer le highlight bleu vif sur le nouveau
        if (current == _btnBoard?.gameObject)
            SetBorder(_btnBoardMat, _highlightBorder, _borderHighlight);
        else if (current == _btnQuit?.gameObject)
            SetBorder(_btnQuitMat, _highlightBorder, _borderHighlight);

        _previousSelected = current;
    }

    void SetBorder(Material mat, Color color, float width)
    {
        if (mat == null) return;
        mat.SetColor("_BorderColor", color);
        mat.SetFloat("_BorderWidth", width);
    }

    // ── Navigation manette ───────────────────────────────────────

    void RefreshControllers()
    {
        var list = new List<InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, list);
        if (list.Count > 0) _leftCtrl = list[0];

        list.Clear();

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, list);
        if (list.Count > 0) _rightCtrl = list[0];
    }

    void SelectFirst()
    {
        if (EventSystem.current == null) return;
        if (_btnBoard != null)
            EventSystem.current.SetSelectedGameObject(_btnBoard.gameObject);
    }

    void HandleNavigation()
    {
        if (Time.unscaledTime - _lastNavTime < navigationCooldown) return;

        Vector2 axis = Vector2.zero;
        if (_rightCtrl.isValid)
            _rightCtrl.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis);
        if (axis.magnitude < joystickDeadzone && _leftCtrl.isValid)
            _leftCtrl.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis);

        if (axis.magnitude < joystickDeadzone) return;

        MoveDirection dir = Mathf.Abs(axis.x) >= Mathf.Abs(axis.y)
            ? (axis.x > 0 ? MoveDirection.Right : MoveDirection.Left)
            : (axis.y > 0 ? MoveDirection.Up    : MoveDirection.Down);

        if (EventSystem.current != null)
        {
            var axisEvent = new AxisEventData(EventSystem.current) { moveDir = dir };
            ExecuteEvents.Execute(
                EventSystem.current.currentSelectedGameObject,
                axisEvent,
                ExecuteEvents.moveHandler);
        }

        _lastNavTime = Time.unscaledTime;
    }

    void HandleClick()
    {
        bool trigR = false, trigL = false;

        if (_rightCtrl.isValid)
            _rightCtrl.TryGetFeatureValue(CommonUsages.triggerButton, out trigR);
        if (_leftCtrl.isValid)
            _leftCtrl.TryGetFeatureValue(CommonUsages.triggerButton, out trigL);

        bool pressed = trigR || trigL;
        bool wasPressed = _lastTriggerR || _lastTriggerL;

        if (pressed && !wasPressed)
        {
            GameObject sel = EventSystem.current?.currentSelectedGameObject;
            if (sel != null)
            {
                ExecuteEvents.Execute(sel, new BaseEventData(EventSystem.current),
                    ExecuteEvents.submitHandler);
                Button btn = sel.GetComponent<Button>();
                if (btn != null && btn.interactable)
                    btn.onClick.Invoke();
            }
        }

        _lastTriggerR = trigR;
        _lastTriggerL = trigL;
    }

    // Activer la navigation au clavier aussi (flèches haut/bas)
    void OnGUI()
    {
        Event e = Event.current;
        if (e.type != EventType.KeyDown) return;

        if (e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.DownArrow)
        {
            if (Time.unscaledTime - _lastNavTime < navigationCooldown) return;

            MoveDirection dir = e.keyCode == KeyCode.UpArrow
                ? MoveDirection.Up : MoveDirection.Down;

            if (EventSystem.current != null)
            {
                var axisEvent = new AxisEventData(EventSystem.current) { moveDir = dir };
                ExecuteEvents.Execute(
                    EventSystem.current.currentSelectedGameObject,
                    axisEvent,
                    ExecuteEvents.moveHandler);
            }
            _lastNavTime = Time.unscaledTime;
        }

        if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.Space)
        {
            GameObject sel = EventSystem.current?.currentSelectedGameObject;
            if (sel != null)
            {
                Button btn = sel.GetComponent<Button>();
                if (btn != null && btn.interactable)
                    btn.onClick.Invoke();
            }
        }
    }
}
