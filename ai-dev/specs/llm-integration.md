# ProPilot — LLM Integration

> Covers all three LLM providers, the system prompt, JSON schema,
> GBNF grammar, and map context injection.

---

## Provider Architecture

ProPilot supports three LLM providers behind the `ILlmClient` interface.
All share the same system prompt, command schema, and map context format.

| Provider | Class | When Used | Structured Output |
|---|---|---|---|
| LLamaSharp (DEFAULT) | `LLamaSharpClient` | Bundled, zero-dependency, first-run download | GBNF grammar |
| Ollama | `OllamaClient` | Power user fallback, localhost HTTP | `format` parameter (JSON schema) |
| OpenAI | `OpenAiClient` | Cloud opt-in, requires API key | `response_format.json_schema` |

### Model Tiers (LLamaSharp)

| Tier | Model | GGUF Size | RAM Required | HuggingFace Repo |
|---|---|---|---|---|
| Light | Phi-3 Mini 3.8B Q4_K_M | ~1.5 GB | 8 GB+ | `microsoft/Phi-3-mini-4k-instruct-gguf` |
| Standard | Mistral 7B Instruct Q4_K_M | ~4 GB | 16 GB+ | `TheBloke/Mistral-7B-Instruct-v0.2-GGUF` |

Model files stored in `%APPDATA%\ProPilot\models\`.
Users can also manually place any GGUF file in that directory.

---

## System Prompt

Assembled fresh per request by `SystemPromptBuilder`. Three sections:

### 1. Identity & Rules (Static)

```
You are ProPilot, a command parser for ArcGIS Pro. Your ONLY job is to parse
natural language commands into structured JSON. You are NOT a chatbot. You do
NOT explain, apologize, or converse. You ONLY output valid JSON matching the
provided schema.

## Rules

1. Match the user's intent to exactly ONE command from the Available Commands list
2. Resolve layer names fuzzily — "streams" matches "Streams_KY" or "NHD_Streams"
3. Resolve field names fuzzily — "population" matches "POP_TOTAL" or "POPULATION"
4. If the command is ambiguous, pick the most likely interpretation
5. If the command cannot be mapped to any available command, set command_type to "unknown"
6. Never invent layer names or field names not in the Current Map Context
7. Always output valid JSON matching the schema — no prose, no markdown, no explanation
```

### 2. Available Commands (Injected from CommandRegistry)

```
## Available Commands

zoom_to_layer — Zoom the map to a layer's full extent
zoom_to_selection — Zoom to currently selected features
zoom_to_full_extent — Fit all layers in view
pan_to_coordinates — Center the map on X,Y coordinates
set_scale — Set the map scale
go_to_bookmark — Navigate to a named bookmark
toggle_layer_visibility — Turn a layer on or off
solo_layers — Turn off all layers except specified ones
set_transparency — Set a layer's transparency percentage
reorder_layer — Move a layer above or below another layer
remove_layer — Remove a layer from the map
add_layer — Add a data source to the map
select_by_attribute — Select features matching a WHERE expression
select_by_location — Select features by spatial relationship to another layer
clear_selection — Clear all selections in the map
select_all — Select all features in a layer
invert_selection — Invert the current selection on a layer
change_color — Change a layer's symbol color
set_line_width — Change a line layer's stroke width
set_point_size — Change a point layer's marker size
change_renderer — Change renderer type (simple, unique values, graduated colors)
toggle_labels — Turn labels on/off or change label field
set_definition_query — Apply a WHERE filter to a layer
clear_definition_query — Remove a layer's definition query
get_feature_count — Return the number of features in a layer
list_fields — List all field names and types in a layer
buffer — Create buffer polygons around features
clip — Clip features by another layer
export_data — Export a layer to a file format
dissolve — Dissolve features on a field
merge — Merge two or more layers
```

### 3. Map Context (Injected from MapContextBuilder)

```
## Current Map Context

Map: "Mining_Operations"
Scale: 24000
Extent: {xmin: -83.5, ymin: 37.8, xmax: -82.1, ymax: 38.6, wkid: 4326}
Bookmarks: Study Area, Boyd County, Pike County

