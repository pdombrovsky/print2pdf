using System.Collections.Generic;

namespace SstPrint2Pdf.PlotSettingsConfiguration
{
    public interface IPageSettingsService
    {
        bool Exist(PageSettings pageSettings);
        PaperValidationResult ValidatePaperSize(List<PageSettings> pageSettingses, int tolerance, double mrgnstolerance);
        void Update(List<PageSettings> listps);
        void Create(List<PageSettings> listps);
        void Delete(List<PageSettings> listpageSettings);
       // void CreateLayouts(List<PageSettings> listps);
    }
}