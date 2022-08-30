using System.IO;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using SstPrint2Pdf.PrinterConfiguration;

namespace SstPrint2Pdf.PlotSettingsConfiguration
{
    public class PageSettings
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public string PaperSize { get; set; }
        public Extents2d  DcsRegion { get; set; }
        public Extents3d WcsRegion { get; set; }
        public FormatDimensions FormatSize { get; set; }
        public string Format { get; set; }
        
    }
}    
        

