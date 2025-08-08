using System.Xml.Serialization;
using System.Linq;

namespace FlexiPane.Serialization
{
    /// <summary>
    /// Root layout document containing metadata and tree structure
    /// </summary>
    [XmlRoot("FlexiPaneLayout")]
    public class FlexiPaneLayout
    {
        /// <summary>
        /// Layout format version for compatibility
        /// </summary>
        [XmlAttribute("version")]
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Timestamp when layout was saved
        /// </summary>
        [XmlAttribute("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional layout name
        /// </summary>
        [XmlAttribute("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Optional layout description
        /// </summary>
        [XmlElement("Description")]
        public string? Description { get; set; }

        /// <summary>
        /// Window width when layout was saved
        /// </summary>
        [XmlIgnore]
        public double? WindowWidth { get; set; }

        /// <summary>
        /// Window height when layout was saved
        /// </summary>
        [XmlIgnore]
        public double? WindowHeight { get; set; }

        /// <summary>
        /// Serializable window width
        /// </summary>
        [XmlAttribute("windowWidth")]
        public double SerializableWindowWidth
        {
            get => WindowWidth ?? 0;
            set => WindowWidth = value > 0 ? value : null;
        }

        /// <summary>
        /// Serializable window height
        /// </summary>
        [XmlAttribute("windowHeight")]
        public double SerializableWindowHeight
        {
            get => WindowHeight ?? 0;
            set => WindowHeight = value > 0 ? value : null;
        }

        /// <summary>
        /// Controls whether WindowWidth should be serialized
        /// </summary>
        public bool ShouldSerializeSerializableWindowWidth()
        {
            return WindowWidth.HasValue && WindowWidth.Value > 0;
        }

        /// <summary>
        /// Controls whether WindowHeight should be serialized
        /// </summary>
        public bool ShouldSerializeSerializableWindowHeight()
        {
            return WindowHeight.HasValue && WindowHeight.Value > 0;
        }

        /// <summary>
        /// The root node of the layout tree
        /// </summary>
        [XmlElement("RootNode")]
        public LayoutNode? RootNode { get; set; }

        /// <summary>
        /// Application-specific metadata
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, string>? Metadata { get; set; }

        /// <summary>
        /// Serializable wrapper for metadata
        /// </summary>
        [XmlArray("Metadata")]
        [XmlArrayItem("Item")]
        public SerializableProperty[]? SerializableMetadata
        {
            get
            {
                if (Metadata == null || Metadata.Count == 0)
                    return null;
                return Metadata.Select(kv => new SerializableProperty { Key = kv.Key, Value = kv.Value }).ToArray();
            }
            set
            {
                if (value == null || value.Length == 0)
                {
                    Metadata = null;
                }
                else
                {
                    Metadata = value.ToDictionary(p => p.Key, p => p.Value);
                }
            }
        }

        /// <summary>
        /// Creates an empty layout
        /// </summary>
        public FlexiPaneLayout()
        {
            Metadata = new Dictionary<string, string>();
        }

        /// <summary>
        /// Creates a layout with a single pane
        /// </summary>
        public static FlexiPaneLayout CreateSinglePane(string? contentKey = null)
        {
            return new FlexiPaneLayout
            {
                RootNode = LayoutNode.CreatePane(contentKey)
            };
        }

        /// <summary>
        /// Validates the layout structure
        /// </summary>
        public bool Validate(out string? error)
        {
            error = null;

            if (RootNode == null)
            {
                error = "Layout has no root node";
                return false;
            }

            if (!Version.StartsWith("1."))
            {
                error = $"Unsupported layout version: {Version}";
                return false;
            }

            return RootNode.Validate(out error);
        }

        /// <summary>
        /// Gets all pane nodes in the layout
        /// </summary>
        public IEnumerable<LayoutNode> GetAllPanes()
        {
            if (RootNode == null)
                yield break;

            foreach (var node in GetPanesRecursive(RootNode))
            {
                yield return node;
            }
        }

        private IEnumerable<LayoutNode> GetPanesRecursive(LayoutNode node)
        {
            if (node.NodeType == LayoutNodeType.Pane)
            {
                yield return node;
            }
            else if (node.NodeType == LayoutNodeType.Container)
            {
                if (node.FirstChild != null)
                {
                    foreach (var child in GetPanesRecursive(node.FirstChild))
                        yield return child;
                }
                if (node.SecondChild != null)
                {
                    foreach (var child in GetPanesRecursive(node.SecondChild))
                        yield return child;
                }
            }
        }

        /// <summary>
        /// Counts total panes in the layout
        /// </summary>
        public int CountPanes()
        {
            return GetAllPanes().Count();
        }
    }
}