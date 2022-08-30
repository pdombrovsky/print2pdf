
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace SstPrint2Pdf.AcDrawing
{
    public interface IDrawing
    {
        Document Doc { get; }
        Database Db { get; }
        Editor Ed { get;  }
    }
}
