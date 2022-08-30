using System.Collections.Generic;
using System.IO;
using System.Text;
using Autodesk.AutoCAD.PlottingServices;
using SstPrint2Pdf.AcDrawing;
using SstPrint2Pdf.PlotSettingsConfiguration;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SstPrint2Pdf.PrinterConfiguration
{
    public class DsdPublisher
    {

        private readonly IDrawing _drawing;
        private readonly string _dwgFile;
        private readonly string _dsdFile;
        private readonly string _outputDir;




        private const string Log = "publish.log";


        public DsdPublisher(IDrawing drawing)
        {

            _drawing = drawing;
            _dwgFile = _drawing.Doc.Name;
            _outputDir = Path.GetDirectoryName(_dwgFile);
            _dsdFile = Path.ChangeExtension(_dwgFile, "dsd");

        }

        public void Publish(DeviceInfo deviceInfo, List<PageSettings> pageSettingses, bool isSingleDoc)
        {
            if (!TryCreateDsd(deviceInfo, pageSettingses, isSingleDoc)) return;
            var cnt = pageSettingses.Count;
            var publisher = Application.Publisher;
            var plotDlg = new PlotProgressDialog(false, cnt, true);


            publisher.PublishDsd(_dsdFile, plotDlg);
            plotDlg.Destroy();
            File.Delete(_dsdFile);
        }

        private bool TryCreateDsd(DeviceInfo deviceInfo, List<PageSettings> pageSettingses, bool isSingleDoc)
        {
            using (var dsd = new DsdData())
            {
                using (var dsdEntries = CreateDsdEntryCollection(pageSettingses))
                {
                    if (dsdEntries == null || dsdEntries.Count <= 0) return false;


                    dsd.SetDsdEntryCollection(dsdEntries);
                }

                if (!Directory.Exists(_outputDir))
                    Directory.CreateDirectory(_outputDir);

                CofigureDsd(dsd, deviceInfo, isSingleDoc);
                PostProcessDsd(dsd, pageSettingses);

                return true;
            }
        }

        private void CofigureDsd(DsdData dsd, DeviceInfo deviceInfo, bool isSingleDoc)
        {
            dsd.SetUnrecognizedData("PwdProtectPublishedDWF", "FALSE");
            dsd.SetUnrecognizedData("PromptForPwd", "FALSE");


            dsd.PromptForDwfName = false;
           
            dsd.SheetType = GetSheetType(deviceInfo, isSingleDoc);
            dsd.NoOfCopies = 1;
            dsd.DestinationName = Path.Combine(_outputDir, Path.GetFileNameWithoutExtension(_dwgFile) + deviceInfo.FileExt);
            dsd.IsHomogeneous = false;
            dsd.LogFilePath = Path.Combine(_outputDir, Log);


        }
        private static SheetType GetSheetType(DeviceInfo deviceInfo, bool isSingleDoc)
        {
            if(!deviceInfo.PlotToFile) return SheetType.OriginalDevice;
            var ext = deviceInfo.FileExt.ToUpper(); 
            if (isSingleDoc)
            {
                if (ext == ".DWFX") return SheetType.MultiDwfx;
                if (ext == ".DWF") return SheetType.MultiDwf;
                if (ext == ".PDF") return SheetType.MultiPdf;
                return SheetType.OriginalDevice;
            }
               
                if (ext == ".DWFX") return SheetType.SingleDwfx;
                if (ext == ".DWF") return SheetType.SingleDwf;
                if (ext == ".PDF") return SheetType.SinglePdf;
                return SheetType.OriginalDevice;

        }
        private DsdEntryCollection CreateDsdEntryCollection(IEnumerable<PageSettings> pageSettingses)
        {
            var entries = new DsdEntryCollection();

            foreach (var ps in pageSettingses)
            {

                var dsdEntry = new DsdEntry();
                dsdEntry.DwgName = _dwgFile;
                dsdEntry.Layout = "Model";
                dsdEntry.Title = ps.Name;
                dsdEntry.Nps = ps.Name;
                dsdEntry.NpsSourceDwg = _dwgFile;
               
                entries.Add(dsdEntry);



            }
            return entries;
        }

        private void PostProcessDsd(DsdData dsd, List<PageSettings> pageSettingses)
        {
            string str, newStr;
            var tmpFile = Path.Combine(_outputDir, "temp.dsd");

            dsd.WriteDsd(tmpFile);
            var cnt = 0;
            using (var reader = new StreamReader(tmpFile, Encoding.Default))
            using (var writer = new StreamWriter(_dsdFile, false, Encoding.Default))
            {
                while (!reader.EndOfStream)
                {
                    str = reader.ReadLine();
                    if (str.Contains("Setup="))
                    {
                        newStr = "Setup=" + pageSettingses[cnt++].Name;
                    }
                    else 
                    //if (str.Contains("Has3DDWF"))
                    //{
                    //    newStr = "Has3DDWF=0";
                    //}
                    //else 
                    if (str.Contains("OriginalSheetPath"))
                    {
                        newStr = "OriginalSheetPath=" + _dwgFile;
                    }
                    else 
                   
                    if (str.Contains("OUT"))
                    {
                        newStr = "OUT=" + _outputDir;
                    }
                    else 
                    if (str.Contains("IncludeLayer"))
                    {
                        newStr = "IncludeLayer=TRUE";
                    }
                    
                    else if (str.Contains("LogFilePath"))
                    {
                        newStr = "LogFilePath=" + Path.Combine(_outputDir, Log);
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
