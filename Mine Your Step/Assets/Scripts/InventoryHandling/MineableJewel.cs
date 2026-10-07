using UnityEngine;

public class MineableJewel : MonoBehaviour
{
    [Tooltip("Name shown in the inventory. Jewels with the same name stack together.")]
    [SerializeField] private string jewelName = "Jewel";
    [SerializeField, Min(1)] private int amount = 1;
    [Tooltip("How many times the mining animation plays before the jewel is collected.")]
    [SerializeField, Min(1)] private int mineLoops = 3;

    [Header("Optional feedback")]
    [Tooltip("Particles / animation spawned where the jewel was when it's mined.")]
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

        if (mineEffectPrefab != null)
        {
            GameObject fx = Instantiate(mineEffectPrefab, transform.position, Quaternion.identity);
            Destroy(fx, effectLifetime);
        }

        Destroy(gameObject);
    }
}