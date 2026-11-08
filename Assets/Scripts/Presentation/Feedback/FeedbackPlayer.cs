using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace VRPT.Presentation
{
    /**
     * CLASS: FeedbackPlayer
     * DESCRIPTION: Confirms the outcome of an action with a sound and a controller vibration.
     * The user may not be looking at the panel, so a mistake must be noticed without reading it.
     *
     */
    [RequireComponent(typeof(AudioSource))]
    public sealed class FeedbackPlayer : MonoBehaviour
    {
        /** Sound of a step completed correctly */
        [SerializeField] private AudioClip successClip;

        /** Sound of a mistake */
        [SerializeField] private AudioClip errorClip;

        /** Haptic players of the two controllers */
        [SerializeField] private HapticImpulsePlayer[] controllers;

        /** Vibration strength for a correct step, from 0 to 1 */
        [Range(0f, 1f)]
        [SerializeField] private float successAmplitude = 0.3f;

        /** Vibration length for a correct step, in seconds */
        [Min(0f)]
        [SerializeField] private float successDuration = 0.08f;

        /** Vibration strength for a mistake, from 0 to 1 */
        [Range(0f, 1f)]
        [SerializeField] private float errorAmplitude = 0.8f;

        /** Vibration length for a mistake, in seconds */
        [Min(0f)]
        [SerializeField] private float errorDuration = 0.25f;

        /** Plays the sounds */
        private AudioSource audioSource;

        /** Finds the audio source and validates the setup */
        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();

            if (successClip == null || errorClip == null || !AreControllersAssigned())
            {
                Debug.LogError($"[{name}] Missing a sound or a controller reference", this);
                enabled = false;
            }
        }

        /** Confirms a correct step */
        public void PlaySuccess()
        {
            Play(successClip, successAmplitude, successDuration);
        }

        /** Signals a mistake */
        public void PlayError()
        {
            Play(errorClip, errorAmplitude, errorDuration);
        }

        /** Plays a sound and vibrates both controllers, since the event does not tell which hand acted */
        private void Play(AudioClip clip, float amplitude, float duration)
        {
            if (!enabled)
                return;

            audioSource.PlayOneShot(clip);

            int sentCount = 0;
            foreach (HapticImpulsePlayer controller in controllers)
            {
                if (controller.SendHapticImpulse(amplitude, duration))
                    sentCount++;
            }

            // The simulator cannot vibrate, so the log is the only way to check the impulse there
            Debug.Log($"[{name}] Haptic impulse {amplitude:F2} for {duration:F2}s sent to {sentCount} of {controllers.Length} controllers", this);
        }

        /** True if there is at least one controller and every slot is filled */
        private bool AreControllersAssigned()
        {
            if (controllers == null || controllers.Length == 0)
                return false;

            foreach (HapticImpulsePlayer controller in controllers)
            {
                if (controller == null)
                    return false;
            }

            return true;
        }
    }
}
