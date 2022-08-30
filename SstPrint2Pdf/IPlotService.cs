using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Publishing;
using Plottings;
using SstPrint2Pdf.AcDrawing;
using SstPrint2Pdf.Extensions;
using PlotType = Autodesk.AutoCAD.DatabaseServices.PlotType;
using SstPrint2Pdf.PrinterConfiguration;
namespace SstPrint2Pdf
{
    public interface IPlotService
    {

        bool PublishToSingleDocument(List<PlotArea> sheets);
        bool PblishToMultiDocuments(List<PlotArea> sheets);


    }

    public class PlotService : IPlotService
    {
        private Document _document;
        private readonly Database _database;

        public PlotService(IDrawing drawing)
        {
            _database = drawing.Db;
            _document = drawing.Doc;
        }

        #region private_members
        private List<PlotSettings> ConfigurePlot(Transaction tr, List<PlotArea> regionstoplot, string printer)
        {


            var blockTable = _database.BlockTableId.OpenAs<BlockTable>(tr);
            var btableRecord = blockTable[BlockTableRecord.ModelSpace].OpenAs<BlockTableRecord>(tr);

            var layout = btableRecord.LayoutId.OpenAs<Layout>(tr, OpenMode.ForWrite);
            var pls = new List<PlotSettings>();
            regionstoplot.ForEach(el =>
            {

                var ps = ConfigurePlotSettings(tr, layout, el, printer);

                pls.Add(ps);



            });
            return pls;

        }
       
        private void CreatePlotSettings(PlotSettings baselayout, string name)
        {
            // Create a new PlotSettings object: 
            // True - model space, False - named layout
            var acPlSet = new PlotSettings(baselayout.ModelType);
            acPlSet.CopyFrom(baselayout);

            acPlSet.PlotSettingsName = name;
            acPlSet.AddToPlotSettingsDictionary(_database);

            _database.UsingTransaction(tr => tr.AddNewlyCreatedDBObject(acPlSet, true));


        }
        private void DeletePlotSettings(List<string> names)
        {
            _database.PlotSettingsDictionaryId.OpenAs<DBDictionary>(OpenMode.ForWrite,
                psd =>
                  names.ForEach(el => { if (psd.Contains(el)) psd.Remove(el); })
                );

        }
        private PlotSettings ConfigurePlotSettings(Transaction tr, PlotSettings baselayout, PlotArea area, string printer)
        {



            //  var createNew = false;

            var plSets = tr.GetObject(_database.PlotSettingsDictionaryId, OpenMode.ForRead) as DBDictionary;
            var plsetname = area.ToString();

            PlotSettings acPlSet;
            // Check to see if the page setup exists
            if (plSets != null && !plSets.Contains(plsetname))
            {
                // createNew = true;

                CreatePlotSettings(baselayout, plsetname);
            }
            //else
            //{
            acPlSet = plSets.GetAt(plsetname).OpenAs<PlotSettings>(tr, OpenMode.ForWrite);
            //}

            // Update the PlotSettings object

            var acPlSetVdr = PlotSettingsValidator.Current;

            // Rebuild plotter, plot style, and canonical media lists 
            // (must be called before setting the plot style)
            acPlSetVdr.RefreshLists(acPlSet);

            // Set the Plotter and page size
            var papersize = string.Empty;// GostFormatMapper.GetPaperSize(area.Format);
            acPlSetVdr.SetPlotConfigurationName(acPlSet, printer, papersize);

            acPlSetVdr.SetPlotWindowArea(acPlSet, area.PlotRegion);
            acPlSetVdr.SetPlotType(acPlSet, PlotType.Window);
            acPlSetVdr.SetUseStandardScale(acPlSet, true);
            acPlSetVdr.SetStdScaleType(acPlSet, StdScaleType.ScaleToFit);
            acPlSetVdr.SetPlotCentered(acPlSet, true);

            // Specify the plot orientation
            acPlSetVdr.SetPlotRotation(acPlSet, PlotRotation.Degrees000);

            // Set the plot offset
            acPlSetVdr.SetPlotOrigin(acPlSet, new Point2d(0, 0));

            //Set the plot scale
            //acPlSetVdr.SetUseStandardScale(acPlSet, true);
            //acPlSetVdr.SetStdScaleType(acPlSet, StdScaleType.ScaleToFit);
            acPlSetVdr.SetPlotPaperUnits(acPlSet, PlotPaperUnit.Millimeters);
            acPlSet.ScaleLineweights = true;
            // Specify the plot options
            acPlSet.PrintLineweights = true;


            // Specify if plot styles should be displayed on the layout
            // acPlSet.ShowPlotStyles = true;

            // Rebuild plotter, plot style, and canonical media lists 
            // (must be called before setting the plot style)
            //acPlSetVdr.RefreshLists(acPlSet);

            // Specify the shaded viewport options
            //  acPlSet.ShadePlot = PlotSettingsShadePlotType.AsDisplayed;

            //acPlSet.ShadePlotResLevel = ShadePlotResLevel.Normal;

           
            // acPlSet.PlotTransparency = false;
            //  acPlSet.PlotPlotStyles = true;
            //  acPlSet.DrawViewportsFirst = true;


            // Use only on named layouts - Hide paperspace objects option
            //   acPlSet.PlotHidden = true;

          

            // Set the plot style
            // acPlSetVdr.SetCurrentStyleSheet(acPlSet, _database.PlotStyleMode ? "acad.ctb" : "acad.stb");

            // Zoom to show the whole paper
            //acPlSetVdr.SetZoomToPaperOnUpdate(acPlSet, true);

            acPlSet.DowngradeOpen();
            return acPlSet;

        }





