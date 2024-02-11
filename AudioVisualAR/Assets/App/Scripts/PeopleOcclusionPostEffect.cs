
using UnityEngine;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// https://qiita.com/Tanktop_in_Feb/items/55201612a8b449800100
/// </summary> <summary>
/// 
/// </summary>

[RequireComponent(typeof(ARCameraManager))]
public class PeopleOcclusionPostEffect : MonoBehaviour
{
    [SerializeField] private ARSessionOrigin m_arOrigin = null;
    [SerializeField] private ARHumanBodyManager m_humanBodyManager = null;
    [SerializeField] private ARCameraManager m_cameraManager = null;
    [SerializeField] private Shader m_peopleOcclusionShader = null;
    [SerializeField] Texture2D testTexture;

    private Texture2D m_cameraFeedTexture = null;
    private Material m_material = null;

    void Awake()
    {
        m_material = new Material(m_peopleOcclusionShader);
        GetComponent<Camera>().depthTextureMode |= DepthTextureMode.Depth;
    }

    private void OnEnable()
    {
        m_cameraManager.frameReceived += OnCameraFrameReceived;
    }

    private void OnDisable()
    {
        m_cameraManager.frameReceived -= OnCameraFrameReceived;
    }
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {

        if (m_cameraFeedTexture != null)
        {
            m_material.SetFloat("_UVMultiplierLandScape", CalculateUVMultiplierLandScape(m_cameraFeedTexture));
            m_material.SetFloat("_UVMultiplierPortrait", CalculateUVMultiplierPortrait(m_cameraFeedTexture));
        }

        if (Input.deviceOrientation == DeviceOrientation.LandscapeRight)
        {
            m_material.SetFloat("_UVFlip", 0);
            m_material.SetInt("_ONWIDE", 1);
        }
        else if (Input.deviceOrientation == DeviceOrientation.LandscapeLeft)
        {
            m_material.SetFloat("_UVFlip", 1);
            m_material.SetInt("_ONWIDE", 1);
        }
        else
        {
            m_material.SetInt("_ONWIDE", 0);
        }


        m_material.SetTexture("_OcclusionDepth", m_humanBodyManager.humanDepthTexture);
        m_material.SetTexture("_OcclusionStencil", m_humanBodyManager.humanStencilTexture);

        // m_material.SetFloat("_ARWorldScale", 1f/m_arOrigin.transform.localScale.x);

        Graphics.Blit(source, destination, m_material);
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        RefreshCameraFeedTexture();
    }

    // background
    private void RefreshCameraFeedTexture()
    {
        XRCameraImage cameraImage;
        m_cameraManager.TryGetLatestImage(out cameraImage);
        if (m_cameraFeedTexture == null || m_cameraFeedTexture.width != cameraImage.width || m_cameraFeedTexture.height != cameraImage.height)
        {
            m_cameraFeedTexture = new Texture2D(cameraImage.width, cameraImage.height, TextureFormat.RGBA32, false);
            // m_cameraFeedTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGBA32, false);
        }

        CameraImageTransformation imageTransformation = Input.deviceOrientation == DeviceOrientation.LandscapeRight ? CameraImageTransformation.MirrorY : CameraImageTransformation.MirrorX;
        XRCameraImageConversionParams conversionParams = new XRCameraImageConversionParams(cameraImage, TextureFormat.RGBA32, imageTransformation);

        NativeArray<byte> rawTextureData = m_cameraFeedTexture.GetRawTextureData<byte>();

        try
        {
            unsafe
            {
                cameraImage.Convert(conversionParams, new IntPtr(rawTextureData.GetUnsafePtr()), rawTextureData.Length);
            }
        }
        finally
        {
            cameraImage.Dispose();
        }

        m_cameraFeedTexture.Apply();
        m_material.SetTexture("_CameraFeed", testTexture);

    }

    private float CalculateUVMultiplierLandScape(Texture2D cameraTexture)
    {
        float screenAspect = (float)Screen.width / (float)Screen.height;
        float cameraTextureAspect = (float)cameraTexture.width / (float)cameraTexture.height;
        return screenAspect / cameraTextureAspect;

    }
    private float CalculateUVMultiplierPortrait(Texture2D cameraTexture)
    {
        float screenAspect = (float)Screen.height / (float)Screen.width;
        float cameraTextureAspect = (float)cameraTexture.width / (float)cameraTexture.height;
        return screenAspect / cameraTextureAspect;

    }
}
