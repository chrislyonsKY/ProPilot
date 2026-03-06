# ProPilot — Command Vocabulary

> v1 target: 30 curated commands across 6 categories.
> Each command maps to either a direct Pro SDK call or a GP tool invocation.

---

## Navigation (6 commands)

| Command Type | Natural Language Pattern | Action | Execution |
|---|---|---|---|
| `zoom_to_layer` | "Zoom to [layer]" | Set map extent to layer extent | SDK: MapView.ZoomToAsync() |
| `zoom_to_selection` | "Zoom to selection" / "Zoom to selected" | Zoom to selected features | SDK: MapView.ZoomToAsync(selection) |
| `zoom_to_full_extent` | "Zoom to full extent" / "Show everything" | Fit all layers in view | SDK: MapView.ZoomToFullExtentAsync() |
| `pan_to_coordinates` | "Pan to [x], [y]" / "Go to [coordinates]" | Center map on location | SDK: MapView.PanToAsync() |
| `set_scale` | "Set scale to [value]" / "Zoom to 1:[value]" | Set map scale | SDK: Camera.Scale |
| `go_to_bookmark` | "Go to bookmark [name]" | Navigate to named bookmark | SDK: MapView.ZoomToBookmarkAsync() |

## Layer Management (6 commands)

| Command Type | Natural Language Pattern | Action | Execution |
|---|---|---|---|
| `toggle_layer_visibility` | "Turn on/off [layer]" / "Show/hide [layer]" | Toggle layer visibility | SDK: Layer.SetVisibility() |
| `solo_layers` | "Turn off everything except [layers]" | Solo one or more layers | SDK: iterate layers, SetVisibility() |
| `set_transparency` | "Set [layer] transparency to [%]" | Adjust layer transparency | SDK: Layer.SetTransparency() |
| `reorder_layer` | "Move [layer] above/below [layer]" | Reorder layers in TOC | SDK: Map.MoveLayer() |
| `remove_layer` | "Remove [layer]" | Remove layer from map | SDK: Map.RemoveLayer() |
| `add_layer` | "Add [data source path]" | Add data to map | SDK: LayerFactory.Instance.CreateLayer() |

**Note:** `remove_layer` is marked `IsDestructive = true`.

## Selection (5 commands)

| Command Type | Natural Language Pattern | Action | Execution |
|---|---|---|---|
| `select_by_attribute` | "Select [features] where [expression]" | Select by attribute query | SDK: QueryFilter + FeatureLayer.Select() |
| `select_by_location` | "Select [features] within [distance] of [source]" | Select by spatial relationship | GP: SelectLayerByLocation_management |
| `clear_selection` | "Clear selection" / "Deselect all" | Clear all selections | SDK: Map.ClearSelection() |
| `select_all` | "Select all features in [layer]" | Select all features | SDK: QueryFilter("1=1") |
| `invert_selection` | "Invert selection on [layer]" | Flip selection | SDK: SelectionCombinationMethod.XOR |

## Symbology (5 commands)

| Command Type | Natural Language Pattern | Action | Execution |
|---|---|---|---|
| `change_color` | "Change [layer] color to [color]" | Change simple renderer color | SDK: CIMSimpleRenderer modification |
| `set_line_width` | "Set [layer] line width to [value]" | Change stroke width | SDK: CIM symbol modification |
| `set_point_size` | "Set [layer] point size to [value]" | Change marker size | SDK: CIM symbol modification |
| `change_renderer` | "Make [layer] [type] on [field]" | Change renderer type | SDK: FeatureLayer.SetRenderer() |
| `toggle_labels` | "Label [layer] by [field]" / "Turn off labels on [layer]" | Toggle/configure labels | SDK: LabelClass configuration |

**CIM pattern required:** All symbology commands use read → modify → set on CIM definitions.

## Query & Filter (4 commands)

| Command Type | Natural Language Pattern | Action | Execution |
|---|---|---|---|
| `set_definition_query` | "Filter [layer] where [expression]" | Apply definition query | SDK: FeatureLayer.SetDefinitionQuery() |
| `clear_definition_query` | "Clear filter on [layer]" | Remove definition query | SDK: FeatureLayer.SetDefinitionQuery("") |
| `get_feature_count` | "How many features in [layer]?" | Return feature count | SDK: FeatureLayer.GetFeatureCount() |
| `list_fields` | "What fields does [layer] have?" | List field names and types | SDK: FeatureLayer.GetFieldDescriptions() |

**Note:** `get_feature_count` and `list_fields` are read-only informational commands — they display results in the preview panel without modifying the map.

## Geoprocessing (5 commands)

| Command Type | Natural Language Pattern | Action | Execution |
|---|---|---|---|
| `buffer` | "Buffer [layer] by [distance]" | Create buffer polygons | GP: analysis.Buffer |
| `clip` | "Clip [input] by [clip layer]" | Clip features | GP: analysis.Clip |
| `export_data` | "Export [layer] to [format/path]" | Export data | GP: conversion.ExportFeatures |
| `dissolve` | "Dissolve [layer] on [field]" | Dissolve features | GP: management.Dissolve |
| `merge` | "Merge [layer1] and [layer2]" | Merge feature classes | GP: management.Merge |

**All GP commands** use `Geoprocessing.ExecuteToolAsync()` with `GPExecuteToolFlags.None`.

---

## Command Type Enum (for JSON Schema)

```
zoom_to_layer, zoom_to_selection, zoom_to_full_extent,
pan_to_coordinates, set_scale, go_to_bookmark,
toggle_layer_visibility, solo_layers, set_transparency,
reorder_layer, remove_layer, add_layer,
select_by_attribute, select_by_location, clear_selection,
select_all, invert_selection,
change_color, set_line_width, set_point_size,
change_renderer, toggle_labels,
set_definition_query, clear_definition_query,
get_feature_count, list_fields,
buffer, clip, export_data, dissolve, merge,
unknown
```

Total: 31 values (30 commands + `unknown` fallback)
