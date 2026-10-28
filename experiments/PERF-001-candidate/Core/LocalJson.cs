using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Perf001Candidate.Core;

public static class LocalJson
{
    // Preserve game converters/source-generated metadata for known DTOs; own anonymous envelopes
    // need explicit reflection fallback. Never change or MakeReadOnly the game's global options.
    public static JsonSerializerOptions ValueOptions(JsonSerializerOptions source)
        =>new(source){TypeInfoResolver=JsonTypeInfoResolver.Combine(source.TypeInfoResolver??new DefaultJsonTypeInfoResolver(),new DefaultJsonTypeInfoResolver())};
}
