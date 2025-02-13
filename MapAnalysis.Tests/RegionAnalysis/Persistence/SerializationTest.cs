using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MapAnalysis.Tests.RegionAnalysis.Persistence;

public class SerializationTest {
    private interface IThing {
        public IReadOnlySet<Vector2> Locations { get; }
    }

    private interface IThang {
        public Vector2 Location { get; }
        public IThing TheThing { get; }
    }

    class Thing : IThing {
        [JsonInclude] public HashSet<Vector2> ConcreteLocations { get; init; }

        [JsonIgnore] public IReadOnlySet<Vector2> Locations => ConcreteLocations;

        [JsonConstructor]
        public Thing() {}

        public Thing(HashSet<Vector2> concreteLocations) {
            ConcreteLocations = concreteLocations;
        }
    }

    class Thang : IThang {
        public readonly Thing ConcreteTheThing;

        public Vector2 Location { get; }
        [JsonIgnore] public IThing TheThing => ConcreteTheThing;

        public Thang(Thing thing, Vector2 location) {
            ConcreteTheThing = thing;
            Location = location;
        }
    }

    class TestThang : IThang {
        public Thing ConcreteTheThing;

        public Vector2 Location { get; init; }
        public IThing TheThing => ConcreteTheThing;
    }

    [Fact]
    public void ShouldSerialize() {
        var options = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve,
            MaxDepth = 256,
            Converters =
            {
                new JsonSerializers.Vector2JsonConverter(),
                new JsonSerializers.Vector3JsonConverter(),
            },
            IncludeFields = true,
            WriteIndented = true,
        };

        var thing = new Thing(new HashSet<Vector2> { new Vector2(1, 1), new Vector2(2, 2) });
        var thang = new Thang(thing, new Vector2(3, 3));

        var jsonString = JsonSerializer.Serialize(thang, options);

        var testThang = JsonSerializer.Deserialize<TestThang>(jsonString, options);

        Assert.Equivalent(testThang, thang);
    }
}
