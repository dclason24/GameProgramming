using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] PlayerHealth health;
    [SerializeField] Shield shield;

    [Header("Hearts")]
    [Tooltip("Left to right: PlayerHartFull(1), (2), (3).")]
    [SerializeField] GameObject[] hearts;

    [Header("Shield")]
    [Tooltip("Left to right: PlayerShield(1), (2), (3).")]
    [SerializeField] Image[] shieldSegments;

    [Tooltip("Show the recharging segment filling up. Requires Image Type = Filled on the segments.")]
    [SerializeField] bool showRechargeFill = true;

    void OnEnable()
    {
        if (health != null) health.OnChanged += Refresh;
        if (shield != null) shield.OnChanged += Refresh;
    }

    void OnDisable()
    {
        if (health != null) health.OnChanged -= Refresh;
        if (shield != null) shield.OnChanged -= Refresh;
    }

    void Start() => Refresh();

    void Refresh()
    {
        if (health != null)
            for (int i = 0; i < hearts.Length; i++)
                hearts[i].SetActive(i < health.Health);

        if (shield != null)
            for (int i = 0; i < shieldSegments.Length; i++)
            {
                Image seg = shieldSegments[i];

                if (i < shield.Hits)
                {
                    seg.enabled = true;
                    seg.fillAmount = 1f;
                }
                else if (showRechargeFill && seg.type == Image.Type.Filled
                         && i == shield.Hits && shield.IsRecharging)
                {
                    seg.enabled = true;
                    seg.fillAmount = shield.RegenProgress;
                }
                else
                {
                    seg.enabled = false;
                }
            }
    }
}