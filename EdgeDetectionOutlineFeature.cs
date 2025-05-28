using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class EdgeDetectionOutlineFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class OutlineSettings
    {
        [Header("Outline Appearance")]
        public Color outlineColor = Color.black;
        [Range(0f, 10f)] public float outlineThickness = 1f;
        
        [Header("Detection Sensitivity")]
        [Range(0f, 1f)] public float depthSensitivity = 0.5f;
        [Range(0f, 1f)] public float normalSensitivity = 0.4f;
        [Range(0f, 1f)] public float colorSensitivity = 0.1f;
        
        [Header("Rendering Layers")]
        [Tooltip("Rendering layer mask for objects that should have outlines")]
        public uint outlineRenderingLayerMask = 1;
        
        [Header("Render Settings")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }
    
    public OutlineSettings settings = new OutlineSettings();
    private OutlineObjectsPass outlineObjectsPass;
    private EdgeDetectionPass edgeDetectionPass;
    private Material outlineMaterial;
    
    public override void Create()
    {
        outlineObjectsPass = new OutlineObjectsPass();
        edgeDetectionPass = new EdgeDetectionPass();
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Don't render for some camera types
        if (renderingData.cameraData.cameraType == CameraType.Preview ||
            renderingData.cameraData.cameraType == CameraType.Reflection)
            return;
            
        // Create material if needed
        if (outlineMaterial == null)
        {
            Shader outlineShader = Shader.Find("Hidden/EdgeDetectionOutline");
            if (outlineShader == null)
            {
                Debug.LogWarning("EdgeDetectionOutline shader not found!");
                return;
            }
            outlineMaterial = new Material(outlineShader);
        }
        
        // Configure passes
        outlineObjectsPass.Setup(settings);
        outlineObjectsPass.renderPassEvent = settings.renderPassEvent - 1; // Render outline objects first
        outlineObjectsPass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        
        edgeDetectionPass.Setup(outlineMaterial, settings);
        edgeDetectionPass.renderPassEvent = settings.renderPassEvent;
        edgeDetectionPass.ConfigureInput(ScriptableRenderPassInput.Color);
        
        renderer.EnqueuePass(outlineObjectsPass);
        renderer.EnqueuePass(edgeDetectionPass);
    }
    
    protected override void Dispose(bool disposing)
    {
        if (outlineMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(outlineMaterial);
            else
                DestroyImmediate(outlineMaterial);
        }
    }
}

// Pass 1: Render outline objects to separate textures
public class OutlineObjectsPass : ScriptableRenderPass
{
    private EdgeDetectionOutlineFeature.OutlineSettings settings;
    private FilteringSettings filteringSettings;
    private RenderStateBlock renderStateBlock;
    
    private RTHandle outlineColorTexture;
    private RTHandle outlineDepthTexture;
    private RTHandle outlineNormalTexture;
    
    private static readonly int OutlineColorTextureId = Shader.PropertyToID("_OutlineColorTexture");
    private static readonly int OutlineDepthTextureId = Shader.PropertyToID("_OutlineDepthTexture");
    private static readonly int OutlineNormalTextureId = Shader.PropertyToID("_OutlineNormalTexture");
    
    public void Setup(EdgeDetectionOutlineFeature.OutlineSettings settings)
    {
        this.settings = settings;
        
        // Set up filtering to only render objects with the outline rendering layer
        filteringSettings = new FilteringSettings(RenderQueueRange.all);
        filteringSettings.renderingLayerMask = settings.outlineRenderingLayerMask;
        
        renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
    }
    
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0; // No depth buffer for color texture
        
        // Create render textures for outline objects
        RenderingUtils.ReAllocateIfNeeded(ref outlineColorTexture, descriptor, name: "_OutlineColorTexture");
        
        descriptor.colorFormat = RenderTextureFormat.RFloat; // Single channel for depth
        RenderingUtils.ReAllocateIfNeeded(ref outlineDepthTexture, descriptor, name: "_OutlineDepthTexture");
        
        descriptor.colorFormat = RenderTextureFormat.ARGB32; // RGB for normals
        RenderingUtils.ReAllocateIfNeeded(ref outlineNormalTexture, descriptor, name: "_OutlineNormalTexture");
        
