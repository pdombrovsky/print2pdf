// (C) Copyright 2018 by  
//
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.Publishing;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using SstPrint2Pdf.AcDrawing;
using SstPrint2Pdf.ErrorVisualizator;
using Exception = System.Exception;
using PlotType = Autodesk.AutoCAD.DatabaseServices.PlotType;
using SstPrint2Pdf.PrinterConfiguration;
using SstPrint2Pdf.PlotSettingsConfiguration;
using SstPrint2Pdf.Extensions;
// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(SstPrint2Pdf.MyCommands))]

namespace SstPrint2Pdf
{

    // This class is instantiated by AutoCAD for each document when
    // a command is called by the user the first time in the context
    // of a given document. In other words, non static data in this class
    // is implicitly per-document!
    public static class MyCommands
    {

        #region private static methods

        //private methods
        private static PromptSelectionResult GetSelection(string promptObjMessage, SelectionFilter filter)
        {
            var selOpt = new PromptSelectionOptions();

            selOpt.MessageForAdding = String.Format("\n{0}", promptObjMessage);
            selOpt.AllowDuplicates = false;

            return CurrentDrawing.Editor.GetSelection(selOpt, filter);

        }
        private static PromptSelectionResult GetSelection(string promptObjMessage,
                                        SelectionFilter filter,
                                        string promptKeyWordMessage,
                                        IEnumerable<string> keyWords,
                                        Action<string> actionKeyWord)
        {
            var selOpt = new PromptSelectionOptions();


            foreach (var keyword in keyWords)
            {
                selOpt.Keywords.Add(keyword);

            }

            var kwds = selOpt.Keywords.GetDisplayString(true);

            selOpt.MessageForAdding = String.Format("\n{0} или {1} {2}", promptObjMessage, promptKeyWordMessage, kwds);
            selOpt.AllowDuplicates = false;
            selOpt.KeywordInput += (s, e) => actionKeyWord(e.Input);

            return CurrentDrawing.Editor.GetSelection(selOpt, filter);

        }


        private static PromptSelectionResult GetObjectIdsInRegion(string promptObjMessage, SelectionFilter filter)
        {
            PromptResult prsKw;
            PromptSelectionResult prselres;
            string msg;
            do
            {

                prselres = GetSelection(String.Format("\n{0}", promptObjMessage), filter);
                if (prselres.Status == PromptStatus.OK) break;

                msg = "\nОбъекты не выбраны. Повторить?";
                prsKw = PromptForKeywordSelection(msg, new[] { "Да", "Нет" }, false, "Да");


                if (prsKw.Status != PromptStatus.OK) break;
            }
            while (prsKw.StringResult != "Нет");

            return prselres;

        }
        //not tested
        private static PromptResult PromptStringWithKeywords(string promptStringMessage,
                                                     string promptKeyWordMessage,
                                                      IEnumerable<string> keywords,

                                                       string defaultKeyword = "")
        {
            var promptStringOptions = new PromptStringOptions("\n" + promptStringMessage + " или " + promptKeyWordMessage + ": ");

            foreach (var keyword in keywords)
            {
                promptStringOptions.Keywords.Add(keyword);

            }
            if (defaultKeyword != "")
            {
                promptStringOptions.Keywords.Default = defaultKeyword;
            }



            promptStringOptions.AppendKeywordsToMessage = true;

            var keywordResult = CurrentDrawing.Editor.GetString(promptStringOptions);
            return keywordResult;
        }

        private static PromptResult PromptForKeywordSelection(string promptMessage,
                                                       IEnumerable<string> keywords,
                                                        bool allowNone,
                                                        string defaultKeyword = "")
        {
            var promptKeywordOptions = new PromptKeywordOptions("\n" + promptMessage) { AllowNone = allowNone };
            foreach (var keyword in keywords)
            {
                promptKeywordOptions.Keywords.Add(keyword);
            }
            if (defaultKeyword != "")
            {
                promptKeywordOptions.Keywords.Default = defaultKeyword;
            }
            promptKeywordOptions.AppendKeywordsToMessage = true;
            var keywordResult = CurrentDrawing.Editor.GetKeywords(promptKeywordOptions);
            return keywordResult;
        }


