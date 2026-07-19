namespace EosToQLab.Infrastructure.Import.Csv;

[AttributeUsage(AttributeTargets.Property)]
public sealed class EosCsvColumnAttribute(string name) : Attribute
{
    public string Name { get; } = name;
    public bool Required { get; init; }
    public bool Trim { get; init; } = true;
    public string[] Aliases { get; init; } = [];
}