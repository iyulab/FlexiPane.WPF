using System;
using System.IO;
using System.Xml.Serialization;
using FlexiPane.Serialization;

Console.WriteLine("Testing XML Serialization Fix for nullable types...\n");

// Create a layout with nullable values
var layout = new FlexiPaneLayout
{
    Name = "Test Layout",
    WindowWidth = 1920,
    WindowHeight = 1080,
    RootNode = LayoutNode.CreateContainer(
        SplitOrientation.Horizontal,
        0.3,
        LayoutNode.CreatePane("explorer"),
        LayoutNode.CreateContainer(
            SplitOrientation.Vertical,
            0.7,
            LayoutNode.CreatePane("editor"),
            LayoutNode.CreatePane("terminal")
        )
    )
};

// Also test with null window dimensions
var layoutWithNulls = new FlexiPaneLayout
{
    Name = "Layout with nulls",
    WindowWidth = null,
    WindowHeight = null,
    RootNode = LayoutNode.CreatePane("single")
};

// Test serialization
try
{
    var serializer = new XmlSerializer(typeof(FlexiPaneLayout));
    
    Console.WriteLine("Testing layout WITH window dimensions:");
    Console.WriteLine("=====================================");
    
    // Serialize to string
    using (var writer = new StringWriter())
    {
        serializer.Serialize(writer, layout);
        var xml = writer.ToString();
        
        Console.WriteLine("✅ Serialization successful!");
        Console.WriteLine("\nGenerated XML (first 500 chars):");
        Console.WriteLine(xml.Substring(0, Math.Min(500, xml.Length)));
        
        // Test deserialization
        using (var reader = new StringReader(xml))
        {
            var deserializedLayout = (FlexiPaneLayout)serializer.Deserialize(reader);
            
            Console.WriteLine("\n✅ Deserialization successful!");
            Console.WriteLine($"Layout Name: {deserializedLayout.Name}");
            Console.WriteLine($"Window Width: {deserializedLayout.WindowWidth}");
            Console.WriteLine($"Window Height: {deserializedLayout.WindowHeight}");
            Console.WriteLine($"Pane Count: {deserializedLayout.CountPanes()}");
        }
    }
    
    Console.WriteLine("\n\nTesting layout WITHOUT window dimensions:");
    Console.WriteLine("=========================================");
    
    // Test with nulls
    using (var writer = new StringWriter())
    {
        serializer.Serialize(writer, layoutWithNulls);
        var xml = writer.ToString();
        
        Console.WriteLine("✅ Serialization with nulls successful!");
        Console.WriteLine("\nGenerated XML (first 500 chars):");
        Console.WriteLine(xml.Substring(0, Math.Min(500, xml.Length)));
        
        // Test deserialization
        using (var reader = new StringReader(xml))
        {
            var deserializedLayout = (FlexiPaneLayout)serializer.Deserialize(reader);
            
            Console.WriteLine("\n✅ Deserialization successful!");
            Console.WriteLine($"Layout Name: {deserializedLayout.Name}");
            Console.WriteLine($"Window Width: {deserializedLayout.WindowWidth ?? 0} (null: {deserializedLayout.WindowWidth == null})");
            Console.WriteLine($"Window Height: {deserializedLayout.WindowHeight ?? 0} (null: {deserializedLayout.WindowHeight == null})");
            Console.WriteLine($"Pane Count: {deserializedLayout.CountPanes()}");
        }
    }
    
    Console.WriteLine("\n✅ All tests passed! XML serialization is working correctly.");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"   Inner: {ex.InnerException.Message}");
    }
    Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
}