namespace AKG.Core.Parser;

/// <summary>
/// Класс, описывающий свойства материала.
/// </summary>
public class Material
{
    /// <summary>
    /// Имя материала.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Путь к текстуре диффузного цвета (map_Kd).
    /// </summary>
    public string DiffuseMap { get; set; } = string.Empty;

    /// <summary>
    /// Путь к эмиссивной текстуре (map_Ke), если есть.
    /// </summary>
    public string EmissiveMap { get; set; } = string.Empty;

    /// <summary>
    /// Путь к нормальной карте (norm).
    /// </summary>
    public string NormalMap { get; set; } = string.Empty;

    /// <summary>
    /// Путь к текстуре зеркальной карты MRAO (map_MRAO).
    /// </summary>
    public string SpecularMap { get; set; } = string.Empty;
}