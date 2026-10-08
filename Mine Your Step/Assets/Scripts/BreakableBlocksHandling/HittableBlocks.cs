//using Unity.Cinemachine;
//using Unity.VisualScripting;
//using UnityEngine;
//using UnityEngine.Events;
//using UnityEngine.Rendering;

//public class HittableBlocks : MonoBehaviour
//{
//    [SerializeField] private UnityEvent hit;


//    private void OnCollisionEnter2D(Collision2D collision)
//    {
//        var player = collision.collider.GetComponent<MinerController>();

//        if( (bool) player && collision.contacts[0].normal.y > 0)
//        {
//            hit?.Invoke();
//        }
//    }
//}

using UnityEngine;
using UnityEngine.Events;

public class HittableBlocks : MonoBehaviour
{
    [SerializeField] private UnityEvent hit;

    [Tooltip("If ticked, bumping into the block from above fires the Hit event. Untick this on jewel blocks so the event only fires when mining finishes.")]
    [SerializeField] private bool hitOnPlayerCollision = true;

    // Public so other scripts (e.g. MineableJewel) can trigger the same event.
    public void Hit()
    {
        hit?.Invoke();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!hitOnPlayerCollision) return;

        var player = collision.collider.GetComponent<MinerController>();

        if ((bool)player && collision.contacts[0].normal.y > 0)
        {
            Hit();
        }
    }
}
