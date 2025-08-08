using System.Windows;

namespace FlexiPane.Serialization
{
    /// <summary>
    /// Interface for creating content when loading layouts
    /// </summary>
    public interface IContentFactory
    {
        /// <summary>
        /// Creates content for a pane based on the provided information
        /// </summary>
        /// <param name="paneInfo">Information about the pane being created</param>
        /// <returns>The UI element to place in the pane</returns>
        UIElement CreateContent(PaneInfo paneInfo);

        /// <summary>
        /// Checks if this factory can handle the given content key
        /// </summary>
        /// <param name="contentKey">The content key to check</param>
        /// <returns>True if this factory can create content for this key</returns>
        bool CanCreateContent(string contentKey);
    }

    /// <summary>
    /// Delegate for creating content during layout loading
    /// </summary>
    public delegate UIElement ContentCreationDelegate(PaneInfo paneInfo);
}