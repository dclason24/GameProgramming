using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Shared launch/navigation and small, resolution-independent help overlay.
public class GameFlow : MonoBehaviour
{
    public const string MenuScene = "StartMenu";
    public const string LevelScene = "PlanetRoom";
    public const string EndScene = "EndRoom";
    static GameFlow instance;
    TextMeshProUGUI status;
    string message;
    float messageUntil;
    PlayerInventory inventory;
    PlayerHealth player;
    float fallLimit;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;
        var host = new GameObject("Game Flow");
        instance = host.AddComponent<GameFlow>();
        DontDestroyOnLoad(host);
        SceneManager.sceneLoaded += instance.OnSceneLoaded;
    }

    public static bool LoadScene(string scene)
    {
        if (string.IsNullOrWhiteSpace(scene) || !Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError($"Scene '{scene}' is not enabled in Build Profiles.");
            Notify("Unable to open this area. Check the scene configuration.");
            return false;
        }
        pauseMenu.ResetPauseState();
        SceneManager.LoadScene(scene);
        return true;
    }

    public static void Notify(string text)
    {
        if (instance == null) return;
        instance.message = text;
        instance.messageUntil = Time.unscaledTime + 4f;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "SampleScene")
        {
            LoadScene(MenuScene);
            return;
        }
        pauseMenu.ResetPauseState();
        message = null;
        inventory = FindAnyObjectByType<PlayerInventory>();
        player = FindAnyObjectByType<PlayerHealth>();
        // A fall below the level should restart rather than leave an unreachable player.
        fallLimit = player != null ? player.transform.position.y - 30f : float.NegativeInfinity;
        foreach (var map in FindObjectsByType<UnityEngine.Tilemaps.TilemapCollider2D>())
            fallLimit = Mathf.Min(fallLimit, map.bounds.min.y - 10f);

        EnsureEventSystem();
        BuildOverlay(scene.name);
    }

    void Update()
    {
        if (player != null && Time.timeScale > 0f && player.transform.position.y < fallLimit)
        {
            LoadScene(LevelScene);
            return;
        }
        if (status == null) return;
        status.text = Time.unscaledTime < messageUntil && !string.IsNullOrEmpty(message)
            ? message
            : inventory != null && inventory.hasKey
                ? "KEY COLLECTED  |  Find the exit door"
                : "OBJECTIVE  |  Collect the golden key, then reach the door";
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var events = new GameObject("EventSystem", typeof(EventSystem));
        events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    void BuildOverlay(string scene)
    {
        status = null;
        var canvasObject = new GameObject("Game Help", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.GetComponent<Canvas>().sortingOrder = 50;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        var parent = canvasObject.transform;

        if (scene == LevelScene)
        {
            var controls = Panel(parent, "Controls", new Vector2(1, 1), new Vector2(-18, -18), new Vector2(410, 82));
            Label(controls, "A / D or arrows: move   W / Space / Up: jump\nEsc / controller Start: pause", 19);
            var objective = Panel(parent, "Objective", new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(760, 52));
            status = Label(objective, "", 20);
        }
        else if (scene == MenuScene)
        {
            var help = Panel(parent, "How to play", new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(850, 100));
            Label(help, "Find the golden key and escape through the door. Avoid enemies!\nMove: A / D or arrows     Jump: W / Space / Up     Pause: Esc\nController: left stick / D-pad, bottom button, Start", 20);
            var play = FindAnyObjectByType<StartMenuScript>()?.GetComponentInChildren<Button>();
            if (play != null) EventSystem.current.SetSelectedGameObject(play.gameObject);
        }
        else if (scene == EndScene)
        {
            var card = Panel(parent, "Escape complete", new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(580, 150));
            var title = Label(card, "You escaped!", 28);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0, 0.62f);
            var replay = AddButton(card, "Play again", new Vector2(0.27f, 0.3f), () => LoadScene(LevelScene));
            AddButton(card, "Main menu", new Vector2(0.73f, 0.3f), () => LoadScene(MenuScene));
            EventSystem.current.SetSelectedGameObject(replay.gameObject);
        }
    }

    static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var image = go.GetComponent<Image>();
        image.color = new Color(0.025f, 0.045f, 0.09f, 0.92f);
        image.raycastTarget = false;
        return rect;
    }

    static TextMeshProUGUI Label(Transform parent, string text, float size)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12, 8);
        rect.offsetMax = new Vector2(-12, -8);
        var label = go.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = size;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    static Button AddButton(Transform parent, string text, Vector2 anchor, UnityEngine.Events.UnityAction action)
    {
        var rect = Panel(parent, text, anchor, Vector2.zero, new Vector2(220, 52));
        rect.pivot = new Vector2(0.5f, 0.5f);
        var image = rect.GetComponent<Image>();
        image.raycastTarget = true;
        image.color = new Color(0.08f, 0.3f, 0.44f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        Label(rect, text, 22);
        return button;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }
}
