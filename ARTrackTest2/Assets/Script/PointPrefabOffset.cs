using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class TrackedImagePrefabOffset : MonoBehaviour
{
    [SerializeField] private GameObject prefabToSpawn; // �� Inspector �з���� Prefab
    [SerializeField] private Vector3 fretOffset = new Vector3(0f, 0f, 0.1f); // Ʒ��ƫ�����������ͼ��
    [SerializeField] private string targetImageName = "FretboardImage"; // Ŀ������ͼ������

    private ARTrackedImageManager trackedImageManager;
    private Dictionary<string, GameObject> spawnedPrefabs = new Dictionary<string, GameObject>();

    void Awake()
    {
        // ��ȡ AR Tracked Image Manager ���
        trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    void OnEnable()
    {
        // ����ͼ������¼�
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        // ȡ�������¼�
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // ��������ӵ�ͼ��
        foreach (var trackedImage in eventArgs.added)
        {
            if (trackedImage.referenceImage.name == targetImageName)
            {
                SpawnPrefabWithOffset(trackedImage);
            }
        }

        // ������µ�ͼ��
        foreach (var trackedImage in eventArgs.updated)
        {
            if (trackedImage.referenceImage.name == targetImageName)
            {
                UpdatePrefabWithOffset(trackedImage);
            }
        }

        // �����Ƴ���ͼ��
        foreach (var trackedImage in eventArgs.removed)
        {
            if (trackedImage.referenceImage.name == targetImageName)
            {
                DisablePrefab(trackedImage);
            }
        }
    }

    private void SpawnPrefabWithOffset(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        // �����ͼ����δʵ���� Prefab���򴴽�
        if (!spawnedPrefabs.ContainsKey(imageName))
        {
            // ����ƫ�ƺ��λ��
            Vector3 spawnPosition = trackedImage.transform.position + trackedImage.transform.TransformDirection(fretOffset);
            Quaternion spawnRotation = trackedImage.transform.rotation;

            // ʵ���� Prefab
            GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);
            spawnedPrefabs.Add(imageName, spawnedObject);
            Debug.Log($"Spawned Prefab for {imageName} at {spawnPosition}");
        }
    }

    private void UpdatePrefabWithOffset(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        // ���� Prefab ��λ�ú���ת
        if (spawnedPrefabs.ContainsKey(imageName) && trackedImage.trackingState == TrackingState.Tracking)
        {
            GameObject spawnedObject = spawnedPrefabs[imageName];
            // ����ƫ�ƺ��λ��
            Vector3 updatedPosition = trackedImage.transform.position + trackedImage.transform.TransformDirection(fretOffset);
            spawnedObject.transform.position = updatedPosition;
            spawnedObject.transform.rotation = trackedImage.transform.rotation;
            spawnedObject.SetActive(true);
            Debug.Log($"Updated Prefab for {imageName} to {updatedPosition}");
        }
        else if (spawnedPrefabs.ContainsKey(imageName))
        {
            // ���ͼ���ٸ��٣����� Prefab
            spawnedPrefabs[imageName].SetActive(false);
        }
    }

    private void DisablePrefab(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        // ���� Prefab��֧�ֶ���أ�
        if (spawnedPrefabs.ContainsKey(imageName))
        {
            spawnedPrefabs[imageName].SetActive(false);
            Debug.Log($"Disabled Prefab for {imageName}");
        }
    }
}