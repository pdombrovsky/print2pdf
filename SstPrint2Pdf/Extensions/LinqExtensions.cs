using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

namespace SstPrint2Pdf.Extensions
{
    /// <summary>
    /// This class contains additional LINQ extension methods that allow you to more easily work with
    /// AutoCAD objects and LINQ.
    /// </summary>
    public static class LinqExtensions
    {
        /// <summary>
        /// Extension method that allows you to iterate through the objects in a block table
        /// record and perform an action on a specific type of object.
        /// </summary>
        /// <typeparam name="T">The type of object to search for.</typeparam>
        /// <param name="btr">The block table record to iterate.</param>
        /// <param name="tr">The active transaction.</param>
        /// <param name="action">A delegate that is called for each object found of the specified type.</param>
        public static void ForEach<T>(this IEnumerable<ObjectId> btr, Transaction tr, Action<T> action)
            where T : DBObject
        {
            var theClass = RXObject.GetClass(typeof(T));

            // Loop through the entities in model space
            foreach (var objectId in btr.Where(objectId => objectId.ObjectClass.IsDerivedFrom(theClass)))
            {
                action(objectId.OpenAs<T>(tr));
            }
        }

        /// <summary>
        /// Returns an <c>IEnumerable&lt;T&gt;</c> based on the specified enumerable,
        /// and using the specified transaction and open mode.
        /// </summary>
        /// <typeparam name="T">A type that derives from <c>DBObject</c>.</typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="tr">The current transaction.</param>
        /// <param name="openMode">The open mode.</param>
        /// <returns>an <c>IEnumerable&lt;T&gt;</c>, where <c>T</c> is a type that derives from <c>DBObject</c>.</returns>
        public static IEnumerable<T> OfType<T>(this IEnumerable<ObjectId> enumerable,
            Transaction tr,
            OpenMode openMode)
            where T : DBObject
        {
            var rxClass = RXObject.GetClass(typeof(T));

            foreach (var objectId in enumerable)
            {
                if (objectId.ObjectClass.IsDerivedFrom(rxClass)) yield return objectId.OpenAs<T>(tr, openMode);
            }
        }

        /// <summary>
        /// Returns an <c>IEnumerable&lt;T&gt;</c> based on the specified enumerable,
        /// and using the specified transaction.
        /// </summary>
        /// <typeparam name="T">A type that derives from <c>DBObject</c>.</typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="tr">The current transaction.</param>
        /// <returns>an <c>IEnumerable&lt;T&gt;</c>, where <c>T</c> is a type that derives from <c>DBObject</c>.</returns>
        public static IEnumerable<T> OfType<T>(this IEnumerable<ObjectId> enumerable,
            Transaction tr)
            where T : DBObject
        {
            return enumerable.OfType<T>(tr, OpenMode.ForRead);
        }

      

        /// <summary>
        /// Upgrades the open mode of each object in the <b>IEnumerable</b>.
        /// </summary>
        /// <typeparam name="T">The type of object.</typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <returns>A new enumerable with the same set of objects.</returns>
        public static IEnumerable<T> UpgradeOpen<T>(this IEnumerable<T> enumerable) where T : DBObject
        {
            return enumerable.Select(obj => obj.ForWrite());
        }
        /// <summary>
        /// Downgrades the open mode of each object in the <b>IEnumerable</b>.
        /// </summary>
        /// <typeparam name="T">The type of object.</typeparam>
        /// <param name="enumerable">The enumerable.</param>
        /// <returns>A new enumerable with the same set of objects.</returns>
        public static IEnumerable<T> DowngradeOpen<T>(this IEnumerable<T> enumerable) where T : DBObject
        {
            return enumerable.Select(obj => obj.ForRead());
        }
    }
}
