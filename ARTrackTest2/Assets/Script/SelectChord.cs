using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using TMPro; // ��� TextMeshPro �����ռ�

[RequireComponent(typeof(ARTrackedImageManager))]
public class ARChordPractice : MonoBehaviour
{
    [SerializeField] private List<ChordConfig> chordConfigs = new List<ChordConfig>(); // ���������б�
    [SerializeField] private Vector3 defaultOffset = new Vector3(0, 0.1f, 0); // Ĭ��ƫ����
    [SerializeField] private TMP_Dropdown chordDropdown; // �û�ѡ����ҵ� UI �����˵�����ѡ��

    private ARTrackedImageManager trackedImageManager;
    private readonly Dictionary<string, GameObject> instantiatedPrefabs = new Dictionary<string, GameObject>();
    private string currentChord; // ��ǰѡ�еĺ�������
    private string trackedImageName; // ��ǰ���ٵ�ͼ������

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
    }

    void Start()
    {
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
        List<string> chordNames = new List<string>();
        foreach (var config in chordConfigs)
        {
            chordNames.Add(config.chordName);
        }
        chordDropdown.AddOptions(chordNames);

        // Ĭ��ѡ���һ������
        if (chordNames.Count > 0)
        {
            currentChord = chordConfigs[0].chordName;
            chordDropdown.value = 0;
        }

        // ���������˵�ѡ���¼�
        chordDropdown.onValueChanged.AddListener(OnChordSelected);
       
    }

    // ���û�ѡ�����ʱ����
    void OnChordSelected(int index)
    {
        Debug.Log("ChordSelected");
        currentChord = chordConfigs[index].chordName;
        UpdatePrefabForCurrentImage();
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // �����¼�⵽��ͼ��
        foreach (var trackedImage in eventArgs.added)
        {
            trackedImageName = trackedImage.referenceImage.name;
            UpdatePrefabForImage(trackedImage);
        }

        // ��������ͼ��ĸ���״̬
        foreach (var trackedImage in eventArgs.updated)
        {
            trackedImageName = trackedImage.referenceImage.name;
            UpdatePrefabForImage(trackedImage);
        }

        // �����Ƴ���ͼ��
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

        // ���û��ѡ����ң�����
        if (string.IsNullOrEmpty(currentChord))
        {
            Debug.LogWarning("No chord selected.");
            return;
        }

        // ����Ƿ����� prefab ʵ��
        if (instantiatedPrefabs.TryGetValue(imageName, out var existingPrefab))
        {
            // ������ʾ״̬
            existingPrefab.SetActive(trackedImage.trackingState == TrackingState.Tracking);
            return;
        }

        // ���ҵ�ǰ���ҵ� prefab ��ƫ��
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

        // ʵ���� prefab
        GameObject instantiatedObject = Instantiate(prefabToInstantiate, trackedImage.transform.position, trackedImage.transform.rotation);

        // Ӧ��ƫ�ƣ������ͼ��ı�������ϵ��
        instantiatedObject.transform.position += trackedImage.transform.TransformVector(offset);

        // ���ø������Ը���ͼ��
        instantiatedObject.transform.SetParent(trackedImage.transform, true);

        // �洢ʵ������ prefab
        instantiatedPrefabs[imageName] = instantiatedObject;

        // ���ó�ʼ��ʾ״̬
        instantiatedObject.SetActive(trackedImage.trackingState == TrackingState.Tracking);
    }

    // �����ұ��ʱ�����µ�ǰͼ��� prefab
    void UpdatePrefabForCurrentImage()
    {
        if (string.IsNullOrEmpty(trackedImageName) || string.IsNullOrEmpty(currentChord))
        {
            return;
        }

        // �������� prefab
        if (instantiatedPrefabs.TryGetValue(trackedImageName, out var existingPrefab))
        {
            Destroy(existingPrefab);
            instantiatedPrefabs.Remove(trackedImageName);
        }

        // ���ҵ�ǰ���ٵ�ͼ�񲢸��� prefab
        foreach (var trackedImage in trackedImageManager.trackables)
        {
            if (trackedImage.referenceImage.name == trackedImageName)
            {
                UpdatePrefabForImage(trackedImage);
                break;
            }
        }
    }

    // ��������������ͨ���ű�ѡ����ң����簴ť���ã�
    public void SelectChord(string chordName)
    {
        if (chordConfigs.Exists(config => config.chordName == chordName))
        {
            currentChord = chordName;
            UpdatePrefabForCurrentImage();

            // ���������˵������ʹ�ã�
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
