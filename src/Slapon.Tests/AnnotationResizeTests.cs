using System.Drawing;
using Slapon.Core.Models;

namespace Slapon.Tests;

/// <summary>
/// Tests for the new resize functionality across all annotation types
/// </summary>
public class AnnotationResizeTests
{
    [Fact]
    public void RectangleAnnotation_GetResizeHandle_SelectedAnnotation_ReturnsCorrectHandle()
    {
        // Arrange
        var bounds = new RectangleF(100, 100, 200, 150);
        var rectangle = new RectangleAnnotation(bounds, Color.Red, 1.0f)
        {
            IsSelected = true
        };

        // Test points at corner handles (accounting for inflation and handle size)
        var topLeftPoint = new Point(100 - 6 - 4, 100 - 6 - 4); // bounds corner - inflation - handle offset
        var topRightPoint = new Point(300 + 6 - 4, 100 - 6 - 4);
        var bottomRightPoint = new Point(300 + 6 - 4, 250 + 6 - 4);
        var bottomLeftPoint = new Point(100 - 6 - 4, 250 + 6 - 4);

        // Act & Assert
        Assert.Equal(ResizeHandle.TopLeft, rectangle.GetResizeHandle(topLeftPoint));
        Assert.Equal(ResizeHandle.TopRight, rectangle.GetResizeHandle(topRightPoint));
        Assert.Equal(ResizeHandle.BottomRight, rectangle.GetResizeHandle(bottomRightPoint));
        Assert.Equal(ResizeHandle.BottomLeft, rectangle.GetResizeHandle(bottomLeftPoint));
    }

    [Fact]
    public void RectangleAnnotation_GetResizeHandle_NotSelected_ReturnsNone()
    {
        // Arrange
        var bounds = new RectangleF(50, 50, 100, 100);
        var rectangle = new RectangleAnnotation(bounds, Color.Blue, 1.0f)
        {
            IsSelected = false
        };
        var cornerPoint = new Point(50, 50);

        // Act
        var handle = rectangle.GetResizeHandle(cornerPoint);

        // Assert
        Assert.Equal(ResizeHandle.None, handle);
    }

    [Fact]
    public void RectangleAnnotation_ResizeToHandle_BottomRight_ResizesCorrectly()
    {
        // Arrange
        var initialBounds = new RectangleF(50, 50, 100, 100);
        var rectangle = new RectangleAnnotation(initialBounds, Color.Green, 1.0f)
        {
            IsSelected = true
        };
        var newPosition = new Point(200, 200);

        // Act
        rectangle.ResizeToHandle(ResizeHandle.BottomRight, newPosition);

        // Assert
        var expectedBounds = new RectangleF(50, 50, 150, 150);
        Assert.Equal(expectedBounds, rectangle.Bounds);
    }

    [Fact]
    public void RectangleAnnotation_ResizeToHandle_TopLeft_ResizesCorrectly()
    {
        // Arrange
        var initialBounds = new RectangleF(100, 100, 100, 100);
        var rectangle = new RectangleAnnotation(initialBounds, Color.Purple, 1.0f)
        {
            IsSelected = true
        };
        var newPosition = new Point(50, 75);

        // Act
        rectangle.ResizeToHandle(ResizeHandle.TopLeft, newPosition);

        // Assert
        var expectedBounds = new RectangleF(50, 75, 150, 125);
        Assert.Equal(expectedBounds, rectangle.Bounds);
    }

    [Fact]
    public void RectangleAnnotation_ResizeToHandle_MinimumSize_EnforcesLimits()
    {
        // Arrange
        var initialBounds = new RectangleF(100, 100, 20, 20);
        var rectangle = new RectangleAnnotation(initialBounds, Color.Orange, 1.0f)
        {
            IsSelected = true
        };
        // Try to resize to make it smaller than minimum
        var newPosition = new Point(115, 115);

        // Act
        rectangle.ResizeToHandle(ResizeHandle.BottomRight, newPosition);

        // Assert
        Assert.True(rectangle.Bounds.Width >= 5);
        Assert.True(rectangle.Bounds.Height >= 5);
    }

    [Fact]
    public void CircleAnnotation_GetResizeHandle_InheritsFromBaseAnnotation()
    {
        // Arrange
        var bounds = new RectangleF(50, 50, 100, 100);
        var circle = new CircleAnnotation(bounds, Color.Cyan, 1.0f)
        {
            IsSelected = true
        };
        var topLeftPoint = new Point(50 - 6 - 4, 50 - 6 - 4);

        // Act
        var handle = circle.GetResizeHandle(topLeftPoint);

        // Assert
        Assert.Equal(ResizeHandle.TopLeft, handle);
    }

    [Fact]
    public void LineAnnotation_GetResizeHandle_ArrowStart_ReturnsCorrectHandle()
    {
        // Arrange
        var start = new Point(100, 100);
        var end = new Point(200, 200);
        var line = new LineAnnotation(start, end, Color.Black)
        {
            IsSelected = true
        };
        var startPoint = new Point(100, 100); // Exactly at start

        // Act
        var handle = line.GetResizeHandle(startPoint);

        // Assert
        Assert.Equal(ResizeHandle.ArrowStart, handle);
    }

    [Fact]
    public void LineAnnotation_GetResizeHandle_ArrowEnd_ReturnsCorrectHandle()
    {
        // Arrange
        var start = new Point(50, 50);
        var end = new Point(150, 150);
        var line = new LineAnnotation(start, end, Color.DarkGreen)
        {
            IsSelected = true
        };
        var endPoint = new Point(150, 150); // Exactly at end

        // Act
        var handle = line.GetResizeHandle(endPoint);

        // Assert
        Assert.Equal(ResizeHandle.ArrowEnd, handle);
    }

