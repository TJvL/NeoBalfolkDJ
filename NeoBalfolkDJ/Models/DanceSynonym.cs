using System.Collections.Generic;

namespace NeoBalfolkDJ.Models;

/// <summary>
/// Represents a dance name and its synonyms/aliases.
/// </summary>
public class DanceSynonym
{
    public string Name { get; set; } = string.Empty;
    public List<string> Synonyms { get; set; } = new();
}

