using SstPrint2Pdf.AcDrawing;

namespace SstPrint2Pdf.Factories
{
    public static class Factories
    {
    public static IPageSettingsFactory GetFactory(IDrawing drawing, FactoryType type )
    {
        if (type == FactoryType.FramesOnLayerFactory) return new FramesOnLayerFactory(drawing);
        if (type == FactoryType.BlocksSstFactory) return new SstFramesFactory(drawing);

        return new FramesOnLayerFactory(drawing);
    }
    
    
    
    }
}
