using System.Windows.Media;
using System.Windows.Media.Imaging;
using AKG.Core.Objects;

namespace AKG.Core.Renderer;

public static class RendererFacade
{
    public static void Render(Scene scene, WriteableBitmap? wb, Color backgroundColor, Color foregroundColor, RenderMode mode)
    {
        if (wb == null) return;
        
        WireframeRenderer.ClearBitmap(wb, backgroundColor);
        
        scene.Camera.ChangeEye();
        scene.UpdateAllModels();
        
        switch (mode)
        {
            case RenderMode.Wireframe:
                foreach (var model in scene.Models)
                {
                    WireframeRenderer.DrawWireframe(model, wb, foregroundColor, scene.Camera);
                }
                break;
            case RenderMode.FilledTriangles:
                Rasterizer.ClearZBuffer(scene.CanvasWidth, scene.CanvasHeight, scene.Camera);
                foreach (var model in scene.Models)
                {
                    Rasterizer.DrawFilledTriangleLambert(model, wb, foregroundColor, scene.Camera, scene.Lights);
                }
                break;
            default:
                throw new NotSupportedException("Неизвестный режим рендеринга");
        }
        
        if (scene.SelectedModel is not null)
            WireframeRenderer.Draw3DSelectionHighlight(scene, scene.SelectedModel, wb, Colors.Aqua);

    }
}