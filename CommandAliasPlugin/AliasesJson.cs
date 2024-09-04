using System.Text.Json.Serialization;

namespace CommandAliasPlugin;
#pragma warning disable CA1812 // ASF uses this class during runtime
internal sealed class Alias {
	[JsonInclude]
	[JsonRequired]
	[JsonPropertyName("Alias")]
	public string? AliasName { get; private init; }
	[JsonInclude]
	[JsonRequired]
	public byte ParamNumber { get; private init; }
	[JsonInclude]
	public string? Access { get; private init; }
	[JsonInclude]
	[JsonRequired]
	public string[]? Commands { get; private init; }
	[JsonInclude]
	public bool AllResponses { get; private init; }
}
#pragma warning restore CA1812 // ASF uses this class during runtime
