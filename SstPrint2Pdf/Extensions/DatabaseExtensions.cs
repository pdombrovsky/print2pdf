using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using Exception = System.Exception;

namespace SstPrint2Pdf.Extensions
{
    public static class DatabaseExtensions
    {
        /// <summary>
        /// Executes a delegate function within the context of a transaction on the specified database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="action">A delegate that takes the <b>Transaction</b> as an argument.</param>
        public static void UsingTransaction(this Database database, Action<Transaction> action)
        {
            using (var tr = database.TransactionManager.StartTransaction())
            {
                try
                {
                    action(tr);
                    tr.Commit();
                }
                catch (Exception)
                {
                    tr.Abort();
                    throw;
                }
            }
        }
        /// <summary>
        /// Executes a delegate function in the context of a transaction, and passes it the collection
        /// of ObjectIds for the specified block table record.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="blockName">Name of the block.</param>
        /// <param name="action">A delegate that takes the transaction and the ObjectIds as arguments.</param>
        public static void BlockTableAction(this Database database,
            string blockName,
            Action<Transaction, IEnumerable<ObjectId>> action)
        {
            database.UsingTransaction(
                tr =>
                {
                    // Get the block table
                    var blockTable = database.BlockTableId.OpenAs<BlockTable>(tr);

                    // Get the block table record
                    var tableRecord = blockTable[blockName].OpenAs<BlockTableRecord>(tr);

                    // Invoke the method
                    action(tr, tableRecord.Cast<ObjectId>());
                });
        }

        /// <summary>
        /// Executes a delegate function in the context of a transaction, and passes it the collection
        /// of Entity objects for the specified block table record.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="blockName">Name of the block.</param>
        /// <param name="action">A delegate that takes the transaction and the Entity collection as arguments.</param>
        public static void BlockTableAction(this Database database, string blockName, Action<IEnumerable<Entity>> action)
        {
            database.BlockTableAction
                (blockName,
                    (tr, blockTable) => action(blockTable.Select(id => id.OpenAs<Entity>(tr))));
        }

        /// <summary>
        /// Executes a delegate function in the context of a transaction, and passes it the collection
        /// of objects from model space of the specified type.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="action">A delegate that takes the transaction and the Entity collection as arguments.</param>
        /// <typeparamref name="T">The type of object to retrieve.</typeparamref>
        public static void ModelSpaceAction<T>(this Database database, Action<IEnumerable<T>> action) where T : Entity
        {
            var rxClass = RXObject.GetClass(typeof(T));
            database.ModelSpaceAction(
                (tr, ms) => action(ms.Where(id => id.ObjectClass.IsDerivedFrom(rxClass)).Select(id => id.OpenAs<T>(tr))));
        }

        /// <summary>
        /// Executes a delegate function in the context of a transaction, and passes it the collection
        /// of ObjectIds for the model space block table record.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="action">A delegate that takes the transaction and the ObjectIds as arguments.</param>
        public static void ModelSpaceAction(this Database database, Action<Transaction, IEnumerable<ObjectId>> action)
        {
            database.BlockTableAction(BlockTableRecord.ModelSpace, action);
        }

        /// <summary>
        /// Executes a delegate function in the context of a transaction, and passes it the model space
        /// block table record.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="action">A delegate that takes the transaction and the model space block table record as arguments.</param>
        public static void ModelSpaceAction(this Database database, OpenMode openMode, Action<Transaction, BlockTableRecord> action)
        {
            database.UsingTransaction(
                tr =>
                    {
                        // Get the block table
                        var blockTable = database.BlockTableId.OpenAs<BlockTable>(tr);

                        // Get the block table record
                        var tableRecord = blockTable[BlockTableRecord.ModelSpace].OpenAs<BlockTableRecord>(tr, openMode);

                        // Invoke the method
                        action(tr, tableRecord);
                    });
        }

        /// <summary>
        /// Creates a new entity of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of entity to create.</typeparam>
        /// <param name="database">The database.</param>
        /// <param name="action">A delegate that is called with the newly created entity, just before it is added to the database.</param>
        /// <returns>The <b>ObjectId</b> of the newly created entity.</returns>
        public static ObjectId Create<T>(this Database database, Action<T> action)
            where T : Entity, new()
        {
            var objectId = ObjectId.Null;

            database.ModelSpaceAction(OpenMode.ForWrite,
                (tr, modelSpace) => { objectId = modelSpace.Create(tr,  action); });

            return objectId;
        }


        /// <summary>
        /// Extension method that allows you to iterate through model space and perform an action
        /// on a specific type of object.
        /// </summary>
        /// <typeparam name="T">The type of object to search for.</typeparam>
        /// <param name="database">The database to use.</param>
        /// <param name="action">A delegate that is called for each object found of the specified type.</param>
        public static void ForEach<T>(this Database database, Action<T> action)
            where T : Entity
        {
            database.ModelSpaceAction((tr, modelSpace) => modelSpace.ForEach(tr, action));
        }

        /// <summary>
        /// Extension method that allows you to iterate through model space and perform an action
        /// on a specific type of object.
        /// </summary>
        /// <typeparam name="T">The type of object to search for.</typeparam>
        /// <param name="database">The database to use.</param>
        /// <param name="predicate"></param>
        /// <param name="action">A delegate that is called for each object found of the specified type.</param>
        public static void ForEach<T>(this Database database, Predicate<T>  predicate, Action<T> action)
            where T : Entity
        {
            database.ForEach<T>(
                obj =>
                    {
                        if (predicate(obj))
                            action(obj);
                    });
        }
        /// <summary>
        /// Creates a layer using the specified name and color.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="layerName">Name of the layer.</param>
        /// <param name="color">The color.</param>
        /// <returns>The <b>ObjectId</b> of the newly created layer.</returns>
        public static ObjectId CreateLayer(this Database database, string layerName, Color color = null)
        {
            var objectId = ObjectId.Null;
            database.UsingTransaction(
                tr =>
                {
                    var layerTable = database.LayerTableId.OpenAs<LayerTable>().ForWrite();

                    var layer = new LayerTableRecord { Name = layerName };
                    if (color != null)
                        layer.Color = color;
                    objectId = layerTable.Add(layer);
                    tr.AddNewlyCreatedDBObject(layer, true);
                });
            return objectId;
        }
        /// <summary>
        /// Executes a delegate function with the collection of layers in the specified database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="action">A delegate that takes the collection of layers as an argument.</param>
        public static void LayerTableAction(this Database database, Action<IEnumerable<LayerTableRecord>> action)
        {
            database.UsingTransaction(
                tr => action(
                    database.LayerTableId.OpenAs<LayerTable>(tr)
                            .Cast<ObjectId>()
                            .Select(id => id.OpenAs<LayerTableRecord>(tr))));
        }
        /// <summary>
        /// Gets an <b>IEnumerable</b> for the layer table of the specified database, within the context
        /// of the specified transaction.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="tr">The transaction.</param>
        /// <param name="openMode">The open mode.</param>
        /// <returns>An <b>IEnumerable</b> for the layer table.</returns>
        public static IEnumerable<LayerTableRecord> Layers(this Database database, Transaction tr,
            OpenMode openMode = OpenMode.ForRead)
        {
            return database.LayerTableId
                .OpenAs<LayerTable>(tr)
                .Cast<ObjectId>()
                .OfType<LayerTableRecord>(tr, openMode);
        }
    }
}
