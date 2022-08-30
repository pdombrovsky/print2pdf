using System;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace SstPrint2Pdf.CoordTransfom
{
    public static class ExtentsTransformation
    {
        //2012
        //[DllImport("acad.exe", CallingConvention = CallingConvention.Cdecl, EntryPoint = "acedTrans")]

        //2013 
        [DllImport("accore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "acedTrans")]
        private static extern int acedTrans(double[] point, IntPtr fromRb, IntPtr toRb, int disp, double[] result);
        public static Extents2d GetExtentsDcs(Extents3d ucsregion)
        {

            var first = ucsregion.MinPoint;
            var second = ucsregion.MaxPoint;
            ResultBuffer rbFrom = new ResultBuffer(new TypedValue(5003, 1)),
                         rbTo = new ResultBuffer(new TypedValue(5003, 2));
            var firres = new double[] { 0, 0, 0 };
            var secres = new double[] { 0, 0, 0 };
            acedTrans(first.ToArray(), rbFrom.UnmanagedObject, rbTo.UnmanagedObject, 0, firres);
            acedTrans(second.ToArray(), rbFrom.UnmanagedObject, rbTo.UnmanagedObject, 0, secres);
            return new Extents2d(firres[0], firres[1], secres[0], secres[1]);
        }
    
    }
}
