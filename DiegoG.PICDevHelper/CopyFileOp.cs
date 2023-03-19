using System.Text.Json.Serialization;

namespace DiegoG.PICDevHelper;

public readonly record struct CopyFileOp(
    string File, 
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool NoOverwrite
);
