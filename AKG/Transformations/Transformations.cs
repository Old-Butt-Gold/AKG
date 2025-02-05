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
    /// Создаёт матрицу преобразования из мирового пространства в пространство наблюдателя (view space).
    /// </summary>
    /// <param name="eye">Позиция камеры в мировом пространстве</param>
    /// <param name="target">Цель, на которую направлена камера</param>
    /// <param name="up">Вектор, указывающий направление «вверх» с точки зрения камеры</param>
    /// <returns>Матрица вида (view matrix) 4×4</returns>
    public static Matrix4x4 CreateViewMatrix(Vector3 eye, Vector3 target, Vector3 up)
    {
        // аналог метода Matrix4x4.CreateLookAt:
        // eye – cameraPosition
        // target – cameraTarget
        // up – cameraUpVector
        
        // Вычисляем базис камеры
        var zAxis = Vector3.Normalize(eye - target);  // Направлена от цели к камере
        var xAxis = Vector3.Normalize(Vector3.Cross(up, zAxis)); // Перпендикулярна up и zAxis
        var yAxis = up; // Обычно up уже нормализован (иначе можно нормализовать yAxis)

        // Вычисляем сдвиги: отрицательные скалярные произведения базисов на позицию камеры.
        float tx = -Vector3.Dot(xAxis, eye);
        float ty = -Vector3.Dot(yAxis, eye);
        float tz = -Vector3.Dot(zAxis, eye);

        // Формируем матрицу вида:
        var view = new Matrix4x4(
            xAxis.X, xAxis.Y, xAxis.Z, tx,
            yAxis.X, yAxis.Y, yAxis.Z, ty,
            zAxis.X, zAxis.Y, zAxis.Z, tz,
            0.0f,    0.0f,    0.0f,    1.0f);

        return view;
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
    
    /// <summary>
    /// Применяет матричное преобразование вида (view transformation) ко всем вершинам модели.
    /// </summary>
    /// <param name="model">Модель, вершины которой необходимо преобразовать</param>
    /// <param name="viewMatrix">Матрица преобразования вида</param>
    public static void ApplyViewTransformation(this ObjModel model, Matrix4x4 viewMatrix)
    {
        ApplyTransformation(model, viewMatrix);
    }
    
    
}