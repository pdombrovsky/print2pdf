using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace SstPrint2Pdf
{
    internal static class Filters
    {

        /// <summary>
        /// Фильтр для выделения вхождений всех блоков
        /// </summary>
        /// <returns>Возвращает объект SelectionFilter</returns>
        internal static SelectionFilter ForAllBlocks()
        {
            var tvs = new TypedValue[] { new TypedValue((int)DxfCode.Start, "INSERT") };
            return new SelectionFilter(tvs);
        }
        /// <summary>
        /// Фильтр для выделения вхождений всех полилиний на слое
        /// </summary>
        /// <returns>Возвращает объект SelectionFilter</returns>
        internal static SelectionFilter ForAllPlines(string layer)
        {
            var tvs = new TypedValue[2];
            tvs.SetValue(new TypedValue((int)DxfCode.LayerName, layer), 0);
            tvs.SetValue( new TypedValue((int)DxfCode.Start, "LWPOLYLINE"), 1 );
            return new SelectionFilter(tvs);
        }
       
    }
}
