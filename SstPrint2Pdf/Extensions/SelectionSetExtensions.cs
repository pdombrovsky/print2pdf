using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace SstPrint2Pdf.Extensions
{
    public static class SelectionSetExtensions
    {
        /// <summary>
        /// Returns an <c>IEnumerable&lt;T&gt;</c> based on the specified selection set,
        /// and using the specified transaction and open mode.
        /// </summary>
        /// <typeparam name="T">A type that derives from <c>DBObject</c>.</typeparam>
        /// <param name="selectionSet">The selection set.</param>
        /// <param name="tr">The current transaction.</param>
        /// <param name="openMode">The open mode.</param>
        /// <returns>an <c>IEnumerable&lt;T&gt;</c>, where <c>T</c> is a type that derives from <c>DBObject</c>.</returns>
        public static IEnumerable<T> OfType<T>(this SelectionSet selectionSet,
            Transaction tr,
            OpenMode openMode)
            where T : DBObject
        {
            return selectionSet.Cast<ObjectId>().OfType<T>(tr, openMode);
        }

        /// <summary>
        /// Returns an <c>IEnumerable&lt;T&gt;</c> based on the specified selection set,
        /// and using the specified transaction.
        /// </summary>
        /// <typeparam name="T">A type that derives from <c>DBObject</c>.</typeparam>
        /// <param name="selectionSet">The selection set.</param>
        /// <param name="tr">The current transaction.</param>
        /// <returns>an <c>IEnumerable&lt;T&gt;</c>, where <c>T</c> is a type that derives from <c>DBObject</c>.</returns>
        public static IEnumerable<T> OfType<T>(this SelectionSet selectionSet,
            Transaction tr)
            where T : DBObject
        {
            return selectionSet.Cast<ObjectId>().OfType<T>(tr, OpenMode.ForRead);
        }
    
    
    }
}
