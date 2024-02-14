
using UnityEngine;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

/// <summary>
/// https://qiita.com/Tanktop_in_Feb/items/55201612a8b449800100
/// </summary> <summary>
/// 
/// </summary>

[RequireComponent(typeof(ARCameraManager))]
public class PeopleOcclusionPostEffect : MonoBehaviour
{
    [SerializeField] private AROcclusionManager m_occlusionManager = null;
    [SerializeField] private ARCameraManager m_cameraManager = null;
    [SerializeField] private Shader m_peopleOcclusionShader = null;
    [SerializeField] Texture2D testTexture;

    private Texture2D m_cameraFeedTexture = null;
    private Material m_material = null;

    delegate bool TryAcquireDepthImageDelegate(out XRCpuImage image);

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
        UpdateDepthImage(m_occlusionManager.TryAcquireHumanStencilCpuImage, m_RawHumanStencilImage);

    }
    /// <summary>
    /// Calls <paramref name="tryAcquireDepthImageDelegate"/> and renders the resulting depth image contents to <paramref name="rawImage"/>.
    /// </summary>
    /// <param name="tryAcquireDepthImageDelegate">The method to call to acquire a depth image.</param>
    /// <param name="rawImage">The Raw Image to use to render the depth image to the screen.</param>
    void UpdateDepthImage(TryAcquireDepthImageDelegate tryAcquireDepthImageDelegate, RawImage rawImage)
    {
        if (tryAcquireDepthImageDelegate(out XRCpuImage cpuImage))
        {
            // XRCpuImages, if successfully acquired, must be disposed.
            // You can do this with a using statement as shown below, or by calling its Dispose() method directly.
            using (cpuImage)
            {
                UpdateRawImage(rawImage, cpuImage, m_Transformation);
            }
        }
        else
        {
            rawImage.enabled = false;
        }
    }

    // background
    private void RefreshCameraFeedTexture()
    {
        // get CPUImage(deleted)
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

    static void UpdateRawImage(RawImage rawImage, XRCpuImage cpuImage, XRCpuImage.Transformation transformation)
    {
        // Get the texture associated with the UI.RawImage that we wish to display on screen.
        var texture = rawImage.texture as Texture2D;

        // If the texture hasn't yet been created, or if its dimensions have changed, (re)create the texture.
        // Note: Although texture dimensions do not normally change frame-to-frame, they can change in response to
        //    a change in the camera resolution (for camera images) or changes to the quality of the human depth
        //    and human stencil buffers.
        if (texture == null || texture.width != cpuImage.width || texture.height != cpuImage.height)
        {
            texture = new Texture2D(cpuImage.width, cpuImage.height, cpuImage.format.AsTextureFormat(), false);
            rawImage.texture = texture;
        }

        // For display, we need to mirror about the vertical access.
        var conversionParams = new XRCpuImage.ConversionParams(cpuImage, cpuImage.format.AsTextureFormat(), transformation);

        // Get the Texture2D's underlying pixel buffer.
        var rawTextureData = texture.GetRawTextureData<byte>();

        // Make sure the destination buffer is large enough to hold the converted data (they should be the same size)
        Debug.Assert(rawTextureData.Length == cpuImage.GetConvertedDataSize(conversionParams.outputDimensions, conversionParams.outputFormat),
            "The Texture2D is not the same size as the converted data.");

        // Perform the conversion.
        cpuImage.Convert(conversionParams, rawTextureData);

        // "Apply" the new pixel data to the Texture2D.
        texture.Apply();

        // Make sure it's enabled.
        rawImage.enabled = true;
    }
}
