using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;

namespace SstPrint2Pdf.PrinterConfiguration
{
    public class FormatDimensions
    {
        public FormatDimensions(int height = 0, int width = 0)
        {
            Height = height;
            Width = width;
          
        }
        public FormatDimensions(FormatDimensions formatdms)
            : this(formatdms.Height, formatdms.Width)
        {
            


        }

       
        public int Width { get; private set; }
        public int Height { get; private set; }

        public FormatDimensions(Extents2d region, CustomScale scale)
        {
            var sc = scale.Numerator/scale.Denominator;
            if (sc >= 1)
            {
                Width =(int) Math.Round(region.MaxPoint.X * sc - region.MinPoint.X * sc);
                Height = (int)Math.Round(region.MaxPoint.Y * sc - region.MinPoint.Y * sc);
            }
            else
            {
                Width = (int)Math.Round((region.MaxPoint.X - region.MinPoint.X) * sc);
                Height = (int)Math.Round((region.MaxPoint.Y - region.MinPoint.Y) * sc);
            }

            
            

        }


    }
}
