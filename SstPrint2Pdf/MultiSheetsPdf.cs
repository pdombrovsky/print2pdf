using System.IO;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using Autodesk.AutoCAD.EditorInput;

using System.Text;
using AcAp = Autodesk.AutoCAD.ApplicationServices.Application;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace Plottings
{
    public class MultiSheetsPdf
    {
        private readonly string _dwgFile;
        private readonly string _pdfFile;
        private readonly string _dsdFile;
        private readonly string _outputDir;
        private int _sheetNum;
        private readonly List<PlotSettings> _plotSettingses;

        private const string Log = "publish.log";

        //public MultiSheetsPdf(string dwgFile, string pdfFile, IEnumerable<Layout> plotSettingses)
        public MultiSheetsPdf(string dwgFile, string pdfFile, List<PlotSettings> plotSettingses)
        {

            _dwgFile = dwgFile;
            _pdfFile = pdfFile;
            _outputDir = Path.GetDirectoryName(_pdfFile);
            _dsdFile = Path.ChangeExtension(_pdfFile, "dsd");
            _plotSettingses = plotSettingses;
        }

        public void Publish()
        {
            if (TryCreateDsd())
            {
                var publisher = Application.Publisher;
                var plotDlg = new PlotProgressDialog(false, _sheetNum, true);
                publisher.PublishDsd(_dsdFile, plotDlg);
                plotDlg.Destroy();
                File.Delete(_dsdFile);
            }
        }

        private bool TryCreateDsd()
        {
            using (var dsd = new DsdData())
            using (var dsdEntries = CreateDsdEntryCollection(_plotSettingses))
            {
                if (dsdEntries == null || dsdEntries.Count <= 0) return false;

                if (!Directory.Exists(_outputDir))
                    Directory.CreateDirectory(_outputDir);

                _sheetNum = dsdEntries.Count;

                dsd.SetDsdEntryCollection(dsdEntries);

                dsd.SetUnrecognizedData("PwdProtectPublishedDWF", "FALSE");
                dsd.SetUnrecognizedData("PromptForPwd", "FALSE");
                dsd.SheetType = SheetType.MultiDwf;
                dsd.NoOfCopies = 1;
                dsd.DestinationName = _pdfFile;
                dsd.IsHomogeneous = false;
                dsd.LogFilePath = Path.Combine(_outputDir, Log);

                PostProcessDsd(dsd);

                return true;
            }
        }

        //private DsdEntryCollection CreateDsdEntryCollection(IEnumerable<Layout> plotSettingses)
        private DsdEntryCollection CreateDsdEntryCollection(List<PlotSettings> plotSettingses)
        {
            var entries = new DsdEntryCollection();

            foreach (var layout in plotSettingses)
            {
                var dsdEntry = new DsdEntry();
                dsdEntry.DwgName = _dwgFile;
                dsdEntry.Layout = "Model";// layout.PlotSettingsName;
                dsdEntry.Title = Path.GetFileNameWithoutExtension(_dwgFile) + "-" + layout.PlotSettingsName;
                dsdEntry.Nps = layout.PlotSettingsName;
                entries.Add(dsdEntry);
            }
            return entries;
        }

        private void PostProcessDsd(DsdData dsd)
        {
            string str, newStr;
            var tmpFile = Path.Combine(this._outputDir, "temp.dsd");

            dsd.WriteDsd(tmpFile);
            var cnt = 0;
            using (var reader = new StreamReader(tmpFile, Encoding.Default))
            using (var writer = new StreamWriter(this._dsdFile, false, Encoding.Default))
            {
                while (!reader.EndOfStream)
                {
                    str = reader.ReadLine();
                    if (str.Contains("Setup="))
                    {
                        newStr = "Setup=" + _plotSettingses[cnt++].PlotSettingsName;
                    }
                    else if (str.Contains("Has3DDWF"))
                    {
                        newStr = "Has3DDWF=0";
                    }
                    else if (str.Contains("OriginalSheetPath"))
                    {
                        newStr = "OriginalSheetPath=" + this._dwgFile;
                    }
                    else if (str.Contains("Type"))
                    {
                        newStr = "Type=6";
                    }
                    else if (str.Contains("OUT"))
                    {
                        newStr = "OUT=" + this._outputDir;
                    }
                    else if (str.Contains("IncludeLayer"))
                    {
                        newStr = "IncludeLayer=TRUE";
                    }
                    else if (str.Contains("PromptForDwfName"))
                    {
                        newStr = "PromptForDwfName=FALSE";
                    }
                    else if (str.Contains("LogFilePath"))
                    {
                        newStr = "LogFilePath=" + Path.Combine(this._outputDir, Log);
                    }
                    else
                    {
                        newStr = str;
                    }
                    writer.WriteLine(newStr);
                }
            }
            File.Delete(tmpFile);
        }
    }
}



namespace PublishToPdf
{
    public class Commands : IExtensionApplication
    {
        [CommandMethod("PlotPdf")]
        public void PlotPdf()
        {
            Database db = HostApplicationServices.WorkingDatabase;
            short bgp = (short)AcAp.GetSystemVariable("BACKGROUNDPLOT");
            try
            {
                AcAp.SetSystemVariable("BACKGROUNDPLOT", 0);
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    List<Layout> layouts = new List<Layout>();
                    DBDictionary layoutDict =
                        (DBDictionary)db.LayoutDictionaryId.GetObject(OpenMode.ForRead);
                    foreach (DBDictionaryEntry entry in layoutDict)
                    {
                        layouts.Add((Layout)tr.GetObject(entry.Value, OpenMode.ForRead));
                    }
                    layouts.Sort((l1, l2) => l1.TabOrder.CompareTo(l2.TabOrder));

                    string filename = Path.ChangeExtension(db.Filename, "pdf");

                    //Plottings.MultiSheetsPdf plotter = new Plottings.MultiSheetsPdf(filename, layouts);
                    //plotter.Publish();

                    tr.Commit();
                }
            }
            catch (System.Exception e)
            {
                Editor ed = AcAp.DocumentManager.MdiActiveDocument.Editor;
                ed.WriteMessage("\nError: {0}\n{1}", e.Message, e.StackTrace);
            }
            finally
            {
                AcAp.SetSystemVariable("BACKGROUNDPLOT", bgp);
            }
        }

        public void Initialize() { }
        public void Terminate() { }
    }
}