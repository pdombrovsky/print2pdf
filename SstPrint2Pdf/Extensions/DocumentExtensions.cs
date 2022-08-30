using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace SstPrint2Pdf.Extensions
{
    public static class DocumentExtensions
    {
        /// <summary>
        /// Executes a delegate function within the context of a transaction on the specified document.
        /// The document is locked before the transaction starts.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="action">A delegate that takes the <b>Transaction</b> as an argument.</param>
        public static void UsingTransaction(this Document document, Action<Transaction> action)
        {
            using (document.LockDocument())
            {
                document.Database.UsingTransaction(action);
            }
        }

        /// <summary>
        /// Extension method that allows you to iterate through model space and perform an action
        /// on a specific type of object.
        /// </summary>
        /// <typeparam name="T">The type of object to search for.</typeparam>
        /// <param name="document">The document to use.</param>
        /// <param name="action">A delegate that is called for each object found of the specified type.</param>
        /// <remarks>This method locks the specified document.</remarks>
        public static void ForEach<T>(this Document document, Action<T> action)
            where T : Entity
        {
            using (document.LockDocument())
            {
                document.Database.ForEach(action);
            }
        }


        /// <summary>
        /// Locks the document, opens the specified object, and passes it to the specified delegate.
        /// </summary>
        /// <typeparam name="T">The type of object the objectId represents.</typeparam>
        /// <param name="document">The document.</param>
        /// <param name="objectId">The object id.</param>
        /// <param name="openMode">The open mode.</param>
        /// <param name="action">A delegate that takes the opened object as an argument.</param>
        public static void OpenAs<T>(this Document document, ObjectId objectId, OpenMode openMode, Action<T> action)
            where T : DBObject
        {
            document.UsingTransaction(tr => action(objectId.OpenAs<T>(tr, openMode)));
        }
    }
}
