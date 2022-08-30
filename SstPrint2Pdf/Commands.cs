using System.Globalization;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using SstPrint2Pdf.AcDrawing;
using SstPrint2Pdf.ErrorVisualizator;
using SstPrint2Pdf.Extensions;
using SstPrint2Pdf.Factories;
using SstPrint2Pdf.PlotSettingsConfiguration;
using SstPrint2Pdf.PrinterConfiguration;
using Exception = System.Exception;

[assembly: CommandClass(typeof(SstPrint2Pdf.Commands))]
namespace SstPrint2Pdf
{
   
    public class Commands
    {
        private readonly IDrawing _drawing;
        private readonly IPlotDeviceService _plotDeviceService;
        private readonly IPageSettingsService _pss;
        private readonly FormatErrorVisualizator _fev;
        public Commands()
        {

            _drawing = new Drawing();
            _plotDeviceService = new PlotDeviceService(_drawing);
            _fev = new FormatErrorVisualizator(_drawing, 1, LineWeight.LineWeight080);
            _pss=new PageSettingsService(_drawing.Db, _fev);


        }
       [CommandMethod("PrintFrames")]
       public void PrintFrames()
        {
            try
            {


                var resDevice = _plotDeviceService.ValidateDevice();
                if (resDevice == DevValidationResult.CurrentNotExist)
                {
                    Application.ShowAlertDialog("Не задана конфигурация печати." +
                                                "\nДля вкладки 'Модель' откройте Диспетчер параметров листов," +
                                                "\nвыберите набор параметров *Модель* и установите устройство вывода." +
                                                "\nПосле этого запустите команду еще раз.");
                    return;
                }
                if (resDevice == DevValidationResult.DeviceChanged)
                {
                    Application.ShowAlertDialog("Текущее устройство вывода может отличаться от заданного ранее." +
                                                "\nДля вкладки 'Модель' откройте Диспетчер параметров листов," +
                                                "\nвыберите набор параметров *Модель* и проверьте(установите) устройство вывода." +
                                                "\nПосле этого запустите команду еще раз.");
                    return;
                }
                if (resDevice == DevValidationResult.NoneDevice)
                {
                    Application.ShowAlertDialog("Не задано устройство вывода. " +
                                                "\nДля вкладки 'Модель' откройте Диспетчер параметров листов," +
                                                "\nвыберите набор параметров *Модель* и установите устройство вывода." +
                                                "\nПосле этого запустите команду еще раз.");
                    return;
                }
                var device = _plotDeviceService.Device;
                _drawing.Ed.WriteMessage("\nТекущее устройство вывода: {0}", device.DevName);
                _drawing.Ed.WriteMessage("\nПечать в файл: {0}", device.PlotToFile);
                _drawing.Ed.WriteMessage("\nРасширение: {0}", device.FileExt);


                var kw = !device.PlotToFile
                         ? "Да"
                         : _drawing.Ed.PromptForKeywordSelection("Печать в один файл?", new[] { "Да", "Нет" }, false, "Да").StringResult;
                var issingle = (kw == "Да");
                var prjnumber = string.Empty;
                var pref = string.Empty;
                var firstn = 1;
                
                if (!issingle)
                {
                    prjnumber = _drawing.Ed.GetStringValue("Укажите номер проекта: ");

                    if (string.IsNullOrEmpty(prjnumber))
                    {
                        _drawing.Ed.WriteMessage("\nНекорректный ввод.");
                        _drawing.Ed.WriteMessage("\nВыход из процедуры.");
                        return;

                    }
                    prjnumber = prjnumber.Replace("\\", "_");
                    prjnumber = prjnumber.Replace("/", "_");

                    var kw1 = _drawing.Ed.PromptForKeywordSelection("Указать постоянную часть (префикс) номера листа? (например: 2. или 1.):", new[] { "Да", "Нет" }, false, "Да").StringResult;
                    if (kw1 == "Да")
                    {
                        pref = _drawing.Ed.GetStringValue("Укажите постоянную часть (префикс) номера листа: ");

                        if (string.IsNullOrEmpty(pref))
                        {
                            _drawing.Ed.WriteMessage("\nНекорректный ввод.");
                            _drawing.Ed.WriteMessage("\nВыход из процедуры.");
                            return;

                        }
                    }
                    firstn = _drawing.Ed.GetNaturalValue("Введите номер первого листа:",1,false);

                    if (firstn==-1)
                    {
                        _drawing.Ed.WriteMessage("\nНекорректный ввод.");
                        _drawing.Ed.WriteMessage("\nВыход из процедуры.");
                        return;

                    }

                }
                

                var fc = Factories.Factories.GetFactory(_drawing, FactoryType.FramesOnLayerFactory);
                var ps = fc.GetPages();

                if ( ps.Count == 0)
                {
                    _drawing.Ed.WriteMessage("\nНекорректный выбор.");
                    _drawing.Ed.WriteMessage("\nВыход из процедуры.");
                    return;

                }

              
                ps.ForEach(el => el.Name = prjnumber + "_л." +pref+ (firstn++).ToString(CultureInfo.InvariantCulture));


                var vl = _pss.ValidatePaperSize(ps, 1, 0.1);
                if (vl != PaperValidationResult.AllValidationSuccess)
                {
                    if (vl == PaperValidationResult.PaperSizeValidationError) _fev.OutputUnrecognizedFrames();
                    if (vl == PaperValidationResult.FormatValidationError) _fev.OutputUnrecognizedFormats();


                    
                    return;
                }
                
               
                try
                {
                    _pss.Create(ps);
                    _plotDeviceService.Publish(ps,issingle);
                }
                finally
                {
                    _pss.Delete(ps);
                }

            }

            catch (Exception ex)
            {

                CurrentDrawing.Editor.WriteMessage("В процессе выполнения команды произошла ошибка...\n");
                CurrentDrawing.Editor.WriteMessage(ex.Message);

                CurrentDrawing.Editor.WriteMessage("\nПопробуйте запустить команду еще раз или свяжитесь с разработчиком");
            }


        }
       [CommandMethod("PrintSstFrames")]
       public void PrintSstFrames()
       {
           try
           {


               var resDevice = _plotDeviceService.ValidateDevice();
               if (resDevice == DevValidationResult.CurrentNotExist)
               {
                   Application.ShowAlertDialog("\nНе задана конфигурация печати." +
                                               "\nДля вкладки 'Модель' откройте Диспетчер параметров листов," +
                                               "\nвыберите набор параметров *Модель* и установите устройство вывода." +
                                               "\nПосле этого запустите команду еще раз.");
                   return;
               }
               if (resDevice == DevValidationResult.DeviceChanged)
               {
                   Application.ShowAlertDialog("Текущее устройство вывода может отличаться от заданного ранее." +
                                               "\nДля вкладки 'Модель' откройте Диспетчер параметров листов," +
                                               "\nвыберите набор параметров *Модель* и проверьте(установите) устройство вывода." +
                                               "\nПосле этого запустите команду еще раз.");
                   return;
               }
               if (resDevice == DevValidationResult.NoneDevice)
               {
                   Application.ShowAlertDialog("Не задано устройство вывода. " +
                                               "\nДля вкладки 'Модель' откройте Диспетчер параметров листов," +
                                               "\nвыберите набор параметров *Модель* и установите устройство вывода." +
                                               "\nПосле этого запустите команду еще раз.");
                   return;
               }
               var device = _plotDeviceService.Device;
               _drawing.Ed.WriteMessage("\nТекущее устройство вывода: {0}", device.DevName);
               _drawing.Ed.WriteMessage("\nПечать в файл: {0}", device.PlotToFile);
               _drawing.Ed.WriteMessage("\nРасширение: {0}", device.FileExt);


               var kw = !device.PlotToFile
                        ? "Да"
                        : _drawing.Ed.PromptForKeywordSelection("Печать в один файл?", new[] { "Да", "Нет" }, false, "Да").StringResult;
               var issingle = (kw == "Да");
              
               var fc = Factories.Factories.GetFactory(_drawing, FactoryType.BlocksSstFactory);
               var ps = fc.GetPages();



               if (ps.Count == 0)
               {
                   _drawing.Ed.WriteMessage("\nНекорректный выбор.");
                   _drawing.Ed.WriteMessage("\nВыход из процедуры.");
                   return;

               }

              


               var vl = _pss.ValidatePaperSize(ps, 1, 0.1);
               if (vl != PaperValidationResult.AllValidationSuccess)
               {
                   if (vl == PaperValidationResult.PaperSizeValidationError) _fev.OutputUnrecognizedFrames();
                   if (vl == PaperValidationResult.FormatValidationError) _fev.OutputUnrecognizedFormats();



                   return;
               }


               try
               {
                   _pss.Create(ps);
                   _plotDeviceService.Publish(ps, issingle);
               }
               finally
               {
                   _pss.Delete(ps);
               }

           }

           catch (Exception ex)
           {

               CurrentDrawing.Editor.WriteMessage("В процессе выполнения команды произошла ошибка...\n");
               CurrentDrawing.Editor.WriteMessage(ex.Message);

               CurrentDrawing.Editor.WriteMessage("\nПопробуйте запустить команду еще раз или свяжитесь с разработчиком");
           }


       }
    }
}
