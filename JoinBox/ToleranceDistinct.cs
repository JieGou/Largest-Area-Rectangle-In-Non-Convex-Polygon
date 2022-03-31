using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace JoinBox
{
    public class ToleranceDistinct : IEqualityComparer<Point3d>
    {

        public bool Equals(Point3d a, Point3d b)//Point3d是struct不会为null
        {
            if (ReferenceEquals(a, b))//同一对象
                return true;
#if true
            // 方形限定
            // 在 0~1e-6 范围实现 圆形限定 则计算部分在浮点数6位后,没有啥意义
            // 在 0~1e-6 范围实现 从时间和CPU消耗来说,圆形限定 都没有 方形限定 的好
            return a.Equals(b);
#else
            // 圆形限定
            // DistanceTo 分别对XYZ进行了一次乘法,也是总数3次乘法,然后求了一次平方根
            // (X86.CPU.FSQRT指令用的牛顿迭代法/软件层面可以使用快速平方根....我还以为CPU会采取快速平方根这样的取表操作)
            return a.DistanceTo(b) <= Tolerance.Distinct;
#endif
        }

        public int GetHashCode(Point3d obj)
        {
            //结构体直接返回 obj.GetHashCode(); Point3d ToleranceDistinct3d
            //因为结构体是用可值叠加来判断?或者因为结构体兼备了一些享元模式的状态?
            //而类是构造的指针,所以取哈希值要改成x+y+z..s给Equals判断用,+是会溢出,所以用^
            return (int)obj.X ^ (int)obj.Y ^ (int)obj.Z;
        }
    }

    public class Tolerance
    {
        public static double Distinct = 1e-6;
    }

}