        #endregion



        public bool PublishToSingleDocument(List<PlotArea> sheets)
        {

            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;

            var viewsToPlot = new StringCollection();
            viewsToPlot.Add("Test1");
            viewsToPlot.Add("Test2");

            // Create page setup based on the views
            using (var Tx = db.TransactionManager.StartTransaction())
            {
                var layoutId = LayoutManager.Current.GetLayoutId(LayoutManager.Current.CurrentLayout);
                var layout = Tx.GetObject(layoutId, OpenMode.ForWrite) as Layout;


                foreach (var viewName in viewsToPlot)
                {

                    var plotSettings = new PlotSettings(layout.ModelType);
                    plotSettings.CopyFrom(layout);
                    var psv = PlotSettingsValidator.Current;
                    psv.SetPlotConfigurationName(plotSettings, "DWF6 ePlot.pc3", "ANSI_A_(8.50_x_11.00_Inches)");
                    psv.RefreshLists(plotSettings);
                    psv.SetPlotViewName(plotSettings, viewName);
                    psv.SetPlotType(plotSettings, PlotType.View);
                    psv.SetUseStandardScale(plotSettings, true);
                    psv.SetStdScaleType(plotSettings, StdScaleType.ScaleToFit);
                    psv.SetPlotCentered(plotSettings, true);
                    psv.SetPlotRotation(plotSettings, PlotRotation.Degrees000);
                    psv.SetPlotPaperUnits(plotSettings, PlotPaperUnit.Millimeters);

                    plotSettings.PlotSettingsName = String.Format("{0}{1}", viewName, "PS");
                    plotSettings.PrintLineweights = true;
                    plotSettings.AddToPlotSettingsDictionary(db);



                    Tx.AddNewlyCreatedDBObject(plotSettings, true);

                    psv.RefreshLists(plotSettings);
                    layout.CopyFrom(plotSettings);

                }



                Tx.Commit();

            }



            //put the plot in foreground

            var bgPlot = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
            Autodesk.AutoCAD.ApplicationServices.Core.Application.SetSystemVariable("BACKGROUNDPLOT", 0);
            var dwgFileName = Application.GetSystemVariable("DWGNAME") as string;
            var dwgPath = Application.GetSystemVariable("DWGPREFIX") as string;
            using (var Tx = db.TransactionManager.StartTransaction())
            {

                var collection = new DsdEntryCollection();
                var activeLayoutId = LayoutManager.Current.GetLayoutId(LayoutManager.Current.CurrentLayout);
                foreach (var viewName in viewsToPlot)
                {

                    var layout = Tx.GetObject(activeLayoutId, OpenMode.ForRead) as Layout;



                    var entry = new DsdEntry();



                    entry.DwgName = dwgPath + dwgFileName;

                    entry.Layout = layout.LayoutName;

                    entry.Title = viewName;

                    entry.NpsSourceDwg = entry.DwgName;

                    entry.Nps = String.Format("{0}{1}", viewName, "PS");



                    collection.Add(entry);

                }



                // remove the ".dwg" extension

                dwgFileName = dwgFileName.Substring(0, dwgFileName.Length - 4);



                var dsdData = new DsdData();



                dsdData.SheetType = SheetType.MultiDwf;

                dsdData.ProjectPath = dwgPath;

                dsdData.DestinationName = dsdData.ProjectPath + dwgFileName + ".dwf";



                if (System.IO.File.Exists(dsdData.DestinationName))

                    System.IO.File.Delete(dsdData.DestinationName);



                dsdData.SetDsdEntryCollection(collection);



                var dsdFile

                        = dsdData.ProjectPath + dwgFileName + ".dsd";



                //Workaround to avoid promp for pdf file name

                //set PromptForDwfName=FALSE in dsdData using StreamReader/StreamWriter



                dsdData.WriteDsd(dsdFile);



                System.IO.StreamReader sr = new System.IO.StreamReader(dsdFile);

                var str = sr.ReadToEnd();

                sr.Close();



                // Replace PromptForDwfName

                str = str.Replace("PromptForDwfName=TRUE", "PromptForDwfName=FALSE");



                // Workaround to have the page setup names included in the DSD file

                // Replace Setup names based on the created page setups

                // May not be required if Nps is output to the DSD

                int occ = 0;

                int index = str.IndexOf("Setup=");

                int startIndex = 0;

                StringBuilder dsdText = new StringBuilder();

                while (index != -1)
                {

                    // 6 for length of "Setup="

                    var str1 = str.Substring(startIndex, index + 6 - startIndex);



                    dsdText.Append(str1);

                    dsdText.Append(String.Format("{0}{1}", viewsToPlot[occ], "PS"));



                    startIndex = index + 6;

                    index = str.IndexOf("Setup=", index + 6);



                    if (index == -1)
                    {

                        dsdText.Append(

                       str.Substring(startIndex, str.Length - startIndex));

                    }

                    occ++;

                }
                // Write the DSD

                var sw = new System.IO.StreamWriter(dsdFile);
                sw.Write(dsdText.ToString());
                sw.Close();
                // Read the updated DSD file
                dsdData.ReadDsd(dsdFile);
                // Erase DSD as it is no longer needed
                System.IO.File.Delete(dsdFile);
                var plotConfig = PlotConfigManager.SetCurrentConfig("DWF6 ePlot.pc3");
                var publisher = Application.Publisher;
                // Publish it
                publisher.PublishExecute(dsdData, plotConfig);
                Tx.Commit();
            }

            //reset the background plot value
            Application.SetSystemVariable("BACKGROUNDPLOT", bgPlot);
            return true;
        }

