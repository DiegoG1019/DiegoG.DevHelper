using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DiegoG.PICDevHelper;

public sealed class FileProcessingStage : Stage
{
    public const string IncludedFileListFormatKey = "{IncludedFileList}";
    public const string ExcludedFileListFormatKey = "{ExcludedFileList}";
    public const string ExternalFileListFormatKey = "{ExternalFileList}";
    public const string UserFormatKey = "{User}";
    public const string UserDataFormatKey = "{UserData}";
    public const string ProgramDataFormatKey = "{ProgramData}";
    public const string FileFormatKey = "{File}";

    public static ImmutableArray<string> ReservedWords { get; } = new string[]
    {
        IncludedFileListFormatKey,
        ExcludedFileListFormatKey,
        ExternalFileListFormatKey,
        FileFormatKey
    }.ToImmutableArray();

    public HashSet<string>? IncludedFiles { get; init; }
    public HashSet<string>? IncludedFileTypes { get; init; }

    public HashSet<string>? ExcludedFiles { get; init; }
    public HashSet<string>? ExternalFiles { get; init; }

    public List<KeyValuePair<string, string>>? CommandValues { get; init; }

    public string? IncludedFileFormat { get; init; }
    public int? IncludedFileTerminationTrim { get; init; }
    public string? IncludedFileTermination { get; init; }

    public string? ExcludedFileFormat { get; init; }
    public int? ExcludedFileTerminationTrim { get; init; }
    public string? ExcludedFileTermination { get; init; }

    public string? ExternalFileFormat { get; init; }
    public int? ExternalFileTerminationTrim { get; init; }
    public string? ExternalFileTermination { get; init; }

    public string? CommandArgumentsFormat { get; init; }

    public string FileName { get; init; }
    public string? Files { get; init; }
    public string? Output { get; init; }

