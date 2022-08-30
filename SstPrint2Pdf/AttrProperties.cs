using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace SstPrint2Pdf
{
    internal class AttrProperties
    {
        public bool IsMText { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public double WidthFactor { get; set; }
        /// <summary>
        /// Метод работает как с обычными атрибутами так и многострочными
        /// </summary>
        /// <_param name="attributereference">Ссылка на атрибут </_param>
        /// <returns>Возвращает значение атрибута</returns>
       internal static string GetAttributeValue(AttributeReference attributereference)
        {
            return attributereference.IsMTextAttribute ? attributereference.MTextAttribute.Text : attributereference.TextString;
        }
        
        /// <summary>
        /// Ищет в коллекции атрибутов атрибут с  именем attname 
        /// </summary>
        /// <_param name="transaction">Транзакция</_param>
        /// <_param name="attributecollection">Коллекция атрибутов блока</_param>
        /// <_param name="attname">Имя втрибута для поиска</_param>
        /// <returns>Возвращает объект AttributeReference если такой атрибут найден, иначе null</returns>
        internal static AttributeReference FindAttribute(Transaction transaction, AttributeCollection attributecollection, string attname)
        {
            foreach (ObjectId arId in attributecollection)
            {
                var ar = transaction.GetObject(arId, OpenMode.ForRead) as AttributeReference;
                if (ar != null && ar.Tag.ToUpper() == attname.ToUpper()) return ar;
            }
            return null;
        }
        /// <summary>
        /// Возвращает свойства атрибута по его ID 
        /// </summary>
        /// <_param name="transaction">Транзакция</_param>
        /// <_param name="id">ID атрибута</_param>
        /// <returns>Возвращает свойства атрибута</returns>
        private static AttrProperties GetAttributeProperties(Transaction transaction, ObjectId id)
        {
            var attr = new AttrProperties();
            var ar = transaction.GetObject(id, OpenMode.ForRead) as AttributeReference;
            if (ar != null)
            {
                attr.Name = ar.Tag.ToUpper();

                if (ar.IsMTextAttribute)
                {
                    attr.IsMText = true;
                    attr.Value = ar.MTextAttribute.Contents;
                    

                }
                else
                {
                    attr.IsMText = false;
                    attr.Value = ar.TextString;
                    attr.WidthFactor = ar.WidthFactor;

                }


            }

            return attr;
        }
        /// <summary>
        /// Присваивает новые значения свойствам
        /// </summary>
        /// <_param name="attributereference">Объект attributereference</_param>
        /// <_param name="value">Новое значение</_param>
        private static void SetAttributeProperties(AttributeReference attributereference, AttrProperties attrprop)
        {
            attributereference.UpgradeOpen();
            if (attributereference.IsMTextAttribute)
            {

                attributereference.TextString = "{\\W" + attrprop.WidthFactor + ";" + attrprop.Value + "}";
                attributereference.ForceAnnoAllVisible = true;
                attributereference.UpdateMTextAttribute();


            }
            else
            {

                attributereference.TextString = attrprop.Value;
                attributereference.WidthFactor = attrprop.WidthFactor;

            }

            attributereference.DowngradeOpen();
        }
        internal static bool TryGetAttributeReference(Transaction transaction, AttributeCollection attributecollection, string attname, out AttributeReference attributereference)
        {

            foreach (ObjectId arId in attributecollection)
            {
                var ar = transaction.GetObject(arId, OpenMode.ForRead) as AttributeReference;
                if (ar != null && ar.Tag.ToUpper() == attname.ToUpper())
                {
                    attributereference = ar; return true;
                }
            }
            attributereference = null;
            return false;
        }
        /// <summary>
        /// Проеряет присутствует ли все атрибут с именем attname в коллекции attributecollection
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="attributecollection"></param>
        /// <param name="attname"></param>
        /// <returns></returns>
        internal static bool HasAttribute(Transaction transaction, AttributeCollection attributecollection, string attname)
        {

            foreach (ObjectId arId in attributecollection)
            {
               var ar = transaction.GetObject(arId, OpenMode.ForRead) as AttributeReference;
                if (ar == null) continue;
                if (ar.Tag.ToUpper() == attname.ToUpper())
                {
                    return true;
                }
            }
           
            return false;
        }
        /// <summary>
        /// Возращает свойства атрибута. Документ должен быть заблокирован 
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="attrids"></param>
        /// <returns></returns>
        internal static AttrProperties[] GetAttributesProperties(Transaction transaction, ObjectId[] attrids)
        {
           
            var res = new List<AttrProperties>();
           
                

                    foreach (var atId in attrids)
                    {
                        var attrprop = GetAttributeProperties(transaction, atId);

                        res.Add(attrprop);
                    }
                    
                
            
            return res.ToArray();

        }
       /// <summary>
        /// Копирует свойства атрибутов. Документ должен быть заблокирован. 
       /// </summary>
       /// <param name="transaction"></param>
       /// <param name="blocksIds"></param>
       /// <param name="attrprop"></param>
        internal static void CloneAttributeProperties(Transaction transaction, ObjectId[] blocksIds, AttrProperties[] attrprop)
        {
           
               //
           
                    foreach (var blockid in blocksIds)
                    {
                        var blref = transaction.GetObject(blockid, OpenMode.ForRead) as BlockReference;

                        if (blref != null)
                        {
                            var btr = transaction.GetObject(blref.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

                            foreach (var attrname in attrprop)
                            {
                                AttributeReference ar = null;
                                if (btr != null && !(btr.HasAttributeDefinitions && TryGetAttributeReference(transaction, blref.AttributeCollection, attrname.Name, out ar)))
                                    break;
                                SetAttributeProperties(ar, attrname);
                            }
                        }
                    }

                    
               
            }
        internal static ObjectId[] GetBlockWithAttributes(Transaction transaction, ObjectId[] blocksIds, Predicate<AttributeCollection> cond)
        {

            var res = new List<ObjectId>();
           
                foreach (var blockid in blocksIds)
                {
                    var blref = transaction.GetObject(blockid, OpenMode.ForRead) as BlockReference;

                    if (blref != null)
                    {
                        var btr = transaction.GetObject(blref.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                        if (btr != null && btr.HasAttributeDefinitions)
                        {
                       
                            if (cond(blref.AttributeCollection)) res.Add(blockid);

                        }
                    }
                }


            
            return res.ToArray();
        }



        

    }
}
