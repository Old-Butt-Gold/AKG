using System.Numerics;
using AKG.Parser;

namespace AKG.Transformations;

public static class Transformations
{
    /// <summary>
    /// Создаёт итоговую матрицу преобразования, объединяя масштабирование, вращение и перевод.
    /// Порядок умножения: итоговая матрица = Translation * Rotation * Scale.
    /// В данном случае векторы представляют как столбцы (OpenGL)
    /// </summary>
    /// <param name="scale">Однородный коэффициент масштабирования или вектор масштабирования</param>
    /// <param name="rotation">Матрица вращения (можно получить, последовательно перемножая поворот вокруг осей)</param>
    /// <param name="translation">Вектор перемещения</param>
    /// <returns>Итоговая матрица преобразования 4×4</returns>
    public static Matrix4x4 CreateWorldTransform(float scale, Matrix4x4 rotation, Vector3 translation)
    {
        // Если нужен равномерный масштаб:
        var scaleMatrix = Matrix4x4.CreateScale(scale);

        // Матрица перемещения:
        var translationMatrix = Matrix4x4.CreateTranslation(translation);

        // Итоговая матрица (порядок: сначала масштаб, затем вращение, затем перевод)
        // Если представлять вершину в виде столбца (как у нас и в OpenGL), то итоговое преобразование: M = T * R * S.
        var worldMatrix = translationMatrix * rotation * scaleMatrix; // сначала T, потом R, затем S
        
        return worldMatrix;
        
        /*Matrix4x4 rotation = Matrix4x4.CreateRotationY(MathF.PI / 2);  // 90° = PI/2 радиан
        Matrix4x4 worldTransform = Transformations.CreateWorldTransform(model.Scale, rotation, new Vector3(0, 0, 10));
        Transformations.ApplyTransformation(model, worldTransform);*/
    }

    /// <summary>
    /// Применяет матричное преобразование ко всем вершинам модели.
    /// </summary>
    /// <param name="model">Модель, вершины которой необходимо преобразовать</param>
    /// <param name="transform">Матрица преобразования</param>
    public static void ApplyTransformation(this ObjModel model, Matrix4x4 transform)
    {
        for (int i = 0; i < model.Vertices.Count; i++)
        {
            // Преобразование вершины. Функция Vector4.Transform учитывает матрицу 4×4.
            model.Vertices[i] = Vector4.Transform(model.Vertices[i], transform);
        }
    }
}