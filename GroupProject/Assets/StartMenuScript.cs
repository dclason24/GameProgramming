using UnityEngine;

public class StartMenuScript : MonoBehaviour
{
    public void PlayGame() => GameFlow.LoadScene(GameFlow.LevelScene);
}
