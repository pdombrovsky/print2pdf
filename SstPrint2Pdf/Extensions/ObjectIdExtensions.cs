using System;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;

namespace SstPrint2Pdf.Extensions
{
    /// <summary>
    /// Contains extension methods that facilitate working with objects in the context of a transaction.
    /// </summary>
    public static class ObjectIdExtensions
    {
       
        /// <summary>
        /// Opens a database-resident object as the specified type within the context of the specified transaction,
        /// using the specified open mode.
        /// </summary>
        /// <typeparam name="T">The type of object that the objectId represents.</typeparam>
        /// <param name="objectId">The object id.</param>
        /// <param name="tr">The transaction.</param>
        /// <param name="openMode">The open mode.</param>
        /// <returns>The database-resident object.</returns>
        public static T OpenAs<T>(this ObjectId objectId, Transaction tr, OpenMode openMode = OpenMode.ForRead)
            where T : DBObject
        {
            return (T)tr.GetObject(objectId, openMode);
        }

        /// <summary>
        /// Opens a database-resident object as the specified type within the context of the specified transaction,
        /// using the specified open mode.
        /// </summary>
        /// <typeparam name="T">The type of object that the objectId represents.</typeparam>
        /// <param name="objectId">The object id.</param>
        /// <returns>The database-resident object.</returns>
        public static T OpenAs<T>(this ObjectId objectId)
            where T : DBObject
        {
            return (T)objectId.GetObject(OpenMode.ForRead);
           
        }
       
        /// <summary>
        /// Iterates through the specified symbol table, and performs an action on each symbol table record.
        /// </summary>
        /// <typeparam name="T">The type of symbol table record.</typeparam>
        /// <param name="tableId">The table id.</param>
        /// <param name="action">A delegate that is called for each record.</param>
        public static void ForEach<T>(this ObjectId tableId, Action<T> action) where T : SymbolTableRecord
        {
            tableId.Database.UsingTransaction(tr => tableId.OpenAs<SymbolTable>(tr).Cast<ObjectId>().ForEach(tr, action));
        }

        /// <summary>
        /// Used to get a single value from a database-resident object.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="objectId">The object id.</param>
        /// <param name="func">A delegate that takes the object as an argument and returns the value.</param>
        /// <returns>A value of the specified type.</returns>
        public static TResult GetValue<TObject, TResult>(this ObjectId objectId, Func<TObject, TResult> func)
            where TObject : DBObject
        {
            var result = default(TResult);

            objectId.Database.UsingTransaction(
                tr => { result = func(objectId.OpenAs<TObject>(tr)); });

            return result;
        }

        /// <summary>
        /// Opens the database object with the specified object ID and open mode, and calls a delegate
        /// with the opened object.
        /// </summary>
        /// <typeparam name="T">The type of <b>DBObject</b> being opened.</typeparam>
        /// <param name="objectId">The object id.</param>
        /// <param name="openMode">The open mode.</param>
        /// <param name="action">A delegate that will receive the opened object.</param>
        public static void OpenAs<T>(this ObjectId objectId, OpenMode openMode, Action<T> action)
            where T : DBObject
        {
            objectId.Database.UsingTransaction(tr => action(objectId.OpenAs<T>(tr, openMode)));
        }

        /// <summary>
        /// Opens the database object with the specified object ID for write, and calls a delegate
        /// with the opened object.
        /// </summary>
        /// <typeparam name="T">The type of <b>DBObject</b> being opened.</typeparam>
        /// <param name="objectId">The object id.</param>
        /// <param name="action">A delegate that will receive the opened object.</param>
        public static void OpenForWriteAs<T>(this ObjectId objectId, Action<T> action)
            where T : DBObject
        {
            objectId.Database.UsingTransaction(tr => action(objectId.OpenAs<T>(tr, OpenMode.ForWrite)));
        }
    }
}
