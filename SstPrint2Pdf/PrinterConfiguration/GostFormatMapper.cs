using System;
using System.Collections.Generic;
using System.Linq;

namespace SstPrint2Pdf.PrinterConfiguration
{
    
    public static class GostFormatMapper
    {
        public static Dictionary<string, FormatDimensions> Map { get; private set; } 
        static GostFormatMapper()
        {
          
            Map = new Dictionary<string, FormatDimensions>
                {
                    {"A4+", new FormatDimensions(297, 210)},

                    {"A4x3", new FormatDimensions(297, 630)},
                    {"A4x4", new FormatDimensions(297, 841)},
                    {"A4x5", new FormatDimensions(297, 1051)},
                    {"A4x6", new FormatDimensions(297, 1261)},
                    {"A4x7", new FormatDimensions(297, 1471)},
                    {"A4x8", new FormatDimensions(297, 1682)},
                    {"A4x9", new FormatDimensions(297, 1982)},

                    {"A3", new FormatDimensions(297, 420)},
                    {"A3+", new FormatDimensions(420, 297)},

                    {"A3x3", new FormatDimensions(420, 891)},
                    {"A3x4", new FormatDimensions(420, 1189)},
                    {"A3x5", new FormatDimensions(420, 1486)},
                    {"A3x6", new FormatDimensions(420, 1783)},
                    {"A3x7", new FormatDimensions(420, 2080)},

                    {"A2", new FormatDimensions(420, 594)},
                    {"A2+", new FormatDimensions(594, 420)},

                    {"A2x3", new FormatDimensions(594, 1261)},
                    {"A2x4", new FormatDimensions(594, 1682)},
                    {"A2x5", new FormatDimensions(594, 2102)},

                    {"A1", new FormatDimensions(594, 841)},
                    {"A1+", new FormatDimensions(841, 594)},

                    {"A1x3", new FormatDimensions(841, 1783)},
                    {"A1x4", new FormatDimensions(841, 2378)},

                    {"A0", new FormatDimensions(841, 1189)},
                    {"A0+", new FormatDimensions(1189, 841)},

                    {"A0x2", new FormatDimensions(1189, 1682)},
                    {"A0x3", new FormatDimensions(1189, 2523)}
                };
        }
        public static string GetFormat(int height, int width, int tolerance=0)
        {

            var dimensions = new FormatDimensions(height, width);
            return GetFormat(dimensions,tolerance);

        }
        public static string GetFormat(FormatDimensions format, int tolerance=0)
        {

           
            return Map.FirstOrDefault(
                el =>
                (Math.Abs(el.Value.Height - format.Height)<=tolerance) && Math.Abs(el.Value.Width - format.Width)<=tolerance).Key;

        }
       
        //public static string GetPaperSize(string format)
        //{

        //    return string.Format("ISO_{0}_({1}.00_x_{2}.00_MM)", format.Replace("+", ""), Map[format].Width, Map[format].Height);

        //}
        //public static string GetPaperSize(int height, int width)
        //{

        //    var ft = GetFormat(height, width);
        //    return string.Format("ISO_{0}_({1}.00_x_{2}.00_MM)", ft, width, height);

        //}
    }
}
