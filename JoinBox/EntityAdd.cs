using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;

namespace JoinBox
{
    internal class EntityAdd
    {
        internal static Polyline AddPolyLineToEntity(List<BulgeVertex> bv)
        {
            var pl = new Polyline();
            int i = 0;
            foreach (var item in bv)
            {
                var pt = item.Vertex;
                var bulge = item.Bulge;
                pl.AddVertexAt(i, pt, bulge, 0, 0);
                i++;
            }
            pl.Closed = true;
            return pl;
        }
    }
}