using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameAudioManager : MonoBehaviour
{
    [System.Serializable]
    public class UnitAudioBinding
    {
        [Tooltip("Case-insensitive text matched against UnitData.unitName. Example: fire, frost, zeus.")]
        public string matchText;

        public AudioClip clip;

        [Range(0f, 2f)]
        public float volumeMultiplier = 1f;
    }

    private enum AudioBus
    {
        Ui,
        Gameplay,
        Unit,
        EnemyHit
    }

    public static GameAudioManager Instance { get; private set; }

    [Header("Scene Music")]
    [SerializeField] private string mainMenuSceneName = "Main_UI";
    [SerializeField] private string battleSceneName = "BattleScene";
    [SerializeField] private bool autoPlaySceneMusic = true;
    [SerializeField] private bool persistAcrossScenes = true;

    [Header("Global Controls")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;
    [SerializeField] private bool musicMuted = false;
    [SerializeField] private bool sfxMuted = false;

    [Header("Music Clip Volumes")]
    [Range(0f, 1f)]
    [SerializeField] private float mainMenuMusicVolume = 0.36f;
    [Range(0f, 1f)]
    [SerializeField] private float gameMusicVolume = 0.32f;
    [Range(0f, 1f)]
    [SerializeField] private float battleMusicVolume = 0.34f;

    [Header("SFX Volumes")]
    [Range(0f, 1f)]
    [SerializeField] private float uiVolume = 0.55f;
    [Range(0f, 1f)]
    [SerializeField] private float gameplayVolume = 0.72f;
    [Range(0f, 1f)]
    [SerializeField] private float unitVolume = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float enemyHitVolume = 0.22f;

    [Header("Throttles")]
    [SerializeField] private float minimumUnitSoundInterval = 0.08f;
    [SerializeField] private float minimumEnemyHitInterval = 0.06f;

    [Header("Music Clips")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip gameMusic;
    [SerializeField] private AudioClip battleMusic;

    [Header("UI Clips")]
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip buttonConfirmClip;

    [Header("Gameplay Clips")]
    [SerializeField] private AudioClip summonClip;
    [SerializeField] private AudioClip mergeClip;
    [SerializeField] private AudioClip enemyHitClip;

    [Header("Unit Attack Clips")]
    [SerializeField] private UnitAudioBinding[] unitAttackBindings;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource unitSource;

    [Header("Automation")]
    [SerializeField] private bool autoWireUiButtons = true;
    [SerializeField] private bool loadMissingClipsFromResources = true;

    private readonly HashSet<Button> wiredButtons = new HashSet<Button>();
    private readonly Dictionary<string, UnitAudioBinding> unitBindingLookup = new Dictionary<string, UnitAudioBinding>();

    private AudioClip currentSceneMusic;
    private float currentSceneMusicVolume = 1f;
    private float lastUnitSoundTime = -99f;
    private float lastEnemyHitTime = -99f;
    private bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        GameAudioManager prefab = Resources.Load<GameAudioManager>("GameAudioManager");
        if (prefab != null)
        {
            GameObject managerObject = Instantiate(prefab.gameObject);
            managerObject.name = "GameAudioManager";
            return;
        }

        GameObject fallbackObject = new GameObject("GameAudioManager");
        fallbackObject.AddComponent<GameAudioManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void Start()
    {
        if (autoPlaySceneMusic)
            PlayMusicForScene(SceneManager.GetActiveScene());

        if (autoWireUiButtons)
            WireSceneButtons();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnValidate()
    {
        ClampInspectorValues();
        ApplySourceVolumes();
    }

    public static void PlayButtonClick()
    {
        if (Instance != null)
            Instance.PlayOneShot(Instance.buttonClickClip, AudioBus.Ui, 0.98f, 1.05f);
    }

    public static void PlayButtonConfirm()
    {
        if (Instance != null)
            Instance.PlayOneShot(Instance.buttonConfirmClip, AudioBus.Gameplay, 0.96f, 1.04f);
    }

    public static void PlaySummon()
    {
        if (Instance != null)
            Instance.PlayOneShot(Instance.summonClip, AudioBus.Gameplay, 0.92f, 1.08f);
    }

    public static void PlayMerge()
    {
        if (Instance != null)
            Instance.PlayOneShot(Instance.mergeClip, AudioBus.Gameplay, 0.95f, 1.06f);
    }

    public static void PlayEnemyHit()
    {
        if (Instance == null)
            return;

        float now = Time.unscaledTime;
        if (now - Instance.lastEnemyHitTime < Instance.minimumEnemyHitInterval)
            return;

        Instance.lastEnemyHitTime = now;
        Instance.PlayOneShot(Instance.enemyHitClip, AudioBus.EnemyHit, 0.9f, 1.12f);
    }

    public static void PlayUnitAttack(UnitData unitData)
    {
        PlayUnitAttack(unitData != null ? unitData.unitName : string.Empty);
    }

    public static void PlayUnitAttack(string unitName)
    {
        if (Instance == null)
            return;

        float now = Time.unscaledTime;
        if (now - Instance.lastUnitSoundTime < Instance.minimumUnitSoundInterval)
            return;

        Instance.lastUnitSoundTime = now;

        UnitAudioBinding binding = Instance.GetUnitAttackBinding(unitName);
        AudioClip clip = binding != null ? binding.clip : Instance.buttonConfirmClip;
        float multiplier = binding != null ? binding.volumeMultiplier : 1f;
        Instance.PlayOneShot(clip, AudioBus.Unit, 0.92f, 1.1f, multiplier);
    }

    public static void PlayAbilityClip(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (Instance != null)
            Instance.PlayOneShot(clip, AudioBus.Gameplay, 0.96f, 1.04f, volumeMultiplier);
    }

    public void RefreshButtonHooks()
    {
        if (autoWireUiButtons)
            WireSceneButtons();
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        ApplySourceVolumes();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        ApplySourceVolumes();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        ApplySourceVolumes();
    }

    public void SetMusicMuted(bool isMuted)
    {
        musicMuted = isMuted;
        ApplySourceVolumes();
    }

    public void SetSfxMuted(bool isMuted)
    {
        sfxMuted = isMuted;
        ApplySourceVolumes();
    }

    public void ToggleMusicMuted()
    {
        SetMusicMuted(!musicMuted);
    }

    public void ToggleSfxMuted()
    {
        SetSfxMuted(!sfxMuted);
    }

    public void PlayMainMenuMusic()
    {
        PlayMusic(mainMenuMusic, mainMenuMusicVolume);
    }

    public void PlayGameMusic()
    {
        PlayMusic(gameMusic, gameMusicVolume);
    }

    public void PlayBattleMusic()
    {
        PlayMusic(battleMusic, battleMusicVolume);
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    private void Initialize()
    {
        if (initialized)
            return;

        initialized = true;

        ClampInspectorValues();
        EnsureAudioSources();

        if (loadMissingClipsFromResources)
            LoadMissingDefaultClips();

        BuildUnitLookup();
        ApplySourceVolumes();
    }

    private void ClampInspectorValues()
    {
        minimumUnitSoundInterval = Mathf.Max(0f, minimumUnitSoundInterval);
        minimumEnemyHitInterval = Mathf.Max(0f, minimumEnemyHitInterval);
    }

    private void EnsureAudioSources()
    {
        if (musicSource == null)
            musicSource = CreateSource("MusicSource", true);

        if (sfxSource == null)
            sfxSource = CreateSource("SfxSource", false);

        if (unitSource == null)
            unitSource = CreateSource("UnitSource", false);

        ConfigureSource(musicSource, true);
        ConfigureSource(sfxSource, false);
        ConfigureSource(unitSource, false);
    }

    private AudioSource CreateSource(string sourceName, bool loop)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        ConfigureSource(source, loop);
        return source;
    }

    private static void ConfigureSource(AudioSource source, bool loop)
    {
        if (source == null)
            return;

        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    private void LoadMissingDefaultClips()
    {
        mainMenuMusic = LoadIfMissing(mainMenuMusic, "Audio/Music/main_menu_forest_loop");
        gameMusic = LoadIfMissing(gameMusic, "Audio/Music/game_ambient_loop");
        battleMusic = LoadIfMissing(battleMusic, "Audio/Music/battle_arcane_loop");
        buttonClickClip = LoadIfMissing(buttonClickClip, "Audio/SFX/UI/button_click");
        buttonConfirmClip = LoadIfMissing(buttonConfirmClip, "Audio/SFX/UI/button_confirm");
        summonClip = LoadIfMissing(summonClip, "Audio/SFX/Gameplay/summon_cast");
        mergeClip = LoadIfMissing(mergeClip, "Audio/SFX/Gameplay/merge_burst");
        enemyHitClip = LoadIfMissing(enemyHitClip, "Audio/SFX/Gameplay/enemy_hit");

        if (unitAttackBindings == null || unitAttackBindings.Length == 0)
            unitAttackBindings = CreateDefaultUnitBindings();
        else
            FillMissingUnitBindingClips();
    }

    private UnitAudioBinding[] CreateDefaultUnitBindings()
    {
        return new[]
        {
            CreateBinding("fire", "Audio/SFX/Units/fire_mage_attack"),
            CreateBinding("frost", "Audio/SFX/Units/frost_witch_attack"),
            CreateBinding("ice", "Audio/SFX/Units/frost_witch_attack"),
            CreateBinding("golden", "Audio/SFX/Units/golden_spirit_attack"),
            CreateBinding("spirit", "Audio/SFX/Units/golden_spirit_attack"),
            CreateBinding("magic", "Audio/SFX/Units/magic_archer_attack"),
            CreateBinding("archer", "Audio/SFX/Units/magic_archer_attack"),
            CreateBinding("poison", "Audio/SFX/Units/poison_druid_attack"),
            CreateBinding("druid", "Audio/SFX/Units/poison_druid_attack"),
            CreateBinding("shape", "Audio/SFX/Units/shapeshifter_attack"),
            CreateBinding("stone", "Audio/SFX/Units/stone_guardian_attack"),
            CreateBinding("guardian", "Audio/SFX/Units/stone_guardian_attack"),
            CreateBinding("princess", "Audio/SFX/Units/princess_attack"),
            CreateBinding("enchant", "Audio/SFX/Units/enchantress_attack"),
            CreateBinding("zeus", "Audio/SFX/Units/zeus_attack"),
            CreateBinding("thunder", "Audio/SFX/Units/zeus_attack")
        };
    }

    private static UnitAudioBinding CreateBinding(string matchText, string resourcesPath)
    {
        return new UnitAudioBinding
        {
            matchText = matchText,
            clip = LoadClip(resourcesPath),
            volumeMultiplier = 1f
        };
    }

    private void FillMissingUnitBindingClips()
    {
        for (int i = 0; i < unitAttackBindings.Length; i++)
        {
            UnitAudioBinding binding = unitAttackBindings[i];
            if (binding == null || binding.clip != null || string.IsNullOrEmpty(binding.matchText))
                continue;

            binding.clip = LoadDefaultUnitClip(binding.matchText);
        }
    }

    private static AudioClip LoadDefaultUnitClip(string matchText)
    {
        string normalized = matchText.ToLowerInvariant();

        if (normalized.Contains("fire"))
            return LoadClip("Audio/SFX/Units/fire_mage_attack");
        if (normalized.Contains("frost") || normalized.Contains("ice"))
            return LoadClip("Audio/SFX/Units/frost_witch_attack");
        if (normalized.Contains("gold") || normalized.Contains("spirit"))
            return LoadClip("Audio/SFX/Units/golden_spirit_attack");
        if (normalized.Contains("magic") || normalized.Contains("archer"))
            return LoadClip("Audio/SFX/Units/magic_archer_attack");
        if (normalized.Contains("poison") || normalized.Contains("druid"))
            return LoadClip("Audio/SFX/Units/poison_druid_attack");
        if (normalized.Contains("shape"))
            return LoadClip("Audio/SFX/Units/shapeshifter_attack");
        if (normalized.Contains("stone") || normalized.Contains("guardian"))
            return LoadClip("Audio/SFX/Units/stone_guardian_attack");
        if (normalized.Contains("princess"))
            return LoadClip("Audio/SFX/Units/princess_attack");
        if (normalized.Contains("enchant"))
            return LoadClip("Audio/SFX/Units/enchantress_attack");
        if (normalized.Contains("zeus") || normalized.Contains("thunder"))
            return LoadClip("Audio/SFX/Units/zeus_attack");

        return LoadClip("Audio/SFX/Units/magic_archer_attack");
    }

    private void BuildUnitLookup()
    {
        unitBindingLookup.Clear();

        if (unitAttackBindings == null)
            return;

        for (int i = 0; i < unitAttackBindings.Length; i++)
        {
            UnitAudioBinding binding = unitAttackBindings[i];
            if (binding == null || string.IsNullOrEmpty(binding.matchText) || binding.clip == null)
                continue;

            string key = NormalizeName(binding.matchText);
            if (!unitBindingLookup.ContainsKey(key))
                unitBindingLookup.Add(key, binding);
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (autoPlaySceneMusic)
            PlayMusicForScene(scene);

        if (autoWireUiButtons)
            WireSceneButtons();
    }

    private void PlayMusicForScene(Scene scene)
    {
        if (scene.name == mainMenuSceneName)
        {
            PlayMusic(mainMenuMusic, mainMenuMusicVolume);
            return;
        }

        if (scene.name == battleSceneName)
        {
            PlayMusic(battleMusic, battleMusicVolume);
            return;
        }

        PlayMusic(gameMusic, gameMusicVolume);
    }

    private void PlayMusic(AudioClip clip, float clipVolume)
    {
        if (clip == null || musicSource == null)
            return;

        currentSceneMusic = clip;
        currentSceneMusicVolume = Mathf.Clamp01(clipVolume);

        if (musicSource.clip == clip && musicSource.isPlaying)
        {
            ApplySourceVolumes();
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = true;
        ApplySourceVolumes();
        musicSource.Play();
    }

    private void WireSceneButtons()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || wiredButtons.Contains(button))
                continue;

            wiredButtons.Add(button);
            button.onClick.AddListener(PlayButtonClick);
        }
    }

    private void PlayOneShot(AudioClip clip, AudioBus bus, float minPitch, float maxPitch, float volumeMultiplier = 1f)
    {
        if (clip == null || sfxMuted)
            return;

        AudioSource source = bus == AudioBus.Unit || bus == AudioBus.EnemyHit ? unitSource : sfxSource;
        if (source == null)
            return;

        source.pitch = Random.Range(minPitch, maxPitch);
        source.PlayOneShot(clip, GetBusVolume(bus) * volumeMultiplier);
    }

    private float GetBusVolume(AudioBus bus)
    {
        if (sfxMuted)
            return 0f;

        float busVolume = gameplayVolume;

        switch (bus)
        {
            case AudioBus.Ui:
                busVolume = uiVolume;
                break;
            case AudioBus.Unit:
                busVolume = unitVolume;
                break;
            case AudioBus.EnemyHit:
                busVolume = enemyHitVolume;
                break;
        }

        return Mathf.Clamp01(busVolume);
    }

    private UnitAudioBinding GetUnitAttackBinding(string unitName)
    {
        if (unitBindingLookup.Count == 0)
            BuildUnitLookup();

        if (string.IsNullOrEmpty(unitName))
            return GetFallbackUnitBinding();

        string normalizedName = NormalizeName(unitName);

        foreach (KeyValuePair<string, UnitAudioBinding> pair in unitBindingLookup)
        {
            if (normalizedName.Contains(pair.Key) && pair.Value != null && pair.Value.clip != null)
                return pair.Value;
        }

        return GetFallbackUnitBinding();
    }

    private UnitAudioBinding GetFallbackUnitBinding()
    {
        if (unitBindingLookup.TryGetValue("magic", out UnitAudioBinding fallback) && fallback != null)
            return fallback;

        foreach (UnitAudioBinding binding in unitBindingLookup.Values)
        {
            if (binding != null && binding.clip != null)
                return binding;
        }

        return null;
    }

    private void ApplySourceVolumes()
    {
        if (musicSource != null)
            musicSource.volume = musicMuted ? 0f : masterVolume * musicVolume * currentSceneMusicVolume;

        float sfxSourceVolume = sfxMuted ? 0f : masterVolume * sfxVolume;

        if (sfxSource != null)
            sfxSource.volume = sfxSourceVolume;

        if (unitSource != null)
            unitSource.volume = sfxSourceVolume;
    }

    private static string NormalizeName(string value)
    {
        return value.ToLowerInvariant().Replace("_", string.Empty).Replace(" ", string.Empty);
    }

    private static AudioClip LoadIfMissing(AudioClip clip, string resourcesPath)
    {
        return clip != null ? clip : LoadClip(resourcesPath);
    }

    private static AudioClip LoadClip(string resourcesPath)
    {
        AudioClip clip = Resources.Load<AudioClip>(resourcesPath);
        if (clip == null)
            Debug.LogWarning("GameAudioManager missing clip at Resources/" + resourcesPath);

        return clip;
    }
}
