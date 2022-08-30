using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SstPrint2Pdf.ErrorVisualizator;
using SstPrint2Pdf.Extensions;
using SstPrint2Pdf.PrinterConfiguration;

namespace SstPrint2Pdf.PlotSettingsConfiguration
{
    public class PageSettingsService:IPageSettingsService
    {
        private readonly Database _database;
        private readonly FormatErrorVisualizator _vis;
        
        public PageSettingsService(Database database, FormatErrorVisualizator visualisator)
        {
           
            _database = database;
            _vis = visualisator;
        }


        public bool Exist(PageSettings pageSettings)
        {

            var res = false;
            _database.UsingTransaction(tr =>
                {
                    res = Exist(tr, pageSettings.Name);

                });
            return res;
        }
       
        
        public PaperValidationResult ValidatePaperSize(List<PageSettings> pageSettingses, int tolerance, double mrgnstolerance)
        {
            var res = PaperValidationResult.PaperSizeValidationError;
            _database.UsingTransaction(tr=>res=ValidatePaperSize(tr,pageSettingses,tolerance, mrgnstolerance));
            return res;
        }
       

        public void Update( List<PageSettings> listps)
        {

            _database.UsingTransaction(tr => Update(tr, listps));


        }
       
        
        public void Create(List<PageSettings> listps)
        {

            _database.UsingTransaction(tr => Create(tr,listps));


        }
        //public void CreateLayouts(List<PageSettings> listps)
        //{

        //    _database.UsingTransaction(tr => CreateLayouts(tr, listps));


        //}
        public void Delete(List<PageSettings> listpageSettings)
        {
            _database.UsingTransaction(tr=>Delete( tr, listpageSettings));
               
        }
        

        #region private_methods

        
        
        private PaperValidationResult ValidatePaperSize(Transaction tr, List<PageSettings> pageSettingses, int tolerance,
                                                        double mrgnstolerance)
        {

            using (var blockTable = _database.BlockTableId.OpenAs<BlockTable>(tr))
            {
                using (var btableRecord = blockTable[BlockTableRecord.ModelSpace].OpenAs<BlockTableRecord>(tr))
                {
                    using (var layout = btableRecord.LayoutId.OpenAs<PlotSettings>(tr))
                    {
                        var errors = new List<PageSettings>();
                        var scale = layout.CustomPrintScale;
                        foreach (var pageSettings in pageSettingses)
                        {
                            if (FormatValidation(scale, pageSettings, tolerance)) continue;
                            errors.Add(pageSettings);
                        }
                        if (errors.Count > 0)
                        {
                            ////передать сборщику ошибок страницы с "неправильными" форматами
                            
                            _vis.SetUnrecognizedFrames(errors);

                            return PaperValidationResult.PaperSizeValidationError;
                        }
                        ////////формируем список страниц с уникальными форматами
                        var distinctpages = pageSettingses.GroupBy(x => x.Format).Select(g => g.First()).ToList();

                       
                        using (var ps = new PlotSettings(true))
                        {
                            ps.CopyFrom(layout);
                            var psv = PlotSettingsValidator.Current;
                            psv.RefreshLists(ps);

                            var canMedNames = psv.GetCanonicalMediaNameList(ps);

                            foreach (var pageSettings in distinctpages)
                            {


                                if (ValidatePaperSize(psv, ps, canMedNames, pageSettings, tolerance, mrgnstolerance)) continue;
                               errors.Add(pageSettings);


                            }

                        }
                        if (errors.Count > 0)
                        {
                            ////передать сборщику ошибок страницы с отсутствующим форматами
                            _vis.SetUnrecognizedFrames(errors);
                            return PaperValidationResult.FormatValidationError;

                        }

                        foreach (var pg in distinctpages)
                        {
                            pageSettingses.FindAll(el => el.Format == pg.Format)
                                          .ForEach(it => it.PaperSize = pg.PaperSize);
                        }

                    }

                }

            }
            return PaperValidationResult.AllValidationSuccess;
        }
        private void Update(Transaction tr, List<PageSettings> listps)
        {

            var dictid = _database.PlotSettingsDictionaryId;
            using (var dict = dictid.OpenAs<DBDictionary>(tr))
            {
                foreach (var ps in listps)
                {

                    if (dict.Contains(ps.Name))
                    {

                        Update(tr, ps);

                    }

                }

            }


        }
        
        private void Create(Transaction tr, List<PageSettings> listps)
        {
            using (var blockTable = _database.BlockTableId.OpenAs<BlockTable>(tr))
            {
                using (var btableRecord = blockTable[BlockTableRecord.ModelSpace].OpenAs<BlockTableRecord>(tr))
                {
                    listps.ForEach(el => Create(tr, btableRecord.LayoutId, el));
                }

            }



        }
        
        //private static void CreateLayouts(Transaction tr, List<PageSettings> listps)
        //{
           
                
        //            listps.ForEach(el => CreateLayout(tr, el));
                

            


        //}
        
        
        private void Delete(Transaction tr, List<PageSettings> listpageSettings)
        {


            using (var dict = _database.PlotSettingsDictionaryId.OpenAs<DBDictionary>(tr))
            {
                listpageSettings.ForEach(el =>
                    {

                        Remove(dict, el);

                        using (var pset = el.Id.OpenAs<PlotSettings>(tr))
                        {
                            if (!pset.IsErased)
                            {
                                pset.ForWrite();
                                pset.Erase(true);
                            }
                        }

                    });
            }





        }
        
