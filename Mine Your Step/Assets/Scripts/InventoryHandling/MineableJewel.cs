using UnityEngine;

public class MineableJewel : MonoBehaviour
{
    [Tooltip("Name shown in the inventory. Jewels with the same name stack together.")]
    [SerializeField] private string jewelName = "";
    [SerializeField, Min(1)] private int amount = 1;
    [Tooltip("How many times the mining animation plays before the jewel is collected.")]
    [SerializeField, Min(1)] private int mineLoops = 3;

    [Header("On mined")]
    [Tooltip("Runs the block's HittableBlocks Hit event (e.g. Spawner.Spawn for particles) when mining finishes.")]
    [SerializeField] private bool triggerHitEvent = true;
    [Tooltip("What gets destroyed when mined. Leave empty to destroy the object this script is on. If this script is on a child, drag the whole block here.")]
    [SerializeField] private GameObject destroyTarget;

    [Header("Optional feedback")]
    [Tooltip("Extra particles / animation spawned where the jewel was when it's mined (separate from the Hit event).")]
    [SerializeField] private GameObject mineEffectPrefab;
    [SerializeField, Min(0.1f)] private float effectLifetime = 2f;

    public string JewelName => jewelName;
    public int MineLoops => mineLoops;
    public bool IsMined { get; private set; }

    /// <summary>Called by MinerMining when the mining animation has looped enough times.</summary>
    public void Collect(PlayerInventory inventory)
    {
        if (IsMined) return;
        IsMined = true;

        if (inventory != null) inventory.Add(jewelName, amount);

        // Fire the block's Hit event BEFORE destroying, so the Spawner still exists
        // and spawns the particles at the block's position.
        if (triggerHitEvent)
        {
            HittableBlocks block = GetComponent<HittableBlocks>();
            if (block == null) block = GetComponentInParent<HittableBlocks>();
            if (block != null) block.Hit();
        }

        if (mineEffectPrefab != null)
        {
            GameObject fx = Instantiate(mineEffectPrefab, transform.position, Quaternion.identity);
            Destroy(fx, effectLifetime);
        }

        Destroy(destroyTarget != null ? destroyTarget : gameObject);
    }
}