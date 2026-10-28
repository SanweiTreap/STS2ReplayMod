using System.Text.Json.Serialization;
namespace SyntheticGeneratedJson;

public sealed record KnownDto(int Value);
[JsonSerializable(typeof(KnownDto))]
[JsonSourceGenerationOptions(PropertyNamingPolicy=JsonKnownNamingPolicy.CamelCase,IncludeFields=true)]
public partial class GameLikeContext:JsonSerializerContext{}
