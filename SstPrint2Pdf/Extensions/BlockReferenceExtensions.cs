using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace SstPrint2Pdf.Extensions
{
    public static  class BlockReferenceExtensions
    {
         public static IEnumerable<AttributeReference> Attributes(this BlockReference br, Transaction tr,
                                                                  OpenMode openMode = OpenMode.ForRead)
         {

             return br.AttributeCollection.Cast<ObjectId>().OfType<AttributeReference>(tr, openMode);

         }
         public static void AttributeCollectionAction(this BlockReference br,
                                                      Action<IEnumerable<AttributeReference>> action)
         {
             br.Database.UsingTransaction(tr => action(
                 br.AttributeCollection.Cast<ObjectId>().Select(id => id.OpenAs<AttributeReference>(tr))));

         }

        public static Extents3d GetBounds(this BlockReference br)
        {
            if (!br.IsDynamicBlock) return br.GeometricExtents;

            var blockExt = new Extents3d(Point3d.Origin, Point3d.Origin);
            var mat = Matrix3d.Identity;
            
           
           br.Database.UsingTransaction(tr=> GetDynamicBlockExtents(tr,br, ref blockExt, ref mat));
            

            return blockExt;
        }
        

    
//#region TestinExts

//         private static void testext(Extents3d blockExt,  int clr)
//         {
           
            
//             using (
//                 var curSpace =
//                     CurrentDrawing.Database.CurrentSpaceId.GetObject(OpenMode.ForWrite) as BlockTableRecord)
//             {
//                 var pts = new Point3dCollection();
//                 pts.Add(blockExt.MinPoint);
//                 pts.Add(new Point3d(blockExt.MinPoint.X, blockExt.MaxPoint.Y, blockExt.MinPoint.Z));
//                 pts.Add(blockExt.MaxPoint);
//                 pts.Add(new Point3d(blockExt.MaxPoint.X, blockExt.MinPoint.Y, blockExt.MinPoint.Z));
//                 using (var poly = new Polyline3d(Poly3dType.SimplePoly, pts, true))
//                 {
//                     poly.ColorIndex = clr;
//                     if (curSpace != null) curSpace.AppendEntity(poly);
//                 }


               
//             }


            
//         }

//         #endregion
         private static bool IsLayerVisible(ObjectId layerid, Transaction tr)
         {
             var layer = layerid.OpenAs<LayerTableRecord>(tr);

             return !(layer == null || layer.IsFrozen || layer.IsOff);
         }
         /// <summary>
         /// Определят не пустой ли габаритный контейнер.
         /// </summary>
         /// <param name="ext">Габаритный контейнер.</param>
         /// <returns></returns>
         private static bool IsEmptyExt(ref Extents3d ext)
         {
             return ext.MinPoint.DistanceTo(ext.MaxPoint) < Tolerance.Global.EqualPoint;
         }
         private static void UpateExtents(ref Extents3d curVal, Extents3d updatewithVal)
         {
             if (IsEmptyExt(ref curVal))
             {
                 curVal = updatewithVal;
                 return;
             }
            curVal.AddExtents(updatewithVal);
         }
         private static void GetTransformedExtents(Entity en, ref Extents3d ext, ref Matrix3d mat)
         {
             if (mat.IsUniscaledOrtho())
             {
                 using (var enTr = en.GetTransformedCopy(mat))
                 {
                    
                         UpateExtents(ref ext, enTr.GeometricExtents);
                     
                    
                 }
                 return;
             }
             
                     var curExt = en.GeometricExtents;
                     curExt.TransformBy(mat);

                     UpateExtents(ref ext, curExt);
                    
                 
         }

         private static IEnumerable<MTextFragmentGeometry> GetMTextFragmentsGeometry(MText mtext)
         {
             var res = new List<MTextFragmentGeometry>();
             var cb = new MTextFragmentCallback((frag, obj) =>
             {
                 res.Add(new MTextFragmentGeometry(frag.Direction, frag.Location, frag.Extents));

                 return MTextFragmentCallbackStatus.Continue;
             });

             mtext.ExplodeFragments(cb);
             return res;

         }
         private static void  GetMTextFragmentExtents(MTextFragmentGeometry frgmt, ref Extents3d extents, ref  Matrix3d mat)
         {


             var minPt = Point3d.Origin;
             var maxPt = (new Point3d(frgmt.Extents.X, frgmt.Extents.Y, 0));

             var pts = new Point3dCollection();
             pts.Add(minPt);
             pts.Add(new Point3d(minPt.X, maxPt.Y, minPt.Z));
             pts.Add(maxPt);
             pts.Add(new Point3d(maxPt.X, minPt.Y, maxPt.Z));
             using (var pline = new Polyline3d(Poly3dType.SimplePoly, pts, true))
             {
                 var angle = Vector3d.XAxis.GetAngleTo(frgmt.Direction, Vector3d.ZAxis);
                 var trmat=Matrix3d.Displacement(frgmt.Location.GetAsVector()) ;
                 trmat *= Matrix3d.Rotation(angle, Vector3d.ZAxis, minPt);
                 pline.TransformBy(trmat);
                

                 GetTransformedExtents(pline, ref extents, ref mat);
             }

         }
       

        //Источник:https://forums.autodesk.com/t5/net/dynamic-blockreference-with-visibilitystates-geometricextents/td-p/5806303
         //Автор-Ривилис, усовершенствовано (Домбровский) -теперь корректно определяет границы с учетом атрибутов и мтекста
         /// <summary>
         /// Рекурсивное получение габаритного контейнера для вставки блока.
         /// </summary>
         /// <param name="tr">Транзакция</param>
         /// <param name="entity">Имя примитива</param>
         /// <param name="extents">Габаритный контейнер</param>
         /// <param name="mat">Матрица преобразования из системы координат блока в МСК.</param>

         static void GetDynamicBlockExtents(Transaction tr, Entity entity, ref Extents3d extents, ref Matrix3d mat)
         {
             if (!IsLayerVisible(entity.LayerId, tr)) return;
             if (entity is BlockReference)
             {
                 var bref = entity as BlockReference;
                 var matIns = mat * bref.BlockTransform;
                 using (var btr = bref.BlockTableRecord.OpenAs<BlockTableRecord>(tr))
                 {

                     if (btr.HasAttributeDefinitions)
                     {
                         foreach (var ar in bref.Attributes(tr).Where(ar => ar.Visible && ar.Bounds.HasValue))
                         {
                             GetTransformedExtents(ar, ref extents, ref mat); 
                         }

                     }
                    
                          foreach (var id in btr)
                          {
                              using (var ent = id.OpenAs<Entity>(tr))
                              {

                                  if (ent != null && ent.Bounds.HasValue && ent.Visible)
                                  {
                                     
                                      GetDynamicBlockExtents(tr, ent, ref extents, ref matIns);
                                  }

                              }
                          }

                 }
             }
             else
             {
                 if (entity is MText)
                 {
                     var mtext = entity as MText;
                     var frgmnts = GetMTextFragmentsGeometry(mtext);
                    foreach (var frgmnt in frgmnts)
                    {
                        GetMTextFragmentExtents(frgmnt, ref extents, ref mat);
                    }



                 }
                 else GetTransformedExtents(entity, ref extents, ref mat);
                 
             }
         }

        


    }
    public class MTextFragmentGeometry
    {
        
        public Vector3d Direction { get; private set; }
        public Point3d Location { get; private set; }
        public Point2d Extents { get; private set; }
        public MTextFragmentGeometry(Vector3d direction, Point3d location, Point2d extents)
        {
            Direction = direction;
            Location = location;
            Extents = extents;
        }

    }
}