    [Fact]
    public void LineAnnotation_ResizeToHandle_ArrowStart_MovesStartPoint()
    {
        // Arrange
        var originalStart = new Point(100, 100);
        var originalEnd = new Point(200, 200);
        var line = new LineAnnotation(originalStart, originalEnd, Color.Red)
        {
            IsSelected = true
        };
        var newStartPosition = new Point(80, 90);

        // Act
        line.ResizeToHandle(ResizeHandle.ArrowStart, newStartPosition);

        // Assert - Check that bounds have been updated (line should recalculate bounds)
        Assert.True(line.Bounds.Contains(80, 90)); // New start should be in bounds
        Assert.True(line.Bounds.Contains(200, 200)); // Original end should still be in bounds
    }

    [Fact]
    public void ArrowAnnotation_GetResizeHandle_EndPoint_ReturnsArrowEnd()
    {
        // Arrange
        var start = new Point(10, 10);
        var end = new Point(100, 100);
        var arrow = new ArrowAnnotation(start, end, Color.Blue)
        {
            IsSelected = true
        };
        var endPointNear = new Point(100, 100);

        // Act
        var handle = arrow.GetResizeHandle(endPointNear);

        // Assert
        Assert.Equal(ResizeHandle.ArrowEnd, handle);
    }

    [Fact]
    public void ArrowAnnotation_ResizeToHandle_ArrowEnd_MovesEndPoint()
    {
        // Arrange
        var originalStart = new Point(0, 0);
        var originalEnd = new Point(50, 50);
        var arrow = new ArrowAnnotation(originalStart, originalEnd, Color.Purple)
        {
            IsSelected = true
        };
        var newEndPosition = new Point(75, 75);

        // Act
        arrow.ResizeToHandle(ResizeHandle.ArrowEnd, newEndPosition);

        // Assert - Check that bounds have been updated
        Assert.True(arrow.Bounds.Contains(0, 0)); // Original start should still be in bounds
        Assert.True(arrow.Bounds.Contains(75, 75)); // New end should be in bounds
    }

    [Fact]
    public void TextAnnotation_GetResizeHandle_CustomImplementation_ReturnsCorrectHandle()
    {
        // Arrange
        var location = new Point(100, 100);
        var textAnnotation = new TextAnnotation(location, Color.Black, "Test Text")
        {
            IsSelected = true
        };
        
        // Calculate approximate position for bottom right handle based on text size
        // This is approximate since text size calculation is complex
        var bottomRightApprox = new Point(location.X + 50, location.Y + 20);

        // Act
        var handle = textAnnotation.GetResizeHandle(bottomRightApprox);

        // Assert - Should return some handle (exact one depends on text rendering)
        Assert.NotEqual(ResizeHandle.None, handle);
    }

    [Fact]
    public void TextAnnotation_ResizeToHandle_BottomRight_ScalesText()
    {
        // Arrange
        var location = new Point(50, 50);
        var textAnnotation = new TextAnnotation(location, Color.Black, "Resize Test")
        {
            IsSelected = true
        };
        var originalBounds = textAnnotation.Bounds;
        var newPosition = new Point((int)(originalBounds.Right + 50), (int)(originalBounds.Bottom + 30));

        // Act
        textAnnotation.ResizeToHandle(ResizeHandle.BottomRight, newPosition);

        // Assert
        Assert.True(textAnnotation.Bounds.Width > originalBounds.Width);
        Assert.True(textAnnotation.Bounds.Height > originalBounds.Height);
    }

    [Fact]
    public void BaseAnnotation_HitTest_IncludesResizeHandles()
    {
        // Arrange
        var bounds = new RectangleF(100, 100, 100, 100);
        var rectangle = new RectangleAnnotation(bounds, Color.Red, 1.0f)
        {
            IsSelected = true
        };
        
        // Point outside the main annotation but on a resize handle
        var handlePoint = new Point(100 - 6 - 4, 100 - 6 - 4); // Top-left handle

        // Act
        var hitTest = rectangle.HitTest(handlePoint);

        // Assert
        Assert.True(hitTest); // Should hit because it's on a resize handle
    }

    [Fact]
    public void HighlightAnnotation_InheritsResizeFunctionality_WorksCorrectly()
    {
        // Arrange
        var bounds = new RectangleF(25, 25, 50, 50);
        var highlight = new HighlightAnnotation(bounds, Color.Yellow)
        {
            IsSelected = true
        };
        var newPosition = new Point(100, 100);

        // Act
        highlight.ResizeToHandle(ResizeHandle.BottomRight, newPosition);

        // Assert
        var expectedBounds = new RectangleF(25, 25, 75, 75);
        Assert.Equal(expectedBounds, highlight.Bounds);
    }

    [Fact]
    public void BlurAnnotation_KeepsExistingResizeFunctionality()
    {
        // Arrange
        var bounds = new RectangleF(10, 10, 80, 60);
        var blur = new BlurAnnotation(bounds)
        {
            IsSelected = true
        };
        var topLeftPoint = new Point(10 - 4 - 4, 10 - 4 - 4); // Adjusted for BlurAnnotation's inflation

        // Act
        var handle = blur.GetResizeHandle(topLeftPoint);

        // Assert
        Assert.Equal(ResizeHandle.TopLeft, handle);
    }
}