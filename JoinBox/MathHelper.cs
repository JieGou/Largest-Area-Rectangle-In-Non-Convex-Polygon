using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using JoinBox.BasalMath;
using JoinBox.Extensions;
using System;

public static partial class MathHelper
{
    /// 原理见:http://www.cnblogs.com/graphics/archive/2012/08/10/2627458.html
    /// 以及:http://www.doc88.com/p-786491590188.html
    /// <summary>
    /// 输入一个法向量(用户Z轴),获取它与世界坐标X轴和Y轴的夹角,
    /// 旋转这两个夹角可以重合世界坐标的Z轴
    /// </summary>
    /// <param name="userZxis">用户坐标系的Z轴</param>
    /// <param name="alx">输出X轴夹角</param>
    /// <param name="aly">输出Y轴夹角</param>
    public static void ToWcsAngles(this Vector3d userZxis, out double alx, out double aly)
    {
        //处理精度
        double X = Math.Abs(userZxis.X) < 1e-10 ? 0 : userZxis.X;
        double Y = Math.Abs(userZxis.Y) < 1e-10 ? 0 : userZxis.Y;
        double Z = Math.Abs(userZxis.Z) < 1e-10 ? 0 : userZxis.Z;

        //YOZ面==旋转X轴..投影的
        var oq = Point3d.Origin.GetVectorTo(new Point3d(0, Y, Z));
        alx = Vector3d.ZAxis.GetAngleTo(oq);
        alx = oq.Y > 0 ? alx : Constant.Pi2 - alx;
        alx = Math.Abs(Constant.Pi2 - alx) < 1e-10 ? 0 : alx;

        //XOZ面==旋转Y轴..旋转的
        var userZ = Math.Pow(Y * Y + Z * Z, 0.5);
        var or = Point3d.Origin.GetVectorTo(new Point3d(X, 0, userZ));
        aly = Vector3d.ZAxis.GetAngleTo(or);
        aly = or.X < 0 ? aly : Constant.Pi2 - aly;
        aly = Math.Abs(Constant.Pi2 - aly) < 1e-10 ? 0 : aly;
    }

    /// <summary>
    /// X轴到向量的弧度,cad的获取的弧度是1PI,所以转换为2PI(上小,下大)
    /// </summary>
    /// <param name="ve">向量</param>
    /// <returns>弧度</returns>
    public static double GetAngle2XAxis(this Vector2d ve)
    {
        double alz = Vector2d.XAxis.GetAngleTo(ve);//观察方向不要设置 
        alz = ve.Y > 0 ? alz : Constant.Pi2 - alz; //逆时针为正,如果-负值控制正反
        alz = Math.Abs(Constant.Pi2 - alz) < 1e-10 ? 0 : alz;
        return alz;
    }

    /// <summary>
    /// X轴到向量的弧度,cad的获取的弧度是1PI,所以转换为2PI(上小,下大)
    /// </summary>
    public static double GetAngle2XAxis(this Point2d startPoint, Point2d endtPoint)
    {
        return startPoint.GetVectorTo(endtPoint).GetAngle2XAxis();
    }

    /// <summary>
    /// 三个轴角度
    /// 重合两个坐标系的Z轴,求出三个轴旋转角度,后续利用旋转角度正负和次序来变换用户和世界坐标系
    /// </summary>
    /// <param name="ucs">用户坐标系</param>
    /// <param name="alx">坐标系间的X轴旋转角度</param>
    /// <param name="aly">坐标系间的Y轴旋转角度</param>
    /// <param name="alz">坐标系间的Z轴旋转角度</param>
    public static void ToWcsAngles(this CoordinateSystem3d ucs, out double alx, out double aly, out double alz)
    {
        //ucs可能带有新原点,设置到0,世界坐标系原点重合
        var ucs2o = new CoordinateSystem3d(Point3d.Origin, ucs.Xaxis, ucs.Yaxis);
        //XY轴通过叉乘求得Z轴,但是这个类帮我求了
        ucs2o.Zaxis.ToWcsAngles(out alx, out aly);
        //使用户X轴与世界XOY面共面,求出Z轴旋转角
        var newXa = ucs2o.Xaxis.RotateBy(alx, Vector3d.XAxis)
                               .RotateBy(aly, Vector3d.YAxis);
        alz = -((PointV)newXa).GetAngle2XAxis();
    }

