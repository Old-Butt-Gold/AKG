using System.Numerics;
using AKG.Parser;
using AKG.Transformations;

string pathToObj = @"C:\drive-D\marci.obj"; 

try
{
    ObjModel model = ObjParser.Parse(pathToObj);

    Console.WriteLine($"Прочитано вершин: {model.Vertices.Count}");
    Console.WriteLine($"Прочитано текстурных координат: {model.TextureCoords.Count}");
    Console.WriteLine($"Прочитано нормалей: {model.Normals.Count}");
    Console.WriteLine($"Прочитано граней: {model.Faces.Count}");
    
}
catch (Exception ex)
{
    Console.WriteLine("Ошибка при парсинге файла: " + ex.Message);
}