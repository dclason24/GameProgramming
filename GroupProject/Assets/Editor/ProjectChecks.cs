using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ProjectChecks
{
    [MenuItem("Game/Play from Main Menu")]
    public static void PlayGame()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/StartMenu.unity");
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("Game/Validate Connections")]
    public static void Validate()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before validating.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();
            Require(scenes.Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path))
                .SequenceEqual(new[] { GameFlow.MenuScene, GameFlow.LevelScene, GameFlow.EndScene }), "Build scene order");
            foreach (var entry in scenes)
            {
                var scene = EditorSceneManager.OpenScene(entry.path);
                foreach (var root in scene.GetRootGameObjects()) CheckHierarchy(root);
                if (scene.name == GameFlow.MenuScene)
                {
                    Require(UnityEngine.Object.FindAnyObjectByType<StartMenuScript>() != null, "Start menu controller");
                    Require(UnityEngine.Object.FindObjectsByType<Button>()
                        .Any(b => b.onClick.GetPersistentMethodName(0) == "PlayGame"), "Play button");
                }
                if (scene.name == GameFlow.LevelScene) CheckLevel();
                Debug.Log($"CHECK PASS: {entry.path}");
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                CheckHierarchy(prefab);
            }
            Debug.Log("PROJECT CONNECTION CHECKS PASSED");
        }
        finally
        {
            if (setup.Any(s => s.isLoaded && s.isActive)) EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    static void CheckHierarchy(GameObject root)
    {
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0,
                $"Missing script: {transform.name}");
            foreach (var button in transform.GetComponents<Button>())
                for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                {
                    Require(button.onClick.GetPersistentTarget(i) != null, $"Button target: {transform.name}");
                    var method = button.onClick.GetPersistentMethodName(i);
                    Require(!string.IsNullOrEmpty(method) && button.onClick.GetPersistentTarget(i).GetType().GetMethod(method) != null,
                        $"Button method: {transform.name}/{method}");
                }
        }
    }

    static void CheckLevel()
    {
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        Require(player != null && player.CompareTag("Player"), "Player controller/tag");
        Require(player.GetComponent<Rigidbody2D>() != null && player.GetComponent<Collider2D>() != null, "Player physics");
        Require(player.GetComponent<PlayerInventory>() != null, "Player inventory");
        Require(player.GetComponent<PlayerHealth>() != null && player.GetComponent<Shield>() != null, "Player health/shield");
        var data = new SerializedObject(player);
        foreach (var field in new[] { "groundCheck", "moveAction", "jumpAction" })
            Require(data.FindProperty(field).objectReferenceValue != null, "Player " + field);
        foreach (var field in new[] { "moveAction", "jumpAction" })
        {
            var reference = (InputActionReference)data.FindProperty(field).objectReferenceValue;
            Require(reference.action != null && reference.action.bindings.Count > 0, "Input binding " + field);
        }
        Require(data.FindProperty("groundLayer").intValue != 0, "Ground layer mask");
        var anim = player.GetComponent<Animator>();
        Require(anim != null && anim.parameters.Any(p => p.name == "isGrounded" && p.type == AnimatorControllerParameterType.Bool)
            && anim.parameters.Any(p => p.name == "Speed" && p.type == AnimatorControllerParameterType.Float), "Animation parameters");
        Require(UnityEngine.Object.FindObjectsByType<Camera>().Count(c => c.enabled) == 1, "One active camera");
        var followCamera = UnityEngine.Object.FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
        Require(followCamera != null && followCamera.Follow == player.transform, "Camera follows player");
        var confiner = followCamera.GetComponent<Unity.Cinemachine.CinemachineConfiner2D>();
        Require(confiner != null && confiner.BoundingShape2D != null, "Camera boundary reference");
        Require(UnityEngine.Object.FindObjectsByType<EventSystem>().Length == 1, "Exactly one UI input event system");
        var hud = UnityEngine.Object.FindAnyObjectByType<PlayerHUD>();
        Require(hud != null, "HUD");
        var hudData = new SerializedObject(hud);
        foreach (var name in new[] { "health", "shield" }) Require(hudData.FindProperty(name).objectReferenceValue != null, "HUD " + name);
        foreach (var name in new[] { "hearts", "shieldSegments" })
        {
            var array = hudData.FindProperty(name);
            Require(array.arraySize == 3, "HUD " + name + " count");
            for (int i = 0; i < array.arraySize; i++) Require(array.GetArrayElementAtIndex(i).objectReferenceValue != null, "HUD " + name + " reference");
        }
        var key = UnityEngine.Object.FindAnyObjectByType<KeyCollectible>();
        var exit = UnityEngine.Object.FindAnyObjectByType<LevelExit>();
        Require(key != null && key.GetComponent<Collider2D>().isTrigger, "Key trigger");
        Require(exit != null && exit.GetComponent<Collider2D>().isTrigger && exit.sceneToLoad == GameFlow.EndScene, "Exit connection");
        var pause = UnityEngine.Object.FindAnyObjectByType<pauseMenu>();
        Require(pause != null && pause.pauseMenuUI != null, "Pause panel");
    }

    public static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("CHECK FAILED: " + message);
    }
}