        private static void Remove(DBDictionary dict, PageSettings pageSettings)
        {

            if (dict.Contains(pageSettings.Id))
            {
                dict.ForWrite();
                dict.Remove(pageSettings.Id);
                dict.ForRead();



            }
        }

       
        private static bool ValidatePaperSize(PlotSettingsValidator psv, PlotSettings plotSettings,StringCollection canMedNames, PageSettings pageSettings, int fttolerance,
                                              double mrgnstolerance)
        {
           

            

            foreach (var canMedName in canMedNames)
            {
                psv.SetCanonicalMediaName(plotSettings, canMedName);
                var papUnts = plotSettings.PlotPaperUnits;
                var papMargins = plotSettings.PlotPaperMargins;
                if (papUnts != PlotPaperUnit.Millimeters ||
                    Math.Abs(papMargins.MinPoint.X) > mrgnstolerance ||
                    Math.Abs(papMargins.MinPoint.Y) > mrgnstolerance ||
                    Math.Abs(papMargins.MaxPoint.X) > mrgnstolerance ||
                    Math.Abs(papMargins.MaxPoint.Y) > mrgnstolerance) continue;


                var papSize = plotSettings.PlotPaperSize;
                var fd = pageSettings.FormatSize;
                if (Math.Abs(fd.Width - papSize.X) <= fttolerance && Math.Abs(fd.Height - papSize.Y) <= fttolerance)
                {
                    pageSettings.PaperSize = canMedName;
                    return true;
                }

            }
            return false;
        }

        private static bool FormatValidation(CustomScale scale, PageSettings pageSettings,int tolerance)
        {
            var fd = new FormatDimensions(pageSettings.DcsRegion, scale);
            var str = GostFormatMapper.GetFormat(fd, tolerance);


            if (string.IsNullOrEmpty(str)) return false;
            var val = GostFormatMapper.Map[str];
            pageSettings.FormatSize = new FormatDimensions(val.Height, val.Width);
            pageSettings.Format = str;
            return true;
        }
      
        private bool Exist(Transaction tr, string name)
        {
            var dictid = _database.PlotSettingsDictionaryId;
            return dictid.OpenAs<DBDictionary>(tr).Contains(name);
        }

       
        private void Create(Transaction tr, ObjectId fromobjid, PageSettings newpageSettings)
        {

            using (var layout = fromobjid.OpenAs<PlotSettings>(tr))
            {

                var dictid = _database.PlotSettingsDictionaryId;
                var dict= dictid.OpenAs<DBDictionary>(tr);
                if (dict.Contains(newpageSettings.Name))
                {

                   using (var pls = dict.GetAt(newpageSettings.Name).OpenAs<PlotSettings>(tr, OpenMode.ForWrite))
                   {
                       
                       pls.CopyFrom(layout);
                       newpageSettings.Id = pls.ObjectId;
                   }
                }
                else
                {
                    // Create a new PlotSettings object: 
                    // True - model space, False - named layout
                    using (var acPlSet = new PlotSettings(true))//true
                    {
                        acPlSet.CopyFrom(layout);

                        acPlSet.PlotSettingsName = newpageSettings.Name;
                        acPlSet.AddToPlotSettingsDictionary(_database);

                        tr.AddNewlyCreatedDBObject(acPlSet, true);

                        newpageSettings.Id = acPlSet.ObjectId;

                    }
                }
                
                
               
            }
            Update(tr, newpageSettings);

        }
    
        //private static void CreateLayout(Transaction tr,  PageSettings newpageSettings)
        //{
        //    // Get the layout and plot settings of the named pagesetup
        //    using (tr)
        //    {
        //        // Reference the Layout Manager
        //        var acLayoutMgr = LayoutManager.Current;
              
        //        // Create the new layout with default settings
        //        var objId = acLayoutMgr.CreateLayout(newpageSettings.Name);

        //        // Open the layout
        //        var acLayout = objId.OpenAs<Layout>(tr,OpenMode.ForWrite);
        //        var ps = newpageSettings.Id.OpenAs<PlotSettings>(tr);
        //        acLayout.CopyFrom(ps);



        //    }
            



        //}

        private static void Update(Transaction tr, PageSettings pageSettings)
        {




            var acPlSet = pageSettings.Id.OpenAs<PlotSettings>(tr, OpenMode.ForWrite);

            // Update the PlotSettings object

            var acPlSetVdr = PlotSettingsValidator.Current;

            // Rebuild plotter, plot style, and canonical media lists 
            // (must be called before setting the plot style)
            acPlSetVdr.RefreshLists(acPlSet);

            // Set the Plotter and page size

            acPlSetVdr.SetCanonicalMediaName(acPlSet, pageSettings.PaperSize);
            acPlSetVdr.SetPlotWindowArea(acPlSet, pageSettings.DcsRegion);
            acPlSetVdr.SetPlotType(acPlSet, PlotType.Window);


            acPlSetVdr.SetPlotCentered(acPlSet, true);

            // Specify the plot orientation
            acPlSetVdr.SetPlotRotation(acPlSet, PlotRotation.Degrees000);

            // Set the plot offset
            acPlSetVdr.SetPlotOrigin(acPlSet, new Point2d(0, 0));

            pageSettings.Id = acPlSet.ObjectId;  



        }  



        #endregion
    }
}