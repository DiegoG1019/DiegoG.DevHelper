namespace DiegoG.PICDevHelper;

public sealed class StagePropertiesDictionary : Dictionary<string, object>
{
    public string? TempDirectory
    {
        get => TryGetValue("temp", out var value) ? value as string ?? throw new InvalidOperationException("Property 'temp' is not a string") : null;
        set => this["temp"] = value ?? throw new ArgumentNullException(nameof(value));
    }
    
    public string? WorkingDirectory
    {
        get => TryGetValue("working", out var value) ? value as string ?? throw new InvalidOperationException("Property 'working' is not a string") : null;
        set => this["working"] = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string? CommandArguments
    {
        get => TryGetValue("cmdArgs", out var value) ? value as string ?? throw new InvalidOperationException("Property 'cmdArgs' is not a string") : null;
        set => this["cmdArgs"] = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string? CommandFileName
    {
        get => TryGetValue("cmdFile", out var value) ? value as string ?? throw new InvalidOperationException("Property 'cmdFile' is not a string") : null;
        set => this["cmdFile"] = value ?? throw new ArgumentNullException(nameof(value));
    }
}
