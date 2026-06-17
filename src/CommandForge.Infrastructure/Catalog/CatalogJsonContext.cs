using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommandForge.Infrastructure.Catalog;

/// <summary>
/// System.Text.Json source-generation context for the embedded catalog. camelCase JSON,
/// enums as strings (e.g. <c>"Caution"</c>, <c>"FromExitCode"</c>), comments allowed.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(CategoryDto[]))]
[JsonSerializable(typeof(CommandDto[]))]
internal sealed partial class CatalogJsonContext : JsonSerializerContext;
