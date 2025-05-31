using System.Text.Json.Serialization;
using System.Text.Json;

public class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    private const string FORMAT = "HH:mm:ss";
    public override TimeOnly Read(ref Utf8JsonReader r, Type _, JsonSerializerOptions __)
        => TimeOnly.ParseExact(r.GetString()!, FORMAT);
    public override void Write(Utf8JsonWriter w, TimeOnly v, JsonSerializerOptions _)
        => w.WriteStringValue(v.ToString(FORMAT));
}