    private static Point3d Wcs2Ucs(this Point3d pt, CoordinateSystem3d ucs)
    {
        ucs.ToWcsAngles(out double alx, out double aly, out double alz);

        pt = new Point3d(pt.X - ucs.Origin.X,
                         pt.Y - ucs.Origin.Y,
                         pt.Z - ucs.Origin.Z);

        pt = pt.RotateBy(alx, Vector3d.XAxis, Point3d.Origin)
               .RotateBy(aly, Vector3d.YAxis, Point3d.Origin)
               .RotateBy(alz, Vector3d.ZAxis, Point3d.Origin);
        return pt;
    }

    private static Point3d Ucs2Wcs(this Point3d pt, CoordinateSystem3d ucs)
    {
        ucs.ToWcsAngles(out double alx, out double aly, out double alz);
        //把pt直接放在世界坐标系上,此时无任何重合.
        //进行逆旋转,此时向量之间重合.
        pt = pt.RotateBy(-alz, Vector3d.ZAxis, Point3d.Origin)
               .RotateBy(-aly, Vector3d.YAxis, Point3d.Origin)
               .RotateBy(-alx, Vector3d.XAxis, Point3d.Origin);
        //平移之后就是点和点重合,此点为世界坐标系.
        pt = new Point3d(pt.X + ucs.Origin.X,
                         pt.Y + ucs.Origin.Y,
                         pt.Z + ucs.Origin.Z);
        return pt;
    }

    /// <summary>
    /// 把一个点从一个坐标系统变换到另外一个坐标系统
    /// </summary>
    /// <param name="userPt">来源点</param>
    /// <param name="source">来源坐标系</param>
    /// <param name="target">目标坐标系</param>
    /// <returns>目标坐标系的点</returns>
    public static Point3d Transform(this Point3d userPt, CoordinateSystem3d source, CoordinateSystem3d target)
    {
        //世界坐标是独特的
        var wcs = Matrix3d.Identity.CoordinateSystem3d;
        Point3d pt = Point3d.Origin;
        if (Equals(source, target))
            pt = userPt;
        //al的角度是一样的,旋转方式取决于正负号
        else if (!Equals(source, wcs) && !Equals(target, wcs))//用户转用户
            pt = userPt.Ucs2Wcs(source).Wcs2Ucs(target);
        if (Equals(target, wcs))
            pt = userPt.Ucs2Wcs(source);
        else if (!Equals(target, wcs))
            pt = userPt.Wcs2Ucs(target);
        return pt;
    }

    /// <summary>
    /// 获取坐标系统三维
    /// </summary>
    /// <param name="ed"></param>
    /// <param name="wcs"></param>
    /// <param name="ucs"></param>
    public static void GetWcsUcs(this Editor ed, out CoordinateSystem3d wcs, out CoordinateSystem3d ucs)
    {
        Matrix3d Ide_wcs = Matrix3d.Identity;//获取世界坐标系
        wcs = Ide_wcs.CoordinateSystem3d;
        Matrix3d used_ucs = ed.CurrentUserCoordinateSystem;//当前用户坐标系
        ucs = used_ucs.CoordinateSystem3d;
    }
    public static double GetArcBulge(Point3d arc1, Point3d arc2, Point3d arc3)
    {
        return GetArcBulge(arc1.ToPoint2d(), arc2.ToPoint2d(), arc3.ToPoint2d());
    }
        /// http://www.lee-mac.com/bulgeconversion.html
        /// <summary>
        /// 求凸度,判断三点是否一条直线上
        /// </summary>
        /// <param name="arc1">圆弧起点</param>
        /// <param name="arc2">圆弧腰点</param>
        /// <param name="arc3">圆弧尾点</param>
        /// <returns>逆时针为正,顺时针为负</returns>
        public static double GetArcBulge(Point2d arc1, Point2d arc2, Point2d arc3)
    {
        double dStartAngle = GetAngle2XAxis(arc2, arc1);
        double dEndAngle = GetAngle2XAxis(arc2, arc3);
        //求的P1P2与P1P3夹角
        var talAngle = (Math.PI - dStartAngle + dEndAngle) / 2;
        //凸度==拱高/半弦长==拱高比值/半弦长比值
        //有了比值就不需要拿到拱高值和半弦长值了,因为接下来是相除得凸度
        double bulge = Math.Sin(talAngle) / Math.Cos(talAngle);

        //处理精度
        if (bulge > 0.9999 && bulge < 1.0001)
            bulge = 1;
        else if (bulge < -0.9999 && bulge > -1.0001)
            bulge = -1;
        else if (Math.Abs(bulge) < 1e-10)
            bulge = 0;
        return bulge;
    }


