using System.Windows;
using System.Windows.Markup;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,            //where theme specific resource dictionaries are located
                                                //(used if a resource is not found in the page,
                                                // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly   //where the generic resource dictionary is located
                                                //(used if a resource is not found in the page,
                                                // app, or any theme specific resource dictionaries)
)]

// XML namespace definitions for easier XAML usage
// This allows using xmlns:flexi="http://schemas.flexipane.com/wpf" instead of clr-namespace
[assembly: XmlnsDefinition("http://schemas.flexipane.com/wpf", "FlexiPane.Controls")]
[assembly: XmlnsDefinition("http://schemas.flexipane.com/wpf", "FlexiPane.Events")]
[assembly: XmlnsDefinition("http://schemas.flexipane.com/wpf", "FlexiPane.Serialization")]
[assembly: XmlnsDefinition("http://schemas.flexipane.com/wpf", "FlexiPane.Converters")]
[assembly: XmlnsDefinition("http://schemas.flexipane.com/wpf", "FlexiPane.Managers")]

// Optional: Add a prefix recommendation
[assembly: XmlnsPrefix("http://schemas.flexipane.com/wpf", "flexi")]

// Alternative shorter namespace
[assembly: XmlnsDefinition("https://flexipane.wpf", "FlexiPane.Controls")]
[assembly: XmlnsDefinition("https://flexipane.wpf", "FlexiPane.Events")]
[assembly: XmlnsDefinition("https://flexipane.wpf", "FlexiPane.Serialization")]
[assembly: XmlnsDefinition("https://flexipane.wpf", "FlexiPane.Converters")]
[assembly: XmlnsDefinition("https://flexipane.wpf", "FlexiPane.Managers")]

// Prefix for the shorter namespace
[assembly: XmlnsPrefix("https://flexipane.wpf", "fp")]
