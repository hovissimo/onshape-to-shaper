# FeatureScript Face to SVG Guide

This guide explains how to use the FeatureScript custom feature to export faces from Onshape as SVG files for CNC machining with Shaper Origin.

## Overview

The `faceToSVG` FeatureScript custom feature:
- ✅ Converts a selected planar face to SVG format
- ✅ Projects 3D edges onto the 2D face plane
- ✅ Handles outer boundaries and holes (inner loops)
- ✅ Configurable units, precision, and orientation
- ✅ Stores SVG as a feature attribute for extraction

## Installation

### Step 1: Create a Custom Feature Studio

1. Go to https://cad.onshape.com
2. Click "Create" → "Feature Studio"
3. Name it "Face to SVG Exporter"

### Step 2: Add the FeatureScript Code

1. Copy the entire contents of `featurescript/faceToSVG.fs`
2. Paste it into the Feature Studio editor
3. Click the checkmark (✓) to compile
4. Fix any errors if they appear

### Step 3: Publish the Feature (Optional but Recommended)

1. Click the version dropdown in the Feature Studio
2. Click "Create version"
3. Add a version name like "v1.0"
4. Click "OK"

This makes the feature available across all your documents.

## Usage

### Step 1: Add Feature to Part Studio

1. Open any Part Studio with the face you want to export
2. Click "Insert" → "Custom features..."
3. Search for "Face to SVG" (or navigate to your Feature Studio)
4. Click to add the feature

### Step 2: Configure the Feature

The feature has several parameters:

| Parameter | Description | Default |
|-----------|-------------|---------|
| **Face to export** | The planar face to convert to SVG | (required) |
| **SVG Units** | Output units (mm, cm, in, etc.) | mm |
| **Decimal precision** | Number of decimal places | 3 |
| **Include dimensions** | Add width/height text to SVG | false |
| **Flip Y axis** | Flip Y for CNC coordinate system | false |

**Configuration steps:**

1. Click the "Face to export" field
2. Click a planar face in the 3D viewer
3. Choose your preferred units (typically mm for Shaper Origin)
4. Adjust precision (3 decimals is usually fine)
5. Check "Flip Y axis" if needed for your CNC machine

### Step 3: Generate SVG

1. Click the green checkmark (✓) to execute the feature
2. The SVG is now generated and stored in the feature

You should see a message like: `SVG generated: 100.50 x 75.25 mm`

## Extracting the SVG

There are two ways to get the SVG data out of Onshape:

### Method 1: Copy from Feature Properties (Manual)

**Note:** This method currently doesn't work because Onshape doesn't show computed parameters in the UI. Use Method 2 instead.

### Method 2: Use the SVG Extractor Tool (Recommended)

We provide a companion tool that extracts the SVG via the Onshape REST API.

#### Setup

```bash
# Set your Onshape API credentials
export ONSHAPE_ACCESS_KEY="your_access_key"
export ONSHAPE_SECRET_KEY="your_secret_key"

# Get credentials from: https://dev-portal.onshape.com/keys
```

#### Extract SVG

```bash
# Build the project
npm run build

# Extract SVG from a document
npm run extract-svg "https://cad.onshape.com/documents/YOUR_DOC_URL" output.svg
```

The tool will:
1. Connect to Onshape using your credentials
2. Find the `faceToSVG` feature in your Part Studio
3. Extract the SVG data
4. Save it to `output.svg`

## Example Workflow

Let's export a rectangular face with a circular hole:

### 1. Create the Part

```
┌─────────────────────────────┐
│                             │
│                             │
│         ╭───────╮           │
│         │   ○   │           │  100mm x 75mm rectangle
│         ╰───────╯           │  with 20mm diameter hole
│                             │
│                             │
└─────────────────────────────┘
```

### 2. Add the Feature

1. Insert "Face to SVG" custom feature
2. Select the top face of the rectangle
3. Set units to "mm"
4. Check "Flip Y axis" (if needed for your CNC)
5. Click ✓

### 3. Extract the SVG

```bash
npm run extract-svg "https://cad.onshape.com/documents/.../w/.../e/..." rectangle-with-hole.svg
```

### 4. Result

You'll get an SVG file like:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg"
     width="100mm" height="75mm"
     viewBox="-5 -5 110 85">
  <desc>Generated from Onshape face - Width: 100mm Height: 75mm</desc>
  <g id="face-outline" fill="none" stroke="black" stroke-width="0.5">
    <path d="M 0 0 L 100 0 L 100 75 L 0 75 Z" />
    <path d="M 50 37.5 ... Z" />  <!-- hole -->
  </g>
