using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.Json;

public class TimeSpanJsonConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader r, Type _, JsonSerializerOptions __)
        => TimeSpan.Parse(r.GetString()!, CultureInfo.InvariantCulture);
    public override void Write(Utf8JsonWriter w, TimeSpan v, JsonSerializerOptions _)
        => w.WriteStringValue(v.ToString("c", CultureInfo.InvariantCulture));
}