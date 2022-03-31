using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinBox.Extensions
{
    public static class ObjectIdExtension
    {
        //ToEntity
        public static Entity ToEntity(this ObjectId id,Transaction tr)
        {
          return  tr.GetObject(id, OpenMode.ForRead) as Entity;
        }
    }
}
