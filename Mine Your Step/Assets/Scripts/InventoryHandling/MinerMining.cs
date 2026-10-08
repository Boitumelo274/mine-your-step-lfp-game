using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class MinerMining : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("The 'Mine' action from your Input Actions asset.")]
    [SerializeField] private InputActionReference mineAction;

    [Header("Mining")]
    [Tooltip("How far from the miner's centre a jewel can be and still be mineable. The green circle in the Scene view shows it.")]
    [SerializeField, Min(0.1f)] private float mineRange = 2f;
    [Tooltip("Animator Bool parameter. True while mining, false when done.")]
    [SerializeField] private string mineBoolName = "IsMining";
    [Tooltip("Name of the mining state in your Animator Controller.")]
    [SerializeField] private string mineStateName = "mine";
    [Tooltip("Name of your idle state. The miner is sent back to it when mining ends.")]
    [SerializeField] private string idleStateName = "idle";
    [Tooltip("Safety limit in seconds, so the miner can never get stuck mining.")]
    [SerializeField, Min(1f)] private float maxMineSeconds = 15f;
    [Tooltip("Drag your control scripts here (e.g. MinerController). They're switched off while mining so the miner stays put.")]
    [SerializeField] private Behaviour[] disableWhileMining;

    [Tooltip("If ticked, the prompt is shown only until the first time the miner mines something, then never again.")]
    [SerializeField] private bool showPromptOnlyOnce = true;

    private bool promptDismissed;

    [Header("Prompt (optional)")]
    [Tooltip("Object shown while a jewel is in range, e.g. a small text label.")]
    [SerializeField] private GameObject promptObject;
    [SerializeField] private TMP_Text promptText;
    [Tooltip("{0} is replaced with the current key name, so it updates if you change the binding.")]
    [SerializeField] private string promptFormat = "Press {0} to mine";


    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D bodyCollider;
    private PlayerInventory inventory;
    private PlayerHealth health;
    private PlayerRespawn respawn;

    private readonly Collider2D[] hits = new Collider2D[32];
    private ContactFilter2D filter;

    private MineableJewel target;
    private bool isMining;

    public bool IsMining => isMining;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        bodyCollider = GetComponent<Collider2D>();
        inventory = GetComponent<PlayerInventory>();
        health = GetComponent<PlayerHealth>();
        respawn = GetComponent<PlayerRespawn>();

        filter = new ContactFilter2D { useTriggers = true, useLayerMask = false };

        if (inventory == null)
            Debug.LogWarning("[MinerMining] No PlayerInventory on the miner. Mined jewels won't be stored.", this);
    }

    private void OnEnable()
    {
        if (mineAction != null) mineAction.action.Enable();
    }

    private void OnDisable()
    {
        if (mineAction != null) mineAction.action.Disable();
    }

    private void Update()
    {
        bool blocked = isMining
            || (health != null && health.IsDead)
            || (respawn != null && respawn.IsRespawning);

        target = blocked ? null : FindNearestJewel();
        UpdatePrompt();

        if (target != null && mineAction != null && mineAction.action.WasPressedThisFrame() && IsStandingStill())
        {
            StartCoroutine(MineRoutine(target));
        }
    }

    private MineableJewel FindNearestJewel()
    {
        Vector2 center = bodyCollider != null ? (Vector2)bodyCollider.bounds.center : (Vector2)transform.position;
        int count = Physics2D.OverlapCircle(center, mineRange, filter, hits);

        MineableJewel best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            MineableJewel jewel = hits[i].GetComponentInParent<MineableJewel>();
            if (jewel == null || jewel.IsMined) continue;

            float d = Vector2.Distance(center, hits[i].ClosestPoint(center));
            if (d < bestDist)
            {
                bestDist = d;
                best = jewel;
            }
        }
        return best;
    }

    // Stops mining from starting in mid-air.
    private bool IsStandingStill()
    {
        return Mathf.Abs(rb.linearVelocity.y) < 0.1f;
    }

    private void UpdatePrompt()
    {
        bool show = target != null && !promptDismissed;

        if (promptObject != null && promptObject.activeSelf != show) promptObject.SetActive(show);

        if (show && promptText != null && mineAction != null)
        {
            promptText.text = string.Format(promptFormat, mineAction.action.GetBindingDisplayString());
        }
    }

    private IEnumerator MineRoutine(MineableJewel jewel)
    {
        isMining = true;
        if (showPromptOnlyOnce) promptDismissed = true;
        if (promptObject != null) promptObject.SetActive(false);

        // The miner can't move or act for the whole mining sequence.
        SetControl(false);
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            if (HasParameter(mineBoolName)) animator.SetBool(mineBoolName, true);
            else Debug.LogError($"[MinerMining] The Animator has no Bool parameter called '{mineBoolName}'.", this);

            // Jump straight into the mining state by name, so it plays even if a transition is misconfigured.
            animator.CrossFade(mineStateName, 0.05f, 0);
        }

        // Wait until the mining clip has played the jewel's number of loops.
        int loops = jewel.MineLoops;
        bool completed = true;
        bool entered = false;
        float timer = 0f;

        while (timer < maxMineSeconds)
        {
            if (IsInterrupted(jewel)) { completed = false; break; }

            if (animator == null)
            {
                if (timer >= 3f) break;
            }
            else
            {
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);

                if (info.IsName(mineStateName))
                {
                    entered = true;
                    if (info.normalizedTime >= loops) break; // enough loops
                }
                else if (entered)
                {
                    break; // something else pulled the Animator out of the mining state
                }
                else if (timer > 1.5f)
                {
                    string controllerName = animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "NONE";
                    Debug.LogWarning($"[MinerMining] The Animator never entered a state called '{mineStateName}'. Controller on the miner: '{controllerName}'. Animator enabled: {animator.isActiveAndEnabled}.", this);
                    break;
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        // Jewel goes into the inventory.
        if (completed && jewel != null) jewel.Collect(inventory);

        // Tell the Animator to go back to idle, and wait until it has actually left the mining pose.
        if (animator != null)
        {
            if (HasParameter(mineBoolName)) animator.SetBool(mineBoolName, false);

            if (completed)
            {
                animator.CrossFade(idleStateName, 0.05f, 0);

                float wait = 0f;
                while (wait < 1f && animator.GetCurrentAnimatorStateInfo(0).IsName(mineStateName))
                {
                    wait += Time.deltaTime;
                    yield return null;
                }
            }
        }

        // Give control back, unless death / respawn has taken over.
        if (!IsInterrupted(null, ignoreJewel: true)) SetControl(true);

        isMining = false;
    }

    private bool HasParameter(string paramName)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == paramName) return true;
        }
        return false;
    }

    private bool IsInterrupted(MineableJewel jewel, bool ignoreJewel = false)
    {
        return (!ignoreJewel && jewel == null)
            || (health != null && health.IsDead)
            || (respawn != null && respawn.IsRespawning);
    }

    private void SetControl(bool enabled)
    {
        foreach (Behaviour b in disableWhileMining)
        {
            if (b == null) continue;

            // Switching the Animator off freezes the sprite mid-pose, so never do that here.
            if (b is Animator)
            {
                Debug.LogWarning("[MinerMining] An Animator is in the control list. It was skipped, because disabling it freezes the animation. Put only MinerController there.", this);
                continue;
            }

            b.enabled = enabled;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D c = GetComponent<Collider2D>();
        Vector3 center = c != null ? c.bounds.center : transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, mineRange);
    }
}