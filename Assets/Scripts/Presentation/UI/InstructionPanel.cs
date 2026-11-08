using TMPro;
using UnityEngine;

namespace VRPT.Presentation
{
    /**
     * CLASS: InstructionPanel
     * DESCRIPTION: Panel above the machine that tells the user what to do and what went wrong.
     * It only displays what it receives, so it never decides the outcome of a step.
     *
     */
    public sealed class InstructionPanel : MonoBehaviour
    {
        /** Text component of the panel */
        [SerializeField] private TMP_Text panelText;

        /** Position in the procedure, e.g. "Step 2 of 5" */
        private string progress = string.Empty;

        /** What the user must do now */
        private string instruction = string.Empty;

        /** Last mistake of the user on the current step */
        private string error = string.Empty;

        /** Validates the setup */
        private void Awake()
        {
            if (panelText == null)
            {
                Debug.LogError($"[{name}] Missing reference to {nameof(panelText)}", this);
                enabled = false;
            }
        }

        /** Shows a new step and removes the mistake made on the previous one */
        public void ShowStep(int stepNumber, int totalSteps, string stepInstruction)
        {
            progress = $"Step {stepNumber} of {totalSteps}";
            instruction = stepInstruction;
            error = string.Empty;
            Refresh();
        }

        /** Tells the user the control is not the next one */
        public void ShowWrongOrder(string controlName)
        {
            error = $"Wrong order: {controlName} is not the next step.";
            Refresh();
        }

        /** Tells the user the control is dangerous at this point */
        public void ShowCriticalError(string controlName)
        {
            error = $"Danger: {controlName} must not be operated at this point.";
            Refresh();
        }

        /** Shows the outcome; a failure keeps the mistake on screen, since it explains why */
        public void ShowFinished(bool completed)
        {
            progress = string.Empty;
            instruction = completed ? "Procedure completed" : "Procedure failed";

            if (completed)
                error = string.Empty;

            Refresh();
        }

        /** Writes the current content to the text component */
        private void Refresh()
        {
            panelText.text = $"<size=70%>{progress}</size>\n<b>{instruction}</b>\n<color=#FF6060>{error}</color>";
        }
    }
}
