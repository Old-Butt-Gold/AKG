namespace AKG.Core.Objects;

public enum RenderMode
{
    Wireframe,
    FilledTrianglesLambert,
    FilledTrianglesPhong,         
    FilledTrianglesAverageFaceNormalPhong   // Фонговое затенение с вычислением цвета в вершинах (Гуравское)
}