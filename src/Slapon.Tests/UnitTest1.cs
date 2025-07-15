using System.Drawing;
using Slapon.Core.Models;
using Slapon.Core.Services;

namespace Slapon.Tests;

public class CircleAnnotationTests
{
    [Fact]
    public void CircleAnnotation_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var bounds = new RectangleF(10, 20, 100, 80);
        var color = Color.Red;
        var opacity = 0.7f;

        // Act
        var circle = new CircleAnnotation(bounds, color, opacity);

        // Assert
        Assert.Equal(bounds, circle.Bounds);
        Assert.Equal(color, circle.Color);
        Assert.Equal(opacity, circle.Opacity);
        Assert.Equal(2f, circle.BorderThickness);
        Assert.False(circle.IsSelected);
        Assert.NotEqual(Guid.Empty, circle.Id);
    }

    [Fact]
    public void CircleAnnotation_Contains_PointInsideCircle_ReturnsTrue()
    {
        // Arrange - Create a circle with center at (50, 50) and radius 25
        var bounds = new RectangleF(25, 25, 50, 50);
        var circle = new CircleAnnotation(bounds, Color.Blue, 1.0f);
        var pointInsideCircle = new PointF(50, 50); // Center point

        // Act
        var result = circle.Contains(pointInsideCircle);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CircleAnnotation_Contains_PointOutsideCircle_ReturnsFalse()
    {
        // Arrange - Create a circle with center at (50, 50) and radius 25
        var bounds = new RectangleF(25, 25, 50, 50);
        var circle = new CircleAnnotation(bounds, Color.Blue, 1.0f);
        var pointOutsideCircle = new PointF(100, 100); // Far outside

        // Act
        var result = circle.Contains(pointOutsideCircle);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CircleAnnotation_Contains_PointOnBorder_ReturnsTrue()
    {
        // Arrange - Create a circle with center at (50, 50) and radius 25
        var bounds = new RectangleF(25, 25, 50, 50);
        var circle = new CircleAnnotation(bounds, Color.Blue, 1.0f);
        var pointOnBorder = new PointF(75, 50); // On the right edge

        // Act
        var result = circle.Contains(pointOnBorder);

        // Assert
        Assert.True(result); // Should be true due to border thickness tolerance
    }

    [Fact]
    public void CircleAnnotation_HitTest_UsesContainsMethod()
    {
        // Arrange
        var bounds = new RectangleF(0, 0, 100, 100);
        var circle = new CircleAnnotation(bounds, Color.Green, 0.8f);
        var point = new Point(50, 50);

        // Act
        var result = circle.HitTest(point);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CircleAnnotation_Clone_CreatesIdenticalCopy()
    {
        // Arrange
        var bounds = new RectangleF(10, 20, 80, 60);
        var color = Color.Purple;
        var opacity = 0.9f;
        var original = new CircleAnnotation(bounds, color, opacity)
        {
            BorderThickness = 3f,
            IsSelected = true
        };

        // Act
        var clone = (CircleAnnotation)original.Clone();

        // Assert
        Assert.NotEqual(original.Id, clone.Id); // Should have different IDs
        Assert.Equal(original.Bounds, clone.Bounds);
        Assert.Equal(original.Color, clone.Color);
        Assert.Equal(original.Opacity, clone.Opacity);
        Assert.Equal(original.BorderThickness, clone.BorderThickness);
        Assert.Equal(original.IsSelected, clone.IsSelected);
    }

    [Fact]
    public void CircleAnnotation_Move_UpdatesPosition()
    {
        // Arrange
        var initialBounds = new RectangleF(10, 20, 50, 50);
        var circle = new CircleAnnotation(initialBounds, Color.Black, 1.0f);
        var deltaX = 15;
        var deltaY = 25;

        // Act
        circle.Move(deltaX, deltaY);

        // Assert
        var expectedBounds = new RectangleF(25, 45, 50, 50);
        Assert.Equal(expectedBounds, circle.Bounds);
    }

    [Fact]
    public void CircleAnnotation_Resize_ScalesDimensions()
    {
        // Arrange
        var initialBounds = new RectangleF(10, 20, 40, 60);
        var circle = new CircleAnnotation(initialBounds, Color.Orange, 0.5f)
        {
            BorderThickness = 4f
        };
        var scaleX = 2.0f;
        var scaleY = 1.5f;

        // Act
        circle.Resize(scaleX, scaleY);

        // Assert
        var expectedBounds = new RectangleF(20, 30, 80, 90);
        Assert.Equal(expectedBounds, circle.Bounds);
        Assert.Equal(6f, circle.BorderThickness); // Should scale by minimum scale factor (1.5f) * 4f = 6f
    }

    [Fact]
    public void CircleAnnotation_Contains_ZeroSizeCircle_ReturnsFalse()
    {
        // Arrange
        var bounds = new RectangleF(50, 50, 0, 0);
        var circle = new CircleAnnotation(bounds, Color.Red, 1.0f);
        var point = new PointF(50, 50);

        // Act
        var result = circle.Contains(point);

        // Assert
        Assert.False(result); // Zero-size circle should not contain any points
    }
}

public class AnnotationFactoryTests
{
    [Fact]
    public void AnnotationFactory_CreateCircleAnnotation_ReturnsCircleAnnotation()
    {
        // Arrange
        var factory = new AnnotationFactory();
        var bounds = new RectangleF(0, 0, 100, 100);
        var color = Color.Blue;
        var opacity = 0.8f;

        // Act
        var annotation = factory.CreateAnnotation(AnnotationType.Circle, bounds, color, opacity);

        // Assert
        Assert.IsType<CircleAnnotation>(annotation);
        Assert.Equal(bounds, annotation.Bounds);
        Assert.Equal(color, annotation.Color);
        Assert.Equal(opacity, annotation.Opacity);
    }

    [Fact]
    public void AnnotationFactory_CreateCircleAnnotation_WithDefaultOpacity_UsesDefaultValue()
    {
        // Arrange
        var factory = new AnnotationFactory();
        var bounds = new RectangleF(10, 10, 50, 50);
        var color = Color.Green;

        // Act
        var annotation = factory.CreateAnnotation(AnnotationType.Circle, bounds, color);

        // Assert
        Assert.IsType<CircleAnnotation>(annotation);
        Assert.Equal(0.8f, annotation.Opacity);
    }
}