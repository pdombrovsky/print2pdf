using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.DatabaseServices;
using SstPrint2Pdf.Extensions;
using SstPrint2Pdf.PrinterConfiguration;

namespace SstPrint2Pdf
{
   
    public class PlotArea
    {
      //2012
        //[DllImport("acad.exe", CallingConvention = CallingConvention.Cdecl, EntryPoint = "acedTrans")]
      
       //2013 
        [DllImport("accore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "acedTrans")]
        private static extern int acedTrans(double[] point, IntPtr fromRb, IntPtr toRb, int disp, double[] result);

        public Extents3d WcsRegion { get; set; } 
       public Extents2d PlotRegion { get; set; }
       public string Format { get; set; }
       public string  SheetNumber { get; set; }
        
       public string ProjectNumber { get; set; }
       public override string ToString()
       {
           return String.Format("{0}_л.{1}_({2}_{3})", ProjectNumber, SheetNumber, Format.Replace("+", ""),
                                Format.Contains("+") ? "v" : "h");
       }



       #region private_members
       private static Extents2d GetPlotRegionDcs(Extents3d ucsregion)
       {

           var first = ucsregion.MinPoint;
           var second = ucsregion.MaxPoint;
           ResultBuffer rbFrom = new ResultBuffer(new TypedValue(5003, 1)),
                        rbTo = new ResultBuffer(new TypedValue(5003, 2));
           var firres = new double[] { 0, 0, 0 };
           var secres = new double[] { 0, 0, 0 };
           acedTrans(first.ToArray(), rbFrom.UnmanagedObject, rbTo.UnmanagedObject, 0, firres);
           acedTrans(second.ToArray(), rbFrom.UnmanagedObject, rbTo.UnmanagedObject, 0, secres);
           return  new Extents2d(firres[0], firres[1], secres[0], secres[1]);
       }

      

       #endregion

      public static List<string> GetDistinctFormats(List<PlotArea> plotAreas)
      {
          return plotAreas.GroupBy(i => i.Format,(key, group) => group.First().Format).ToList();
      }
      public static List<PlotArea> GetPlotAreas(Transaction tr, ObjectId[] blockids)
       {
           var res = new List<PlotArea>();

           foreach (var blockid in blockids)
           {
               var blref = tr.GetObject(blockid, OpenMode.ForRead) as BlockReference;
               if (blref != null)
               {
                   var btr = tr.GetObject(blref.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                   if (btr!= null && btr.HasAttributeDefinitions)
                   {
                       
                       var plotarea = new PlotArea();

                       var ar = AttrProperties.FindAttribute(tr, blref.AttributeCollection, "ЛИСТ");
                       plotarea.SheetNumber = AttrProperties.GetAttributeValue(ar);
                       
                      if (AttrProperties.TryGetAttributeReference(tr, blref.AttributeCollection, "НОМЕР", out ar))
                          plotarea.ProjectNumber = AttrProperties.GetAttributeValue(ar);
                      else
                      {
                          ar = AttrProperties.FindAttribute(tr, blref.AttributeCollection, "НОМЕРПРОЕКТА");
                          plotarea.ProjectNumber = AttrProperties.GetAttributeValue(ar);
                          plotarea.Format = "A4+";
                          
                      }
                       if (blref.Name.ToUpper() != "РАЗРЕШЕНИЕ")
                       {
                           plotarea.PlotRegion = GetPlotRegionDcs(blref.GetBounds());
                           plotarea.WcsRegion = blref.GetBounds();
                           if (!blref.IsDynamicBlock)
                           {
                              
                              plotarea.Format =blref.Name.Replace("А","A");
                              
                           }
                           else
                           {

                             
                               foreach (
                                   DynamicBlockReferenceProperty prop in blref.DynamicBlockReferencePropertyCollection)
                               {
                                   if (prop.PropertyName.ToUpper() == "ФОРМАТ")
                                   {
                                       plotarea.Format = prop.Value.ToString().Replace("А", "A");
                                       
                                       break;
                                   }

                               }
                           }
                       }
                       res.Add(plotarea);
                   }

               }
               
           }

           return res;
       }
       public static List<PlotArea> GetPlotAreasLines(Transaction tr, ObjectId[] linesid)
        {
            var res = new List<PlotArea>();
           foreach (var id in linesid)
           {
               var ext = id.OpenAs<Entity>(tr).GeometricExtents;
               var plar = new PlotArea();
               plar.PlotRegion = GetPlotRegionDcs(ext);
               plar.WcsRegion = ext;
               var h = (int) (plar.PlotRegion.MaxPoint.Y - plar.PlotRegion.MinPoint.Y);
               var w = (int) (plar.PlotRegion.MaxPoint.X - plar.PlotRegion.MinPoint.X);
               plar.Format = GostFormatMapper.GetFormat(h, w);
               plar.ProjectNumber = "Project_Number";
               res.Add(plar);
           }
           res.Sort((el1,el2)=>el1.PlotRegion.MinPoint.X.CompareTo(el2.PlotRegion.MinPoint.X));
           for (var i = 0; i < res.Count; i++) res[i].SheetNumber = i.ToString();
           return res;
        }
    }
      
}
