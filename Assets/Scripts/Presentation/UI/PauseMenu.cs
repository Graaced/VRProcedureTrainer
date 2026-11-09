using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace VRPT.Presentation
{
    /**
     * CLASS: PauseMenu
     * DESCRIPTION: Menu opened with the ESC key, to go back to the training or close the application.
     * It appears in front of the user as a panel in the world, because the simulator operates
     * the UI with the controller ray and disables the normal mouse input.
     *
     */
    public sealed class PauseMenu : MonoBehaviour
    {
        /** Visible part of the menu; must be a child, because this component keeps reading the key */
        [SerializeField] private GameObject panel;

        /** Button that closes the menu */
        [SerializeField] private Button resumeButton;

        /** Button that closes the application */
        [SerializeField] private Button exitButton;

        /** Distance of the menu from the user's head, in meters */
        [Min(0.3f)]
        [SerializeField] private float distanceFromHead = 1.2f;

        /** Validates the setup and starts with the menu closed */
        private void Awake()
        {
            if (panel == null || resumeButton == null || exitButton == null)
            {
                Debug.LogError($"[{name}] Missing reference to {nameof(panel)}, {nameof(resumeButton)} or {nameof(exitButton)}", this);
                enabled = false;
                return;
            }

            resumeButton.onClick.AddListener(Close);
            exitButton.onClick.AddListener(ApplicationExit.Quit);
            panel.SetActive(false);
        }

        /** Stops listening to the buttons */
        private void OnDestroy()
        {
            if (resumeButton != null)
                resumeButton.onClick.RemoveListener(Close);

            if (exitButton != null)
                exitButton.onClick.RemoveListener(ApplicationExit.Quit);
        }

        /** Opens or closes the menu when ESC is pressed */
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
                return;

            if (panel.activeSelf)
                Close();
            else
                Open();
        }

        /** Shows the menu in front of the user */
        private void Open()
        {
            Camera head = Camera.main;
            if (head == null)
            {
                Debug.LogError($"[{name}] No camera tagged MainCamera to place the menu", this);
                return;
            }

            // Horizontal direction, so the menu stays upright even when looking up or down
            Vector3 direction = Vector3.ProjectOnPlane(head.transform.forward, Vector3.up);
            if (direction.sqrMagnitude < 0.001f)
                direction = head.transform.up;

            direction.Normalize();
            transform.SetPositionAndRotation(head.transform.position + direction * distanceFromHead, Quaternion.LookRotation(direction));
            panel.SetActive(true);
        }

        /** Hides the menu */
        private void Close()
        {
            panel.SetActive(false);
        }
    }
}
