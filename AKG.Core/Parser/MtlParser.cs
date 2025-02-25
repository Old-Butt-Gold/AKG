using System.IO;

namespace AKG.Core.Parser;

public static class MtlParser
{
    public static Dictionary<string, Material> Parse(string mtlFilePath)
    {
        var materials = new Dictionary<string, Material>();
        Material? current = null;
        string mtlDirectory = Path.GetDirectoryName(mtlFilePath)!;

        foreach (var line in File.ReadLines(mtlFilePath))
        {
            var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            switch (parts[0].ToLowerInvariant())
            {
                case "newmtl":
                    if (parts.Length >= 2)
                    {
                        if (current != null)
                            materials[current.Name] = current;
                        current = new Material { Name = parts[1] };
                    }
                    break;
                case "map_kd":
                    if (current != null && parts.Length >= 2)
                        current.DiffuseMap = GetFullPath(mtlDirectory, parts[1]);
                    break;
                case "norm":
                    if (current != null && parts.Length >= 2)
                        current.NormalMap = GetFullPath(mtlDirectory, parts[1]);
                    break;
                case "map_mrao":
                    if (current != null && parts.Length >= 2)
                        current.SpecularMap = GetFullPath(mtlDirectory, parts[1]);
                    break;
                case "map_ke":
                    if (current != null && parts.Length >= 2)
                        current.EmissiveMap = GetFullPath(mtlDirectory, parts[1]);
                    break;
            }
        }

        if (current != null)
            materials[current.Name] = current;

        return materials;
    }

    private static string GetFullPath(string baseDirectory, string relativePath)
    {
        return Path.Combine(baseDirectory, relativePath);
    }
}