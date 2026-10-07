using System.Collections;
using UnityEngine;

public class BeepingLight : MonoBehaviour
{
    [Header("Light Settings")]
    public SpriteRenderer lightSprite;

    [Header("Timing")]
    public float timeOn = 0.15f; // How long the light stays red (a quick flash)
    public float timeOff = 0.85f; // How long it stays invisible

    private void Start()
    {
        if (lightSprite != null)
        {
            StartCoroutine(BeepSequence());
        }
    }

    private IEnumerator BeepSequence()
    {
        // Loops infinitely while the object is active
        while (true)
        {
            lightSprite.enabled = true;
            yield return new WaitForSeconds(timeOn);

            lightSprite.enabled = false;
            yield return new WaitForSeconds(timeOff);
        }
    }
}