using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinBox.Extensions
{
    public static class TransactionExtension
    {
        public static void AddEntityToMsPs(this Transaction trans, Database db, Entity ent)
        {
            //以写方式打开模型空间块表记录.
            BlockTableRecord btr = (BlockTableRecord)trans.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
            btr.AppendEntity(ent);//将图形对象的信息添加到块表记录中
            trans.AddNewlyCreatedDBObject(ent, true);//把对象添加到事务处理中
        }
    }
}
