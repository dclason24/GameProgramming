using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Speed in units per second.")]
    [SerializeField] float speed = 1.5f;

    [Tooltip("Time spent walking before stopping (seconds).")]
    [SerializeField] float walkTime = 3f;

    [Tooltip("Time spent stopped before turning around (seconds).")]
    [SerializeField] float pauseTime = 0.5f;

    [Header("Direction")]
    [Tooltip("Start walking to the right?")]
    [SerializeField] bool startFacingRight = true;

    [Tooltip("Check if the original sprite faces LEFT.")]
    [SerializeField] bool spriteFacesLeft = false;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator anim;
    bool hasSpeedParam;
    int dir;

    static readonly int SpeedHash = Animator.StringToHash("Speed");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();

        if (anim != null && anim.runtimeAnimatorController != null)
            foreach (var p in anim.parameters)
                if (p.nameHash == SpeedHash) { hasSpeedParam = true; break; }

        dir = startFacingRight ? 1 : -1;
        ApplyFlip();
    }

    void OnEnable() => StartCoroutine(PatrolLoop());

    void OnDisable()
    {
        StopAllCoroutines();
        if (rb != null) SetVelocityX(0f);
    }

    IEnumerator PatrolLoop()
    {
        while (true)
        {
            yield return Walk(walkTime);

            SetVelocityX(0f);
            yield return new WaitForSeconds(pauseTime);

            dir = -dir;
            ApplyFlip();
        }
    }

    IEnumerator Walk(float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            SetVelocityX(dir * speed);
            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    void SetVelocityX(float x)
    {
        Vector2 v = rb.linearVelocity;
        v.x = x;
        rb.linearVelocity = v;

        if (hasSpeedParam) anim.SetFloat(SpeedHash, Mathf.Abs(x));
    }

    void ApplyFlip()
    {
        if (sr == null) return;
        sr.flipX = spriteFacesLeft ? dir > 0 : dir < 0;
    }

    void OnDrawGizmosSelected()
    {
        float distance = speed * walkTime;
        int d = Application.isPlaying ? dir : (startFacingRight ? 1 : -1);
        Vector3 start = transform.position;
        Vector3 end = start + Vector3.right * d * distance;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, 0.1f);
    }
}