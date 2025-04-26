using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using TMPro; // 添加 TextMeshPro 命名空间

[RequireComponent(typeof(ARTrackedImageManager))]
public class ARChordPractice : MonoBehaviour
{
    [SerializeField] private List<ChordConfig> chordConfigs = new List<ChordConfig>(); // 和弦配置列表
    [SerializeField] private Vector3 defaultOffset = new Vector3(0, 0.1f, 0); // 默认偏移量
    [SerializeField] private TMP_Dropdown chordDropdown; // 用户选择和弦的 UI 下拉菜单（可选）

    private ARTrackedImageManager trackedImageManager;
    private readonly Dictionary<string, GameObject> instantiatedPrefabs = new Dictionary<string, GameObject>();
    private string currentChord; // 当前选中的和弦名称
    private string trackedImageName; // 当前跟踪的图像名称

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
    }

    void Start()
    {
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
        List<string> chordNames = new List<string>();
        foreach (var config in chordConfigs)
        {
            chordNames.Add(config.chordName);
        }
        chordDropdown.AddOptions(chordNames);

        // 默认选择第一个和弦
        if (chordNames.Count > 0)
        {
            currentChord = chordConfigs[0].chordName;
            chordDropdown.value = 0;
        }

        // 监听下拉菜单选择事件
        chordDropdown.onValueChanged.AddListener(OnChordSelected);
       
    }

    // 当用户选择和弦时调用
    void OnChordSelected(int index)
    {
        Debug.Log("ChordSelected");
        currentChord = chordConfigs[index].chordName;
        UpdatePrefabForCurrentImage();
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // 处理新检测到的图像
        foreach (var trackedImage in eventArgs.added)
        {
            trackedImageName = trackedImage.referenceImage.name;
            UpdatePrefabForImage(trackedImage);
        }

        // 更新现有图像的跟踪状态
        foreach (var trackedImage in eventArgs.updated)
        {
            trackedImageName = trackedImage.referenceImage.name;
            UpdatePrefabForImage(trackedImage);
        }

        // 处理移除的图像
        foreach (var trackedImage in eventArgs.removed)
        {
            if (instantiatedPrefabs.TryGetValue(trackedImage.referenceImage.name, out var prefab))
            {
                Destroy(prefab);
                instantiatedPrefabs.Remove(trackedImage.referenceImage.name);
            }
        }
    }

    void UpdatePrefabForImage(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        // 如果没有选择和弦，跳过
        if (string.IsNullOrEmpty(currentChord))
        {
            Debug.LogWarning("No chord selected.");
            return;
        }

        // 检查是否已有 prefab 实例
        if (instantiatedPrefabs.TryGetValue(imageName, out var existingPrefab))
        {
            // 更新显示状态
            existingPrefab.SetActive(trackedImage.trackingState == TrackingState.Tracking);
            return;
        }

        // 查找当前和弦的 prefab 和偏移
        GameObject prefabToInstantiate = null;
        Vector3 offset = defaultOffset;

        foreach (var config in chordConfigs)
        {
            if (config.chordName == currentChord)
            {
                prefabToInstantiate = config.prefab;
                offset = config.offset;
                break;
            }
        }

        if (prefabToInstantiate == null)
        {
            Debug.LogWarning($"No prefab found for chord: {currentChord}");
            return;
        }

        // 实例化 prefab
        GameObject instantiatedObject = Instantiate(prefabToInstantiate, trackedImage.transform.position, trackedImage.transform.rotation);

        // 应用偏移（相对于图像的本地坐标系）
        instantiatedObject.transform.position += trackedImage.transform.TransformVector(offset);

        // 设置父对象以跟随图像
        instantiatedObject.transform.SetParent(trackedImage.transform, true);

        // 存储实例化的 prefab
        instantiatedPrefabs[imageName] = instantiatedObject;

        // 设置初始显示状态
        instantiatedObject.SetActive(trackedImage.trackingState == TrackingState.Tracking);
    }

    // 当和弦变更时，更新当前图像的 prefab
    void UpdatePrefabForCurrentImage()
    {
        if (string.IsNullOrEmpty(trackedImageName) || string.IsNullOrEmpty(currentChord))
        {
            return;
        }

        // 销毁现有 prefab
        if (instantiatedPrefabs.TryGetValue(trackedImageName, out var existingPrefab))
        {
            Destroy(existingPrefab);
            instantiatedPrefabs.Remove(trackedImageName);
        }

        // 查找当前跟踪的图像并更新 prefab
        foreach (var trackedImage in trackedImageManager.trackables)
        {
            if (trackedImage.referenceImage.name == trackedImageName)
            {
                UpdatePrefabForImage(trackedImage);
                break;
            }
        }
    }

    // 公共方法，允许通过脚本选择和弦（例如按钮调用）
    public void SelectChord(string chordName)
    {
        if (chordConfigs.Exists(config => config.chordName == chordName))
        {
            currentChord = chordName;
            UpdatePrefabForCurrentImage();

            // 更新下拉菜单（如果使用）
            if (chordDropdown != null)
            {
                int index = chordConfigs.FindIndex(config => config.chordName == chordName);
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