        private static PromptResult Getstringvalue(string message)
        {

            var pso = new PromptStringOptions(String.Format("\n{0}", message));
            pso.DefaultValue = String.Empty;
            pso.AllowSpaces = true;

            return CurrentDrawing.Editor.GetString(pso);


        }

        private static string GetString(string promptMessage)
        {

            PromptResult prsKw;
            string msg;
            do
            {
                var prs = Getstringvalue(promptMessage);
                if (prs.Status == PromptStatus.OK && !string.IsNullOrEmpty(prs.StringResult)) return prs.StringResult;

                msg = "\nНекорректный ввод. Необходимо ввести один или несколько символов. Повторить?";
                prsKw = PromptForKeywordSelection(msg, new[] { "Да", "Нет" }, false, "Да");


                if (prsKw.Status != PromptStatus.OK) break;



            } while (prsKw.StringResult != "Нет");


            return null;


        }



        #endregion
        /// <summary>
        /// Для демоверсии
        /// </summary>
        private static byte _counter1 = 0;
        private static string _pg = "Да";
        static DBObjectCollection _markers = null;
        static void ClearTransientGraphics()
        {

            TransientManager tm = TransientManager.CurrentTransientManager;

            IntegerCollection intCol = new IntegerCollection();

            if (_markers != null)
            {

                foreach (DBObject marker in _markers)
                {

                    tm.EraseTransient(marker, intCol);

                    marker.Dispose();

                }

            }

        }

