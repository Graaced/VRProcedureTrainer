using UnityEngine;

namespace VRPT.Presentation
{
    /**
     * CLASS: ApplicationExit
     * DESCRIPTION: Closes the application from any button.
     * Application.Quit does nothing in the editor, so there it stops Play mode instead.
     *
     */
    public static class ApplicationExit
    {
        /** Closes the application, or stops Play mode in the editor */
        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
