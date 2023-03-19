using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace DiegoG.PICDevHelper;

public abstract class Stage
{
    private static readonly ImmutableDictionary<string, Type> StageTypes;

    static Stage()
    {
        StageTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
            .Where(x => x.IsAssignableTo(typeof(Stage)) && x.IsAbstract is false)
            .ToImmutableDictionary(k => k.Name);
    }

    public string? Name { get; init; }
    public string StageName { get; }

    protected Stage()
    {
        StageName = GetType().Name;
    }

    public abstract void Validate(List<Exception> exceptions);
    public abstract void ExecuteStage(string directory, StageProcessor processor, StagePropertiesDictionary properties);
    public static Stage[] DeserializeStages(Stream jsonStream)
    {
        var doc = JsonDocument.Parse(jsonStream);
        return doc.RootElement.EnumerateObject().Select(x => JsonSerializer.Deserialize(x.Value, StageTypes[x.Name], Program.JsonOptions)).Cast<Stage>().ToArray();
    }
}
