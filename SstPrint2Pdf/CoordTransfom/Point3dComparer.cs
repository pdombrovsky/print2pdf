using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Autodesk.AutoCAD.Geometry;

namespace SstPrint2Pdf.CoordTransfom
{
    public class Point3DComparer:IComparer<Point3d>
    {

        public int Compare(Point3d first, Point3d second)
        {
            return (Math.Abs(first.X - second.X) < Tolerance.Global.EqualPoint)
                       ? first.Y.CompareTo(second.Y)
                       : first.X.CompareTo(second.X); 
               
        }
    
    }
  
}
