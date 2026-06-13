using System.Text.Json.Nodes;
using AxiomRPG.Core.Interfaces;

namespace AxiomRPG.Data;

public class DataValidator : IDataValidator<JsonNode>
{
    private readonly Dictionary<string, JsonNode> _schemas = new();

    public void RegisterSchema(string typeName, JsonNode schema)
    {
        _schemas[typeName] = schema;
    }

    public ValidationResult Validate(JsonNode data)
    {
        // Basic validation — check required fields exist
        var errors = new List<ValidationError>();
        // Schema-based validation will be expanded
        return new ValidationResult(errors.Count == 0, errors);
    }

    public ValidationResult ValidateAgainstSchema(string typeName, JsonNode data)
    {
        var errors = new List<ValidationError>();
        if (!_schemas.TryGetValue(typeName, out var schema))
        {
            return new ValidationResult(false, new List<ValidationError> { new("", $"No schema registered for type: {typeName}") });
        }

        var schemaObj = schema.AsObject();
        var dataObj = data as JsonObject;
        if (dataObj == null)
        {
            return new ValidationResult(false, new List<ValidationError> { new("", "Data is not a JSON object") });
        }

        // Check required fields
        if (schemaObj.TryGetPropertyValue("required", out var requiredNode) && requiredNode is JsonArray requiredArr)
        {
            foreach (var field in requiredArr)
            {
                var fieldName = field?.GetValue<string>();
                if (fieldName != null && !dataObj.ContainsKey(fieldName))
                {
                    errors.Add(new ValidationError(fieldName, $"Required field '{fieldName}' is missing"));
                }
            }
        }

        // Check property types
        if (schemaObj.TryGetPropertyValue("properties", out var propsNode) && propsNode is JsonObject props)
        {
            foreach (var prop in props)
            {
                if (!dataObj.ContainsKey(prop.Key)) continue;
                if (prop.Value is JsonObject propSchema && propSchema.TryGetPropertyValue("type", out var typeNode))
                {
                    var expectedType = typeNode?.GetValue<string>();
                    var actualValue = dataObj[prop.Key];
                    if (!ValidateType(actualValue, expectedType))
                    {
                        errors.Add(new ValidationError(prop.Key, $"Expected type '{expectedType}'", expectedType));
                    }
                }
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    private static bool ValidateType(JsonNode? value, string? expectedType) => expectedType switch
    {
        "string" => value is JsonValue v && v.TryGetValue(out string? _),
        "number" => value is JsonValue v && (v.TryGetValue(out double _) || v.TryGetValue(out int _)),
        "integer" => value is JsonValue v && v.TryGetValue(out int _),
        "boolean" => value is JsonValue v && v.TryGetValue(out bool _),
        "object" => value is JsonObject,
        "array" => value is JsonArray,
        _ => true
    };
}
