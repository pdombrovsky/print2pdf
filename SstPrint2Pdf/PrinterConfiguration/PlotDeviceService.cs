using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.PlottingServices;
using SstPrint2Pdf.AcDrawing;
using SstPrint2Pdf.Extensions;
using SstPrint2Pdf.PlotSettingsConfiguration;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace SstPrint2Pdf.PrinterConfiguration
{
    public class PlotDeviceService:IPlotDeviceService
    {
        private readonly Database _database;
        private readonly IDrawing _drawing;
        public  DeviceInfo Device { get; private set; }
       
        
        public PlotDeviceService(IDrawing  drawing)
        {
            _database = drawing.Db;
            _drawing = drawing;
            Device = new DeviceInfo();
        }


        public DevValidationResult ValidateDevice()
        {
            var res = DevValidationResult.CurrentNotExist;
            _database.UsingTransaction(tr =>res= ValidateDevice(tr, Device));

            return res;


        }
       

        public void Publish( List<PageSettings> pageSettingses, bool isSingleDoc)
        {
            var bgp = (short)Application.GetSystemVariable("BACKGROUNDPLOT");
            try
            {

                Application.SetSystemVariable("BACKGROUNDPLOT", 0);
                var plotter = new DsdPublisher(_drawing);
                plotter.Publish(Device, pageSettingses, isSingleDoc);
            }
            finally
            {
                Application.SetSystemVariable("BACKGROUNDPLOT", bgp);
            }
            



        }

        #region private_methods
        private DevValidationResult ValidateDevice(Transaction tr, DeviceInfo deviceInfo)
        {

            PlotConfig plotcfg;
            try
            {
                plotcfg = PlotConfigManager.CurrentConfig;

            }
            catch
            {

                return DevValidationResult.CurrentNotExist;
            }

            using (var blockTable = _database.BlockTableId.OpenAs<BlockTable>(tr))
            {
                using (var btableRecord = blockTable[BlockTableRecord.ModelSpace].OpenAs<BlockTableRecord>(tr))
                {
                    using (var layout = btableRecord.LayoutId.OpenAs<PlotSettings>(tr))
                    {
                        var noneDev = PlotConfigManager.Devices;
                        if (layout.PlotConfigurationName == noneDev[0].DeviceName) return DevValidationResult.NoneDevice;
                        if (layout.PlotConfigurationName != plotcfg.DeviceName) return DevValidationResult.DeviceChanged;

                        deviceInfo.DevName = plotcfg.DeviceName;
                        deviceInfo.FileExt = plotcfg.DefaultFileExtension;
                        deviceInfo.PlotToFile = plotcfg.IsPlotToFile;
                        return DevValidationResult.ValidationSuccess;
                    }


                }

            }



        }

        #endregion
        
    }
}