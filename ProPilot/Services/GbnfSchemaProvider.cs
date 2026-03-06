namespace ProPilot.Services;

/// <summary>
/// Provides a GBNF (GGML BNF) grammar that constrains LLamaSharp output
/// to valid JSON matching the ProPilotCommand schema.
/// </summary>
public static class GbnfSchemaProvider
{
    /// <summary>
    /// Returns a GBNF grammar string that constrains output to a valid
    /// ProPilotCommand JSON object.
    /// </summary>
    public static string GetGrammar()
    {
        return """
            root ::= "{" ws
              "\"command_type\"" ws ":" ws command-type ws "," ws
              "\"confidence\"" ws ":" ws number ws "," ws
              "\"target_layer\"" ws ":" ws (string | "null") ws "," ws
              "\"parameters\"" ws ":" ws (parameters-obj | "null") ws "," ws
              "\"human_description\"" ws ":" ws string
              ws "}"

            command-type ::= "\"zoom_to_layer\"" | "\"zoom_to_selection\"" | "\"zoom_to_full_extent\"" | "\"pan_to_coordinates\"" | "\"set_scale\"" | "\"go_to_bookmark\"" | "\"toggle_visibility\"" | "\"solo_layers\"" | "\"set_transparency\"" | "\"reorder_layer\"" | "\"remove_layer\"" | "\"add_layer\"" | "\"select_by_attribute\"" | "\"select_by_location\"" | "\"clear_selection\"" | "\"select_all\"" | "\"invert_selection\"" | "\"change_color\"" | "\"set_line_width\"" | "\"set_point_size\"" | "\"change_renderer\"" | "\"toggle_labels\"" | "\"set_definition_query\"" | "\"clear_definition_query\"" | "\"get_feature_count\"" | "\"list_fields\"" | "\"buffer\"" | "\"clip\"" | "\"export_data\"" | "\"dissolve\"" | "\"merge\"" | "\"unknown\""

            parameters-obj ::= "{" ws (param-pair (ws "," ws param-pair)*)? ws "}"

            param-pair ::= param-key ws ":" ws value

            param-key ::= "\"expression\"" | "\"field_name\"" | "\"distance\"" | "\"distance_unit\"" | "\"color\"" | "\"value\"" | "\"source_layer\"" | "\"output_path\"" | "\"output_format\"" | "\"visibility\"" | "\"renderer_type\"" | "\"scale\"" | "\"bookmark_name\"" | "\"coordinates\"" | "\"spatial_relationship\"" | "\"position\"" | "\"reference_layer\"" | "\"layer_names\""

            value ::= string | number | "true" | "false" | "null" | object | array

            object ::= "{" ws (string ws ":" ws value (ws "," ws string ws ":" ws value)*)? ws "}"

            array ::= "[" ws (value (ws "," ws value)*)? ws "]"

            string ::= "\"" ([^"\\] | "\\" .)* "\""

            number ::= "-"? ("0" | [1-9] [0-9]*) ("." [0-9]+)? ([eE] [+-]? [0-9]+)?

            ws ::= [ \t\n\r]*
            """;
    }
}
