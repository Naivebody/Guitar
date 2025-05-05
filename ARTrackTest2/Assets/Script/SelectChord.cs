using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(ARTrackedImageManager))]
public class ARChordPractice : MonoBehaviour
{
    [SerializeField] private List<ChordConfig> chordConfigs = new List<ChordConfig>(); // 和弦配置列表
    [SerializeField] private Vector3 defaultOffset = new Vector3(0, 0.1f, 0); // 默认偏移量
    [SerializeField] private TMP_Dropdown chordDropdown; // 用户选择和弦的 UI 下拉菜单（可选）
    [SerializeField] private TMP_Text promptText; // UI 文本，用于显示提示

    private ARTrackedImageManager trackedImageManager;
    private ARTrackedImage currentTrackedImage; // 缓存当前跟踪的图像
    private readonly Dictionary<string, GameObject> instantiatedPrefabs = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject> prefabPool = new Dictionary<string, GameObject>(); // 对象池
    private readonly Dictionary<string, ChordConfig> chordConfigDict = new Dictionary<string, ChordConfig>(); // 和弦配置字典
    private string currentChord; // 当前选中的和弦名称
    private string trackedImageName; // 当前跟踪的图像名称
    private GameObject activePrefab; // 当前激活的 prefab

    // 和弦配置数据结构
    [System.Serializable]
    public class ChordConfig
    {
        public string chordName; // 和弦名称（例如 "C Major"）
        public GameObject prefab; // 对应的 prefab
        public Vector3 offset; // 和弦特定的偏移量
    }

    void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
        trackedImageManager.maxNumberOfMovingImages = 1; // 限制为单张图像跟踪

        // 初始化对象池和和弦配置字典
        foreach (var config in chordConfigs)
        {
            chordConfigDict[config.chordName] = config;
            GameObject prefab = Instantiate(config.prefab);
            prefab.SetActive(false);
            prefabPool[config.chordName] = prefab;
        }
    }

    void Start()
    {
        // 初始时设置提示文本
        UpdateTrackingState(null);
        // 初始化 UI 下拉菜单（如果使用）
        if (chordDropdown != null)
        {
            InitializeChordDropdown();
        }
    }

    void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    // 初始化下拉菜单，填充和弦选项
    void InitializeChordDropdown()
    {
        chordDropdown.ClearOptions();
        List<string> chordNames = new List<string>(chordConfigDict.Keys);
        chordDropdown.AddOptions(chordNames);

        // 默认选择第一个和弦
        if (chordNames.Count > 0)
        {
            currentChord = chordNames[0];
            chordDropdown.value = 0;
        }

        // 监听下拉菜单选择事件
        chordDropdown.onValueChanged.AddListener(OnChordSelected);
    }

    // 当用户选择和弦时调用
    void OnChordSelected(int index)
    {
        currentChord = chordDropdown.options[index].text;
        UpdatePrefabForCurrentImage();
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // 处理新检测到的图像
        if (eventArgs.added.Count > 0)
        {
            currentTrackedImage = eventArgs.added[0];
            trackedImageName = currentTrackedImage.referenceImage.name;
            UpdateTrackingState(currentTrackedImage);
            UpdatePrefabForImage(currentTrackedImage);
        }

        // 更新当前图像
        if (eventArgs.updated.Count > 0 && currentTrackedImage != null)
        {
            currentTrackedImage = eventArgs.updated[0];
            UpdateTrackingState(currentTrackedImage);
            UpdatePrefabForImage(currentTrackedImage);
        }

        // 处理图像移除
        if (eventArgs.removed.Count > 0 && currentTrackedImage != null)
        {
            if (activePrefab != null)
            {
                activePrefab.SetActive(false);
                instantiatedPrefabs.Clear();
            }
            currentTrackedImage = null;
            trackedImageName = null;
            UpdateTrackingState(null);
        }
    }

    void UpdatePrefabForImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;
        if (string.IsNullOrEmpty(currentChord)) return;

        // 停用当前激活的 prefab
        if (activePrefab != null)
        {
            activePrefab.SetActive(false);
        }

        // 从对象池获取 prefab
        if (!prefabPool.TryGetValue(currentChord, out var prefabToUse))
        {
            Debug.LogWarning($"No prefab found for chord: {currentChord}");
            return;
        }

        // 设置位置、旋转和父对象
        if (!chordConfigDict.TryGetValue(currentChord, out var config))
        {
            Debug.LogWarning($"No config found for chord: {currentChord}");
            return;
        }

        prefabToUse.transform.SetParent(trackedImage.transform, false);
        prefabToUse.transform.localPosition = config.offset + defaultOffset;
        prefabToUse.SetActive(trackedImage.trackingState == TrackingState.Tracking);
        activePrefab = prefabToUse;

        // 更新 instantiatedPrefabs
        instantiatedPrefabs[imageName] = prefabToUse;
    }

    void UpdatePrefabForCurrentImage()
    {
        if (string.IsNullOrEmpty(trackedImageName) || string.IsNullOrEmpty(currentChord) || currentTrackedImage == null)
        {
            return;
        }

        UpdatePrefabForImage(currentTrackedImage);
    }

    void UpdateTrackingState(ARTrackedImage trackedImage)
    {
        string newText;
        if (trackedImage == null)
        {
            newText = "Makesure your guitar and featured Image are in your screen.";
        }
        else if (trackedImage.trackingState != TrackingState.Tracking )
        {
            newText = "Move your camera and make sure the featured image is clear to see.";
        }
        else
        {
            newText = "";
        }

        if (promptText.text != newText)
        {
            promptText.text = newText;
            Debug.Log($"Prompt text updated to: '{newText}', TrackedImage: {(trackedImage != null ? trackedImage.trackingState.ToString() : "None")}");
        }
    }

    public void SelectChord(string chordName)
    {
        if (chordConfigDict.ContainsKey(chordName))
        {
            currentChord = chordName;
            UpdatePrefabForCurrentImage();

            // 更新下拉菜单（如果使用）
            if (chordDropdown != null)
            {
                int index = chordDropdown.options.FindIndex(option => option.text == chordName);
                if (index >= 0)
                {
                    chordDropdown.value = index;
                }
            }
        }
        else
        {
            Debug.LogWarning($"Chord {chordName} not found in chord configs.");
        }
    }
}