</svg>
```

## Current Limitations

The current implementation has some limitations:

### ✅ Implemented
- ✅ Planar face detection and validation
- ✅ Edge extraction and projection to 2D
- ✅ SVG document generation with viewBox
- ✅ Configurable units and precision
- ✅ Bounding box calculation
- ✅ Straight line edge conversion

### ⚠️ Partial / In Progress
- ⚠️ **Arc/Circle edges**: Currently approximated as straight lines
- ⚠️ **Spline/Bezier curves**: Currently approximated as straight lines
- ⚠️ **Inner loops (holes)**: Basic support, needs refinement
- ⚠️ **Edge ordering**: May not produce optimal path ordering

### 📋 Future Enhancements
- 📋 Proper arc-to-SVG conversion (using `A` command)
- 📋 Spline approximation with cubic Bezier curves
- 📋 Edge loop detection and ordering
- 📋 Toolpath generation with offsets
- 📋 Tab/bridge insertion for part holding
- 📋 Multiple face export in one SVG
- 📋 Layer organization (cuts, pockets, etc.)

## Troubleshooting

### "Selected face must be planar"

**Problem:** You selected a curved or non-flat face.

**Solution:** Only planar (flat) faces can be exported to SVG. Use a flat surface like the top of a box, a side panel, etc.

### "No edges found on selected face"

**Problem:** The face has no boundary edges (shouldn't happen in normal cases).

**Solution:** Try selecting a different face or recreating the part.

### SVG Extractor: "Not implemented" error

**Problem:** The API extraction methods are still stubs.

**Solution:** You need to implement the API calls in `src/svg-extractor.ts`:
- `findFaceToSVGFeatures()` - Call `/api/partstudios/.../features`
- `getFeatureSVG()` - Extract the `svgOutput` computed parameter

See the Onshape API documentation for details: https://cad.onshape.com/glassworks/explorer/

### Arcs appear as straight lines

**Problem:** Circular edges are currently approximated.

**Solution:** This is a known limitation. Future versions will properly convert arcs to SVG arc commands. For now, you can:
- Increase tessellation by using more edges
- Manually edit the SVG to use proper arc commands
- Wait for the arc conversion feature to be implemented

## Advanced: Modifying the FeatureScript

### Adding Custom Features

You can extend the FeatureScript to add:

**1. Toolpath Offsets**
```javascript
annotation { "Name" : "Tool radius", "Default" : 3.175 * millimeter }
definition.toolRadius is ValueWithUnits;
```

**2. Tab Generation**
```javascript
annotation { "Name" : "Add tabs for holding" }
definition.addTabs is boolean;

annotation { "Name" : "Tab width" }
definition.tabWidth is ValueWithUnits;
```

**3. Multi-face Export**
```javascript
annotation { "Name" : "Faces to export", "Filter" : EntityType.FACE }
definition.faces is Query; // Allow multiple
```

### Debugging Tips

1. Use `println()` to output debug info:
   ```javascript
   println("Edge count: " ~ size(edgeArray));
   ```

2. Check the FeatureScript console (bottom of editor) for errors

3. Use `debug()` to highlight geometry:
   ```javascript
   debug(context, edges); // Highlights edges in purple
   ```

## Next Steps

Once you have SVG files:

1. **Import to Shaper Origin**: Load the SVG into Shaper Trace or ShaperHub
2. **Set cut depth**: Configure how deep to cut
3. **Add tabs if needed**: Shaper Origin can auto-generate tabs
4. **Generate toolpath**: Let Shaper calculate the cutting path
5. **Execute on workpiece**: Use the Shaper Origin to cut your part

## References

- [FeatureScript Documentation](https://cad.onshape.com/FsDoc/)
- [Onshape API Documentation](https://cad.onshape.com/glassworks/explorer/)
- [SVG Path Specification](https://www.w3.org/TR/SVG/paths.html)
- [Shaper Origin](https://www.shapertools.com/en-us/origin)

## Contributing

Improvements to the FeatureScript are welcome! Areas that need work:

- Arc and circle conversion to SVG arc commands
- Bezier curve approximation for splines
- Proper hole/inner loop detection
- Edge ordering and optimization
- Toolpath generation with offsets

See the code comments in `featurescript/faceToSVG.fs` for TODOs.