        public bool PblishToMultiDocuments(List<PlotArea> sheets)
        {

            var pdfname = string.Format("{0}_л.{1}_{2}.pdf", sheets[0].ProjectNumber, sheets[0].SheetNumber,
                                            sheets[sheets.Count - 1].SheetNumber);
            var full = Path.Combine(CurrentDrawing.Directory(), pdfname);
            var bgp = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
            Application.SetSystemVariable("BACKGROUNDPLOT", 0);

            _database.UsingTransaction(tr =>
            {
                var pls = ConfigurePlot(tr, sheets, "Custom DWG To PDF.pc3");
                var plotter = new MultiSheetsPdf(CurrentDrawing.Document.Name, full, pls);
                plotter.Publish();


            });

            DeletePlotSettings(sheets.Select(el => el.ToString()).ToList());

            Application.SetSystemVariable("BACKGROUNDPLOT", bgp);




            return true;
        }

        //Tests the following scenario:
        // theLayout is configured: Plot To PDF, sized custom media 7A4 (== w=7X210, h=297)
        // when changing to another device, the media 7A4 must be added to that device(pc3), as the "previous media size"
        // it appeares to work only when the new device is a PC3 device AND
        // it already contains a media sized larger than the "previous media size", like 2500x841
        // We can make a PC3 file for a printer, change \\printserver\printername into printername.pc3,
        // however you cannot attach a PMP file to the PC3 file.
        // The PMP file contains the non standard media size (among other settings) and thus this will work only for
        // already defined PC3 devices with PMP files and one custom media size larger than the required media size.

