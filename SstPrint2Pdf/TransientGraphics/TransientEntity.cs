using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace SstPrint2Pdf.TransientGraphics
{
    internal class TransientEntity
    {
       
        internal IntegerCollection ViewPorts { get; private set; }
        internal Entity Entity { get; private set; }
        internal bool IsErased { get; private set; }
        
        internal TransientEntity(Entity entity)
        {
           
            Entity = entity;
            ViewPorts = new IntegerCollection();
            IsErased = false;

        }
       
        internal void Erase()
        {
            
               
            Entity.Dispose();
            IsErased = true;

           


        }


    }
}