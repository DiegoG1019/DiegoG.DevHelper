using System.Diagnostics;
using System.Text.Json;

namespace DiegoG.PICDevHelper;

static class Program
{
    public static JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions()
    {
        WriteIndented = true
    };

    public static string? Dir { get; private set; }
    public static string? Working { get; private set; }
    public static string? Self { get; private set; }

    public static bool Exclude(string fn) 
        => !fn.EndsWith("picdevhelper_config.json", StringComparison.InvariantCultureIgnoreCase)
        && (Self is null || !fn.Contains(Self, StringComparison.InvariantCultureIgnoreCase));

    static void Main(string[] args)
    {
        try
        {
            Self = Environment.GetCommandLineArgs()[0];

    #if DEBUG
            Dir = "C:\\Users\\duden\\Workspace\\Programming\\Microprocessors\\PIC16XX\\Test";
    #else
            Dir = args.Length > 0 ? args[0] : Environment.CurrentDirectory;
    #endif
            Directory.CreateDirectory(Dir);

            Console.WriteLine($"Starting PICDevHelper in {Dir}");

            Console.WriteLine($"Reading config...");

            StageProcessor processor;
            var cfile = Path.Combine(Dir, "picdevhelper_config.json");
            if (File.Exists(cfile))
            {
                using var file = File.OpenRead(cfile);
                processor = new(Stage.DeserializeStages(file));
            }
            else
            {
                Console.WriteLine($"Could not find a configuration file in {Dir}"); 
                Console.WriteLine("Creating new empty config file");
                using var file = File.OpenWrite(cfile);
                JsonSerializer.Serialize(file, Array.Empty<object>(), JsonOptions);
                return;
            }

            processor.Start(Dir);

            Console.WriteLine("Finished, waiting 5 seconds before shuttng down");
            Thread.Sleep(5_000);
        }
        catch(Exception e)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("An error ocurred");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(e);
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Press return/enter key to continue");
            Console.ReadLine();
        }
    }
}
