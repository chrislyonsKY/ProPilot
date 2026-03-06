using System.Collections.Generic;

namespace ProPilot.Commands;

/// <summary>
/// Snapshot of the current map state, passed to the LLM as context
/// and used by IMapCommand implementations for validation and preview.
/// Built by IMapContextBuilder on the MCT (QueuedTask.Run).
/// </summary>
public class MapContext
{
    /// <summary>All layers in the active map.</summary>
    public List<LayerInfo> Layers { get; set; } = new();

    /// <summary>Named bookmarks in the active map.</summary>
    public List<string> Bookmarks { get; set; } = new();

    /// <summary>Current map extent.</summary>
    public ExtentInfo? Extent { get; set; }

    /// <summary>Current map scale.</summary>
    public double Scale { get; set; }

    /// <summary>Active map name.</summary>
    public string MapName { get; set; } = string.Empty;

    /// <summary>Spatial reference WKID of the active map.</summary>
    public int SpatialReferenceWkid { get; set; }
}

/// <summary>
/// Metadata about a single layer in the map, used for LLM context
/// and fuzzy name resolution.
/// </summary>
public class LayerInfo
{
    /// <summary>Layer name as displayed in the TOC.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Geometry type: Point, Polyline, Polygon, or Table.</summary>
    public string GeometryType { get; set; } = string.Empty;

    /// <summary>Total feature count.</summary>
    public long FeatureCount { get; set; }

    /// <summary>Number of currently selected features.</summary>
    public long SelectedCount { get; set; }

    /// <summary>Whether the layer is currently visible.</summary>
    public bool IsVisible { get; set; }

    /// <summary>Field names in this layer.</summary>
    public List<string> FieldNames { get; set; } = new();

    /// <summary>Current definition query, if any.</summary>
    public string? DefinitionQuery { get; set; }

    /// <summary>Current transparency (0-100).</summary>
    public double Transparency { get; set; }
}

/// <summary>
/// Current map view extent.
/// </summary>
public class ExtentInfo
{
    public double XMin { get; set; }
    public double YMin { get; set; }
    public double XMax { get; set; }
    public double YMax { get; set; }
    public int Wkid { get; set; }
}
