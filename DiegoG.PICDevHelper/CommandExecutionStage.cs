using System.Diagnostics;

namespace DiegoG.PICDevHelper;

public sealed class CommandExecutionStage : Stage
{
    public List<KeyValuePair<string, string>>? Environment { get; init; }
    public bool UseShellExecute { get; init; }
    public bool ClearWorkingDirectory { get; init; }
    public bool WaitForEnd { get; init; }
    public string? WorkingDirectory { get; init; }

    public override void Validate(List<Exception> exceptions)
    {
    }

    public override void ExecuteStage(string directory, StageProcessor processor, StagePropertiesDictionary properties)
    {
        string working = WorkingDirectory is null
            ? properties.WorkingDirectory ?? throw new InvalidOperationException("This stage's Output property is null, but WorkingDirectory has not been set")
            : Path.Combine(directory, WorkingDirectory ?? "output");

        if (Directory.Exists(working) && ClearWorkingDirectory)
            Directory.Delete(working, true);
        Directory.CreateDirectory(working);

        var pstart = new ProcessStartInfo();
        if (Environment?.Count is > 0)
            foreach (var kv in Environment)
                pstart.Environment.Add(kv!);

        pstart.RedirectStandardOutput = false;
        pstart.FileName = properties.CommandFileName;
        pstart.Arguments = properties.CommandArguments;
        pstart.WorkingDirectory = working;
        pstart.UseShellExecute = UseShellExecute;

        if (WaitForEnd)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(new string('=', Console.BufferWidth));
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();

            Process.Start(pstart)!.WaitForExit();

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(new string('=', Console.BufferWidth));

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine();
        }
        else
        {
            pstart.RedirectStandardOutput = true;
            Process.Start(pstart);
        }
    }
}
