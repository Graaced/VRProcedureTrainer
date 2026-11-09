using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using VRPT.Core;

namespace VRPT.Presentation
{
    /**
     * CLASS: ReportWriter
     * DESCRIPTION: Saves a report to disk as readable JSON.
     * A failed save is reported and never thrown: the user must still see the result on the panel.
     *
     */
    public sealed class ReportWriter
    {
        /** Name of each file; the date keeps every session in its own file */
        private const string fileNameFormat = "'report_'yyyyMMdd_HHmmss'.json'";

        /** Folder where the reports are saved */
        private readonly string directoryPath;

        /** Creates a writer for a folder, which is created at the first save */
        public ReportWriter(string directoryPath)
        {
            this.directoryPath = directoryPath;
        }

        /** Saves the report; the result is the file path, or the reason it could not be saved */
        public bool TrySave(ProcedureReport report, out string result)
        {
            string filePath = Path.Combine(directoryPath, DateTime.Now.ToString(fileNameFormat, CultureInfo.InvariantCulture));

            try
            {
                Directory.CreateDirectory(directoryPath);
                File.WriteAllText(filePath, JsonUtility.ToJson(report, true));

                result = filePath;
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogError($"[{nameof(ReportWriter)}] Could not save the report to '{filePath}': {exception}");

                result = exception.Message;
                return false;
            }
        }
    }
}
