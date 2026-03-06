using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProPilot.Commands;

namespace ProPilot.Services;

/// <summary>
/// Captures the current state of the active map view for LLM context injection.
/// All map access is performed within QueuedTask.Run().
/// </summary>
public class MapContextBuilder : IMapContextBuilder
{
    /// <inheritdoc />
    public async Task<MapContext> BuildContextAsync()
    {
        return await QueuedTask.Run(() =>
        {
            var mapView = MapView.Active;
            var map = mapView?.Map;
            if (map == null)
            {
                Debug.WriteLine("[ProPilot] No active map view — returning empty context.");
                return new MapContext();
            }

            var context = new MapContext
            {
                MapName = map.Name,
                Scale = mapView!.Camera.Scale,
                SpatialReferenceWkid = map.SpatialReference?.Wkid ?? 0
            };

            var extent = mapView.Extent;
            if (extent != null)
            {
                context.Extent = new ExtentInfo
                {
                    XMin = extent.XMin,
                    YMin = extent.YMin,
                    XMax = extent.XMax,
                    YMax = extent.YMax,
                    Wkid = extent.SpatialReference?.Wkid ?? 0
                };
            }

            try
            {
                var bookmarks = map.GetBookmarks();
                context.Bookmarks = bookmarks.Select(b => b.Name).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProPilot] Failed to get bookmarks: {ex.Message}");
            }

            var layers = map.GetLayersAsFlattenedList();
            foreach (var layer in layers)
            {
                try
                {
                    var layerInfo = new LayerInfo
                    {
                        Name = layer.Name,
                        IsVisible = layer.IsVisible
                    };

                    if (layer is FeatureLayer featureLayer)
                    {
                        layerInfo.GeometryType = featureLayer.ShapeType.ToString();
                        layerInfo.DefinitionQuery = featureLayer.DefinitionQuery;
                        layerInfo.Transparency = featureLayer.Transparency;

                        try
                        {
                            using var table = featureLayer.GetTable();
                            if (table != null)
                            {
                                layerInfo.FeatureCount = table.GetCount();
                                var tableDef = table.GetDefinition();
                                layerInfo.FieldNames = tableDef.GetFields()
                                    .Select(f => f.Name).ToList();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[ProPilot] Failed to read table for {layer.Name}: {ex.Message}");
                        }

                        try
                        {
                            var selection = featureLayer.GetSelection();
                            layerInfo.SelectedCount = selection.GetCount();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[ProPilot] Failed to get selection for {layer.Name}: {ex.Message}");
                        }
                    }

                    context.Layers.Add(layerInfo);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ProPilot] Failed to process layer {layer.Name}: {ex.Message}");
                }
            }

            Debug.WriteLine($"[ProPilot] Map context built: {context.Layers.Count} layers, {context.Bookmarks.Count} bookmarks");
            return context;
        });
    }
}
