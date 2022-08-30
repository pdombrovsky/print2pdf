using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using SstPrint2Pdf.AcDrawing;
using SstPrint2Pdf.PlotSettingsConfiguration;
using SstPrint2Pdf.Extensions;
namespace SstPrint2Pdf.ErrorVisualizator
{
    public class FormatErrorVisualizator
    {
        private readonly IDrawing _drawing;
        private readonly int _coloridx;
        private readonly LineWeight _weight;
        private  List<PageSettings> _frames;
        private bool _isSetOfFrames;
        public FormatErrorVisualizator(IDrawing drawing, int coloridx, LineWeight weight)
        {
            _coloridx = coloridx;
            _weight = weight;
            _drawing = drawing;
           _isSetOfFrames = false;
        }
        public void SetUnrecognizedFrames(List<PageSettings> frames)
        {
            if (frames != null && frames.Count > 0)
            {
                _frames = frames;
                _isSetOfFrames = true;
            }
               
        }
        public void OutputUnrecognizedFrames()
        {
            if (!_isSetOfFrames) return;

            const string msg = "Размеры выделенных листов не соответствуют ГОСТ." +
                               "\nУбедитесь, что выделены только рамки." +
                               "\nДля вкладки 'Модель' откройте Диспетчер параметров листов, выберите набор параметров *Модель* и установите корректный масштаб."; 
                      //"\nи(или) измените допуски в настройках команды.";
            
            DrawUnrecognizedFrames();
            Application.ShowAlertDialog(msg);

        }
        public void OutputUnrecognizedFormats()
        {
            if (!_isSetOfFrames) return;
            var  badformats=string.Empty;
           // _frames.ForEach(el => badformats += el.Format + ", ");
            for (var i = 0; i < _frames.Count - 1; i++) badformats += _frames[i].Format + ", ";
            badformats += _frames[_frames.Count-1].Format + ".";
            
            var msg = "В настройках устройства печати отсутствуют страницы c допустимыми полями для следующих форматов:" +
                      "\n" + badformats+
                      "\nДля вкладки 'Модель' откройте Диспетчер параметров листов, выберите набор параметров *Модель*, проверьте масштаб," +
                      "\nпри необходимости измените устройство печати" +
                      "\nи(или) добавьте для заданного устройства необходимые форматы с отступами раными 0.";

            
            Application.ShowAlertDialog(msg);

        }
        
        
        #region   private_methods



    private Polyline3d CreateLine(PageSettings frame)
    {
        var pt1 = new Point3d(frame.WcsRegion.MinPoint.X, frame.WcsRegion.MinPoint.Y, frame.WcsRegion.MinPoint.Z);
        var pt3 = new Point3d(frame.WcsRegion.MaxPoint.X, frame.WcsRegion.MaxPoint.Y, frame.WcsRegion.MaxPoint.Z);
        var pt2 = new Point3d(pt1.X, pt3.Y, pt1.Z);
        var pt4 = new Point3d(pt3.X, pt1.Y, pt3.Z);
        var pcol = new Point3dCollection();
        pcol.Add(pt1);
        pcol.Add(pt2);
        pcol.Add(pt3);
        pcol.Add(pt4);

        var poly = new Polyline3d(Poly3dType.SimplePoly, pcol, true);
        poly.ColorIndex =_coloridx;
        poly.LineWeight = _weight;

        return poly;
    }
    private ObjectIdCollection AppendEntities(IEnumerable<Entity> entities)
    {
        var res = new ObjectIdCollection();
        var db = _drawing.Db;
        db.ModelSpaceAction(OpenMode.ForWrite, (tr, btr) =>
            {
                foreach (var ent in entities)
                {
                    var id = btr.AppendEntity(ent);
                    res.Add(id);
                    tr.AddNewlyCreatedDBObject(ent, true);
                
                }

            });


        return res;
    }
    private void CreateGroup(ObjectIdCollection collectionid)
    {
        var database = _drawing.Db;
        database.UsingTransaction(tr =>
            {
              
               var dict= database.GroupDictionaryId.OpenAs<DBDictionary>(tr,OpenMode.ForWrite);        
                 var anonyGroup = new Group();
                  dict.SetAt("*", anonyGroup);
                    foreach (ObjectId id in collectionid)
                    {

                        anonyGroup.Append(id);

                    }
                    tr.AddNewlyCreatedDBObject(anonyGroup, true);

            });


    }
    private void DrawUnrecognizedFrames()
    {
       

        var lines = new List<Polyline3d>();
        _frames.ForEach(el =>
        {

            var line = CreateLine(el);
            lines.Add(line);
        });
        var ids = AppendEntities(lines);
        CreateGroup(ids);
    }
    #endregion



    }
}
