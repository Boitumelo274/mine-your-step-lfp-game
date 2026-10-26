using UnityEngine;

public class MineableJewel : MonoBehaviour, ICheckpointable
{
    [SerializeField] private string jewelName = "";
    [SerializeField, Min(1)] private int amount = 1;
    [SerializeField, Min(1)] private int mineLoops = 3;

    [Header("On mined")]
    [SerializeField] private bool triggerHitEvent = true;
    [SerializeField] private GameObject destroyTarget;

    [Header("Optional feedback")]
    [SerializeField] private GameObject mineEffectPrefab;
    [SerializeField, Min(0.1f)] private float effectLifetime = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip breakSound;
    [SerializeField, Range(0f, 1f)] private float breakVolume = 1f;

    public string JewelName => jewelName;
    public int MineLoops => mineLoops;
    public bool IsMined { get; private set; }

    private GameObject TargetObject => destroyTarget != null ? destroyTarget : gameObject;

    public void Collect(PlayerInventory inventory)
    {
        if (IsMined) return;

        if (inventory != null) inventory.Add(jewelName, amount);

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

        // Rock breaking sound  <- this whole block is what you replace
        AudioClip clipToPlay = breakSound;
        if (clipToPlay == null && GridManager.Instance != null)
            clipToPlay = GridManager.Instance.jewelBreakSound;

        if (clipToPlay != null)
        {
            Vector3 soundPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(clipToPlay, soundPos, breakVolume);
        }

        TileVisualizer tileViz = GetComponent<TileVisualizer>();
        if (tileViz != null && tileViz.isJewel)
        {
            tileViz.ResetToPlainTile();
            IsMined = false;
        }
        else
        {
            IsMined = true;
            TargetObject.SetActive(false); // Switch off so it can be restored
        }
    }

    // --- ICheckpointable ---
    public object CaptureState() => IsMined;

    public void RestoreState(object state)
    {
        IsMined = (bool)state;
        TargetObject.SetActive(!IsMined);
    }
}