using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinBox.Extensions
{
    public static class Point3dExtension
    {
        public static Point2d ToPoint2d(this Point3d point3D)
        {
            return new Point2d(point3D.X, point3D.Y);
        }

        public static List<Point2d> ToPoint2ds(this Point3d[] point3D)
        {
            return point3D.ToList().ToPoint2ds();
        }
        public static List<Point2d> ToPoint2ds(this List<Point3d> point3D)
        {
            var rst = new List<Point2d>();
            point3D.ForEach(p => rst.Add(p.ToPoint2d()));
            return rst;
        }
    }
}
