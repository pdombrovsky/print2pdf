using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

namespace SstPrint2Pdf.Extensions
{
    public static class BlockTableRecordExtensions
    {
        /// <summary>
        /// Creates a new entity in the specified block table record.
        /// </summary>
        /// <typeparam name="T">The type of entity to create.</typeparam>
        /// <param name="blockTableRecord">The block table record.</param>
        /// <param name="transaction">The transaction.</param>
        /// <param name="action">A delegate that is called with the newly created entity, just before it is added to the database.</param>
        /// <returns>The <b>ObjectId</b> of the newly created entity.</returns>
        public static ObjectId Create<T>(this BlockTableRecord blockTableRecord, Transaction transaction, Action<T> action)
            where T : Entity, new()
        {
            var obj = new T();
            obj.SetDatabaseDefaults();
            action(obj);
            var objectId = blockTableRecord.AppendEntity(obj);
            transaction.AddNewlyCreatedDBObject(obj, true);
            return objectId;
        }


        
    }
}
