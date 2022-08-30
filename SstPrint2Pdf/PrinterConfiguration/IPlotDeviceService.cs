using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using SstPrint2Pdf.PlotSettingsConfiguration;

namespace SstPrint2Pdf.PrinterConfiguration
{
    public interface IPlotDeviceService
    {
        DevValidationResult ValidateDevice();
        void Publish(List<PageSettings> pageSettingses, bool isSingleDoc);
        DeviceInfo Device { get;  }
        
    }
}