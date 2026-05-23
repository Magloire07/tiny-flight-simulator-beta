using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using System.Collections.Generic;
using MFlight.Demo;
using DemoPlane = MFlight.Demo.Plane;

/// <summary>
/// Contrôle l'avion avec les manettes Meta Quest 3S.
/// Attacher ce script sur le même GameObject que le Plane, ou sur un GameObject dédié.
///
/// MAPPING MANETTES :
///   Stick gauche  X  → Roulis  (Roll)   — gauche = roll gauche
///   Stick gauche  Y  → Tangage (Pitch)  — vers l'avant = nez bas, vers l'arrière = nez haut
///   Stick droit   X  → Lacet   (Yaw)    — gauche = virer à gauche
///   Stick droit   Y  → Gaz              — haut = augmente, bas = diminue (progressif)
///   Gâchette droite (maintenue)  → Freins
///   Bouton A (manette droite)    → Moteur ON/OFF  (appui bref)
///   Bouton B (manette droite)    → Changer vue caméra (appui bref)
///   Bouton X (manette gauche)    → Rien (libre pour extensions futures)
///   Bouton Y (manette gauche)    → Rien (libre pour extensions futures)
///
/// SETUP UNITY :
///   1. Attacher ce script sur l'avion (ou tout GameObject actif dans la scène).
///   2. Assigner les champs dans l'Inspector :
///      - plane           → script Plane de l'avion
///      - brakeController → BrakeController de l'avion
///      - engineController→ EngineController de l'avion
///      - cameraViewSwitcher → CameraViewSwitcher de la scène
///   3. "useXRInput" sur Plane sera activé automatiquement au démarrage.
/// </summary>
public class XRFlightInput : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Script Plane de l'avion")]
    public DemoPlane plane;

    [Tooltip("BrakeController de l'avion")]
    public BrakeController brakeController;

    [Tooltip("EngineController de l'avion")]
    public EngineController engineController;

    [Tooltip("CameraViewSwitcher de la scène")]
    public CameraViewSwitcher cameraViewSwitcher;

    [Header("Sensibilités vol")]
    [Tooltip("Sensibilité du roulis (stick gauche X)")]
    [Range(0f, 2f)] public float rollSensitivity = 1f;

    [Tooltip("Sensibilité du tangage (stick gauche Y, inversé)")]
    [Range(0f, 2f)] public float pitchSensitivity = 1f;

    [Tooltip("Sensibilité du lacet (stick droit X)")]
    [Range(0f, 2f)] public float yawSensitivity = 1f;

    [Tooltip("Vitesse de changement des gaz (stick droit Y, unités/s)")]
    [Range(0f, 2f)] public float throttleChangeRate = 0.6f;

    [Tooltip("Deadzone des sticks analogiques")]
    [Range(0f, 0.4f)] public float joystickDeadzone = 0.12f;

    [Header("Freins")]
    [Tooltip("Seuil de pression sur la gâchette droite pour activer les freins")]
    [Range(0.1f, 0.9f)] public float brakeThreshold = 0.5f;

    [Header("Vibrations (turbulences)")]
    [Tooltip("Script AtmosphericTurbulence de l'avion")]
    public AtmosphericTurbulence atmosphericTurbulence;

    [Tooltip("Amplitude minimale de vibration (turbulence faible)")]
    [Range(0f, 1f)] public float hapticAmplitudeMin = 0.05f;

    [Tooltip("Amplitude maximale de vibration (turbulence forte)")]
    [Range(0f, 1f)] public float hapticAmplitudeMax = 0.6f;

    [Tooltip("Durée de chaque impulsion haptic (secondes)")]
    [Range(0.01f, 0.2f)] public float hapticPulseDuration = 0.04f;

    [Tooltip("Intervalle entre deux impulsions à intensité minimale (secondes)")]
    [Range(0.05f, 1f)] public float hapticIntervalMax = 0.35f;

    [Tooltip("Intervalle entre deux impulsions à intensité maximale (secondes)")]
    [Range(0.01f, 0.3f)] public float hapticIntervalMin = 0.06f;

    [Tooltip("Seuil d'intensité de turbulence en dessous duquel les vibrations s'arrêtent")]
    [Range(0f, 0.3f)] public float hapticDeadzone = 0.05f;

    [Header("État (lecture seule)")]
    [Tooltip("Manette gauche détectée")]
    public bool leftControllerFound = false;

    [Tooltip("Manette droite détectée")]
    public bool rightControllerFound = false;

    // ── Etat interne ────────────────────────────────────────────────────────
    // Input Actions (nouveau Input System — compatibles XR Device Simulator)
    private InputAction m_LeftStickAction;
    private InputAction m_RightStickAction;
    private InputAction m_RightTriggerAction;
    private InputAction m_EngineToggleAction;

    private bool lastAButton = false;
    private float hapticTimer = 0f;

    // Legacy XR — conservé uniquement pour les vibrations haptic
    private UnityEngine.XR.InputDevice leftCtrl;
    private UnityEngine.XR.InputDevice rightCtrl;
    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        // Auto-résolution des références si non assignées
        if (plane == null)
            plane = GetComponentInParent<DemoPlane>() ?? FindObjectOfType<DemoPlane>();
        if (brakeController == null)
            brakeController = GetComponentInParent<BrakeController>() ?? FindObjectOfType<BrakeController>();
        if (engineController == null)
            engineController = GetComponentInParent<EngineController>() ?? FindObjectOfType<EngineController>();
        if (cameraViewSwitcher == null)
            cameraViewSwitcher = FindObjectOfType<CameraViewSwitcher>();
        if (atmosphericTurbulence == null)
            atmosphericTurbulence = GetComponentInParent<AtmosphericTurbulence>() ?? FindObjectOfType<AtmosphericTurbulence>();
    }

    void OnEnable()
    {
        // ── Nouveau Input System (fonctionne avec simulateur ET vraies manettes) ──
        m_LeftStickAction    = new InputAction("LeftStick",    InputActionType.Value,  "<XRController>{LeftHand}/primary2DAxis");
        m_RightStickAction   = new InputAction("RightStick",   InputActionType.Value,  "<XRController>{RightHand}/primary2DAxis");
        m_RightTriggerAction = new InputAction("RightTrigger", InputActionType.Value,  "<XRController>{RightHand}/trigger");
        m_EngineToggleAction = new InputAction("EngineToggle", InputActionType.Button, "<XRController>{RightHand}/primaryButton");

        m_LeftStickAction.Enable();
        m_RightStickAction.Enable();
        m_RightTriggerAction.Enable();
        m_EngineToggleAction.Enable();

        // Legacy — pour les vibrations haptic uniquement
        RefreshControllers();
        InputDevices.deviceConnected    += OnDeviceConnected;
        InputDevices.deviceDisconnected += OnDeviceDisconnected;

        if (plane != null)
            plane.useXRInput = true;
    }

    void OnDisable()
    {
        m_LeftStickAction?.Disable();    m_LeftStickAction?.Dispose();
        m_RightStickAction?.Disable();   m_RightStickAction?.Dispose();
        m_RightTriggerAction?.Disable(); m_RightTriggerAction?.Dispose();
        m_EngineToggleAction?.Disable(); m_EngineToggleAction?.Dispose();

        InputDevices.deviceConnected    -= OnDeviceConnected;
        InputDevices.deviceDisconnected -= OnDeviceDisconnected;

        if (plane != null)
            plane.useXRInput = false;
    }

    void Update()
    {
        if (plane == null) return;

        // ── Mise à jour état manettes détectées (Info Inspector) ──────────
        leftControllerFound  = m_LeftStickAction?.controls.Count  > 0;
        rightControllerFound = m_RightStickAction?.controls.Count > 0;

        // ── Lecture sticks (nouveau Input System) ─────────────────────────
        Vector2 leftStick  = m_LeftStickAction?.ReadValue<Vector2>()  ?? Vector2.zero;
        Vector2 rightStick = m_RightStickAction?.ReadValue<Vector2>() ?? Vector2.zero;

        // Deadzone circulaire
        if (leftStick.magnitude  < joystickDeadzone) leftStick  = Vector2.zero;
        if (rightStick.magnitude < joystickDeadzone) rightStick = Vector2.zero;

        // ── Commandes de vol ─────────────────────────────────────────────
        plane.Roll  = Mathf.Clamp( leftStick.x  * rollSensitivity,  -1f, 1f);
        plane.Pitch = Mathf.Clamp(-leftStick.y  * pitchSensitivity, -1f, 1f);
        plane.Yaw   = Mathf.Clamp( rightStick.x * yawSensitivity,   -1f, 1f);

        float throttleDelta = rightStick.y * throttleChangeRate * Time.deltaTime;
        plane.throttle = Mathf.Clamp01(plane.throttle + throttleDelta);

        // ── Freins (gâchette droite maintenue) ────────────────────────────
        if (brakeController != null)
        {
            float rightTrigger = m_RightTriggerAction?.ReadValue<float>() ?? 0f;
            brakeController.brakesOn = rightTrigger > brakeThreshold;
        }

        // ── Bouton A → moteur ON/OFF (front montant) ──────────────────────
        if (engineController != null)
        {
            bool aPressed = m_EngineToggleAction?.IsPressed() ?? false;
            if (aPressed && !lastAButton)
                engineController.ToggleEngine();
            lastAButton = aPressed;
        }



        // ── Vibrations turbulences ─────────────────────────────────────────
        UpdateTurbulenceHaptics();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    void RefreshControllers()
    {
        var leftList  = new List<UnityEngine.XR.InputDevice>();
        var rightList = new List<UnityEngine.XR.InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, leftList);
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightList);

        if (leftList.Count > 0)  { leftCtrl  = leftList[0];  leftControllerFound  = true; }
        else                      {                            leftControllerFound  = false; }

        if (rightList.Count > 0) { rightCtrl = rightList[0]; rightControllerFound = true; }
        else                      {                            rightControllerFound = false; }
    }

    private void OnDeviceConnected(UnityEngine.XR.InputDevice device)    => RefreshControllers();
    private void OnDeviceDisconnected(UnityEngine.XR.InputDevice device) => RefreshControllers();

    // ── Haptics turbulences ──────────────────────────────────────────────────────────────
    void UpdateTurbulenceHaptics()
    {
        if (atmosphericTurbulence == null) return;
        if (!leftControllerFound && !rightControllerFound) return;

        float intensity = atmosphericTurbulence.CurrentTurbulenceIntensity;

        if (intensity <= hapticDeadzone)
        {
            hapticTimer = 0f; // réinitialiser pour que la prochaine impulsion soit immédiate
            return;
        }

        hapticTimer -= Time.deltaTime;
        if (hapticTimer > 0f) return;

        // Calculer amplitude et intervalle proportionnels à l'intensité
        float t = Mathf.InverseLerp(hapticDeadzone, 1f, intensity);
        float amplitude = Mathf.Lerp(hapticAmplitudeMin, hapticAmplitudeMax, t);
        float interval  = Mathf.Lerp(hapticIntervalMax, hapticIntervalMin, t);

        // Ajouter une variation aléatoire pour rendre les vibrations moins mécaniques
        amplitude *= Random.Range(0.7f, 1.0f);
        interval  *= Random.Range(0.8f, 1.2f);

        if (leftControllerFound)
            leftCtrl.SendHapticImpulse(0, amplitude, hapticPulseDuration);
        if (rightControllerFound)
            rightCtrl.SendHapticImpulse(0, amplitude, hapticPulseDuration);

        hapticTimer = interval;
    }
}
