using System.Globalization;
using System.Resources;

namespace FlexiPane.Resources
{
    /// <summary>
    /// Provides access to localized strings for FlexiPane controls
    /// </summary>
    public static class LocalizedStrings
    {
        private static ResourceManager? _resourceManager;
        private static CultureInfo? _resourceCulture;

        /// <summary>
        /// Returns the cached ResourceManager instance used by this class
        /// </summary>
        public static ResourceManager ResourceManager
        {
            get
            {
                if (_resourceManager == null)
                {
                    _resourceManager = new ResourceManager("FlexiPane.Resources.Strings", 
                        typeof(LocalizedStrings).Assembly);
                }
                return _resourceManager;
            }
        }

        /// <summary>
        /// Overrides the current thread's CurrentUICulture property for all resource lookups using this strongly typed resource class
        /// </summary>
        public static CultureInfo? Culture
        {
            get => _resourceCulture;
            set => _resourceCulture = value;
        }

        /// <summary>
        /// Close Pane
        /// </summary>
        public static string ClosePane => ResourceManager.GetString("ClosePane", _resourceCulture) ?? "Close Pane";

        /// <summary>
        /// Split Mode
        /// </summary>
        public static string SplitMode => ResourceManager.GetString("SplitMode", _resourceCulture) ?? "Split Mode";

        /// <summary>
        /// ↑↓ Click top/bottom: Vertical split
        /// </summary>
        public static string VerticalSplitInstruction => ResourceManager.GetString("VerticalSplitInstruction", _resourceCulture) ?? "↑↓ Click top/bottom: Vertical split";

        /// <summary>
        /// ←→ Click left/right: Horizontal split
        /// </summary>
        public static string HorizontalSplitInstruction => ResourceManager.GetString("HorizontalSplitInstruction", _resourceCulture) ?? "←→ Click left/right: Horizontal split";

        /// <summary>
        /// ESC: Exit split mode
        /// </summary>
        public static string ExitSplitMode => ResourceManager.GetString("ExitSplitMode", _resourceCulture) ?? "ESC: Exit split mode";

        /// <summary>
        /// ▲ Vertical Split
        /// </summary>
        public static string VerticalSplitGuide => ResourceManager.GetString("VerticalSplitGuide", _resourceCulture) ?? "▲ Vertical Split";

        /// <summary>
        /// ◀ Horizontal Split
        /// </summary>
        public static string HorizontalSplitGuide => ResourceManager.GetString("HorizontalSplitGuide", _resourceCulture) ?? "◀ Horizontal Split";

        /// <summary>
        /// Main pane cannot be closed.
        /// </summary>
        public static string MainPaneCannotBeClosed => ResourceManager.GetString("MainPaneCannotBeClosed", _resourceCulture) ?? "Main pane cannot be closed.";

        /// <summary>
        /// Split Requested
        /// </summary>
        public static string SplitRequested => ResourceManager.GetString("SplitRequested", _resourceCulture) ?? "Split Requested";
    }
}