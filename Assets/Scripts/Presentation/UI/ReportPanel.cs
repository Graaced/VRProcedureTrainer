using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRPT.Core;

namespace VRPT.Presentation
{
    /**
     * CLASS: ReportPanel
     * DESCRIPTION: Shows the result of the session, with a button to try again.
     * It stays hidden until the procedure ends, and covers the instructions when it appears.
     *
     */
    public sealed class ReportPanel : MonoBehaviour
    {
        /** Rich text color of a passed session and of the good news */
        private const string passedColor = "#60FF90";

        /** Rich text color of a failed session and of the mistakes */
        private const string failedColor = "#FF6060";

        /** Text component of the panel */
        [SerializeField] private TMP_Text reportText;

        /** Button that restarts the session */
        [SerializeField] private Button retryButton;

        /** Most recent mistakes listed; the last one is what ended a failed session */
        [Min(1)]
        [SerializeField] private int maxListedErrors = 5;

        /** Validates the setup and hides the panel until the procedure ends */
        private void Awake()
        {
            if (reportText == null || retryButton == null)
            {
                Debug.LogError($"[{name}] Missing reference to {nameof(reportText)} or {nameof(retryButton)}", this);
                enabled = false;
            }
            else
            {
                retryButton.onClick.AddListener(HandleRetryClicked);

                // Windows paths hold backslashes: "\report" would be read as a carriage return
                reportText.parseCtrlCharacters = false;
            }

            gameObject.SetActive(false);
        }

        /** Stops listening to the button */
        private void OnDestroy()
        {
            if (retryButton != null)
                retryButton.onClick.RemoveListener(HandleRetryClicked);
        }

        /** Shows the report; the save result is the file path, or the reason it could not be saved */
        public void Show(ProcedureReport report, IReadOnlyDictionary<string, string> controlNames, bool isSaved, string saveResult)
        {
            if (!enabled)
                return;

            StringBuilder text = new StringBuilder();

            string outcomeColor = report.IsPassed ? passedColor : failedColor;
            text.AppendLine($"<b>{report.ProcedureTitle}</b>");
            text.AppendLine($"<size=130%><b><color={outcomeColor}>{report.Outcome}</color></b></size>");
            text.AppendLine($"Time: {report.DurationSeconds.ToString("F1", CultureInfo.InvariantCulture)} s");
            text.AppendLine($"Errors: {report.ErrorCount} (allowed: {report.MaxAllowedErrors}), critical: {report.CriticalErrorCount}");

            AppendErrors(text, report.Errors, controlNames);

            text.Append(isSaved
                ? $"<size=65%>Saved to: {saveResult}</size>"
                : $"<size=75%><color={failedColor}>Report not saved: {saveResult}</color></size>");

            reportText.text = text.ToString();
            gameObject.SetActive(true);
        }

        /** Lists the most recent mistakes */
        private void AppendErrors(StringBuilder text, List<ReportErrorEntry> errors, IReadOnlyDictionary<string, string> controlNames)
        {
            int hiddenCount = Mathf.Max(0, errors.Count - maxListedErrors);

            if (hiddenCount > 0)
                text.AppendLine($"<size=75%>... and {hiddenCount} earlier</size>");

            for (int i = hiddenCount; i < errors.Count; i++)
                text.AppendLine($"<size=75%>{FormatError(errors[i], controlNames)}</size>");
        }

        /** Describes one mistake in words */
        private static string FormatError(ReportErrorEntry error, IReadOnlyDictionary<string, string> controlNames)
        {
            string label = error.Type == nameof(ProcedureErrorType.Critical) ? "Danger" : "Wrong order";
            string control = controlNames.TryGetValue(error.ControlId, out string displayName) ? displayName : error.ControlId;
            string seconds = error.AtSeconds.ToString("F1", CultureInfo.InvariantCulture);

            return $"<color={failedColor}>{label}:</color> {control}, step '{error.StepId}', at {seconds} s";
        }

        /** Restarts the session by loading the scene again */
        private void HandleRetryClicked()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
