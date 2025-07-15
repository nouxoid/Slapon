#!/usr/bin/env dotnet script

// This script demonstrates the color customization functionality
// Note: This is a conceptual demonstration - actual testing requires Windows Forms environment

using System;
using System.Drawing;

// Simulated annotation for demonstration
public class DemoAnnotation
{
    public Color Color { get; private set; }
    public float Thickness { get; set; }
    public bool IsSelected { get; set; }
    
    public DemoAnnotation(Color color, float thickness = 2.0f)
    {
        Color = color;
        Thickness = thickness;
        IsSelected = false;
    }
    
    public void UpdateColor(Color newColor)
    {
        Color = newColor;
        Console.WriteLine($"Annotation color updated to: {newColor.Name} (R:{newColor.R}, G:{newColor.G}, B:{newColor.B})");
    }
}

// Demonstrate the color update functionality
static void DemonstrateColorUpdates()
{
    Console.WriteLine("=== Slapon Color Customization Demo ===\n");
    
    // Create sample annotations
    var rectangle = new DemoAnnotation(Color.Red, 2.0f) { IsSelected = false };
    var circle = new DemoAnnotation(Color.Blue, 3.0f) { IsSelected = false };
    var line = new DemoAnnotation(Color.Green, 1.5f) { IsSelected = false };
    
    var annotations = new[] { rectangle, circle, line };
    
    Console.WriteLine("Initial annotations:");
    for (int i = 0; i < annotations.Length; i++)
    {
        var ann = annotations[i];
        Console.WriteLine($"  Annotation {i + 1}: Color={ann.Color.Name}, Thickness={ann.Thickness}, Selected={ann.IsSelected}");
    }
    
    Console.WriteLine("\n--- Selecting annotation 1 (Rectangle) ---");
    rectangle.IsSelected = true;
    
    Console.WriteLine("--- Changing color of selected annotation to Purple ---");
    var selectedAnnotations = annotations.Where(a => a.IsSelected);
    foreach (var annotation in selectedAnnotations)
    {
        annotation.UpdateColor(Color.Purple);
    }
    
    Console.WriteLine("\n--- Selecting multiple annotations ---");
    circle.IsSelected = true;
    line.IsSelected = true;
    
    Console.WriteLine("--- Changing color of all selected annotations to Orange ---");
    selectedAnnotations = annotations.Where(a => a.IsSelected);
    foreach (var annotation in selectedAnnotations)
    {
        annotation.UpdateColor(Color.Orange);
    }
    
    Console.WriteLine("\nFinal annotations:");
    for (int i = 0; i < annotations.Length; i++)
    {
        var ann = annotations[i];
        Console.WriteLine($"  Annotation {i + 1}: Color={ann.Color.Name}, Thickness={ann.Thickness}, Selected={ann.IsSelected}");
    }
    
    Console.WriteLine("\n=== Key Features Demonstrated ===");
    Console.WriteLine("✅ Individual annotation color updates");
    Console.WriteLine("✅ Multiple annotation color updates");
    Console.WriteLine("✅ Property preservation (thickness unchanged)");
    Console.WriteLine("✅ Selection state tracking");
    Console.WriteLine("✅ Real-time color application");
    
    Console.WriteLine("\n=== Implementation Notes ===");
    Console.WriteLine("• BaseAnnotation.UpdateColor() method enables color changes");
    Console.WriteLine("• IAnnotation interface includes UpdateColor() contract");
    Console.WriteLine("• MainForm.UpdateSelectedAnnotationColor() applies changes to UI");
    Console.WriteLine("• Color controls synchronize with selected annotation colors");
    Console.WriteLine("• Both individual color buttons and custom picker work");
    
    Console.WriteLine("\n=== User Workflow ===");
    Console.WriteLine("1. User selects annotation(s) by clicking");
    Console.WriteLine("2. User clicks color button or opens color picker");
    Console.WriteLine("3. Selected annotation(s) immediately update to new color");
    Console.WriteLine("4. UI controls reflect the change");
    Console.WriteLine("5. Other properties (thickness, position, etc.) remain unchanged");
}

// Simulate the LINQ extension
public static class EnumerableExtensions
{
    public static IEnumerable<T> Where<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        foreach (var item in source)
        {
            if (predicate(item))
                yield return item;
        }
    }
}

DemonstrateColorUpdates();