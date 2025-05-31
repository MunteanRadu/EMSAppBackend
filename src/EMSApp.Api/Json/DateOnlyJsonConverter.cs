using System.Text.Json.Serialization;
using System.Text.Json;

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string FORMAT = "yyyy-MM-dd";
    public override DateOnly Read(ref Utf8JsonReader r, Type _, JsonSerializerOptions __)
        => DateOnly.ParseExact(r.GetString()!, FORMAT);
    public override void Write(Utf8JsonWriter w, DateOnly v, JsonSerializerOptions _)
        => w.WriteStringValue(v.ToString(FORMAT));
}