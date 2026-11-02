using System;
using UnityEngine;

public class Shield : MonoBehaviour
{
    [Header("Hits")]
    [SerializeField] int maxHits = 3;

    [Header("Regeneration")]
    [Tooltip("Seconds without taking any hit before regeneration starts.")]
    [SerializeField] float regenDelay = 5f;

    [Tooltip("Seconds to regenerate 1 hit.")]
    [SerializeField] float regenTimePerHit = 2f;

    [Header("Visual")]
    [Tooltip("Child GameObject with the bubble. Active while Hits > 0.")]
    [SerializeField] GameObject bubbleVisual;

    public int Hits { get; private set; }
    public int MaxHits => maxHits;
    public float RegenProgress { get; private set; }
    public bool IsRecharging => Hits < maxHits && timeSinceHit >= regenDelay;

    public event Action OnChanged;

    float timeSinceHit;

    void Awake()
    {
        Hits = maxHits;
        timeSinceHit = regenDelay;
        UpdateVisual();
    }

    void Update()
    {
        if (Hits >= maxHits) return;

        timeSinceHit += Time.deltaTime;
        if (timeSinceHit < regenDelay) return;

        RegenProgress += Time.deltaTime / regenTimePerHit;
        if (RegenProgress >= 1f)
        {
            RegenProgress = 0f;
            Hits++;
            UpdateVisual();
        }
        OnChanged?.Invoke();
    }

    public bool Absorb()
    {
        timeSinceHit = 0f;
        RegenProgress = 0f;

        if (Hits <= 0)
        {
            OnChanged?.Invoke();
            return false;
        }

        Hits--;
        UpdateVisual();
        OnChanged?.Invoke();
        return true;
    }

    void UpdateVisual()
    {
        if (bubbleVisual != null) bubbleVisual.SetActive(Hits > 0);
    }
}