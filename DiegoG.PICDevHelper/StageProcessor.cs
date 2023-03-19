using System.Collections.Immutable;
using System.IO;

namespace DiegoG.PICDevHelper;

public sealed class StageProcessor
{
    public ImmutableArray<Stage> Stages { get; }

    public StageProcessor(IEnumerable<Stage> stages)
    {
        Stages = stages?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(stages));
    }

    public void Start(string dir)
    {
        var temp = Path.Combine(Path.GetTempPath(), $"{{{Guid.NewGuid()}-{Guid.NewGuid()}}}");
        Directory.CreateDirectory(temp);
        StagePropertiesDictionary properties = new();
        properties.TempDirectory = temp;

        Console.WriteLine("Validating stages...");
        List<Exception> exceptions = new();
        foreach (var stage in Stages)
        {
            exceptions.Clear();
            Console.WriteLine($" > Validating stage {stage.StageName}{(stage.Name is null ? "" : $"({stage.Name})")}");
            try
            {
                stage.Validate(exceptions);
            }
            catch(Exception e)
            {
                exceptions.Add(e);
            }

            if (exceptions.Count > 0)
                throw new AggregateException($"One or more errors ocurred while validating stage {stage.StageName}{(stage.Name is null ? "" : $"({stage.Name})")}", exceptions);
        }

        try
        {
            Console.WriteLine("Commencing stages...");
            foreach (var stage in Stages)
            {
                Console.WriteLine($" > Comencing stage {stage.StageName}{(stage.Name is null ? "" : $"({stage.Name})")}");
                stage.ExecuteStage(dir, this, properties);
            }
        }
        finally
        {
            Console.WriteLine("Cleaning up...");
            Directory.Delete(temp, true);
        }
    }
}