        // Set global textures so the edge detection pass can access them
        cmd.SetGlobalTexture(OutlineColorTextureId, outlineColorTexture);
        cmd.SetGlobalTexture(OutlineDepthTextureId, outlineDepthTexture);
        cmd.SetGlobalTexture(OutlineNormalTextureId, outlineNormalTexture);
    }
    
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        CommandBuffer cmd = CommandBufferPool.Get("RenderOutlineObjects");
        
        // Clear the outline textures
        cmd.SetRenderTarget(outlineColorTexture);
        cmd.ClearRenderTarget(false, true, Color.clear);
        
        cmd.SetRenderTarget(outlineDepthTexture);
        cmd.ClearRenderTarget(false, true, Color.clear);
        
        cmd.SetRenderTarget(outlineNormalTexture);
        cmd.ClearRenderTarget(false, true, Color.clear);
        
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        
        // Render outline objects to color texture
        var sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;
        var drawingSettings = CreateDrawingSettings(new ShaderTagId("UniversalForward"), ref renderingData, sortingCriteria);
        
        cmd.SetRenderTarget(outlineColorTexture);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        
        context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
        
        // Render outline objects depth to depth texture (using a simple depth-only shader)
        var depthShaderTagId = new ShaderTagId("DepthOnly");
        var depthDrawingSettings = CreateDrawingSettings(depthShaderTagId, ref renderingData, sortingCriteria);
        
        cmd.SetRenderTarget(outlineDepthTexture);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        
        context.DrawRenderers(renderingData.cullResults, ref depthDrawingSettings, ref filteringSettings, ref renderStateBlock);
        
        // Render outline objects normals to normal texture (using a normals shader)
        var normalShaderTagId = new ShaderTagId("DepthNormals");
        var normalDrawingSettings = CreateDrawingSettings(normalShaderTagId, ref renderingData, sortingCriteria);
        
        cmd.SetRenderTarget(outlineNormalTexture);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        
        context.DrawRenderers(renderingData.cullResults, ref normalDrawingSettings, ref filteringSettings, ref renderStateBlock);
        
        CommandBufferPool.Release(cmd);
    }
    
    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        outlineColorTexture?.Release();
        outlineDepthTexture?.Release();
        outlineNormalTexture?.Release();
    }
}

// Pass 2: Apply edge detection to outline objects and composite
public class EdgeDetectionPass : ScriptableRenderPass
{
    private Material material;
    private EdgeDetectionOutlineFeature.OutlineSettings settings;
    
    private static readonly int OutlineColorProperty = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineThicknessProperty = Shader.PropertyToID("_OutlineThickness");
    private static readonly int DepthSensitivityProperty = Shader.PropertyToID("_DepthSensitivity");
    private static readonly int NormalSensitivityProperty = Shader.PropertyToID("_NormalSensitivity");
    private static readonly int ColorSensitivityProperty = Shader.PropertyToID("_ColorSensitivity");
    
    public void Setup(Material material, EdgeDetectionOutlineFeature.OutlineSettings settings)
    {
        this.material = material;
        this.settings = settings;
        
        // Set shader properties
        material.SetColor(OutlineColorProperty, settings.outlineColor);
        material.SetFloat(OutlineThicknessProperty, settings.outlineThickness);
        material.SetFloat(DepthSensitivityProperty, settings.depthSensitivity);
        material.SetFloat(NormalSensitivityProperty, settings.normalSensitivity);
        material.SetFloat(ColorSensitivityProperty, settings.colorSensitivity);
    }
    
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (material == null) return;
        
        CommandBuffer cmd = CommandBufferPool.Get("EdgeDetectionOutline");
        
        // Get camera target
        var cameraTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
        
        // Create temporary render texture
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;
        
        int tempID = Shader.PropertyToID("_TempOutlineTexture");
        cmd.GetTemporaryRT(tempID, descriptor);
        
        // Blit with outline shader (now using the outline object textures)
        cmd.Blit(cameraTarget, tempID, material, 0);
        cmd.Blit(tempID, cameraTarget);
        
        // Clean up
        cmd.ReleaseTemporaryRT(tempID);
        
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
} 