using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DelayedAudio : MonoBehaviour
{
    [Header("Audio Settings")]
    public float delayInSeconds = 2f;

    private AudioSource audioSource;

    private void Start()
    {
        // Grabs the Audio Source attached to this same object
        audioSource = GetComponent<AudioSource>();

        // Plays the assigned track after the specified delay
        audioSource.PlayDelayed(delayInSeconds);
    }
}