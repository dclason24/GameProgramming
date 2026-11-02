using UnityEngine;
using UnityEngine.SceneManagement; 

public class LevelExit : MonoBehaviour
{
    public string sceneToLoad; 

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerInventory inventory = collision.GetComponent<PlayerInventory>();
            
            if (inventory != null && inventory.hasKey == true)
            {
                SceneManager.LoadScene(sceneToLoad);
            }
            else
            {
                Debug.Log("The door is locked. You need a key!");
            }
        }
    }
}