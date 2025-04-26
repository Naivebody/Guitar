using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class TrackedImagePrefabOffset : MonoBehaviour
{
    [SerializeField] private GameObject prefabToSpawn; // 在 Inspector 中分配的 Prefab
    [SerializeField] private Vector3 fretOffset = new Vector3(0f, 0f, 0.1f); // 品格偏移量（相对于图像）
    [SerializeField] private string targetImageName = "FretboardImage"; // 目标特征图像名称

    private ARTrackedImageManager trackedImageManager;
    private Dictionary<string, GameObject> spawnedPrefabs = new Dictionary<string, GameObject>();

    void Awake()
    {
        // 获取 AR Tracked Image Manager 组件
        trackedImageManager = GetComponent<ARTrackedImageManager>();
    }

    void OnEnable()
    {
        // 订阅图像跟踪事件
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        // 取消订阅事件
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // 处理新添加的图像
        foreach (var trackedImage in eventArgs.added)
        {
            if (trackedImage.referenceImage.name == targetImageName)
            {
                SpawnPrefabWithOffset(trackedImage);
            }
        }

        // 处理更新的图像
        foreach (var trackedImage in eventArgs.updated)
        {
            if (trackedImage.referenceImage.name == targetImageName)
            {
                UpdatePrefabWithOffset(trackedImage);
            }
        }

        // 处理移除的图像
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

        // 如果该图像尚未实例化 Prefab，则创建
        if (!spawnedPrefabs.ContainsKey(imageName))
        {
            // 计算偏移后的位置
            Vector3 spawnPosition = trackedImage.transform.position + trackedImage.transform.TransformDirection(fretOffset);
            Quaternion spawnRotation = trackedImage.transform.rotation;

            // 实例化 Prefab
            GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);
            spawnedPrefabs.Add(imageName, spawnedObject);
            Debug.Log($"Spawned Prefab for {imageName} at {spawnPosition}");
        }
    }

    private void UpdatePrefabWithOffset(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        // 更新 Prefab 的位置和旋转
        if (spawnedPrefabs.ContainsKey(imageName) && trackedImage.trackingState == TrackingState.Tracking)
        {
            GameObject spawnedObject = spawnedPrefabs[imageName];
            // 计算偏移后的位置
            Vector3 updatedPosition = trackedImage.transform.position + trackedImage.transform.TransformDirection(fretOffset);
            spawnedObject.transform.position = updatedPosition;
            spawnedObject.transform.rotation = trackedImage.transform.rotation;
            spawnedObject.SetActive(true);
            Debug.Log($"Updated Prefab for {imageName} to {updatedPosition}");
        }
        else if (spawnedPrefabs.ContainsKey(imageName))
        {
            // 如果图像不再跟踪，禁用 Prefab
            spawnedPrefabs[imageName].SetActive(false);
        }
    }

    private void DisablePrefab(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        // 禁用 Prefab（支持对象池）
        if (spawnedPrefabs.ContainsKey(imageName))
        {
            spawnedPrefabs[imageName].SetActive(false);
            Debug.Log($"Disabled Prefab for {imageName}");
        }
    }
}