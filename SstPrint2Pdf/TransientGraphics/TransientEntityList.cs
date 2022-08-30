using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.GraphicsInterface;

namespace SstPrint2Pdf.TransientGraphics
{
    public class TransientEntityList
    {
        private static readonly List<TransientEntity> Entities; 
        private static readonly TransientManager TransientManager;
        static TransientEntityList()
        {
            TransientManager = TransientManager.CurrentTransientManager;
            Entities = new List<TransientEntity>();

        }
        public TransientEntityList()
        {
           if (Entities.Count>0) RemoveAll();

        }
        public void Add(Entity entity, TransientDrawingMode mode)
        {
            if (Entities.Exists(el => el.Entity == entity)) return;
            
            var trent = new TransientEntity(entity);
            Entities.Add(trent);
            TransientManager.AddTransient(entity, mode, 128, trent.ViewPorts);
        }
        public void Remove(Entity entity)
        {
           var idx= Entities.FindIndex(el => el.Entity == entity);
            if (idx < 0) return;
            var trent = Entities[idx];
            if (!trent.IsErased)
            {
                TransientManager.EraseTransient(trent.Entity, trent.ViewPorts);
                trent.Erase();
            }
            Entities.RemoveAt(idx);
        }
        public void RemoveAll()
        {
            Entities.ForEach(el =>
                {
                    if (!el.IsErased)
                    {
                        TransientManager.EraseTransient(el.Entity, el.ViewPorts);
                        el.Erase();
                    }


                });
            Entities.RemoveAll(el => el.IsErased);

        }

    }
}
