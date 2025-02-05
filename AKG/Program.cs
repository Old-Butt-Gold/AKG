using System.Numerics;
using AKG.Parser;

string pathToObj = @"C:\drive-D\marci.obj"; 

try
{
    ObjModel model = ObjParser.Parse(pathToObj);

    Console.WriteLine($"Прочитано вершин: {model.OriginalVertices.Count}");
    Console.WriteLine($"Прочитано текстурных координат: {model.TextureCoords.Count}");
    Console.WriteLine($"Прочитано нормалей: {model.Normals.Count}");
    Console.WriteLine($"Прочитано граней: {model.Faces.Count}");
    
    model.UpdateImage();
    
}
catch (Exception ex)
{
    Console.WriteLine("Ошибка при парсинге файла: " + ex.Message);
}