    public string GetFileName() 
        => new StringBuilder(FileName).Replace(UserFormatKey, System.Environment.UserName)
            .Replace(UserDataFormatKey, System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile))
            .Replace(ProgramDataFormatKey, System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles))
            .ToString();

    private string BuildCommand(string dir, string temp, out HashSet<string> incl)
    {
        StringBuilder final = new(CommandArgumentsFormat);
        StringBuilder buffer = new(100);
        StringBuilder sub_buffer = new(100);

        Debug.Assert(IncludedFileFormat is not null);
        if (IncludedFiles?.Count is > 0)
        {
            incl = IncludedFiles;
            BuildList(final, buffer, sub_buffer, IncludedFiles, IncludedFileFormat, FileFormatKey, IncludedFileListFormatKey, IncludedFileTerminationTrim, IncludedFileTermination, temp);
        }
        else
        {
            incl = new();
            foreach (var inc in IncludedFileTypes!)
                foreach (var file in Directory.EnumerateFiles(Path.Combine(dir, Files ?? ""), $"*.{inc}").Where(Program.Exclude))
                    incl.Add(file);
            BuildList(final, buffer, sub_buffer, incl, IncludedFileFormat, FileFormatKey, IncludedFileListFormatKey, IncludedFileTerminationTrim, IncludedFileTermination, temp);
        }

        if (ExcludedFiles?.Count is > 0)
        {
            Debug.Assert(ExcludedFileFormat is not null);
            BuildList(final, buffer, sub_buffer, ExcludedFiles, ExcludedFileFormat, FileFormatKey, ExcludedFileListFormatKey, ExcludedFileTerminationTrim, ExcludedFileTermination, temp);
        }

        if (ExternalFiles?.Count is > 0)
        {
            Debug.Assert(ExternalFileFormat is not null);
            BuildList(final, buffer, sub_buffer, ExternalFiles, ExternalFileFormat, FileFormatKey, ExternalFileListFormatKey, ExternalFileTerminationTrim, ExternalFileTermination, temp);
        }

        if (CommandValues?.Count is > 0)
            foreach (var (k, v) in CommandValues)
                final.Replace(k, v);

        return final.Replace(UserFormatKey, System.Environment.UserName)
            .Replace(UserDataFormatKey, System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile))
            .Replace(ProgramDataFormatKey, System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles))
            .ToString();
    }

    private void BuildList(
        StringBuilder final, 
        StringBuilder buffer, 
        StringBuilder sub_buffer, 
        HashSet<string> items, 
        string format, 
        string key, 
        string finalKey,
        int? terminationTrim,
        string? termination,
        string temp
    )
    {
        buffer.Clear();
        foreach (var i in items)
        {
            if (IncludedFileTypes?.Contains(Path.GetExtension(i)[1..]) is false) continue;
            var item = Path.Combine(temp, Path.GetFileName(i));
            buffer.Append(sub_buffer.Clear().Append('"').Append(format).Replace(key, item).Append("\" "));
        }
        final.Replace(finalKey, buffer.Remove(buffer.Length - terminationTrim ?? 0, terminationTrim ?? 0).Append(termination).ToString());
    }

    public override void ExecuteStage(string directory, StageProcessor processor, StagePropertiesDictionary properties)
    {
        var temp = properties.TempDirectory;
        Console.WriteLine("Building command...");
        var cmd = BuildCommand(directory, temp, out var incl);
        properties.CommandArguments = cmd;
        properties.CommandFileName = GetFileName();

        Console.WriteLine($"Succesfully built command, copying files to '{temp}'...");

        foreach (var file in incl)
        {
            var orig = file.Replace(UserFormatKey, System.Environment.UserName)
                           .Replace(UserDataFormatKey, System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile))
                           .Replace(ProgramDataFormatKey, System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles))
                           .ToString();
            var dest = Path.Combine(temp, Path.GetFileName(orig));
            if (File.Exists(dest)) File.Delete(dest);
            File.Copy(orig, dest);
        }

        if (ExternalFiles?.Count is > 0)
            foreach (var file in ExternalFiles)
            {
                var orig = file.Replace(UserFormatKey, System.Environment.UserName)
                               .Replace(UserDataFormatKey, System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile))
                               .Replace(ProgramDataFormatKey, System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles))
                               .ToString();
                var dest = Path.Combine(temp, Path.GetFileName(orig));
                if (File.Exists(dest)) File.Delete(dest);
                File.Copy(orig, dest);
            }

        if (ExcludedFiles?.Count is > 0)
            foreach (var file in ExcludedFiles)
            {
                var orig = file.Replace(UserFormatKey, System.Environment.UserName)
                               .Replace(UserDataFormatKey, System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile))
                               .Replace(ProgramDataFormatKey, System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles))
                               .ToString();
                var dest = Path.Combine(temp, Path.GetFileName(orig));
                if (File.Exists(dest)) File.Delete(dest);
                File.Copy(orig, dest);
            }

        Console.WriteLine("Succesfully copied files");
    }

    public override void Validate(List<Exception> exceptions)
    {
        void Check(List<Exception> excpList, ICollection<string>? data, string? dataFormat, string dataFormatKey, string dataKey,
        [CallerArgumentExpression(nameof(dataFormat))] string dataFormatName = "",
        [CallerArgumentExpression(nameof(dataFormatKey))] string dataFormatKeyName = "",
        [CallerArgumentExpression(nameof(data))] string dataName = "",
        [CallerArgumentExpression(nameof(dataKey))] string dataKeyName = "")
        {
            if (data?.Count is > 0)
            {
                if (string.IsNullOrWhiteSpace(CommandArgumentsFormat))
                {
                    excpList.Add(new InvalidDataException($"CommandArgumentsFormat cannot be null or only whitespace if {dataName} has elements"));
                    return;
                }

                if (string.IsNullOrWhiteSpace(dataFormat))
                {
                    excpList.Add(new InvalidDataException($"{dataFormatName} cannot be null or only whitespace if {dataName} has elements"));
                    return;
                }

                else if (dataFormat.Contains(dataFormatKey) is false) excpList.Add(new InvalidDataException($"{dataFormatName} ({dataFormat}) must contain {dataFormatKeyName} ({dataFormatKey})"));
                else if (CommandArgumentsFormat.Contains(dataKey) is false) excpList.Add(new InvalidDataException($"CommandArgumentsFormat must contain {dataKeyName} ({dataKey}) if {dataName} has elements"));
            }
        }

        if (string.IsNullOrWhiteSpace(FileName)) throw new InvalidDataException("FileName must not be null");

        if (IncludedFiles is null && IncludedFileTypes is null)
            exceptions.Add(new InvalidDataException("IncludedFiles and IncludedFileTypes can't both be null"));
        else
            Check(exceptions, IncludedFiles ?? IncludedFileTypes, IncludedFileFormat, FileProcessingStage.FileFormatKey, FileProcessingStage.IncludedFileListFormatKey);

        Check(exceptions, ExcludedFiles, ExcludedFileFormat, FileProcessingStage.FileFormatKey, FileProcessingStage.ExcludedFileListFormatKey);
        Check(exceptions, ExternalFiles, ExternalFileFormat, FileProcessingStage.FileFormatKey, FileProcessingStage.ExternalFileListFormatKey);

        HashSet<string> keys = new();
        if (CommandValues is not null)
        {
            if (string.IsNullOrWhiteSpace(CommandArgumentsFormat))
            {
                exceptions.Add(new InvalidDataException($"CommandArgumentsFormat cannot be null or only whitespace if CommandValues has elements"));
            }
            else
                foreach (var (k, v) in CommandValues)
                {
                    if (keys.Add(k) is false)
                    {
                        exceptions.Add(new InvalidDataException($"CommandValues has repeat key: {k}"));
                        continue;
                    }

                    if ((k.StartsWith('{') && k.EndsWith('}')) is false)
                        exceptions.Add(new InvalidDataException($"CommandArgumentsFormat value of key '{k}' must start with an opening bracket ('{{') and end with a closing bracket ('}}')"));
                    if (CommandArgumentsFormat.Contains(k) is false)
                        exceptions.Add(new InvalidDataException($"CommandArgumentsFormat does not have a FormatKey for CommandValue of key '{k}'"));

                    foreach (var reserved in FileProcessingStage.ReservedWords)
                    {
                        if (k.Contains(reserved))
                            exceptions.Add(new InvalidDataException($"CommandArgumentsFormat of key '{k}' must not contain reserved word {reserved}"));
                        if (v.Contains(reserved))
                            exceptions.Add(new InvalidDataException($"CommandArgumentsFormat value for key '{k}' must not contain reserved word {reserved}"));
                    }
                }
        }
    }
}
