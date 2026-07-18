namespace EosToQLab.Core.Models;

public sealed record EosImportRequest(string FileName, Stream Content);
