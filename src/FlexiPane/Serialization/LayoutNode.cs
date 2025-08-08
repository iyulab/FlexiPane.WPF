using System.Xml.Serialization;
using System.Linq;

namespace FlexiPane.Serialization
{
    /// <summary>
    /// Serializable key-value pair for XML serialization
    /// </summary>
    public class SerializableProperty
    {
        [XmlAttribute("key")]
        public string Key { get; set; } = string.Empty;
        
        [XmlAttribute("value")]
        public string Value { get; set; } = string.Empty;
    }
    /// <summary>
    /// Represents a node in the layout tree for serialization
    /// </summary>
    [XmlInclude(typeof(LayoutNode))]
    public class LayoutNode
    {
        /// <summary>
        /// Type of the node
        /// </summary>
        [XmlAttribute("type")]
        public LayoutNodeType NodeType { get; set; }

        /// <summary>
        /// Unique identifier for pane nodes
        /// </summary>
        [XmlAttribute("id")]
        public string? Id { get; set; }

        /// <summary>
        /// User-defined key for content mapping
        /// </summary>
        [XmlAttribute("contentKey")]
        public string? ContentKey { get; set; }

        /// <summary>
        /// Split orientation for container nodes
        /// </summary>
        [XmlIgnore]
        public SplitOrientation? Orientation { get; set; }

        /// <summary>
        /// Split ratio for container nodes (0.0 to 1.0)
        /// </summary>
        [XmlIgnore]
        public double? SplitRatio { get; set; }

        /// <summary>
        /// Serializable orientation
        /// </summary>
        [XmlAttribute("orientation")]
        public SplitOrientation SerializableOrientation
        {
            get => Orientation ?? SplitOrientation.Horizontal;
            set => Orientation = value;
        }

        /// <summary>
        /// Serializable split ratio
        /// </summary>
        [XmlAttribute("splitRatio")]
        public double SerializableSplitRatio
        {
            get => SplitRatio ?? 0.5;
            set => SplitRatio = value;
        }

        /// <summary>
        /// Controls whether orientation should be serialized
        /// </summary>
        public bool ShouldSerializeSerializableOrientation()
        {
            return NodeType == LayoutNodeType.Container;
        }

        /// <summary>
        /// Controls whether split ratio should be serialized
        /// </summary>
        public bool ShouldSerializeSerializableSplitRatio()
        {
            return NodeType == LayoutNodeType.Container;
        }

        /// <summary>
        /// First child node (for containers)
        /// </summary>
        [XmlElement("FirstChild")]
        public LayoutNode? FirstChild { get; set; }

        /// <summary>
        /// Second child node (for containers)
        /// </summary>
        [XmlElement("SecondChild")]
        public LayoutNode? SecondChild { get; set; }

        /// <summary>
        /// Custom properties for extensibility
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, string>? CustomProperties { get; set; }

        /// <summary>
        /// Serializable wrapper for custom properties
        /// </summary>
        [XmlArray("CustomProperties")]
        [XmlArrayItem("Property")]
        public SerializableProperty[]? SerializableCustomProperties
        {
            get
            {
                if (CustomProperties == null || CustomProperties.Count == 0)
                    return null;
                return CustomProperties.Select(kv => new SerializableProperty { Key = kv.Key, Value = kv.Value }).ToArray();
            }
            set
            {
                if (value == null || value.Length == 0)
                {
                    CustomProperties = null;
                }
                else
                {
                    CustomProperties = value.ToDictionary(p => p.Key, p => p.Value);
                }
            }
        }

        /// <summary>
        /// Width hint (optional, for restoration)
        /// </summary>
        [XmlIgnore]
        public double? Width { get; set; }

        /// <summary>
        /// Height hint (optional, for restoration)
        /// </summary>
        [XmlIgnore]
        public double? Height { get; set; }

        /// <summary>
        /// Serializable width
        /// </summary>
        [XmlAttribute("width")]
        public double SerializableWidth
        {
            get => Width ?? 0;
            set => Width = value > 0 ? value : null;
        }

        /// <summary>
        /// Serializable height
        /// </summary>
        [XmlAttribute("height")]
        public double SerializableHeight
        {
            get => Height ?? 0;
            set => Height = value > 0 ? value : null;
        }

        /// <summary>
        /// Controls whether width should be serialized
        /// </summary>
        public bool ShouldSerializeSerializableWidth()
        {
            return Width.HasValue && Width.Value > 0;
        }

        /// <summary>
        /// Controls whether height should be serialized
        /// </summary>
        public bool ShouldSerializeSerializableHeight()
        {
            return Height.HasValue && Height.Value > 0;
        }

        public LayoutNode()
        {
            Id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Creates a pane node
        /// </summary>
        public static LayoutNode CreatePane(string? contentKey = null, string? id = null)
        {
            return new LayoutNode
            {
                NodeType = LayoutNodeType.Pane,
                Id = id ?? Guid.NewGuid().ToString(),
                ContentKey = contentKey
            };
        }

        /// <summary>
        /// Creates a container node
        /// </summary>
        public static LayoutNode CreateContainer(SplitOrientation orientation, double splitRatio, 
            LayoutNode firstChild, LayoutNode secondChild)
        {
            return new LayoutNode
            {
                NodeType = LayoutNodeType.Container,
                Orientation = orientation,
                SplitRatio = Math.Max(0.1, Math.Min(0.9, splitRatio)),
                FirstChild = firstChild,
                SecondChild = secondChild
            };
        }

        /// <summary>
        /// Validates the node structure
        /// </summary>
        public bool Validate(out string? error)
        {
            error = null;

            if (NodeType == LayoutNodeType.Container)
            {
                if (!Orientation.HasValue)
                {
                    error = "Container node missing orientation";
                    return false;
                }

                if (!SplitRatio.HasValue || SplitRatio < 0.1 || SplitRatio > 0.9)
                {
                    error = "Invalid split ratio (must be between 0.1 and 0.9)";
                    return false;
                }

                if (FirstChild == null || SecondChild == null)
                {
                    error = "Container node missing children";
                    return false;
                }

                if (!FirstChild.Validate(out error) || !SecondChild.Validate(out error))
                {
                    return false;
                }
            }
            else if (NodeType == LayoutNodeType.Pane)
            {
                if (string.IsNullOrEmpty(Id))
                {
                    error = "Pane node missing ID";
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Node types in the layout tree
    /// </summary>
    public enum LayoutNodeType
    {
        /// <summary>
        /// Leaf node containing content
        /// </summary>
        Pane,

        /// <summary>
        /// Container node with two children
        /// </summary>
        Container
    }

    /// <summary>
    /// Split orientation for containers
    /// </summary>
    public enum SplitOrientation
    {
        /// <summary>
        /// Horizontal split (left/right)
        /// </summary>
        Horizontal,

        /// <summary>
        /// Vertical split (top/bottom)
        /// </summary>
        Vertical
    }
}