using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;

namespace SstPrint2Pdf.Extensions
{
    public static class DbOjectExtensions
    {
        /// <summary>
        /// Upgrades the open mode of the specified object to ForWrite.
        /// </summary>
        /// <typeparam name="T">The type of DBObject.</typeparam>
        /// <param name="obj">The DBObject instance.</param>
        /// <returns>The original instance.</returns>
        public static T ForWrite<T>(this T obj) where T : DBObject
        {
            if (!obj.IsWriteEnabled)
                obj.UpgradeOpen();
            return obj;
        }
        /// <summary>
        /// Downgrades the open mode of the specified object to ForRead.
        /// </summary>
        /// <typeparam name="T">The type of DBObject.</typeparam>
        /// <param name="obj">The DBObject instance.</param>
        /// <returns>The original instance.</returns>
        public static T ForRead<T>(this T obj) where T : DBObject
        {
            if (obj.IsWriteEnabled)
                obj.DowngradeOpen();
            return obj;
        }
    
    }
}