Layers:
- Streams_KY (Polyline, 2847 features, visible)
  Fields: OBJECTID, GNIS_NAME, STREAM_ORDER, LENGTH_KM, HUC12
- Permitted_Boundaries (Polygon, 412 features, visible, 23 selected)
  Fields: OBJECTID, PERMIT_ID, STATUS, OPERATOR_NAME, ACREAGE, COUNTY
  Definition Query: STATUS = 'Active'
- Kentucky_Polygon (Polygon, 1 feature, visible)
  Fields: OBJECTID, STATE_NAME, STATE_FIPS
- DEM_30m (Raster, visible)
```

---

## ProPilotCommand JSON Schema

This schema is used by all three providers (via different mechanisms):

```json
{
  "type": "object",
  "required": ["command_type", "confidence"],
  "properties": {
    "command_type": {
      "type": "string",
      "enum": [
        "zoom_to_layer", "zoom_to_selection", "zoom_to_full_extent",
        "pan_to_coordinates", "set_scale", "go_to_bookmark",
        "toggle_layer_visibility", "solo_layers", "set_transparency",
        "reorder_layer", "remove_layer", "add_layer",
        "select_by_attribute", "select_by_location", "clear_selection",
        "select_all", "invert_selection",
        "change_color", "set_line_width", "set_point_size",
        "change_renderer", "toggle_labels",
        "set_definition_query", "clear_definition_query",
        "get_feature_count", "list_fields",
        "buffer", "clip", "export_data", "dissolve", "merge",
        "unknown"
      ]
    },
    "confidence": {
      "type": "number",
      "minimum": 0.0,
      "maximum": 1.0
    },
    "target_layer": { "type": "string" },
    "parameters": {
      "type": "object",
      "properties": {
        "expression": { "type": "string" },
        "field_name": { "type": "string" },
        "distance": { "type": "number" },
        "distance_unit": { "type": "string", "enum": ["meters", "feet", "miles", "kilometers"] },
        "color": { "type": "string" },
        "value": { "type": "number" },
        "source_layer": { "type": "string" },
        "output_path": { "type": "string" },
        "output_format": { "type": "string", "enum": ["shapefile", "geojson", "fgdb", "geopackage", "csv", "kml"] },
        "visibility": { "type": "boolean" },
        "renderer_type": { "type": "string", "enum": ["simple", "unique_values", "graduated_colors", "graduated_symbols"] },
        "scale": { "type": "number" },
        "bookmark_name": { "type": "string" },
        "coordinates": {
          "type": "object",
          "properties": { "x": { "type": "number" }, "y": { "type": "number" }, "wkid": { "type": "integer" } }
        },
        "spatial_relationship": { "type": "string", "enum": ["intersects", "contains", "within", "within_distance"] },
        "position": { "type": "string", "enum": ["above", "below"] },
        "reference_layer": { "type": "string" },
        "layer_names": { "type": "array", "items": { "type": "string" } }
      }
    },
    "human_description": { "type": "string" }
  }
}
```

---

## GBNF Grammar (LLamaSharp)

LLamaSharp does not have Ollama's built-in `format` parameter. Instead, structured
output is enforced via a GBNF grammar passed to `InferenceParams.Grammar`.

The grammar must:
- Constrain output to valid JSON matching the ProPilotCommand schema
- Enumerate all `command_type` values as string literals
- Allow optional fields (target_layer, parameters, human_description)
- Handle nested objects (parameters, coordinates)

See `GbnfSchemaProvider.cs` for implementation.
GBNF reference: https://github.com/ggerganov/llama.cpp/blob/master/grammars/README.md

---

## Structured Output by Provider

| Provider | Mechanism | Reliability |
|---|---|---|
| LLamaSharp | GBNF grammar constrains token generation | High — tokens physically cannot violate grammar |
| Ollama | `format` param with JSON schema | High — Ollama enforces at generation level |
| OpenAI | `response_format.json_schema` with `strict: true` | High — OpenAI enforces schema compliance |