        [CommandMethod("Test", CommandFlags.Modal)]
        public static void tst()
        {


            var activeDoc = Application.DocumentManager.MdiActiveDocument;

            var db = activeDoc.Database;

            var ed = activeDoc.Editor;

            var peo = new PromptEntityOptions("Select a polyline : ");

            peo.SetRejectMessage("Not a polyline");

            peo.AddAllowedClass(typeof(Autodesk.AutoCAD.DatabaseServices.Polyline), true);

            var per = ed.GetEntity(peo);

            if (per.Status != PromptStatus.OK) return;

            var plOid = per.ObjectId;
            var ppr = ed.GetPoint(new PromptPointOptions("Select an internal point : "));

            if (ppr.Status != PromptStatus.OK) return;

            var testPoint = ppr.Value;



            var pao = new PromptAngleOptions("Specify ray direction");

            pao.BasePoint = testPoint;

            pao.UseBasePoint = true;

            var rayAngle = ed.GetAngle(pao);

            if (rayAngle.Status != PromptStatus.OK)
                return;



            var tempPoint = testPoint.Add(Vector3d.XAxis);
            tempPoint = tempPoint.RotateBy(rayAngle.Value, Vector3d.ZAxis, testPoint);
            var rayDir = tempPoint - testPoint;



            ClearTransientGraphics();
            _markers = new DBObjectCollection();

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {

                var plcurve = tr.GetObject(plOid, OpenMode.ForRead) as Curve;

                for (int cnt = 0; cnt < 2; cnt++)
                {

                    if (cnt == 1) rayDir = rayDir.Negate();
                    using (Ray ray = new Ray())
                    {
                        ray.BasePoint = testPoint;
                        ray.UnitDir = rayDir;

                        var intersectionPts = new Point3dCollection();

                        plcurve.IntersectWith(ray, Intersect.OnBothOperands, intersectionPts, IntPtr.Zero, IntPtr.Zero);



                        foreach (Point3d pt in intersectionPts)
                        {

                            Circle marker = new Circle(pt, Vector3d.ZAxis, 0.2);

                            marker.Color = Color.FromRgb(0, 255, 0);

                            _markers.Add(marker);
                            var intCol = new IntegerCollection();

                            var tm = TransientManager.CurrentTransientManager;
                            tm.AddTransient(marker, TransientDrawingMode.Highlight, 128, intCol);
                            ed.WriteMessage("\n" + pt.ToString());

                        }

                    }

                }

                tr.Commit();



            }









        }
        [CommandMethod("Print2Pdf", CommandFlags.Modal)]
        public static void Print2Pdf() // This method can have any name
        {

            //if (++_counter1 > 4)
            //{
            //    CurrentDrawing.Editor.WriteMessage("\nПревышено максимальное количество запусков команды.");
            //    CurrentDrawing.Editor.WriteMessage("\nДля возобновления работы, перегрузите Автокад и загрузите библиотеку снова.");
            //    CurrentDrawing.Editor.WriteMessage("\nДля получения полной версии свяжитесь с разработчиком.");
            //   CurrentDrawing.Editor.WriteMessage("\nРазработчик- Домбровский П.Э.\ne-mail: pdambrouski@gmail.com");
            //    return;
            //}
            try
            {
                CurrentDrawing.Editor.WriteMessage("\nТекущие настройки: Печать в один файл - \"{0}\"", _pg);
                var prselres = GetSelection("Выделите область чертежа с рамками для печати",

                                           Filters.ForAllBlocks(),
                                            "изменить настройки",
                                            new[] { "Set" },
                                                str =>
                                                {
                                                    if (str == "Set")
                                                    {

                                                        _pg =
                                                            PromptForKeywordSelection(
                                                                "Печать в один файл?", new[] { "Да", "Нет" },
                                                                false, _pg).StringResult;

                                                        CurrentDrawing.Editor.WriteMessage("\nТекущие настройки: Печать в один файл - \"{0}\"", _pg);
                                                    }
                                                });


                if (prselres.Status != PromptStatus.OK)
                {
                    CurrentDrawing.Editor.WriteMessage("\nНекорректный выбор.  Выход из процедуры.");
                    return;
                }
                var arrids = prselres.Value.GetObjectIds();
                CurrentDrawing.Document.UsingTransaction(tr =>
                                                          arrids = AttrProperties.GetBlockWithAttributes(tr, arrids,
                                                                                                         attrcoll => (AttrProperties
                                                                                                                          .HasAttribute
                                                                                                                          (tr,
                                                                                                                           attrcoll,
                                                                                                                           "ЛИСТ") &&
                                                                                                                      (AttrProperties
                                                                                                                           .HasAttribute
                                                                                                                           (tr,
                                                                                                                            attrcoll,
                                                                                                                            "НОМЕР") ||
                                                                                                                       AttrProperties
                                                                                                                           .HasAttribute
                                                                                                                           (tr,
                                                                                                                            attrcoll,
                                                                                                                            "НОМЕРПРОЕКТА")))));





                if (arrids.Length == 0)
                {
                    CurrentDrawing.Editor.WriteMessage("\nНекорректный выбор.  Выход из процедуры.");
                    return;
                }
                List<PlotArea> areas = null;

                CurrentDrawing.Document.UsingTransaction(tr =>
                                                         areas = PlotArea.GetPlotAreas(tr, arrids));

                if (areas.Count == 0)
                {
                    CurrentDrawing.Editor.WriteMessage("\nОбъекты не выбраны.  Выход из процедуры.");
                    return;
                }
                CurrentDrawing.Editor.WriteMessage("\nСформировано {0} листов. Началась подготовка к печати.", areas.Count);

                areas.Sort((el1, el2) =>
                    {


                        try
                        {
                            var dbl1 = Convert.ToDouble(el1.SheetNumber.Replace(".", ","), System.Globalization.CultureInfo.InvariantCulture);
                            var dbl2 = Convert.ToDouble(el2.SheetNumber.Replace(".", ","),
                                                        System.Globalization.CultureInfo.InvariantCulture);
                            return dbl1.CompareTo(dbl2);
                        }
                        catch
                        {
                            return String.Compare(el1.SheetNumber, el2.SheetNumber, StringComparison.Ordinal);

                        }

                    });


                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                var pserv = new PlotService(new AcDrawing.Drawing());
                pserv.PblishToMultiDocuments(areas);



                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////                

            }

            catch (Exception ex)
            {
                CurrentDrawing.Editor.WriteMessage("В процессе выполнения команды произошла ошибка...\n");
                CurrentDrawing.Editor.WriteMessage(ex.Message);

                CurrentDrawing.Editor.WriteMessage("\nПопробуйте запустить команду еще раз или свяжитесь с разработчиком");
            }




        }
        [CommandMethod("PrintExt2Pdf")]
        public static void PrintExt2Pdf() // This method can have any name
        {

            //if (++_counter1 > 4)
            //{
            //    CurrentDrawing.Editor.WriteMessage("\nПревышено максимальное количество запусков команды.");
            //    CurrentDrawing.Editor.WriteMessage("\nДля возобновления работы, перегрузите Автокад и загрузите библиотеку снова.");
            //    CurrentDrawing.Editor.WriteMessage("\nДля получения полной версии свяжитесь с разработчиком.");
            //   CurrentDrawing.Editor.WriteMessage("\nРазработчик- Домбровский П.Э.\ne-mail: pdambrouski@gmail.com");
            //    return;
            //}
            try
            {
                var lr = GetString("Введите имя слоя на котором лежат только рамки: ");
                if (string.IsNullOrEmpty(lr))
                {
                    CurrentDrawing.Editor.WriteMessage("\nНекорректный ввод.  Выход из процедуры.");
                    return;
                }
                CurrentDrawing.Editor.WriteMessage("\nТекущие настройки: Печать в один файл - \"{0}\"", _pg);
                var prselres = GetSelection("Выделите область чертежа с рамками для печати",

                                           Filters.ForAllPlines(lr),
                                            "изменить настройки",
                                            new[] { "Set" },
                                                str =>
                                                {
                                                    if (str == "Set")
                                                    {

                                                        _pg =
                                                            PromptForKeywordSelection(
                                                                "Печать в один файл?", new[] { "Да", "Нет" },
                                                                false, _pg).StringResult;

                                                        CurrentDrawing.Editor.WriteMessage("\nТекущие настройки: Печать в один файл - \"{0}\"", _pg);
                                                    }
                                                });


                if (prselres.Status != PromptStatus.OK)
                {
                    CurrentDrawing.Editor.WriteMessage("\nНекорректный выбор.  Выход из процедуры.");
                    return;
                }
                var arrids = prselres.Value.GetObjectIds();



                if (arrids.Length == 0)
                {
                    CurrentDrawing.Editor.WriteMessage("\nНекорректный выбор.  Выход из процедуры.");
                    return;
                }
                List<PlotArea> areas = null;

                CurrentDrawing.Document.UsingTransaction(tr =>
                                                         areas = PlotArea.GetPlotAreasLines(tr, arrids));

                if (areas.Count == 0)
                {
                    CurrentDrawing.Editor.WriteMessage("\nОбъекты не выбраны.  Выход из процедуры.");
                    return;
                }
                CurrentDrawing.Editor.WriteMessage("\nСформировано {0} листов. Началась подготовка к печати.", areas.Count);
                IDrawing dr = new Drawing();
                IPlotDeviceService plotDeviceService = new PlotDeviceService(dr);
                var vis = new FormatErrorVisualizator(dr, 1, LineWeight.LineWeight050);
                var pss = new PageSettingsService(dr.Db, vis);
                //var devinf = new Device();
                var res = plotDeviceService.ValidateDevice();
                CurrentDrawing.Editor.WriteMessage("\nResult: {0}", res);
                if (res == DevValidationResult.ValidationSuccess)
                {
                    var lps = new List<PageSettings>();
                    var cnt = 0;
                    areas.ForEach(el =>
                        {
                            var ps = new PageSettings();
                            ps.DcsRegion = el.PlotRegion; //new Extents2d(el.PlotRegion.MinPoint, el.PlotRegion.MaxPoint);
                            ps.WcsRegion = el.WcsRegion;
                            ps.Name = (++cnt).ToString();

                            lps.Add(ps);


                        });

                    var vl = pss.ValidatePaperSize(lps, 1, 0.1);
                    if (vl != PaperValidationResult.AllValidationSuccess)
                    {
                        if (vl == PaperValidationResult.PaperSizeValidationError) vis.OutputUnrecognizedFrames();
                        if (vl == PaperValidationResult.FormatValidationError) vis.OutputUnrecognizedFormats();


                        CurrentDrawing.Editor.WriteMessage("\nValidPaper: {0}", vl);
                        return;
                    }
                    try
                    {
                        pss.Create(lps);
                        plotDeviceService.Publish(lps, false);
                    }
                    finally
                    {
                        pss.Delete(lps);
                    }


                }




                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////                

            }

            catch (Exception ex)
            {
                CurrentDrawing.Editor.WriteMessage("В процессе выполнения команды произошла ошибка...\n");
                CurrentDrawing.Editor.WriteMessage(ex.Message);

                CurrentDrawing.Editor.WriteMessage("\nПопробуйте запустить команду еще раз или свяжитесь с разработчиком");
            }




        }
        [CommandMethod("CreateLayout")]
        public static void CreateLayout()
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            // Get the layout and plot settings of the named pagesetup
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Reference the Layout Manager
                LayoutManager acLayoutMgr = LayoutManager.Current;

