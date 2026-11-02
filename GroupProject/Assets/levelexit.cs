using UnityEngine;

public class LevelExit : MonoBehaviour
{
    public string sceneToLoad;
    bool loading;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (loading) return;
        var inventory = collision.GetComponentInParent<PlayerInventory>();
        if (inventory == null || !inventory.CompareTag("Player")) return;
        if (!inventory.hasKey)
        {
            GameFlow.Notify("Door locked. Collect the golden key first.");
            return;
        }
        loading = GameFlow.LoadScene(sceneToLoad);
    }
}