        //public void PlotWithPlotStyle()
        //{
        //     Document doc = null;
        //    Editor ed = null;
        //    Database db = null;
        //    LayoutManager lm = null;

        //    var pc3Name = "DWG To PDF.pc3";

        //    try
        //    {
        //        doc = AcadApp.DocumentManager.MdiActiveDocument;
        //        ed = doc.Editor;
        //        db = doc.Database;
        //        lm = LayoutManager.Current;

        //        //Prereq: Layout initialised and defined LONG Paper
        //        db.TileMode = false; //Paperspace activated
        //        var layoutName = lm.CurrentLayout;
        //        var layoutId = lm.GetLayoutId(layoutName);

        //        AcadApp.SetSystemVariable("BACKGROUNDPLOT", 0);
        //        var plotToPdfConfig = PlotConfigManager.SetCurrentConfig(pc3Name);

        //        string pdfPathname = null;
        //        if (!doc.IsNamedDrawing)
        //        {
        //            pdfPathname = Path.Combine(Path.GetTempPath(), $"{doc.Name}_{layoutName}.pdf");
        //        }
        //        else
        //        {
        //            var pdfPath = Path.GetDirectoryName(doc.Name);
        //            var pdfFilename=Path.GetFileNameWithoutExtension(doc.Name);
        //            pdfPathname=Path.Combine(pdfPath, $"{pdfFilename}_{layoutName}.pdf");
        //        }
        //        if (File.Exists(pdfPathname))
        //        {
        //            File.Delete(pdfPathname);
        //        }

        //        var psv = PlotSettingsValidator.Current;
        //        using (var plotInfo = new PlotInfo())
        //        {
        //            plotInfo.Layout = layoutId;
        //            //  plotInfo.OverrideSettings = plotSet;
        //            plotInfo.DeviceOverride = plotToPdfConfig;

        //            using (var plotEngine = PlotFactory.CreatePublishEngine())
        //            {
        //                plotEngine.BeginPlot(null, null);

        //                var validator = new PlotInfoValidator()
        //                {
        //                    //MediaMatchingPolicy = Autodesk.AutoCAD.PlottingServices.MatchingPolicy.MatchEnabled
        //                    MediaMatchingPolicy=Autodesk.AutoCAD.PlottingServices.MatchingPolicy.MatchEnabledCustom
        //                };
        //                validator.Validate(plotInfo);
        //                var customIsPossible = validator.IsCustomPossible(plotInfo); // 0:ok, 1:PC3 required, 128:TooBig, etc
        //                if (plotInfo.IsValidated)
        //                {
        //                    if (customIsPossible == 0)
        //                    {
        //                        var plotConfig=plotInfo.ValidatedConfig;
        //                        var canonicalName = plotConfig.CanonicalMediaNames[0];
        //                        var mediaBounds=plotConfig.GetMediaBounds(canonicalName);
        //                        var pageSize = mediaBounds.PageSize;
        //                        var plotArea = mediaBounds.UpperRightPrintableArea - mediaBounds.LowerLeftPrintableArea;
        //                        ed.WriteMessage("\n PageSize= {0}" + pageSize.ToString());
        //                        ed.WriteMessage("\n PlotArea= " + plotArea.ToString());

        //                        plotEngine.BeginDocument(plotInfo, doc.Name, null, 1, true, pdfPathname);
        //                        using (var pageInfo = new PlotPageInfo())
        //                        {
        //                            plotEngine.BeginPage(pageInfo, plotInfo, true, null);
        //                        }
        //                        plotEngine.BeginGenerateGraphics(null);
        //                        plotEngine.EndGenerateGraphics(null);
        //                        plotEngine.EndPage(null);
        //                        plotEngine.EndDocument(null);
        //                        plotEngine.EndPlot(null);
        //                    }
        //                }
        //            }
        //        }
        //        if (!File.Exists(pdfPathname))
        //        {
        //            throw new System.Exception("Create PDF failed ");
        //        }
        //    }
        //    catch (System.Exception ex)
        //    {
        //        if (ed != null)
        //        {
        //            ed.WriteMessage($"\nError: {ex.Message}");
        //        }
        //    }
        //}
    }
}
