using System;
using System.Windows;
using FlexiPane.Controls;

namespace FlexiPane.Events
{
    /// <summary>
    /// Content request event handler delegate
    /// </summary>
    public delegate void ContentRequestedEventHandler(object sender, ContentRequestedEventArgs e);

    /// <summary>
    /// Panel split request event handler delegate
    /// </summary>
    public delegate void PaneSplitRequestedEventHandler(object sender, PaneSplitRequestedEventArgs e);

    /// <summary>
    /// Panel closing request event handler delegate
    /// </summary>
    public delegate void PaneClosingEventHandler(object sender, PaneClosingEventArgs e);

    /// <summary>
    /// Last panel closing request event handler delegate
    /// </summary>
    public delegate void LastPaneClosingEventHandler(object sender, LastPaneClosingEventArgs e);

    /// <summary>
    /// Split mode changed event handler delegate
    /// </summary>
    public delegate void SplitModeChangedEventHandler(object sender, SplitModeChangedEventArgs e);

    /// <summary>
    /// New panel created event handler delegate
    /// </summary>
    public delegate void NewPaneCreatedEventHandler(object sender, NewPaneCreatedEventArgs e);

    /// <summary>
    /// Panel split request event data
    /// </summary>
    public class PaneSplitRequestedEventArgs : RoutedEventArgs
    {
        public PaneSplitRequestedEventArgs(FlexiPaneItem sourcePane, bool isVerticalSplit, double splitRatio)
        {
            SourcePane = sourcePane ?? throw new ArgumentNullException(nameof(sourcePane));
            IsVerticalSplit = isVerticalSplit;
            SplitRatio = Math.Max(0.1, Math.Min(0.9, splitRatio));
        }

        /// <summary>
        /// Panel requesting the split
        /// </summary>
        public FlexiPaneItem SourcePane { get; }

        /// <summary>
        /// Split direction (true: vertical/left-right, false: horizontal/top-bottom)
        /// </summary>
        public bool IsVerticalSplit { get; }

        /// <summary>
        /// Split ratio (0.1 ~ 0.9)
        /// </summary>
        public double SplitRatio { get; }

        /// <summary>
        /// Content for the new panel
        /// </summary>
        public object? NewContent { get; set; }

        /// <summary>
        /// Whether to cancel the split
        /// </summary>
        public bool Cancel { get; set; }
    }

    /// <summary>
    /// Panel closing request event data (cancellable)
    /// </summary>
    public class PaneClosingEventArgs : RoutedEventArgs
    {
        public PaneClosingEventArgs(FlexiPaneItem pane, PaneCloseReason reason)
        {
            Pane = pane ?? throw new ArgumentNullException(nameof(pane));
            Reason = reason;
        }

        /// <summary>
        /// Panel being closed
        /// </summary>
        public FlexiPaneItem Pane { get; }

        /// <summary>
        /// Reason for closing
        /// </summary>
        public PaneCloseReason Reason { get; }

        /// <summary>
        /// Whether to cancel the close
        /// </summary>
        public bool Cancel { get; set; }
    }

    /// <summary>
    /// Panel closed event data
    /// </summary>
    public class PaneClosedEventArgs : EventArgs
    {
        public PaneClosedEventArgs(FlexiPaneItem pane, PaneCloseReason reason)
        {
            Pane = pane ?? throw new ArgumentNullException(nameof(pane));
            Reason = reason;
        }

        /// <summary>
        /// Panel that was closed
        /// </summary>
        public FlexiPaneItem Pane { get; }

        /// <summary>
        /// Reason for closing
        /// </summary>
        public PaneCloseReason Reason { get; }
    }

    /// <summary>
    /// Last panel closing request event data (cancellable)
    /// </summary>
    public class LastPaneClosingEventArgs : RoutedEventArgs
    {
        public LastPaneClosingEventArgs(FlexiPaneItem pane, PaneCloseReason reason)
        {
            Pane = pane ?? throw new ArgumentNullException(nameof(pane));
            Reason = reason;
        }

        /// <summary>
        /// Last remaining panel
        /// </summary>
        public FlexiPaneItem Pane { get; }

        /// <summary>
        /// Reason for closing
        /// </summary>
        public PaneCloseReason Reason { get; }

        /// <summary>
        /// Whether to cancel the close (default: false - allow close)
        /// </summary>
        public bool Cancel { get; set; }
    }

    /// <summary>
    /// Split mode changed event data
    /// </summary>
    public class SplitModeChangedEventArgs : RoutedEventArgs
    {
        public SplitModeChangedEventArgs(bool isActive, bool oldValue)
        {
            IsActive = isActive;
            OldValue = oldValue;
        }

        /// <summary>
        /// Current split mode activation state
        /// </summary>
        public bool IsActive { get; }

        /// <summary>
        /// Previous state
        /// </summary>
        public bool OldValue { get; }

        /// <summary>
        /// Whether to cancel the change
        /// </summary>
        public bool Cancel { get; set; }
    }

    /// <summary>
    /// New panel created event data
    /// </summary>
    public class NewPaneCreatedEventArgs : RoutedEventArgs
    {
        public NewPaneCreatedEventArgs(FlexiPaneItem newPane, FlexiPaneItem? sourcePane)
        {
            NewPane = newPane ?? throw new ArgumentNullException(nameof(newPane));
            SourcePane = sourcePane;
        }

        /// <summary>
        /// Newly created panel
        /// </summary>
        public FlexiPaneItem NewPane { get; }

        /// <summary>
        /// Source panel (if created by split)
        /// </summary>
        public FlexiPaneItem? SourcePane { get; }
    }

    /// <summary>
    /// Content request event args - used when FlexiPanel needs content for initial panes
    /// </summary>
    public class ContentRequestedEventArgs : RoutedEventArgs
    {
        public ContentRequestedEventArgs(string purpose)
        {
            Purpose = purpose ?? throw new ArgumentNullException(nameof(purpose));
        }

        /// <summary>
        /// Purpose of the content request (e.g., "InitialPane", "NewPane")
        /// </summary>
        public string Purpose { get; }

        /// <summary>
        /// Requested content - will be set by the event handler
        /// </summary>
        public object? RequestedContent { get; set; }

        /// <summary>
        /// Whether the request was handled
        /// </summary>
        public bool Handled { get; set; }
    }

    /// <summary>
    /// Panel close reason
    /// </summary>
    public enum PaneCloseReason
    {
        /// <summary>
        /// User request (X button click, etc.)
        /// </summary>
        UserRequest,

        /// <summary>
        /// Programmatic request
        /// </summary>
        ProgrammaticRequest,

        /// <summary>
        /// Cleanup by parent container
        /// </summary>
        ParentCleanup,

        /// <summary>
        /// Application shutdown
        /// </summary>
        ApplicationShutdown
    }
}