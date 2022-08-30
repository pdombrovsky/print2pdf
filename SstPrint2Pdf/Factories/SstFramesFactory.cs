using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using SstPrint2Pdf.AcDrawing;
using SstPrint2Pdf.Extensions;
using SstPrint2Pdf.PlotSettingsConfiguration;

namespace SstPrint2Pdf.Factories
{
    public class SstFramesFactory:IPageSettingsFactory
    {
       private readonly IDrawing _drawing;
       public SstFramesFactory(IDrawing drawing)
        {
            _drawing = drawing;


        }
        
        
        public List<PageSettings> GetPages()
        {
            var msg = String.Format("Выделите область чертежа, содержащую рамки(листы) для печати:");
            var prselres = _drawing.Ed.GetObjectIdsInRegion(msg,Filters.ForAllBlocks());
            var res = new List<PageSettings>();
            if (prselres.Status == PromptStatus.OK)
            {
                var ids = prselres.Value.GetObjectIds();
                if (ids.Count() > 0)
                {
                    
                    
                    _drawing.Db.ModelSpaceAction(OpenMode.ForRead, (tr, modelbtr) =>
                        {

                            foreach (var id in ids)
                            {

                                var blref = id.OpenAs<BlockReference>(tr);
                                if (blref != null)
                                {
                                    var btr = blref.BlockTableRecord.OpenAs<BlockTableRecord>(tr);
                                    if (btr != null && btr.HasAttributeDefinitions)
                                    {
                                        var attrcnt = 2;
                                        var sheet = string.Empty;
                                        var prjnum = string.Empty;
                                       blref.AttributeCollectionAction(col =>
                                           {

                                               foreach (var ar in col)
                                               {
                                                   if (ar.Tag.ToUpper() == "ЛИСТ")
                                                   {
                                                       sheet =(ar.IsMTextAttribute) ? ar.MTextAttribute.Text : ar.TextString;
                                                       attrcnt--;
                                                   }
                                                   if (ar.Tag.ToUpper() == "НОМЕР" || ar.Tag.ToUpper() == "НОМЕРПРОЕКТА")
                                                   {
                                                       prjnum = (ar.IsMTextAttribute) ? ar.MTextAttribute.Text : ar.TextString;
                                                       attrcnt--;
                                                   }

                                                   if (attrcnt == 0) break;

                                               }
                                           });
                                        if (attrcnt == 0)
                                        {
                                            var ps = new PageSettings();
                                            ps.Name = prjnum + "_л_" + sheet;
                                   
                                            var ext = blref.GetBounds();
                                            ps.WcsRegion = ext;
                                            ps.DcsRegion = CoordTransfom.ExtentsTransformation.GetExtentsDcs(ext);
                                            res.Add(ps);
                                        }

                                    }
                                }



                            }

                        });

                }
            
            }
            res.Sort((el1, el2) =>
                {
                    var c1 = ReturnCode(el1.Name);
                    var c2 = ReturnCode(el2.Name);
                    return c1.CompareTo(c2);
                });
            return res;

        }
        public static long ReturnCode(string value)
        {
            string digits = "";
            int letters = 0;

            foreach (char s in value)
            {
                if (char.IsDigit(s)) digits += s;
                else if (char.IsLetter(s)) letters += s;
            }

            return (String.IsNullOrEmpty(digits)) ? 0 : long.Parse(digits) + letters;
        }


      
    }
}
