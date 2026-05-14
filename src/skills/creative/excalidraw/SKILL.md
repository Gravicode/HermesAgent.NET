# excalidraw

> Create hand-drawn style diagrams using Excalidraw JSON format. Generate .excalidraw files for architecture diagrams, flowcharts, sequence diagrams, and more.

<!-- created: 2026-01-01T00:00:00Z -->
<!-- updated: 2026-01-01T00:00:00Z -->
<!-- usage_count: 0 -->
<!-- tags: diagrams, visualization, architecture, creative -->

## Overview

Excalidraw is a virtual whiteboard for sketching diagrams with a hand-drawn aesthetic.
Generate `.excalidraw` files that can be opened at [excalidraw.com](https://excalidraw.com) or with the desktop app.

## File Structure

```json
{
  "type": "excalidraw",
  "version": 2,
  "source": "https://excalidraw.com",
  "elements": [...],
  "appState": { "gridSize": null, "viewBackgroundColor": "#ffffff" },
  "files": {}
}
```

## Common Element Types

### Rectangle / Box
```json
{
  "id": "box1",
  "type": "rectangle",
  "x": 100, "y": 100, "width": 200, "height": 60,
  "strokeColor": "#1e1e1e",
  "backgroundColor": "#a5d8ff",
  "fillStyle": "solid",
  "roughness": 1,
  "roundness": {"type": 3},
  "label": {"text": "Service A"}
}
```

### Arrow / Connection
```json
{
  "id": "arrow1",
  "type": "arrow",
  "startBinding": {"elementId": "box1", "focus": 0, "gap": 5},
  "endBinding":   {"elementId": "box2", "focus": 0, "gap": 5},
  "points": [[0,0],[200,0]]
}
```

### Text
```json
{
  "id": "text1",
  "type": "text",
  "x": 120, "y": 120,
  "text": "My Label",
  "fontSize": 16,
  "fontFamily": 1
}
```

## Workflow

```bash
# Generate diagram and write to file
cat > architecture.excalidraw << 'EOF'
{ "type": "excalidraw", "version": 2, "elements": [...], "appState": {}, "files": {} }
EOF

# Open in browser
xdg-open https://excalidraw.com  # then File > Open
```

## Color Palette (Excalidraw defaults)

- Blue: `#a5d8ff`
- Green: `#b2f2bb`  
- Yellow: `#ffec99`
- Red: `#ffc9c9`
- Purple: `#d0bfff`
- Stroke: `#1e1e1e`

## Tips

- Use `roughness: 1` for hand-drawn look, `roughness: 0` for clean
- Group related elements with `groupIds`
- Export as PNG/SVG from excalidraw.com for embedding in docs
