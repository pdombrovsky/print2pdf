using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SstPrint2Pdf.PlotSettingsConfiguration;

namespace SstPrint2Pdf.Factories
{
   public interface IPageSettingsFactory
    {
       List<PageSettings> GetPages();

    }
}