    /// <summary>
    /// 求凸度,判断三点是否一条直线上..慢一点
    /// </summary>
    /// <param name="arc1">圆弧起点</param>
    /// <param name="arc2">圆弧腰点</param>
    /// <param name="arc3">圆弧尾点</param>
    /// <returns>逆时针为正,顺时针为负</returns>
    public static double GetArcBulge(this Arc arc)
    {
        var arc1 = arc.StartPoint;
        var arc2 = arc.GetPointAtDist(arc.GetDistAtPoint(arc.EndPoint) / 2);//圆弧的腰点
        var arc3 = arc.EndPoint;
        return GetArcBulge(arc1, arc2, arc3);
    }


    /// <summary>
    /// 求凸度,判断三点是否一条直线上
    /// </summary>
    /// <param name="arc" >圆弧</ param>
    /// <returns></returns>
    public static double GetArcBulge2(this Arc arc)
    {
        //还有一种求凸度方法是tan(圆心角 / 4),但是丢失了方向,
        //再用叉乘来判断腰点方向,从而凸度是否 * -1

        double bulge = Math.Tan(arc.TotalAngle / 4);                             //凸度是圆心角的四分之一的正切
        Point3d midpt = arc.GetPointAtDist(arc.GetDistAtPoint(arc.EndPoint) / 2);//圆弧的腰点
        Vector3d vmid = midpt - arc.StartPoint;                                  //起点到腰点的向量
        Vector3d vend = arc.EndPoint - arc.StartPoint;                           //起点到尾点的向量
        Vector3d vcross = vmid.CrossProduct(vend);                               //叉乘求正负

        //根据右手定则,腰点向量在尾点向量右侧,则叉乘向量Z值为正,圆弧为逆时针
        if (vcross.Z < 0)
            bulge *= -1;

        //处理精度
        if (bulge > 0.9999 && bulge < 1.0001)
            bulge = 1;
        else if (bulge < -0.9999 && bulge > -1.0001)
            bulge = -1;
        else if (Math.Abs(bulge) < 1e-10)
            bulge = 0;
        return bulge;
    }
    /// http://bbs.xdcad.net/thread-722387-1-1.html
    /// <summary>
    /// 凸度求圆心
    /// </summary>
    /// <param name="arc1">圆弧头点</param>
    /// <param name="arc3">圆弧尾点</param>
    /// <param name="bulge">凸度</param>
    /// <returns>圆心</returns>
    public static Point2d GetArcBulgeCenter(Point2d arc1, Point2d arc3, double bulge)
    {
        if (bulge == 0)
            throw new ArgumentNullException("凸度为0,此线是平的");
        var x1 = arc1.X;
        var y1 = arc1.Y;
        var x2 = arc3.X;
        var y2 = arc3.Y;

        var b = (1 / bulge - bulge) / 2;
        var x = (x1 + x2 - b * (y2 - y1)) / 2;
        var y = (y1 + y2 + b * (x2 - x1)) / 2;
        return new Point2d(x, y);
    }
    /// <summary>
    /// 凸度求弧长
    /// </summary>
    /// <param name="arc1">圆弧头点</param>
    /// <param name="arc3">圆弧尾点</param>
    /// <param name="bulge">凸度</param>
    /// <returns></returns>
    public static double GetLength(Point2d arc1, Point2d arc3, double bulge)
    {
        var bowLength = arc1.GetDistanceTo(arc3);         //弦长
        var bowLength2 = bowLength / 2;                //半弦
        var archHeight = Math.Abs(bulge) * bowLength2; //拱高==凸度*半弦

        //根据三角函数: (弦长/2)²+(半径-拱高)²=半径²
        //再根据:完全平方公式变形: (a+b)²=a²+2ab+b²、(a-b)²=a²-2ab+b²
        var r = (bowLength2 * bowLength2 + archHeight * archHeight) / (2 * archHeight); //半径
                                                                                        //求圆心角:一半圆心角[(对边==半弦长,斜边==半径)]; *2就是完整圆心角
        var asin = Math.Asin(bowLength2 / r) * 2; //反正弦
                                                  //弧长公式: 弧长=绝对值(圆心弧度)*半径...就是单位*比例..缩放过程
        var arcLength = asin * r;
        return arcLength;
    }
    /// <summary>
    /// 曲线长度
    /// </summary>
    /// <param name="curve">曲线</param>
    /// <returns></returns>
    public static double GetLength(this Curve curve)
    {
        return curve.GetDistanceAtParameter(curve.EndParam);
    }
}
