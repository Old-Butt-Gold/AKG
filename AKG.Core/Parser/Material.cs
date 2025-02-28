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
    /// Путь к текстуре карты MRAO (map_MRAO) (metallic-roughness-ambient occlusion).
    /// </summary>
    public string SpecularMap { get; set; } = string.Empty;

    /// <summary>
    /// Путь к bump-карте (map_bump или bump).
    /// Bump-карта используется для имитации рельефа поверхности путем изменения нормалей на основе градаций яркости.
    /// </summary>
    public string BumpMap { get; set; } = string.Empty;
    
    /// <summary>
    /// Коэффициент масштабирования bump‑карты, задающий интенсивность рельефа.
    /// Если строка содержит параметр “-bm”, его значение записывается сюда.
    /// По умолчанию 1.0 (без усиления).
    /// </summary>
    public float BumpScale { get; set; } = 1.0f;
}