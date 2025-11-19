//
// Copyright (c) Brian Hernandez. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.
//

using UnityEngine;
using UnityEngine.UI;

namespace MFlight.Demo
{
    public class Hud : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private MouseFlightController mouseFlight = null;
        [SerializeField] private Plane plane = null;

        [Header("HUD Elements")]
        [SerializeField] private RectTransform boresight = null;
        [SerializeField] private RectTransform mousePos = null;
        [Tooltip("Optional: Slider (0..1) to display throttle")] [SerializeField] private Slider throttleSlider = null;
        [Tooltip("Optional: Text to display throttle percent")] [SerializeField] private Text throttleText = null;

        private Camera playerCam = null;

        private void Awake()
        {
            if (mouseFlight == null)
                Debug.LogError(name + ": Hud - Mouse Flight Controller not assigned!");

            playerCam = Camera.main;

            if (plane == null)
            {
                plane = FindObjectOfType<Plane>();
            }
        }

        private void Update()
        {
            if (mouseFlight == null || playerCam == null)
                return;

            UpdateThrottleUI();
            UpdateGraphics(mouseFlight);
        }

        private void UpdateThrottleUI()
        {
            if (plane == null)
                return;

            if (throttleSlider != null)
            {
                throttleSlider.value = plane.throttle; // expect slider min=0, max=1
            }

            if (throttleText != null)
            {
                int pct = Mathf.RoundToInt(plane.throttle * 100f);
                throttleText.text = pct.ToString() + "%";
            }
        }

        private void UpdateGraphics(MouseFlightController controller)
        {
            if (boresight != null)
            {
                boresight.position = playerCam.WorldToScreenPoint(controller.BoresightPos);
                boresight.gameObject.SetActive(boresight.position.z > 1f);
            }

            if (mousePos != null)
            {
                mousePos.position = playerCam.WorldToScreenPoint(controller.MouseAimPos);
                mousePos.gameObject.SetActive(mousePos.position.z > 1f);
            }
        }

        public void SetReferenceMouseFlight(MouseFlightController controller)
        {
            mouseFlight = controller;
        }
    }
}
