using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameFlow : MonoBehaviour
{
    public const string MenuScene = "StartMenu";
    public const string LevelScene = "PlanetRoom";
    public const string EndScene = "EndRoom";

    private static GameFlow instance;

    private TextMeshProUGUI status;
    private PlayerInventory inventory;
    private PlayerHealth player;
    private TMP_FontAsset uiFont;

    private string message;
    private float messageUntil;
    private float fallLimit;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        GameObject host = new GameObject("Game Flow");
        instance = host.AddComponent<GameFlow>();

        DontDestroyOnLoad(host);
        SceneManager.sceneLoaded += instance.OnSceneLoaded;
    }

    private void Awake()
    {
        uiFont = Resources.Load<TMP_FontAsset>("Fonts/MenuFont SDF");

        if (uiFont == null)
        {
            uiFont = TMP_Settings.defaultFontAsset;
        }
    }

    public static bool LoadScene(string scene)
    {
        if (string.IsNullOrWhiteSpace(scene) ||
            !Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError(
                $"Scene '{scene}' is not enabled in Build Profiles.");

            Notify("Unable to open this area. Check the scene configuration.");
            return false;
        }

        pauseMenu.ResetPauseState();
        SceneManager.LoadScene(scene);
        return true;
    }

    public static void Notify(string text)
    {
        if (instance == null)
            return;

        instance.message = text;
        instance.messageUntil = Time.unscaledTime + 4f;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
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

        fallLimit = player != null
            ? player.transform.position.y - 30f
            : float.NegativeInfinity;

        foreach (var map in
            FindObjectsByType<UnityEngine.Tilemaps.TilemapCollider2D>())
        {
            fallLimit = Mathf.Min(fallLimit, map.bounds.min.y - 10f);
        }

        EnsureEventSystem();
        BuildOverlay(scene.name);
    }

    private void Update()
    {
        if (player != null &&
            Time.timeScale > 0f &&
            player.transform.position.y < fallLimit)
        {
            LoadScene(LevelScene);
            return;
        }

        if (status == null)
            return;

        if (Time.unscaledTime < messageUntil &&
            !string.IsNullOrEmpty(message))
        {
            status.text = message;
        }
        else if (inventory != null && inventory.hasKey)
        {
            status.text = "Key collected! Find the exit door.";
        }
        else
        {
            status.text = "Find the golden key, then reach the door.";
        }
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject events = new GameObject(
            "EventSystem",
            typeof(EventSystem)
        );

        events.AddComponent<InputSystemUIInputModule>()
            .AssignDefaultActions();
    }

    private void BuildOverlay(string scene)
    {
        status = null;

        GameObject canvasObject = new GameObject(
            "Game Help",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        Transform parent = canvasObject.transform;

        if (scene == LevelScene)
        {
            RectTransform controls = Panel(
                parent,
                "Controls",
                new Vector2(1, 1),
                new Vector2(-18, -18),
                new Vector2(430, 90)
            );

            Label(
                controls,
                "A/D or arrows: Move\n" +
                "W/Space/Up: Jump\n" +
                "Esc:Pause/Resume",
                19
            );

            RectTransform objective = Panel(
                parent,
                "Objective",
                new Vector2(0.5f, 0),
                new Vector2(0, 18),
                new Vector2(760, 60)
            );

            status = Label(objective, "", 22);
        }
        else if (scene == MenuScene)
        {
            RectTransform help = Panel(
                parent, "Instructions window", new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(820, 300));

            TextMeshProUGUI instructions = Label(
                help,
                "HOW TO PLAY\n" +
                "Find the key, avoid the enemies, and reach the door.\n" +
                "Move: A/D or arrows   Jump: Space/W/Up   Pause: Esc",
                24);
            instructions.rectTransform.anchorMin = new Vector2(0, 0.3f);
            instructions.lineSpacing = 10f;

            Button instructionsButton = null;
            Button closeButton = AddButton(
                help, "Close", new Vector2(0.5f, 0.16f), () =>
                {
                    help.gameObject.SetActive(false);
                    EventSystem.current.SetSelectedGameObject(
                        instructionsButton.gameObject);
                });

            // A full-screen backdrop blocks clicks on the menu while help is open.
            RectTransform backdrop = Panel(
                parent, "Instructions backdrop", new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = Vector2.zero;
            backdrop.offsetMax = Vector2.zero;
            backdrop.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
            backdrop.GetComponent<Image>().raycastTarget = true;
            help.SetParent(backdrop, false);

            StartMenuScript menuController = FindAnyObjectByType<StartMenuScript>();
            Button playButton = menuController != null
                ? menuController.GetComponentInChildren<Button>() : null;

            closeButton.onClick.AddListener(() =>
            {
                backdrop.gameObject.SetActive(false);
                if (playButton != null) playButton.interactable = true;
            });

            instructionsButton = AddButton(
                parent, "Instructions", new Vector2(0.5f, 0), () =>
                {
                    backdrop.gameObject.SetActive(true);
                    help.gameObject.SetActive(true);
                    backdrop.SetAsLastSibling();
                    if (playButton != null) playButton.interactable = false;
                    EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
                });
            instructionsButton.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(0, 65);
            backdrop.gameObject.SetActive(false);

            StartMenuScript menu = FindAnyObjectByType<StartMenuScript>();

            if (menu != null)
            {
                Button play = menu.GetComponentInChildren<Button>();

                if (play != null)
                {
                    EventSystem.current.SetSelectedGameObject(
                        play.gameObject);
                }
            }
        }
        else if (scene == EndScene)
        {
            RectTransform card = Panel(
                parent,
                "Escape complete",
                new Vector2(0.5f, 0),
                new Vector2(0, 24),
                new Vector2(580, 160)
            );

            TextMeshProUGUI title = Label(card, "You escaped!", 30);
            title.rectTransform.anchorMin = new Vector2(0, 0.62f);

            Button replay = AddButton(
                card,
                "Play again",
                new Vector2(0.27f, 0.3f),
                () => LoadScene(LevelScene)
            );

            AddButton(
                card,
                "Main menu",
                new Vector2(0.73f, 0.3f),
                () => LoadScene(MenuScene)
            );

            EventSystem.current.SetSelectedGameObject(replay.gameObject);
        }
    }

    private static RectTransform Panel(
        Transform parent,
        string name,
        Vector2 anchor,
        Vector2 position,
        Vector2 size)
    {
        GameObject panel = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image)
        );

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.10f, 0.02f, 0.15f, 0.9f);
        image.raycastTarget = false;

        return rect;
    }

    private TextMeshProUGUI Label(
        Transform parent,
        string text,
        float size)
    {
        GameObject textObject = new GameObject(
            "Text",
            typeof(RectTransform)
        );

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12, 8);
        rect.offsetMax = new Vector2(-12, -8);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();

        label.font = uiFont;
        label.text = text;
        label.fontSize = size;
        label.color = new Color(1f, 0.9f, 1f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        return label;
    }

    private Button AddButton(
        Transform parent,
        string text,
        Vector2 anchor,
        UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = Panel(
            parent,
            text,
            anchor,
            Vector2.zero,
            new Vector2(220, 52)
        );

        rect.pivot = new Vector2(0.5f, 0.5f);

        Image image = rect.GetComponent<Image>();
        image.raycastTarget = true;
        image.color = new Color(0.35f, 0.06f, 0.5f);

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        Label(rect, text, 22);

        return button;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (instance == this)
        {
            instance = null;
        }
    }
}