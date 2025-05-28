# Screen-Space Edge Detection Outline System

This folder contains a modern screen-space edge detection outline system for Unity URP that provides clean, crisp outlines like those seen in RTS games and modern titles. This system uses **Rendering Layers** to properly isolate outlined objects.

## Files

- `EdgeDetectionOutlineFeature.cs` - URP Renderer Feature that implements the screen-space outline effect
- `EdgeDetectionOutline.shader` - Shader that performs Sobel edge detection on depth, normal, and color buffers
- `OutlineObject.cs` - Component for controlling which objects get outlined using Rendering Layers
- `README.md` - This documentation file

## Setup Instructions

### 1. Add the Renderer Feature

1. Open your **Universal Renderer Data** asset (usually in `Assets/Settings/`)
2. Click **"Add Renderer Feature"**
3. Select **"Edge Detection Outline Feature"**
4. Configure the settings in the inspector:
   - **Outline Color**: Color of the outlines
   - **Outline Thickness**: Width of the outline effect
   - **Depth/Normal/Color Sensitivity**: How sensitive the edge detection is for each buffer
   - **Outline Rendering Layer Mask**: Which rendering layer(s) to apply outlines to (default: 1)

### 2. Enable Required Buffers

Make sure your URP asset has the following enabled:
- **Depth Texture**: Enabled
- **Opaque Texture**: Enabled (for color-based edge detection)

You can find these settings in your **Universal Render Pipeline Asset**.

### 3. Add OutlineObject Components

To control which specific objects get outlined:

1. Add the `OutlineObject` component to GameObjects you want outlined
2. Set the **Outline Rendering Layer** (default: 0, which corresponds to rendering layer mask 1)
3. Make sure the **Outline Rendering Layer Mask** in the Renderer Feature matches (e.g., if using layer 0, set mask to 1)

## How It Works

This system uses a **two-pass approach** with **Rendering Layers**:

### **Pass 1: Render Outline Objects**
- Only renders objects with the specified rendering layer to separate textures
- Creates isolated color, depth, and normal buffers for outline objects only
- Uses Unity's rendering layer filtering system

### **Pass 2: Edge Detection & Compositing**
- Applies **Sobel edge detection** only to the outline object textures
- Detects edges based on depth, normal, and color discontinuities
- Composites the outline back onto the main scene

This approach ensures that:
- ✅ Only objects with `OutlineObject` components get outlined
- ✅ No interference from other scene objects
- ✅ Clean, precise edge detection
- ✅ Separation from GameObject layers (used for collisions, etc.)

## Key Features

✅ **Selective Outlining** - Only objects with OutlineObject components get outlined  
✅ **Rendering Layer Based** - Separate from GameObject layers, won't interfere with collisions  
✅ **Clean, Crisp Outlines** - Professional quality like modern RTS games  
✅ **No Material Changes** - Works with any existing materials  
✅ **Highly Configurable** - Separate sensitivity controls for depth, normals, and color  
✅ **Performance Optimized** - Two-pass approach with proper culling  
✅ **URP Integration** - Proper integration with Universal Render Pipeline  

## Configuration Options

### Outline Appearance
- **Outline Color**: RGBA color of the outlines
- **Outline Thickness**: Controls the sampling distance for edge detection

### Detection Sensitivity
- **Depth Sensitivity** (0-1): How sensitive to depth changes (good for object silhouettes)
- **Normal Sensitivity** (0-1): How sensitive to surface angle changes (good for surface details)
- **Color Sensitivity** (0-1): How sensitive to color changes (good for texture boundaries)

### Rendering Layers
- **Outline Rendering Layer Mask**: Bitmask for which rendering layers to outline
  - Layer 0 = Mask 1
  - Layer 1 = Mask 2  
  - Layer 2 = Mask 4
  - etc. (powers of 2)

## Usage Examples

### Basic Setup
1. Add the Renderer Feature to your URP Renderer Data
2. Add `OutlineObject` component to objects you want outlined
3. Make sure the rendering layer mask matches (default setup works out of the box)

### Multiple Outline Groups
You can have different groups of outlined objects:
1. Set some objects to Rendering Layer 0 (OutlineObject component)
2. Set others to Rendering Layer 1
3. Use different Renderer Features with different layer masks to control each group separately

### Runtime Control
```csharp
// Control individual objects
OutlineObject outline = GetComponent<OutlineObject>();
outline.SetOutlineEnabled(true);
outline.SetOutlineRenderingLayer(1); // Change to rendering layer 1

// Check if object is currently outlined
bool isOutlined = outline.IsCurrentlyOutlined();
```

## Technical Details

- **Algorithm**: Sobel edge detection with 3x3 kernel
- **Rendering**: Two-pass approach with rendering layer filtering
- **Buffers Used**: Separate color, depth, and normal textures for outline objects
- **Performance**: Efficient culling - only outline objects are processed
- **Compatibility**: Unity URP 12.0+
- **Platform Support**: All platforms supported by URP

## Rendering Layers vs GameObject Layers

**GameObject Layers** (0-31):
- Used for collisions, culling, lighting, etc.
- Limited to 32 layers total
- Affects many Unity systems

**Rendering Layers** (0-31):
- Used only for rendering decisions
- Separate from GameObject layers
- Perfect for outline systems
- Won't interfere with collisions or other systems

This system uses **Rendering Layers** so you can keep using GameObject layers for collisions without any conflicts.

## Troubleshooting

**No outlines visible:**
- Check that the OutlineObject component is added to your objects
- Verify the Rendering Layer Mask in the Renderer Feature matches the layer set in OutlineObject
- Check that Depth Texture is enabled in URP settings
- Adjust sensitivity values (try higher values)

**Outlines on wrong objects:**
- Check the Rendering Layer settings on your OutlineObject components
- Verify the Rendering Layer Mask in the Renderer Feature
- Make sure you're not accidentally setting rendering layers elsewhere

**Outlines too thick/thin:**
- Adjust the **Outline Thickness** parameter
- Lower values = thinner outlines, higher values = thicker outlines

**Too many/few edges detected:**
- Fine-tune the **Sensitivity** parameters
- Depth Sensitivity: Controls object silhouettes
- Normal Sensitivity: Controls surface detail edges  
- Color Sensitivity: Controls texture/material boundaries

**Performance issues:**
- Reduce **Outline Thickness** to sample fewer pixels
- Use fewer objects with OutlineObject components
- Consider using multiple rendering layers for different LOD levels 