                // Create the new layout with default settings
                ObjectId objID = acLayoutMgr.CreateLayout("newLayout");

                // Open the layout
                Layout acLayout = acTrans.GetObject(objID,
                                                    OpenMode.ForRead) as Layout;

                // Set the layout current if it is not already
                if (acLayout.TabSelected == false)
                {
                    acLayoutMgr.CurrentLayout = acLayout.LayoutName;
                }

                // Output some information related to the layout object
                acDoc.Editor.WriteMessage("\nTab Order: " + acLayout.TabOrder +
                                          "\nTab Selected: " + acLayout.TabSelected +
                                          "\nBlock Table Record ID: " +
                                          acLayout.BlockTableRecordId.ToString());

                // Save the changes made
                acTrans.Commit();
            }
        }
        // Lists the available plot styles
        [CommandMethod("PlotStyleList")]
        public static void PlotStyleList()
        {
            // Get the current document and database, and start a transaction
            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            acDoc.Editor.WriteMessage("\nPlot styles: ");

            foreach (string plotStyle in PlotSettingsValidator.Current.GetPlotStyleSheetList())
            {
                // Output the names of the available plot styles
                acDoc.Editor.WriteMessage("\n  " + plotStyle);
            }
        }
        // Lists the available local media names for a specified plot configuration (PC3) file
        [CommandMethod("PlotterLocalMediaNameList")]
        public static void PlotterLocalMediaNameList()
        {
            // Get the current document and database, and start a transaction
            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            using (PlotSettings plSet = new PlotSettings(true))
            {
                PlotSettingsValidator acPlSetVdr = PlotSettingsValidator.Current;

                // Set the Plotter and page size
                acPlSetVdr.SetPlotConfigurationName(plSet, "DWG To PDF.pc3",
                                                    "ANSI_A_(8.50_x_11.00_Inches)");

                acDoc.Editor.WriteMessage("\nCanonical and Local media names: ");

                int cnt = 0;


                foreach (string mediaName in acPlSetVdr.GetCanonicalMediaNameList(plSet))
                {
                    // Output the names of the available media for the specified device
                    acDoc.Editor.WriteMessage("\n  " + mediaName + " | " +
                                              acPlSetVdr.GetLocaleMediaName(plSet, cnt));

                    cnt = cnt + 1;
                }
            }
        }
        [CommandMethod("PlotterList")]
        public static void PlotterList()
        {
            // Get the current document and database, and start a transaction
            Document acDoc = Application.DocumentManager.MdiActiveDocument;

            acDoc.Editor.WriteMessage("\nPlot devices: ");

            foreach (string plotDevice in PlotSettingsValidator.Current.GetPlotDeviceList())
            {
                // Output the names of the available plotter devices
                acDoc.Editor.WriteMessage("\n  " + plotDevice);
            }
        }
        // Lists the available page setups
        [CommandMethod("ListPageSetup")]
        public static void ListPageSetup()
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            // Start a transaction
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                DBDictionary plSettings = acTrans.GetObject(acCurDb.PlotSettingsDictionaryId,
                                                            OpenMode.ForRead) as DBDictionary;

                acDoc.Editor.WriteMessage("\nPage Setups: ");

                // List each named page setup
                foreach (DBDictionaryEntry item in plSettings)
                {
                    acDoc.Editor.WriteMessage("\n  " + item.Key);
                }

                // Abort the changes to the database
                acTrans.Abort();
            }
        }
        // Creates a new page setup or edits the page set if it exists
        [CommandMethod("CreateOrEditPageSetup")]
        public static void CreateOrEditPageSetup()
        {
            // Get the current document and database, and start a transaction
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {

                DBDictionary plSets = acTrans.GetObject(acCurDb.PlotSettingsDictionaryId,
                                                        OpenMode.ForRead) as DBDictionary;


                PlotSettings acPlSet;
                bool createNew = false;

                // Reference the Layout Manager
                LayoutManager acLayoutMgr = LayoutManager.Current;

                // Get the current layout and output its name in the Command Line window
                Layout acLayout = acTrans.GetObject(acLayoutMgr.GetLayoutId(acLayoutMgr.CurrentLayout),
                                                    OpenMode.ForRead) as Layout;

                // Check to see if the page setup exists
                if (plSets.Contains("MyPageSetup") == false)
                {
                    createNew = true;

                    // Create a new PlotSettings object: 
                    //    True - model space, False - named layout
                    acPlSet = new PlotSettings(acLayout.ModelType);
                    acPlSet.CopyFrom(acLayout);

                    acPlSet.PlotSettingsName = "MyPageSetup";
                    acPlSet.AddToPlotSettingsDictionary(acCurDb);
                    acTrans.AddNewlyCreatedDBObject(acPlSet, true);
                }
                else
                {
                    acPlSet = plSets.GetAt("MyPageSetup").GetObject(OpenMode.ForWrite) as PlotSettings;
                }

                // Update the PlotSettings object
                try
                {
                    PlotSettingsValidator acPlSetVdr = PlotSettingsValidator.Current;

                    // Set the Plotter and page size
                    acPlSetVdr.SetPlotConfigurationName(acPlSet, "DWF6 ePlot.pc3", "ANSI_B_(17.00_x_11.00_Inches)");

                    // Set to plot to the current display
                    if (acLayout.ModelType == false)
                    {
                        acPlSetVdr.SetPlotType(acPlSet, Autodesk.AutoCAD.DatabaseServices.PlotType.Layout);
                    }
                    else
                    {
                        acPlSetVdr.SetPlotType(acPlSet, Autodesk.AutoCAD.DatabaseServices.PlotType.Extents);

                        acPlSetVdr.SetPlotCentered(acPlSet, true);
                    }

                    // Use SetPlotWindowArea with PlotType.DcsRegion
                    //acPlSetVdr.SetPlotWindowArea(plSet,
                    //                             new Extents2d(New Point2d(0.0, 0.0),
                    //                             new Point2d(9.0, 12.0)));

                    // Use SetPlotViewName with PlotType.View
                    //acPlSetVdr.SetPlotViewName(plSet, "MyView");

                    // Set the plot offset
                    acPlSetVdr.SetPlotOrigin(acPlSet, new Point2d(0, 0));

                    // Set the plot scale
                    acPlSetVdr.SetUseStandardScale(acPlSet, true);
                    acPlSetVdr.SetStdScaleType(acPlSet, StdScaleType.ScaleToFit);
                    acPlSetVdr.SetPlotPaperUnits(acPlSet, PlotPaperUnit.Inches);
                    acPlSet.ScaleLineweights = true;

                    // Specify if plot styles should be displayed on the layout
                    acPlSet.ShowPlotStyles = true;

                    // Rebuild plotter, plot style, and canonical media lists 
                    // (must be called before setting the plot style)
                    acPlSetVdr.RefreshLists(acPlSet);

                    // Specify the shaded viewport options
                    acPlSet.ShadePlot = PlotSettingsShadePlotType.AsDisplayed;

                    acPlSet.ShadePlotResLevel = ShadePlotResLevel.Normal;

                    // Specify the plot options
                    acPlSet.PrintLineweights = true;
                    acPlSet.PlotTransparency = false;
                    acPlSet.PlotPlotStyles = true;
                    acPlSet.DrawViewportsFirst = true;


                    // Use only on named layouts - Hide paperspace objects option
                    // plSet.PlotHidden = true;

                    // Specify the plot orientation
                    acPlSetVdr.SetPlotRotation(acPlSet, PlotRotation.Degrees000);

                    // Set the plot style
                    if (acCurDb.PlotStyleMode == true)
                    {
                        acPlSetVdr.SetCurrentStyleSheet(acPlSet, "acad.ctb");
                    }
                    else
                    {
                        acPlSetVdr.SetCurrentStyleSheet(acPlSet, "acad.stb");
                    }

                    // Zoom to show the whole paper
                    acPlSetVdr.SetZoomToPaperOnUpdate(acPlSet, true);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception es)
                {

                    CurrentDrawing.Editor.WriteMessage(es.Message);
                }

                // Save the changes made
                acTrans.Commit();

                if (createNew == true)
                {
                    acPlSet.Dispose();
                }
            }
        }
        // Assigns a page setup to a layout
        [CommandMethod("AssignPageSetupToLayout")]
        public static void AssignPageSetupToLayout()
        {
            // Get the current document and database, and start a transaction
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;

            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Reference the Layout Manager
                LayoutManager acLayoutMgr = LayoutManager.Current;

                // Get the current layout and output its name in the Command Line window
                Layout acLayout = acTrans.GetObject(acLayoutMgr.GetLayoutId(acLayoutMgr.CurrentLayout),
                                                    OpenMode.ForRead) as Layout;

                DBDictionary acPlSet = acTrans.GetObject(acCurDb.PlotSettingsDictionaryId,
                                                         OpenMode.ForRead) as DBDictionary;

                // Check to see if the page setup exists
                if (acPlSet.Contains("MyPageSetup") == true)
                {
                    PlotSettings plSet = acPlSet.GetAt("MyPageSetup").GetObject(OpenMode.ForRead) as PlotSettings;

                    // Update the layout
                    acLayout.UpgradeOpen();
                    acLayout.CopyFrom(plSet);

                    // Save the new objects to the database
                    acTrans.Commit();
                }
                else
                {
                    // Ignore the changes made
                    acTrans.Abort();
                }
            }

            // Update the display
            acDoc.Editor.Regen();
        }
        [CommandMethod("PublishViews2MultiSheet")]

        public static void PublishViews2MultiSheet()
        {

            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            StringCollection viewsToPlot = new StringCollection();
            viewsToPlot.Add("Test1");
            viewsToPlot.Add("Test2");

            // Create page setup based on the views
            using (Transaction Tx = db.TransactionManager.StartTransaction())
            {
                ObjectId layoutId = LayoutManager.Current.GetLayoutId(LayoutManager.Current.CurrentLayout);
                Layout layout = Tx.GetObject(layoutId, OpenMode.ForWrite) as Layout;
                foreach (String viewName in viewsToPlot)
                {

                    PlotSettings plotSettings = new PlotSettings(layout.ModelType);
                    plotSettings.CopyFrom(layout);
                    PlotSettingsValidator psv = PlotSettingsValidator.Current;



                    psv.SetPlotConfigurationName(plotSettings, "DWG To PDF.pc3", "ANSI_A_(8.50_x_11.00_Inches)");
                    psv.RefreshLists(plotSettings);
                    psv.SetPlotViewName(plotSettings, viewName);
                    psv.SetPlotType(plotSettings, PlotType.View);

                    psv.SetUseStandardScale(plotSettings, true);
                    psv.SetStdScaleType(plotSettings, StdScaleType.ScaleToFit);

                    psv.SetPlotCentered(plotSettings, true);
                    psv.SetPlotRotation(plotSettings, PlotRotation.Degrees000);

                    psv.SetPlotPaperUnits(plotSettings, PlotPaperUnit.Inches);

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

            short bgPlot = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
            Application.SetSystemVariable("BACKGROUNDPLOT", 0);



            string dwgFileName = Application.GetSystemVariable("DWGNAME") as string;
            string dwgPath = Application.GetSystemVariable("DWGPREFIX") as string;

            using (Transaction Tx = db.TransactionManager.StartTransaction())
            {

                DsdEntryCollection collection = new DsdEntryCollection();

                ObjectId activeLayoutId = LayoutManager.Current.GetLayoutId(LayoutManager.Current.CurrentLayout);



                foreach (String viewName in viewsToPlot)
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
                DsdData dsdData = new DsdData();
                dsdData.SheetType = SheetType.MultiPdf;
                dsdData.ProjectPath = dwgPath;
                dsdData.DestinationName = dsdData.ProjectPath + dwgFileName + ".pdf";

                if (System.IO.File.Exists(dsdData.DestinationName)) System.IO.File.Delete(dsdData.DestinationName);

                dsdData.SetDsdEntryCollection(collection);
                string dsdFile = dsdData.ProjectPath + dwgFileName + ".dsd";

                //Workaround to avoid promp for pdf file name
                //set PromptForDwfName=FALSE in dsdData using StreamReader/StreamWriter

                dsdData.WriteDsd(dsdFile);

                System.IO.StreamReader sr = new System.IO.StreamReader(dsdFile);
                string str = sr.ReadToEnd();
                sr.Close();

                // Replace PromptForDwfName
                str = str.Replace("PromptForDwfName=TRUE", "PromptForDwfName=FALSE");

                // Workaround to have the page setup names included in the DSD file
                // Replace Setup names based on the created page setups
                // May not be required if Nps is output to the DSD

                int occ = 0;
                int index = str.IndexOf("Setup=");
                int startIndex = 0;

                var dsdText = new StringBuilder();
                while (index != -1)
                {

                    var str1 = str.Substring(startIndex, index + 6 - startIndex);
                    dsdText.Append(str1);

                    dsdText.Append(String.Format("{0}{1}", viewsToPlot[occ], "PS"));
                    startIndex = index + 6;
                    index = str.IndexOf("Setup=", index + 6);
                    if (index == -1)
                    {

                        dsdText.Append(str.Substring(startIndex, str.Length - startIndex));

                    }

                    occ++;

                }



                // Write the DSD

                System.IO.StreamWriter sw = new System.IO.StreamWriter(dsdFile);

                sw.Write(dsdText.ToString());

                sw.Close();



                // Read the updated DSD file

                dsdData.ReadDsd(dsdFile);



                // Erase DSD as it is no longer needed

                System.IO.File.Delete(dsdFile);



                PlotConfig plotConfig

                    = PlotConfigManager.SetCurrentConfig("DWG To PDF.pc3");



                Publisher publisher = Application.Publisher;



                // Publish it

                publisher.PublishExecute(dsdData, plotConfig);



                Tx.Commit();

            }



            //reset the background plot value

            Application.SetSystemVariable("BACKGROUNDPLOT", bgPlot);


        }

    }

}
