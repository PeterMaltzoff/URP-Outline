using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class OutlineObject : MonoBehaviour
{
    [Header("Outline Control")]
    [SerializeField] private bool enableOutline = true;
    [SerializeField] [Range(0, 31)] private int outlineRenderingLayer = 0; // Rendering layer for outlines
    
    private Renderer objectRenderer;
    private uint originalRenderingLayerMask;
    
    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalRenderingLayerMask = objectRenderer.renderingLayerMask;
            UpdateOutlineState();
        }
    }
    
    void UpdateOutlineState()
    {
        if (objectRenderer == null) return;
        
        if (enableOutline)
        {
            // Add the outline rendering layer to the renderer's mask
            uint outlineLayerMask = 1u << outlineRenderingLayer;
            objectRenderer.renderingLayerMask = originalRenderingLayerMask | outlineLayerMask;
        }
        else
        {
            // Remove the outline rendering layer from the renderer's mask
            uint outlineLayerMask = 1u << outlineRenderingLayer;
            objectRenderer.renderingLayerMask = originalRenderingLayerMask & ~outlineLayerMask;
        }
    }
    
    void OnValidate()
    {
        // Update outline state when values change in inspector
        if (Application.isPlaying && objectRenderer != null)
        {
            UpdateOutlineState();
        }
    }
    
    // Public methods for runtime control
    public void SetOutlineEnabled(bool enabled)
    {
        enableOutline = enabled;
        UpdateOutlineState();
    }
    
    public void SetOutlineRenderingLayer(int layer)
    {
        outlineRenderingLayer = Mathf.Clamp(layer, 0, 31);
        if (enableOutline)
        {
            UpdateOutlineState();
        }
    }
    
    public bool IsOutlineEnabled()
    {
        return enableOutline;
    }
    
    public int GetOutlineRenderingLayer()
    {
        return outlineRenderingLayer;
    }
    
    public uint GetOutlineRenderingLayerMask()
    {
        return 1u << outlineRenderingLayer;
    }
    
    // Helper method to check if this object is currently set to be outlined
    public bool IsCurrentlyOutlined()
    {
        if (objectRenderer == null) return false;
        uint outlineLayerMask = 1u << outlineRenderingLayer;
        return enableOutline && (objectRenderer.renderingLayerMask & outlineLayerMask) != 0;
    }
} 