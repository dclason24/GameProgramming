using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [Tooltip("1 point = 1 heart.")]
    [SerializeField] int maxHealth = 3;

    [Header("Invincibility")]
    [Tooltip("Seconds of invincibility after any damage (including when the shield absorbs it).")]
    [SerializeField] float invincibleTime = 3f;

    [Tooltip("Blink interval during invincibility.")]
    [SerializeField] float blinkInterval = 0.1f;

    [Header("References (auto-filled if empty)")]
    [SerializeField] Shield shield;

    [Tooltip("The CHARACTER's SpriteRenderer (not the bubble's).")]
    [SerializeField] SpriteRenderer sprite;

    public int Health { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsInvincible { get; private set; }

    public event Action OnChanged;
    public event Action OnDied;

    void Awake()
    {
        Health = maxHealth;
        if (shield == null) shield = GetComponent<Shield>();
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsInvincible || Health <= 0 || Time.timeScale == 0f) return;

        bool absorbed = shield != null && shield.Absorb();

        if (!absorbed)
        {
            Health = Mathf.Max(0, Health - amount);
            OnChanged?.Invoke();

            if (Health == 0)
            {
                Die();
                return;
            }
        }

        StartCoroutine(InvincibilityRoutine());
    }

    IEnumerator InvincibilityRoutine()
    {
        IsInvincible = true;

        float endTime = Time.time + invincibleTime;
        while (Time.time < endTime)
        {
            if (sprite != null) sprite.enabled = !sprite.enabled;
            yield return new WaitForSeconds(Mathf.Min(Mathf.Max(0.01f, blinkInterval), endTime - Time.time));
        }

        if (sprite != null) sprite.enabled = true;
        IsInvincible = false;
    }

    void Die()
    {
        OnDied?.Invoke();
        GameFlow.LoadScene(SceneManager.GetActiveScene().name);
    }
}