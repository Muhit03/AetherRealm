using UnityEngine;
using AetherRealm;

/// <summary>
/// Builds the whole game when the scene starts: the managers, the lighting, the
/// camera, the user interface and the arena. Then it waits on the login screen.
/// It adds itself automatically if the scene doesn't already contain one, so all
/// the player has to do is press Play.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameBootstrap : MonoBehaviour
{
    public static GameBootstrap Instance { get; private set; }

    ArenaLayout arena;
    GameObject playerObject;
    CameraFollow cameraFollow;
    bool built;

    // the archers read the cover-wall positions from here
    public ArenaLayout Arena { get { return arena; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateIfMissing()
    {
        if (FindAnyObjectByType<GameBootstrap>() == null)
        {
            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (built)
        {
            return;
        }
        built = true;

        RemoveOldSceneObjects();
        CreateManagers();
        SetUpLighting();
        SetUpCamera();
        UIBuilder.Build();

        arena = ArenaBuilder.Build();
        SpawnProps();
        SpawnNpc();
        PreparePlayer();

        GameManager.Instance.GoToMenu();

        // If the player chose "Continue" on the end screen, the scene reloaded
        // and we jump straight back into the run instead of the menu.
        if (GameManager.ResumeRequested && AuthManager.IsLoggedIn && RunSave.Has())
        {
            BeginRun(AuthManager.CurrentClassType);
        }
        else
        {
            GameManager.ResumeRequested = false;
            Debug.Log("AetherRealm is ready. Log in to start playing.");
        }
    }

    // The original grey-box scene has a broken plane and a leftover goblin.
    void RemoveOldSceneObjects()
    {
        string[] names = { "Plane", "Goblin", "Archer" };
        foreach (string name in names)
        {
            GameObject old = GameObject.Find(name);
            if (old != null)
            {
                Destroy(old);
            }
        }
    }

    void CreateManagers()
    {
        // These stay alive between scene reloads.
        if (DatabaseManager.Instance == null || AuthManager.Instance == null ||
            AudioManager.Instance == null || LeaderboardManager.Instance == null)
        {
            GameObject persistent = new GameObject("Managers (persistent)");
            if (DatabaseManager.Instance == null) persistent.AddComponent<DatabaseManager>();
            if (AuthManager.Instance == null) persistent.AddComponent<AuthManager>();
            if (AudioManager.Instance == null) persistent.AddComponent<AudioManager>();
            if (LeaderboardManager.Instance == null) persistent.AddComponent<LeaderboardManager>();
        }

        // These are rebuilt every run.
        GameObject runManagers = new GameObject("Managers (run)");
        runManagers.AddComponent<GameManager>();
        runManagers.AddComponent<WaveManager>();
    }

    void SetUpLighting()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.20f, 0.24f, 0.34f);
        RenderSettings.ambientEquatorColor = new Color(0.12f, 0.12f, 0.16f);
        RenderSettings.ambientGroundColor = new Color(0.05f, 0.05f, 0.06f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.06f, 0.07f, 0.10f);
        RenderSettings.fogDensity = 0.018f;

        Light sun = null;
        foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Include))
        {
            if (light.type == LightType.Directional)
            {
                sun = light;
                break;
            }
        }
        if (sun == null)
        {
            sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.transform.rotation = Quaternion.Euler(48f, 35f, 0f);
        sun.color = new Color(1f, 0.92f, 0.78f);
        sun.intensity = 1.15f;
        sun.shadows = LightShadows.Soft;
    }

    void SetUpCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
        }

        // the game needs exactly one AudioListener - put it on the camera
        if (camera.GetComponent<AudioListener>() == null)
        {
            camera.gameObject.AddComponent<AudioListener>();
        }

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.03f, 0.04f, 0.06f);
        camera.fieldOfView = 55f;
        camera.farClipPlane = 400f;

        // Put the camera on a "rig" object. The rig follows the player; the
        // camera itself stays still on the rig, which lets the shake effect
        // move only the camera without fighting the follow code.
        GameObject rig = new GameObject("CameraRig");
        rig.transform.position = camera.transform.position;
        camera.transform.SetParent(rig.transform, false);
        camera.transform.localPosition = Vector3.zero;
        camera.transform.localRotation = Quaternion.identity;

        cameraFollow = rig.AddComponent<CameraFollow>();
    }

    void SpawnProps()
    {
        Vector3[] barrelSpots = { new Vector3(3f, 0.75f, 3f), new Vector3(-3.5f, 0.75f, 2.5f), new Vector3(2.5f, 0.75f, -3.5f) };
        foreach (Vector3 spot in barrelSpots)
        {
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel";
            barrel.transform.position = spot;
            barrel.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            barrel.GetComponent<Renderer>().sharedMaterial = Palette.Material(Palette.Wood);
            barrel.AddComponent<DestructibleBarrel>();
        }

        GameObject chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chest.name = "TreasureChest";
        chest.transform.position = new Vector3(0f, 0.6f, 4f);
        chest.transform.localScale = new Vector3(1.4f, 0.9f, 1f);
        chest.GetComponent<Renderer>().sharedMaterial = Palette.Material(Palette.Gold);
        chest.AddComponent<InteractableChest>();
    }

    void SpawnNpc()
    {
        GameObject npc = new GameObject("Elder Eldrin");
        npc.transform.position = arena.npcPoint;

        CapsuleCollider collider = npc.AddComponent<CapsuleCollider>();
        collider.height = 2f;
        collider.center = new Vector3(0f, 1f, 0f);

        CharacterRig rig = CharacterBuilder.Build(npc.transform, new Color(0.25f, 0.35f, 0.55f), Palette.Skin, 1.05f, WeaponType.Staff);
        npc.transform.position = arena.npcPoint + Vector3.up;

        ProceduralAnimator animator = npc.AddComponent<ProceduralAnimator>();
        animator.Setup(rig);

        npc.AddComponent<NPCQuestGiver>();

        Torch.Create(arena.npcPoint + Vector3.up * 2.5f);
    }

    void PreparePlayer()
    {
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            playerObject = new GameObject("Player");
            playerObject.tag = "Player";
        }

        if (playerObject.GetComponent<CharacterController>() == null)
        {
            playerObject.AddComponent<CharacterController>();
        }

        // Keep only the CharacterController as a collider so overlap checks don't
        // count the player twice.
        foreach (Collider collider in playerObject.GetComponents<Collider>())
        {
            if (!(collider is CharacterController))
            {
                DestroyImmediate(collider);
            }
        }

        // Remove any class script; the chosen one is added in BeginRun.
        foreach (PlayerController old in playerObject.GetComponents<PlayerController>())
        {
            DestroyImmediate(old);
        }

        playerObject.transform.position = arena.playerStart;
        playerObject.SetActive(false);
    }

    // Called by the login screen when the player picks "Continue" on a saved
    // run. No scene reload is needed - the scene is already fresh.
    public void BeginRunResume()
    {
        GameManager.ResumeRequested = true;
        if (UIManager.Instance != null) UIManager.Instance.ShowHUD();
        BeginRun(AuthManager.CurrentClassType);
    }

    // Called by the login screen once a class is chosen.
    public void BeginRun(string className)
    {
        if (GameManager.Instance == null || GameManager.Instance.IsPlaying)
        {
            return;
        }

        PlayerController hero;
        if (className == "Mage")
        {
            hero = playerObject.AddComponent<Mage>();
        }
        else
        {
            hero = playerObject.AddComponent<Warrior>();
        }

        playerObject.transform.position = arena.playerStart;
        playerObject.transform.rotation = Quaternion.identity;
        playerObject.SetActive(true);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowHUD();
        }

        if (cameraFollow != null)
        {
            cameraFollow.target = playerObject.transform;
        }

        GameManager.Instance.StartRun(hero);
        WaveManager.Instance.StartWaves(arena);

        if (DialoguePanel.Instance != null)
        {
            DialoguePanel.Instance.Say("Elder Eldrin",
                "Hold the arena, hero. Come to me between waves and I will make you stronger.");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) && WaveManager.Instance != null)
        {
            WaveManager.Instance.SkipShopBreak();
        }
    }
}
