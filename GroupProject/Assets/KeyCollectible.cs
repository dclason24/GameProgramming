using UnityEngine;

public class KeyCollectible : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        var inventory = collision.GetComponentInParent<PlayerInventory>();
        if (inventory == null || !inventory.CompareTag("Player")) return;
        inventory.hasKey = true;
        GameFlow.Notify("Key collected! Find the door to escape.");
        Destroy(gameObject);
    }
}
