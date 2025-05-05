using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(ARTrackedImageManager))]
public class ARChordPractice : MonoBehaviour
{
    [SerializeField] private List<ChordConfig> chordConfigs = new List<ChordConfig>(); // ���������б�
    [SerializeField] private Vector3 defaultOffset = new Vector3(0, 0.1f, 0); // Ĭ��ƫ����
    [SerializeField] private TMP_Dropdown chordDropdown; // �û�ѡ����ҵ� UI �����˵�����ѡ��
    [SerializeField] private TMP_Text promptText; // UI �ı���������ʾ��ʾ

    private ARTrackedImageManager trackedImageManager;
    private ARTrackedImage currentTrackedImage; // ���浱ǰ���ٵ�ͼ��
    private readonly Dictionary<string, GameObject> instantiatedPrefabs = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject> prefabPool = new Dictionary<string, GameObject>(); // �����
    private readonly Dictionary<string, ChordConfig> chordConfigDict = new Dictionary<string, ChordConfig>(); // ���������ֵ�
    private string currentChord; // ��ǰѡ�еĺ�������
    private string trackedImageName; // ��ǰ���ٵ�ͼ������
    private GameObject activePrefab; // ��ǰ����� prefab

    // �����������ݽṹ
    [System.Serializable]
    public class ChordConfig
    {
        public string chordName; // �������ƣ����� "C Major"��
        public GameObject prefab; // ��Ӧ�� prefab
        public Vector3 offset; // �����ض���ƫ����
    }

    void Awake()
    {
        trackedImageManager = GetComponent<ARTrackedImageManager>();
        trackedImageManager.maxNumberOfMovingImages = 1; // ����Ϊ����ͼ�����

        // ��ʼ������غͺ��������ֵ�
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
        // ��ʼʱ������ʾ�ı�
        UpdateTrackingState(null);
        // ��ʼ�� UI �����˵������ʹ�ã�
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

    // ��ʼ�������˵���������ѡ��
    void InitializeChordDropdown()
    {
        chordDropdown.ClearOptions();
        List<string> chordNames = new List<string>(chordConfigDict.Keys);
        chordDropdown.AddOptions(chordNames);

        // Ĭ��ѡ���һ������
        if (chordNames.Count > 0)
        {
            currentChord = chordNames[0];
            chordDropdown.value = 0;
        }

        // ���������˵�ѡ���¼�
        chordDropdown.onValueChanged.AddListener(OnChordSelected);
    }

    // ���û�ѡ�����ʱ����
    void OnChordSelected(int index)
    {
        currentChord = chordDropdown.options[index].text;
        UpdatePrefabForCurrentImage();
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // �����¼�⵽��ͼ��
        if (eventArgs.added.Count > 0)
        {
            currentTrackedImage = eventArgs.added[0];
            trackedImageName = currentTrackedImage.referenceImage.name;
            UpdateTrackingState(currentTrackedImage);
            UpdatePrefabForImage(currentTrackedImage);
        }

        // ���µ�ǰͼ��
        if (eventArgs.updated.Count > 0 && currentTrackedImage != null)
        {
            currentTrackedImage = eventArgs.updated[0];
            UpdateTrackingState(currentTrackedImage);
            UpdatePrefabForImage(currentTrackedImage);
        }

        // ����ͼ���Ƴ�
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

        // ͣ�õ�ǰ����� prefab
        if (activePrefab != null)
        {
            activePrefab.SetActive(false);
        }

        // �Ӷ���ػ�ȡ prefab
        if (!prefabPool.TryGetValue(currentChord, out var prefabToUse))
        {
            Debug.LogWarning($"No prefab found for chord: {currentChord}");
            return;
        }

        // ����λ�á���ת�͸�����
        if (!chordConfigDict.TryGetValue(currentChord, out var config))
        {
            Debug.LogWarning($"No config found for chord: {currentChord}");
            return;
        }

        prefabToUse.transform.SetParent(trackedImage.transform, false);
        prefabToUse.transform.localPosition = config.offset + defaultOffset;
        prefabToUse.SetActive(trackedImage.trackingState == TrackingState.Tracking);
        activePrefab = prefabToUse;

        // ���� instantiatedPrefabs
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

            // ���������˵������ʹ�ã�
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