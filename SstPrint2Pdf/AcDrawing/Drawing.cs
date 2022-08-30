using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace SstPrint2Pdf.AcDrawing
{
   public class Drawing:IDrawing
    {
       public Document Doc { get;private set; }
       public Database Db { get;private set; }
       public Editor Ed { get;private set; }
       public Drawing()
       {
           Doc = CurrentDrawing.Document;
           Db = CurrentDrawing.Database;
           Ed = CurrentDrawing.Editor;


       }
    }
}
