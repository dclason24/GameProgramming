using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Invoked by the batch check command in README. Runs actual Play-mode scene transitions,
// keyboard input, physics triggers, damage, regeneration and restart checks.
[InitializeOnLoad]
public static class PlayModeSmoke
{
    static IEnumerator routine;
    static double deadline;
    static float waitRemaining;
    static int lastFrame;
    static bool failed;
    static Keyboard keyboard;
    static Key[] pendingKeys;
    const string Armed = "Game.PlaySmoke";

    static PlayModeSmoke()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Armed, false)) return;
            Application.logMessageReceived += OnLog;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.runInBackground = true;
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.onBeforeUpdate += InjectInput;
            routine = Exercise();
            deadline = EditorApplication.timeSinceStartup + 120;
            waitRemaining = 0;
            lastFrame = -1;
            EditorApplication.update += Tick;
        };
    }

    public static void Run()
    {
        SessionState.SetBool(Armed, true);
        if (!Application.isBatchMode)
        {
            var gameView = EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
            gameView.Show();
            gameView.Focus();
        }
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.EnterPlaymode();
    }

    static void OnLog(string message, string trace, LogType type)
    {
        // Unity 6000.5.9f1 can throw in its Search index on batch startup.
        // Keep this known editor-only failure separate from the game runtime.
        if (trace.Contains("UnityEditor.Search.SearchDatabase"))
        {
            Debug.Log("EDITOR INFRASTRUCTURE: Unity Search index startup exception (not game code).");
            return;
        }
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert
            || type == LogType.Warning)
            failed = true;
    }

    static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Smoke test timed out");
            EditorApplication.QueuePlayerLoopUpdate();
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            waitRemaining -= Time.timeScale > 0f ? Time.deltaTime : Time.unscaledDeltaTime;
            if (waitRemaining > 0f) return;
            if (!routine.MoveNext())
            {
                ProjectChecks.Require(!failed, "No runtime errors or missing-parameter warnings");
                Debug.Log("PLAY MODE SMOKE CHECKS PASSED");
                Finish(0);
            }
            else waitRemaining = routine.Current is float seconds ? seconds : 0.1f;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Finish(1);
        }
    }

    static void Finish(int code)
    {
        SessionState.SetBool(Armed, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        InputSystem.onBeforeUpdate -= InjectInput;
        EditorApplication.Exit(code);
    }

    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindAnyObjectByType<T>();
    static void Check(bool value, string name) { ProjectChecks.Require(value, name); Debug.Log("SMOKE PASS: " + name); }
    static void Keys(params Key[] keys) => pendingKeys = keys;
    static void InjectInput()
    {
        if (InputState.currentUpdateType != UnityEngine.InputSystem.LowLevel.InputUpdateType.Dynamic || pendingKeys == null) return;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(pendingKeys));
        pendingKeys = null;
    }
    static void Click(string method)
    {
        var button = UnityEngine.Object.FindObjectsByType<Button>()
            .First(b => Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Any(i => b.onClick.GetPersistentMethodName(i) == method));
        button.onClick.Invoke();
    }
    static void Touch(Collider2D target)
    {
        var player = Find<PlayerController>();
        var body = player.GetComponent<Rigidbody2D>();
        var collider = player.GetComponent<Collider2D>();
        body.linearVelocity = Vector2.zero;
        body.position += (Vector2)(target.bounds.center - collider.bounds.center);
        Physics2D.SyncTransforms();
    }

    static IEnumerator Exercise()
    {
        yield return 1f;
        Check(SceneManager.GetActiveScene().name == GameFlow.MenuScene, "SampleScene opens main menu");
        Check(Find<StartMenuScript>() != null, "Menu is visible/active");
        if (!Application.isBatchMode) ScreenCapture.CaptureScreenshot("/private/tmp/game-menu.png");
        yield return 0.5f;
        var instructionsButton = GameObject.Find("Instructions").GetComponent<Button>();
        instructionsButton.onClick.Invoke();
        yield return 0.1f;
        Check(GameObject.Find("Instructions window") != null, "Instructions opens");
        Check(Find<StartMenuScript>().GetComponentInChildren<Button>().interactable == false,
            "Instructions prevents starting game behind window");
        GameObject.Find("Close").GetComponent<Button>().onClick.Invoke();
        Check(GameObject.Find("Instructions window") == null, "Instructions closes");
        Check(Find<StartMenuScript>().GetComponentInChildren<Button>().interactable,
            "Play is enabled after closing instructions");
        Check(Resources.Load<TMP_FontAsset>("Fonts/MenuFont SDF") != null,
            "Custom UI font is included");
        Click("PlayGame");
        yield return 1f;
        Check(SceneManager.GetActiveScene().name == GameFlow.LevelScene, "Play button loads PlanetRoom");
        var enemies = UnityEngine.Object.FindObjectsByType<EnemyPatrol>()
            .OrderBy(e => e.transform.position.x).ToArray();
        Check(enemies.Length == 2, "Two enemies are present");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        Check(enemyLayer >= 0 && enemies.All(e => e.gameObject.layer == enemyLayer)
            && Physics2D.GetIgnoreLayerCollision(enemyLayer, enemyLayer),
            "Enemies ignore each other");
        Check(enemies[0].GetComponent<Rigidbody2D>().linearVelocity.x > 0
            && enemies[1].GetComponent<Rigidbody2D>().linearVelocity.x < 0,
            "Enemies start moving toward each other");
        Check(enemies[0].GetComponent<SpriteRenderer>().flipX
            && !enemies[1].GetComponent<SpriteRenderer>().flipX,
            "Enemies face toward each other");
        Check(enemies.All(e => e.GetComponent<Animator>().runtimeAnimatorController != null
            && e.GetComponent<Animator>().GetFloat("Speed") > 0),
            "Enemy walking animation receives speed");
        var player = Find<PlayerController>();
        var body = player.GetComponent<Rigidbody2D>();
        float x = body.position.x;
        Keys(Key.D);
        yield return 0.25f;
        Keys();
        yield return 0.1f;
        Check(body.position.x > x + 0.25f, "D moves player using configured input action");
        Keys(Key.Space);
        yield return 0.12f;
        Check(body.linearVelocity.y > 0f, "Space jumps from ground");
        Keys();
        yield return 1f;
        if (!Application.isBatchMode) ScreenCapture.CaptureScreenshot("/private/tmp/game-level.png");
        yield return 0.4f;
        Keys(Key.Escape);
        yield return 0.15f;
        Keys();
        Check(pauseMenu.GameIsPaused && Time.timeScale == 0f, "Escape pauses");
        Keys(Key.Space);
        yield return 0.15f;
        Keys();
        Click("Resume");
        yield return 0.15f;
        Check(!pauseMenu.GameIsPaused && Time.timeScale == 1f && body.linearVelocity.y <= 0.1f, "Resume does not replay jump pressed while paused");
        Find<pauseMenu>().Pause();
        Click("MainMenu");
        yield return 0.5f;
        Click("PlayGame");
        yield return 0.5f;
        Keys(Key.Escape);
        yield return 0.15f;
        Keys();
        Check(pauseMenu.GameIsPaused, "First Escape works after returning from paused game");
        Click("Resume");
        yield return 0.15f;
        var exit = Find<LevelExit>();
        Touch(exit.GetComponent<Collider2D>());
        yield return 0.2f;
        Check(SceneManager.GetActiveScene().name == GameFlow.LevelScene, "Door stays locked without key");
        Check(UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>().Any(t => t.text.Contains("Door locked")), "Locked door shows visible feedback");
        var key = Find<KeyCollectible>();
        Touch(key.GetComponent<Collider2D>());
        yield return 0.2f;
        Check(Find<PlayerInventory>().hasKey && Find<KeyCollectible>() == null, "Physics trigger collects key");
        Touch(exit.GetComponent<Collider2D>());
        yield return 0.5f;
        Check(SceneManager.GetActiveScene().name == GameFlow.EndScene, "Key unlocks door to EndRoom");
        Check(UnityEngine.EventSystems.EventSystem.current != null, "Ending has UI input");
        if (!Application.isBatchMode) ScreenCapture.CaptureScreenshot("/private/tmp/game-ending.png");
        yield return 0.5f;
        GameObject.Find("Play again").GetComponent<Button>().onClick.Invoke();
        yield return 0.5f;
        Check(SceneManager.GetActiveScene().name == GameFlow.LevelScene && !Find<PlayerInventory>().hasKey, "Replay resets level and inventory");
        // Isolate damage checks from the patrolling enemy, keeping authored health/shield settings.
        Keys();
        Find<EnemyPatrol>().gameObject.SetActive(false);
        Find<LevelExit>().gameObject.SetActive(false);
        Find<KeyCollectible>().gameObject.SetActive(false);
        var damagePlayer = Find<PlayerController>();
        damagePlayer.enabled = false;
        damagePlayer.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
        var health = Find<PlayerHealth>();
        var shield = Find<Shield>();
        health.TakeDamage(1);
        Check(shield.Hits == 2 && health.Health == 3, "Shield absorbs contact damage");
        health.TakeDamage(1);
        Check(shield.Hits == 2, "Invincibility blocks repeated damage");
        yield return 7.5f;
        Check(shield.Hits == 3, "Shield regenerates after delay");
        for (int i = 0; i < 3; i++)
        {
            health.TakeDamage(1);
            yield return 3.3f;
        }
        Check(shield.Hits == 0, "Repeated spaced hits deplete shield");
        health.TakeDamage(1);
        Check(health.Health == 2, "Damage reaches health with empty shield");
        yield return 3.3f;
        health.TakeDamage(3);
        yield return 0.5f;
        Check(Find<PlayerHealth>().Health == 3 && Find<Shield>().Hits == 3, "Death reloads level with full health/shield");
        body = Find<PlayerController>().GetComponent<Rigidbody2D>();
        body.position = new Vector2(body.position.x, -1000f);
        yield return 0.5f;
        Check(Find<PlayerController>().transform.position.y > -100f, "Falling out of level restarts");
        GameFlow.LoadScene(GameFlow.EndScene);
        yield return 0.3f;
        GameObject.Find("Main menu").GetComponent<Button>().onClick.Invoke();
        yield return 0.5f;
        Check(SceneManager.GetActiveScene().name == GameFlow.MenuScene, "Ending returns to main menu");
    }
}
