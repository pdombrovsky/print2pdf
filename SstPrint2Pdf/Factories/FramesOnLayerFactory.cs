using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SstPrint2Pdf.AcDrawing;
using SstPrint2Pdf.CoordTransfom;
using SstPrint2Pdf.Extensions;
using SstPrint2Pdf.PlotSettingsConfiguration;

namespace SstPrint2Pdf.Factories
{
    public class FramesOnLayerFactory:IPageSettingsFactory
    {
        private readonly IDrawing _drawing;
        public FramesOnLayerFactory(IDrawing drawing)
        {
            _drawing = drawing;


        }
        public List<PageSettings> GetPages()
        {


            var pls = new List<PageSettings>();
            var layer = _drawing.Ed.GetStringValue("Введите имя слоя, на котором располагаются только рамки: ");
            if (string.IsNullOrEmpty(layer)) return pls;
          
            var rowcnt = 0;
            PromptResult prsKw;
            do
            {

                var msg = String.Format("Выделите область чертежа, содержащую {0}-й ряд рамок(листов) для печати:",++rowcnt);
                var prselres = _drawing.Ed.GetObjectIdsInRegion(msg,Filters.ForAllPlines(layer));
                
                if (prselres.Status != PromptStatus.OK) break;
                
                    var ids = prselres.Value.GetObjectIds();
                    if (ids.Count() == 0) break;
                    

                        var row = GetPlotAreasLines(ids);
                        row.Sort((el1, el2) =>
                            {
                                var p1 = el1.WcsRegion.MinPoint;
                                var p2 = el2.WcsRegion.MinPoint;
                                return p1.X.CompareTo(p2.X);

                            });
                        msg = "\nДобавить следующий ряд?";
                        prsKw = _drawing.Ed.PromptForKeywordSelection(msg, new[] { "Да", "Нет" }, false, "Да");
                        if (prsKw.Status != PromptStatus.OK) break;
                        pls.AddRange(row);
                    

               
                
            } while (prsKw.StringResult != "Нет");
           
           
          

            return pls;
        }

        private  List<PageSettings> GetPlotAreasLines(ObjectId[] ids)
        {
            var res = new List<PageSettings>();
            _drawing.Db.UsingTransaction(tr =>
               {   
                   foreach (var id in ids)
                   {
                       var ext = id.OpenAs<Entity>(tr).GeometricExtents;
                       var plar = new PageSettings();
                       plar.DcsRegion = ExtentsTransformation.GetExtentsDcs(ext);
                       plar.WcsRegion = ext;
                       res.Add(plar);
                   }
               });
            
           
            return res;
        }
    }
}
