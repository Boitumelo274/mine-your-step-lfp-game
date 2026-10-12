using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;   

[RequireComponent(typeof(CinemachineImpulseSource))]
public class ScreenShake : MonoBehaviour
{
    [Header("Shake Settings")]
    [Tooltip("How long the shake lasts, in seconds.")]
    public float duration = 0.5f;

    [Tooltip("Multiplies every shake. 1 = normal.")]
    public float strengthMultiplier = 1f;

    [Header("Testing")]
    public bool enableTestKey = true;

    private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
        ApplyDuration();
    }

    private void OnValidate()
    {
        if (impulseSource == null) impulseSource = GetComponent<CinemachineImpulseSource>();
        if (impulseSource != null) ApplyDuration();
    }

    private void ApplyDuration()
    {
       
        impulseSource.ImpulseDefinition.ImpulseDuration = duration;

        
    }

    private void Update()
    {
        //if (enableTestKey && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        //{
        //    Debug.Log("T pressed, shaking");
        //    Shake(1f);
        //}
    }

    
    public void Shake(float force = 1f)
    {
        impulseSource.GenerateImpulse(force * strengthMultiplier);
    }
}