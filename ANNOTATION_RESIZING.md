# Annotation Resizing Feature

## Overview

The annotation resizing functionality allows users to interactively resize annotations by dragging resize handles when an annotation is selected. This feature enhances the usability and flexibility of the annotation tool.

## Supported Annotation Types

### Rectangle, Circle, Highlight Annotations
- **Resize Handles**: Four corner handles (TopLeft, TopRight, BottomRight, BottomLeft)
- **Behavior**: Corner dragging resizes the annotation while maintaining the opposite corner position
- **Minimum Size**: Enforced minimum size of 5x5 pixels to prevent annotations from becoming too small

### Text Annotations  
- **Resize Handles**: Four corner handles (same as shapes)
- **Behavior**: Proportional text scaling - when resized, the text content scales proportionally to maintain readability
- **Minimum Size**: Enforced minimum size of 20x20 pixels for text annotations

### Line and Arrow Annotations
- **Resize Handles**: Two endpoint handles (ArrowStart, ArrowEnd) 
- **Behavior**: Independent endpoint positioning - each end can be moved independently to change length, direction, and angle
- **Visual Feedback**: Endpoint handles are displayed as circles at the line/arrow endpoints

### Blur Annotations
- **Resize Handles**: Four corner handles (maintains existing implementation)
- **Behavior**: Standard rectangular resizing

## User Interaction

### Selection and Resize Mode
1. **Select Tool**: Switch to the Select tool (cursor icon) in the toolbar
2. **Select Annotation**: Click on any annotation to select it
3. **Resize Handles**: Selected annotations display resize handles at appropriate positions
4. **Resize Operation**: Click and drag any resize handle to resize the annotation

### Visual Feedback
- **Resize Handles**: Blue circular handles with white borders
- **Cursor Changes**: 
  - Corner resize: Diagonal resize cursors (↖↘ or ↗↙)
  - Endpoint resize: Size-all cursor (✥)
  - Move operation: Size-all cursor when over annotation body

### Real-time Updates
- Annotations resize in real-time as handles are dragged
- The annotated image is automatically copied to clipboard after resize operations
- Undo/redo operations are supported through the command system

## Technical Implementation

### Interface Extensions
- Added `GetResizeHandle(Point point)` method to `IAnnotation` interface
- Added `ResizeToHandle(ResizeHandle handle, Point newPosition)` method to `IAnnotation` interface
- Enhanced `HitTest(Point point)` to include resize handle detection

### ResizeHandle Enumeration
```csharp
public enum ResizeHandle
{
    None,
    TopLeft,
    TopRight, 
    BottomLeft,
    BottomRight,
    ArrowStart,
    ArrowEnd
}
```

### Mouse Interaction Flow
1. **MouseDown**: Detect if click is on resize handle vs annotation body
2. **MouseMove**: Update annotation size/position based on handle being dragged
3. **MouseUp**: Complete resize operation and update clipboard

## Benefits

- **Intuitive User Experience**: Standard resize behavior familiar from other graphics applications
- **Flexible Annotation Editing**: Users can easily adjust annotation size, shape, and direction after creation
- **Visual Feedback**: Clear visual indicators for resize operations and appropriate cursor changes
- **Type-Specific Behavior**: Each annotation type has appropriate resize behavior (corners for shapes, endpoints for lines)