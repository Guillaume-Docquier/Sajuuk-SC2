using System;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bot;

public static class JsonSerializers {
    public class Vector2JsonConverter: JsonConverter<Vector2> {
        private const string Divider = "/";

        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var vectorData = reader.GetString()!.Split(Divider);

            return new Vector2(
                float.Parse(vectorData[0], CultureInfo.InvariantCulture.NumberFormat),
                float.Parse(vectorData[1], CultureInfo.InvariantCulture.NumberFormat)
            );
        }

        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options) {
            var x = value.X.ToString(CultureInfo.InvariantCulture.NumberFormat);
            var y = value.Y.ToString(CultureInfo.InvariantCulture.NumberFormat);

            writer.WriteStringValue($"{x}{Divider}{y}");
        }
    }


    public class Vector3JsonConverter: JsonConverter<Vector3> {
        private const string Divider = "/";

        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var vectorData = reader.GetString()!.Split(Divider);

            return new Vector3(
                float.Parse(vectorData[0], CultureInfo.InvariantCulture.NumberFormat),
                float.Parse(vectorData[1], CultureInfo.InvariantCulture.NumberFormat),
                float.Parse(vectorData[2], CultureInfo.InvariantCulture.NumberFormat)
            );
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options) {
            var x = value.X.ToString(CultureInfo.InvariantCulture.NumberFormat);
            var y = value.Y.ToString(CultureInfo.InvariantCulture.NumberFormat);
            var z = value.Z.ToString(CultureInfo.InvariantCulture.NumberFormat);

            writer.WriteStringValue($"{x}{Divider}{y}{Divider}{z}");
        }
    }
}
