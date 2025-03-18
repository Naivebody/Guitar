using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using System.Collections.Generic;

public class GuitarFretboardTracker : MonoBehaviour
{
    public ARTrackedImageManager trackedImageManager;  // AR 识别管理器
    public GameObject infoUIPrefab; // UI 预制体（默认隐藏）

    private Dictionary<string, GameObject> spawnedInfoUI = new Dictionary<string, GameObject>();

    void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // 当新的吉他指板被检测到
        foreach (var trackedImage in eventArgs.added)
        {
            CreateOrUpdateInfoUI(trackedImage);
        }

        // 当指板位置更新时（相机角度变化等）
        foreach (var trackedImage in eventArgs.updated)
        {
            UpdateInfoUI(trackedImage);
        }

        // 当指板丢失时，隐藏 UI
        foreach (var trackedImage in eventArgs.removed)
        {
            if (spawnedInfoUI.ContainsKey(trackedImage.referenceImage.name))
            {
                spawnedInfoUI[trackedImage.referenceImage.name].SetActive(false);
            }
        }
    }

    void CreateOrUpdateInfoUI(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        // 如果 UI 还未创建，生成一个新的 UI，并默认隐藏
        if (!spawnedInfoUI.ContainsKey(imageName))
        {
            GameObject infoUI = Instantiate(infoUIPrefab, trackedImage.transform.position, Quaternion.identity);
            infoUI.transform.SetParent(trackedImage.transform);  // UI 绑定到吉他指板
            infoUI.SetActive(false);  // 默认隐藏
            spawnedInfoUI[imageName] = infoUI;
        }

        UpdateInfoUI(trackedImage);
    }

    void UpdateInfoUI(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        if (spawnedInfoUI.ContainsKey(imageName))
        {
            GameObject infoUI = spawnedInfoUI[imageName];

            // 让 UI 悬浮在吉他指板上方
            infoUI.transform.position = trackedImage.transform.position + new Vector3(0, 0.05f, 0);

            // 让 UI 始终朝向摄像头
            infoUI.transform.LookAt(Camera.main.transform);
            infoUI.transform.Rotate(0, 180, 0);

            // UI 只有在检测到吉他指板时才显示
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                infoUI.SetActive(true);
            }
            else
            {
                infoUI.SetActive(false);
            }
        }
    }
}
