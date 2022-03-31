#if !HC2020
using Autodesk.AutoCAD.Geometry;
#else
using GrxCAD.ApplicationServices;
using GrxCAD.DatabaseServices;
using GrxCAD.EditorInput;
using GrxCAD.Geometry;
using GrxCAD.Runtime;
#endif


namespace JoinBox
{
    public partial class Graham
    {
        class Rectangular
        {
            public Point2d R1;
            public Point2d R2;
            public Point2d R3;
            public Point2d R4;

            /// <summary>
            /// 矩形类
            /// </summary>
            /// <param name="r1">矩形的原点,依照它来旋转</param>
            /// <param name="r2"></param>
            /// <param name="r3"></param>
            /// <param name="r4"></param>
            public Rectangular(Point2d r1, Point2d r2, Point2d r3, Point2d r4)
            {
                R1 = r1;
                R2 = r2;
                R3 = r3;
                R4 = r4;
                R4 = r4;
            }

            /// <summary>
            /// 面积
            /// </summary>
            public double Area
            {
                get
                {
                    var x = R1.GetDistanceTo(R4);
                    var y = R1.GetDistanceTo(R2);
                    return x * y;
                }
            }
        }
    }
}