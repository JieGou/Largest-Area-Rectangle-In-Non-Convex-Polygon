using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace JoinBox.BasalMath
{
    /*
     * 因为cad08在debug时显示不了数值,所以每个用到以下类型的函数都转为本类.
     * 融合Point2d,PointV,Vector2d,Vector3d,点和向量的区别在与sign
     */

    [Serializable]
    [StructLayout(LayoutKind.Sequential)] //4个成员*8=32大小
    public class PointV : IFormattable, IEquatable<PointV>, IComparable<PointV>
    {
        #region 成员
        //字段在内部调用时候是不通过属性的(否则会慢,因为会经过一层函数)
        //如果XYZ不是只读,那么将导致 Origin 等会被修改
        double _X;
        double _Y;
        double _Z;
        double _S;

        public double X { get => _X; }
        public double Y { get => _Y; }
        public double Z { get => _Z; }
        public double S
        {
            get => _S;
            set
            {
                if (value == 0 || value == 1)
                    _S = value;
            }
        }

        public static PointV Origin => new(0, 0, 0, 1);
        public static PointV ZAxis => new(0.0, 0.0, 1.0, 0);
        public static PointV YAxis => new(0.0, 1.0, 0.0, 0);
        public static PointV XAxis => new(1.0, 0.0, 0.0, 0);

        const double Tau = Math.PI * 2.0;
        #endregion

        #region 构造
        public PointV() { _S = 1; }
        public PointV(double x, double y, double z = 0, double s = 1)
        {
            _X = x;
            _Y = y;
            _Z = z;
            _S = s;
        }
        public PointV(double[] xyzs)
        {
            if (xyzs.Length > 0)
                _X = xyzs[0];
            if (xyzs.Length > 1)
                _Y = xyzs[1];
            if (xyzs.Length > 2)
                _Z = xyzs[2];
            if (xyzs.Length > 3)
                _S = xyzs[3];
            else
                _S = 1;
        }
        #endregion

        #region 重载运算符
        public static PointV operator +(PointV p1, PointV p2)
        {
            //有一个为点就是点
            var s1 = (int)p1._S;
            var s2 = (int)p2._S;

            return new PointV(p1._X + p2._X, p1._Y + p2._Y, p1._Z + p2._Z, s1 | s2);
        }
        public static PointV operator +(PointV p1, double a)
        {
            if (a == 0)//就是原本
                return p1;
            return new PointV(p1._X + a, p1._Y + a, p1._Z + a, p1._S);
        }

        public static PointV operator -(PointV p1, PointV p2)
        {
            //点-点=向量     1-1=0
            //向量-向量=向量 0-0=0
            //点-向量=点     1-0=1
            //向量-点=点     0--1=-1 abs
            var s1 = (int)p1._S;
            var s2 = (int)p2._S;
            var s3 = Math.Abs(s1 - s2);
            return new PointV(p1._X - p2._X, p1._Y - p2._Y, p1._Z - p2._Z, s3);
        }
        public static PointV operator -(PointV p1, double a)
        {
            if (a == 0)//就是原本
                return p1;
            return new PointV(p1._X - a, p1._Y - a, p1._Z - a, p1._S);
        }
        public static PointV operator -(PointV ve)
        {
            return ve * -1;
        }

        public static PointV operator *(PointV p1, PointV p2)
        {
            //有一个为点就是点
            var s1 = (int)p1._S;
            var s2 = (int)p2._S;

            return new PointV(p1._X * p2._X, p1._Y * p2._Y, p1._Z * p2._Z, s1 | s2);
        }
        public static PointV operator *(PointV p1, double a)
        {
            if (a == 1)//1就是原本
                return p1;
            return new PointV(p1._X * a, p1._Y * a, p1._Z * a, p1._S);
        }

        public static PointV operator /(PointV p1, PointV p2)
        {
            //有一个为点就是点
            var s1 = (int)p1._S;
            var s2 = (int)p2._S;

            return new PointV(p1._X / p2._X, p1._Y / p2._Y, p1._Z / p2._Z, s1 | s2);
        }
        public static PointV operator /(PointV p1, double a)
        {
            if (a == 1)//1就是原本
                return p1;
            return new PointV(p1._X / a, p1._Y / a, p1._Z / a, p1._S);
        }

        /// <summary>
        /// 获取两点获取向量,就是B点-A点
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public PointV GetVectorTo(PointV point)
        {
            return point - this;
        }
        #endregion

        #region 重载运算符_比较
        public override bool Equals(object? obj)
        {
            return this == obj as PointV;
        }
        public bool Equals(PointV? b)
        {
            return this == b;
        }

        public static bool operator !=(PointV? a, PointV? b)
        {
            return !(a == b);
        }

        public static bool operator ==(PointV? a, PointV? b)
        {
            //此处地方不允许使用==null,因为此处是定义
            if (b is null)
                return a is null;
            else if (a is null)
                return false;
            if (ReferenceEquals(a, b))//同一对象
                return true;

            return a.Equals(b, true, 0);//容差0
        }

        /// <summary>
        /// 比较核心
        /// </summary>
        /// <param name="b"></param>
        /// <param name="ignoreSign">检查同类点或同类向量</param>
        /// <param name="tolerance">容差</param>
        /// <returns></returns>
        public bool Equals(PointV? b, bool ignoreSign = false, double tolerance = 1e-6)
        {
            if (b is null)
                return false;
            if (ReferenceEquals(this, b)) //同一对象
                return true;

            //对比各个字段值
            var p = Math.Abs(_X - b._X) <= tolerance &&
                    Math.Abs(_Y - b._Y) <= tolerance &&
                    Math.Abs(_Z - b._Z) <= tolerance;
            if (ignoreSign)
                p = p && (_S - b._S == 0);
            return p;
        }

        /// <summary>
        /// 跟cad一样
        /// </summary>
        /// <returns></returns>
        public bool IsEqualTo(PointV? b, double tolerance)
        {
            return Equals(b, default, tolerance);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public int CompareTo(PointV? p)
        {
            if (p is null)
                return -1;

            if (_X > p._X)
                return 1;
            else if (_X < p._X)
                return -1;
            else if (_Y > p._Y)
                return 1;
            else if (_Y < p._Y)
                return -1;
            else if (_Z > p._Z)
                return 1;
            else if (_Z < p._Z)
                return -1;
            else
                return 0;
        }

        /// <summary>
        /// Linq消重用的
        /// </summary>
        public static ToleranceDistinctV Distinct = new();
        #endregion

        #region 转换对象
        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns>返回深拷贝后的新对象</returns>
        public PointV Clone()
        {
            return new PointV(_X, _Y, _Z, _S);
        }
        public double[] ToArray()
        {
            return new double[] { _X, _Y, _Z, _S };
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            var array = new string[]
            {
                _X.ToString(format, formatProvider),
                _Y.ToString(format, formatProvider),
                _Z.ToString(format, formatProvider),
                _S.ToString(format, formatProvider),
            };
            var sb = new StringBuilder();
            sb.Append("(");
            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(array[i]);
                if (i < array.Length - 1)
                    sb.Append(",");
            }
            sb.Append(")");
            return sb.ToString();
        }
        public string ToString(IFormatProvider? provider)
        {
            return ToString(null, provider);
        }
        public sealed override string ToString()
        {
            return ToString(null, null);
        }


        // 隐式转换(相当于是重载赋值运算符)
        public static implicit operator PointV(Autodesk.AutoCAD.Geometry.Point2d pt)
        {
            return new PointV(pt.ToArray());
        }
        public static implicit operator PointV(Autodesk.AutoCAD.Geometry.Point3d pt)
        {
            return new PointV(pt.ToArray());
        }
        public static implicit operator PointV(Autodesk.AutoCAD.Geometry.Vector2d ve)
        {
            var v = new PointV(ve.ToArray());
            v._S = 0;
            return v;
        }
        public static implicit operator PointV(Autodesk.AutoCAD.Geometry.Vector3d ve)
        {
            var v = new PointV(ve.ToArray());
            v._S = 0;
            return v;
        }

        public static List<PointV> Parse(IEnumerable<Autodesk.AutoCAD.Geometry.Point2d> pvs)
        {
            List<PointV> ptvs = new();
            var ge = pvs.GetEnumerator();
            while (ge.MoveNext())
                ptvs.Add(ge.Current);
            return ptvs;
        }
        public static List<PointV> Parse(IEnumerable<Autodesk.AutoCAD.Geometry.Point3d> pvs)
        {
            List<PointV> ptvs = new();
            var ge = pvs.GetEnumerator();
            while (ge.MoveNext())
                ptvs.Add(ge.Current);
            return ptvs;
        }
        public static List<PointV> Parse(IEnumerable<Autodesk.AutoCAD.Geometry.Vector2d> pvs)
        {
            List<PointV> ptvs = new();
            var ge = pvs.GetEnumerator();
            while (ge.MoveNext())
                ptvs.Add(ge.Current);
            return ptvs;
        }
        public static List<PointV> Parse(IEnumerable<Autodesk.AutoCAD.Geometry.Vector3d> pvs)
        {
            List<PointV> ptvs = new();
            var ge = pvs.GetEnumerator();
            while (ge.MoveNext())
                ptvs.Add(ge.Current);
            return ptvs;
        }


#if !WinForm
        public System.Windows.Point ToPoint()
        {
            return new System.Windows.Point(_X, _Y);
        }
        public static implicit operator PointV(System.Windows.Point p)
        {
            return new PointV(p.X, p.Y);
        }
#else
        public System.Drawing.PointF ToPoint()
        {
            return new System.Drawing.PointF((float)_X, (float)_Y);
        }
        public static implicit operator PointV(System.Drawing.PointF p)
        {
            return new PointV(p.X, p.Y);
        }
#endif
        #endregion

        #region 点积
        /// <summary>
        /// 点积,求坐标
        /// </summary>
        /// <param name="a">点</param>
        /// <param name="b">点</param>
        /// <returns>坐标点</returns>
        //点乘就是将oa向量投射到ob向量上面,求得op坐标(也就是呈现90度角的坐标)
        public PointV DotProduct(PointV a, PointV b)
        {
            var o = this;             //作为原点
            var oa = o.GetVectorTo(a);//o点平移到原点旁边
            var obUnit = o.GetVectorTo(b).GetUnitNormal();
            var dot = (oa._X * obUnit._X) + (oa._Y * obUnit._Y) + (oa._Z * obUnit._Z);
            var p = obUnit * dot;
            return p + o;//原点旁边平移到o点
        }

        /// <summary>
        /// 点积,求长度
        /// </summary>
        /// <param name="a">点</param>
        /// <param name="b">点</param>
        /// <returns>长度</returns>
        public double DotProductLength(PointV a, PointV b)
        {
            var o = this;//作为原点
            var p = o.DotProduct(a, b);
            return o.GetDistanceTo(p);
        }

        /// <summary>
        /// 点积,求值
        /// <a href="https://zhuanlan.zhihu.com/p/359975221"> 1.是两个向量的长度与它们夹角余弦的积 </a>
        /// <a href="https://www.cnblogs.com/JJBox/p/14062009.html#_label1"> 2.求四个点是否矩形使用 </a>
        /// </summary>
        /// <param name="a">点</param>
        /// <param name="b">点</param>
        /// <returns><![CDATA[>0方向相同,夹角0~90度;=0相互垂直;<0方向相反,夹角90~180度]]></returns>
        public double DotProductValue(PointV a, PointV b)
        {
            var o = this; //作为原点
            var oa = o.GetVectorTo(a);
            var ob = o.GetVectorTo(b);
            return (oa._X * ob._X) + (oa._Y * ob._Y) + (oa._Z * ob._Z);
        }

        #endregion

        #region 叉积
        /// <summary>
        /// 叉积,二维叉乘计算
        /// </summary>
        /// <param name="v2">传参是向量,表示原点是0,0</param>
        /// <param name="v2">传参是向量,表示原点是0,0</param>
        /// <returns>其模为a与b构成的平行四边形面积</returns>
        // 正常求平行四边形面积就是底乘以高,只是换成坐标形式 https://www.zhihu.com/question/22902370
        public double Cross(PointV v2)
        {
            var v1 = this;//原点
            if (v1._S != 0 || v2._S != 0)
                throw new ArgumentNullException("Cross参数不是向量");
            return v1._X * v2._Y - v1._Y * v2._X;
        }

        // 判断点在三角形内部 https://blog.csdn.net/pdcxs007/article/details/51436483
        // 视频,右手螺旋法则 https://www.bilibili.com/video/BV1S741157cb?from=search&seid=1379331852253162293
        /// <summary>
        /// 叉积,二维叉乘计算
        /// </summary>
        /// <param name="o">原点</param>
        /// <param name="a">oa向量</param>
        /// <param name="b">ob向量,此为判断点</param>
        /// <returns>返回值有正负,表示绕原点四象限的位置变换,也就是有向面积</returns>
        public double Cross(PointV a, PointV b)
        {
            var o = this;//原点
            return o.GetVectorTo(a).Cross(o.GetVectorTo(b));
        }

        /// <summary>
        /// 叉积,逆时针方向为真
        /// </summary>
        /// <param name="a">直线点1</param>
        /// <param name="b">直线点2</param>
        /// <param name="c">判断点</param>
        /// <returns>b点在oa的逆时针<see cref="true"/></returns>
        public bool CrossAclockwise(PointV b, PointV c, double tolerance = -1e-6)
        {
            var a = this;//原点
            return a.Cross(b, c) >= tolerance;
        }

        /// <summary>
        /// 叉积,求法向量
        /// </summary>
        /// <param name="a">向量a</param>
        /// <param name="b">向量b</param>
        /// <returns>右手坐标系系法向量</returns>
        public PointV CrossProductNormal(PointV b)
        {
            var a = this;
            //叉乘:依次用手指盖住每列,交叉相乘再相减
            //(a.X  a.Y  a.Z)
            //(b.X  b.Y  b.Z)
            var pv = new PointV(a._Y * b._Z - b._Y * a._Z,    //主-副
                                a._Z * b._X - b._Z * a._X,    //副-主
                                a._X * b._Y - b._X * a._Y);   //主-副
            pv._S = a._S;
            return pv;
        }
        #endregion

        #region 方法
        /// <summary>
        /// 获取长度
        /// </summary>
        public double GetDistanceTo(PointV pv)
        {
            return GetDistanceTo(pv.ToArray());
        }
        public double DistanceTo(PointV pv)
        {
            return GetDistanceTo(pv.ToArray());
        }

        /// <summary>
        /// 获取min和max点
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        public static (PointV Min, PointV Max) GetMinMax(params PointV[] pts)
        {
            return GetMinMax(pts as IEnumerable<PointV>);
        }

        /// <summary>
        /// 获取min和max点(非包围盒)
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        public static (PointV Min, PointV Max) GetMinMax(IEnumerable<PointV> pts)
        {
            // 这里不是等价的,因为矩形方法是求的包围盒
            // RectHelper.GetMinAndMax(pointLst, out PointV minPt, out PointV maxPt);
            pts = pts.OrderBy(a => a.X).ThenBy(a => a.Y).ThenBy(a => a.Z);
            return (pts.First(), pts.Last());
        }

        /// <summary>
        /// 获取长度
        /// </summary>
        public double GetDistanceTo(double[] xyzs)
        {
            // √(x²+y²+z²)
            double a = 0.0,
                   b = 0.0,
                   c = 0.0;
            if (xyzs.Length > 0)
                a = xyzs[0];
            if (xyzs.Length > 1)
                b = xyzs[1];
            if (xyzs.Length > 2)
                c = xyzs[2];
            a = _X - a;
            b = _Y - b;
            c = _Z - c;
            return Math.Sqrt(a * a + b * b + c * c);
        }

        /// <summary>
        /// 获取单位向量
        /// </summary>
        /// <returns></returns>
        /// https://www.bilibili.com/video/BV1qb411M7wL?from=search&seid=9280697047969917119
        public PointV GetUnitNormal()
        {
            //向量/模长  cad是GetNormal()
            var ob_length = Origin.GetDistanceTo(this);
            return this / ob_length;
        }

        /// <summary>
        /// 三角形顶点坐标
        /// </summary>
        /// <param name="o">旋转点</param>
        /// <param name="a">勾</param>
        /// <param name="b">股</param>
        /// <param name="c">弦</param>
        /// <param name="al">返回角度</param>
        /// <returns></returns>
        public PointV TriangleVertices(double a, double b, double c, out double al)
        {
            var o = this;
            al = Math.Acos((b * b + c * c - a * a) / (2 * b * c));   //余弦公式求角度
            double h = b * Math.Sin(al);                             //顶点高
            double w = (b * b + c * c - a * a) / (2 * c);            //顶点宽
            return new PointV(o._X + w, o._Y + h, o._Z);             //三角形顶点坐标
        }

        /// <summary>
        /// 通过两点获取中点
        /// </summary>
        /// <param name="p1">头点</param>
        /// <param name="p2">尾点</param>
        /// <returns>腰点</returns>
        public PointV GetCenter(PointV p2)
        {
            var p1 = this;
            // (p1 + p2) / 2; 溢出风险
            return new PointV(p1._X / 2.0 + p2._X / 2.0,
                              p1._Y / 2.0 + p2._Y / 2.0,
                              p1._Z / 2.0 + p2._Z / 2.0,
                              (int)p1._S | (int)p2._S);
            //整数才可以用这个 (a & b) + (a ^ b) / 2;
        }

        /// <summary>
        /// 极角坐标
        /// </summary>
        /// <param name="pt">起点</param>
        /// <param name="alz">角度</param>
        /// <param name="dist">起点x增量</param>
        /// <returns></returns>
        public PointV Polar(double alz, double dist)
        {
            return new PointV(_X + dist, _Y, _Z, _S).RotateBy(alz, ZAxis, this);
        }

        /// <summary>
        /// 旋转
        /// </summary>
        /// <param name="angle">角度</param>
        /// <param name="vector">任意转轴</param>
        /// <param name="centerPoint">绕点</param>
        /// <returns></returns>
        public PointV RotateBy(double angle, PointV vector, PointV? centerPoint = null)
        {
            if (centerPoint is null)
                centerPoint = Origin;

            var roMat = MatrixTransform.GetRotateTransform(angle, vector);
            var pt = this - centerPoint; //绕点旁边平移到原点旁边
            pt = MatrixTransform.WarpRotate(pt, roMat);
            pt += centerPoint; //原点旁边平移到绕点旁边
            pt._S = this._S;
            return pt;
        }

        /// <summary>
        /// 点在闭合多段线内,水平射线法
        /// </summary>
        /// <param name="p">判断的点</param>
        /// <param name="pts">边界点集</param>
        /// <param name="onBoundary">在边界上算不算</param>
        /// <param name="tolerance">容差</param>
        /// <returns></returns>
        public bool InBoundary(IEnumerable<PointV> pts, bool onBoundary = true, double tolerance = 1e-6)
        {
            /// <summary>
            /// 首尾相连
            /// </summary>
            /// <param name="pts"></param>
            /// <returns></returns>
            static List<T>? End2End<T>(IEnumerable<T>? pts)
            {
                if (pts is null)
                    return null;

                var lst = pts.ToList();
                var first = lst[0];
                if (first is null)
                    return lst;
                var last = lst[lst.Count - 1];
                if (last is null)
                    return lst;
                if (first.Equals(last))
                    lst.Add(first);
                return lst;
            }

            var lst = End2End(pts);
            return InBoundary(lst, onBoundary, tolerance);
        }

        /// <summary>
        /// 点在闭合多段线内,水平射线法
        /// </summary>
        /// <param name="p">判断的点</param>
        /// <param name="pts">边界点集</param>
        /// <param name="onBoundary">在边界上算不算</param>
        /// <param name="tolerance">容差</param>
        /// <returns></returns>
        public bool InBoundary(List<PointV>? pts, bool onBoundary = true, double tolerance = 1e-6)
        {
            bool Eq(double a, double b)
            {
                return Math.Abs(a - b) <= tolerance;
            }
            if (pts is null)
                throw new ArgumentNullException(nameof(pts));

            var p = this;
            var flag = false;
            var x = p._X;
            var y = p._Y;

            for (var i = 0; i < pts.Count - 1; i++)
            {
                var x1 = pts[i]._X;//头
                var y1 = pts[i]._Y;
                var x2 = pts[i + 1]._X;//尾
                var y2 = pts[i + 1]._Y;

                // 点与多边形顶点重合
                if ((Eq(x1, x) && Eq(y1, y)) || Eq(x2, x) && Eq(y2, y))
                {
                    flag = true;
                    break;
                }
                // 子段端点是否在水平射线两侧(都在下面 || 都在上面)
                if ((y1 < y && y2 >= y) || (y1 >= y && y2 < y))
                {
                    //射线穿过子段时,得出交点为(ox,y)
                    var derivative = (x2 - x1) / (y2 - y1);  //导数.斜率
                    var high = y - y1;                       //高
                    var ox = x1 + high * derivative;

                    // 点在多边形的边上
                    if (Eq(ox, x) && onBoundary)
                    {
                        flag = true;
                        break;
                    }
                    // 射线穿过多边形的边界
                    if (ox > x && onBoundary)
                        flag = !flag;
                }
            }
            return flag; // 射线穿过多边形边界的次数为奇数时点在多边形内
        }

        public bool IsZeroLength()
        {
            return this.DistanceTo(PointV.Origin) == 0;
        }

#if true33 //这个代码测试起来有问题
        // https://www.cnblogs.com/huangshuqiang/p/8808126.html#:~:text=%E8%A6%81%E5%88%A4%E6%96%AD%E7%82%B9%E6%98%AF%E5%90%A6%E5%9C%A8%E5%A4%9A,%E4%B9%9F%E4%B8%BA%E5%81%B6%E6%95%B0%E4%B8%AA%E4%BA%A4%E7%82%B9%E3%80%82
        /// 判断点是否在多边形内.
        public bool IsInPolygon(PointV[] pts)
        {
            var p = this;
            bool inside = false;
            int ptCount = pts.Length;
            PointV p1, p2;
            for (int i = 0, j = ptCount - 1; i < ptCount; j = i, i++)
            {
                p1 = pts[i];
                p2 = pts[j];
                if (p.Y < p2.Y)
                {
                    if (p1.Y <= p.Y)
                    {
                        if ((p.Y - p1.Y) * (p2.X - p1.X) > (p.X - p1.X) * (p2.Y - p1.Y))
                            inside = !inside;
                    }
                }
                else if (p.Y < p1.Y)
                {
                    if ((p.Y - p1.Y) * (p2.X - p1.X) < (p.X - p1.X) * (p2.Y - p1.Y))
                        inside = !inside;
                }
            }
            return inside;
        }
#endif
        #endregion

        #region 求三维夹角
        /// <summary>
        /// 两向量夹角
        /// </summary>
        /// <param name="ptA">点</param>
        /// <param name="ptB">点</param>
        /// <returns>弧度,求的永远是偏小的角</returns>
        public double GetAngleTo(PointV ptA, PointV ptB, Conversion.AngleUnit angle = Conversion.AngleUnit.Angle)
        {
            var ptO = this;
            return ptO.GetVectorTo(ptA).GetAngleTo(ptO.GetVectorTo(ptB), angle);
        }

        /// <summary>
        /// 两向量夹角
        /// </summary>
        /// <param name="vectorB">向量</param>
        /// <returns>弧度,求的永远是偏小的角</returns>
        //向量夹角 https://www.bilibili.com/video/BV1Pb411J7xJ?p=8
        //https://blog.csdn.net/luols/article/details/7476559
        public double GetAngleTo(PointV vectorB, Conversion.AngleUnit angle = Conversion.AngleUnit.Angle)
        {
            var vectorA = this;
            if (vectorA._S != 0 || vectorB._S != 0)
                throw new ArgumentNullException("GetAngleTo变量并非向量");

            //这里和点乘(构造一个直角三角形)再运算是一样
            var c = vectorA * vectorB;
            var cosfi = c._X + c._Y + c._Z;
            var norm = vectorA.GetDistanceTo(Origin) * vectorB.GetDistanceTo(Origin);
            cosfi /= norm;//cos夹角==正值为锐角,负值为钝角

            if (cosfi >= 1.0)
                return 0;
            if (cosfi <= -1.0)
                return Math.PI;

            var fi = Math.Acos(cosfi);
            if (angle == Conversion.AngleUnit.Degree)
            {
                fi = Conversion.AngleToDegree(fi);
                if (fi > 180)
                    fi = 360 - fi;
            }
            return fi;
        }

#if true2
        //[CommandMethod("CmdTest_al")]
        //public void CmdTest_al()
        //{
        //    var a1 = PointV.XAxis.GetAngleTo(PointV.Origin.GetVectorTo(new PointV(1, 1, 0)));
        //    var a2 = PointV.YAxis.GetAngleTo(PointV.Origin.GetVectorTo(new PointV(0, 1, 1)));
        //    var a3 = PointV.ZAxis.GetAngleTo(PointV.Origin.GetVectorTo(new PointV(1, 0, 1)));
        //}
        public double GetAngle2ZAxis(PointV endtPoint, double tolerance = 1e-6)
        {
            var startPoint = this;
            //防止向量z轴有值造成检测角度不是一个平面
            var pt1 = startPoint.Clone();
            pt1.X = 0;
            var pt2 = endtPoint.Clone();
            pt2.X = 0;
            return pt1.GetVectorTo(pt2).GetAngle2ZAxis(tolerance);
        }
        public double GetAngle2ZAxis(double tolerance = 1e-6)
        {
            var ve = this;
            //世界重合到用户 Vector3d.XAxis->两点向量
            double al = ZAxis.GetAngleTo(ve);         //观察方向不要设置
            al = ve.X > 0 ? al : Tau - al;            //逆时针为正,如果-负值控制正反
            al = Math.Abs(Tau - al) <= tolerance ? 0 : al;
            return al;
        }

        public double GetAngle2YAxis(PointV endtPoint, double tolerance = 1e-6)
        {
            var startPoint = this;
            //防止向量z轴有值造成检测角度不是一个平面
            var pt1 = startPoint.Clone();
            pt1.Y = 0;
            var pt2 = endtPoint.Clone();
            pt2.Y = 0;
            return pt1.GetVectorTo(pt2).GetAngle2YAxis(tolerance);
        }
        public double GetAngle2YAxis(double tolerance = 1e-6)
        {
            var ve = this;
            //世界重合到用户 Vector3d.XAxis->两点向量
            double al = YAxis.GetAngleTo(ve);         //观察方向不要设置
            al = ve.Z > 0 ? al : Tau - al;            //逆时针为正,如果-负值控制正反
            al = Math.Abs(Tau - al) <= tolerance ? 0 : al;
            return al;
        }
#endif

        /// <summary>
        /// 通过两点获取与X轴的弧度,会超一个pi(上小,下大)
        /// </summary>
        /// <param name="startPoint">头点</param>
        /// <param name="endtPoint">尾点</param>
        /// <returns>X轴到向量的弧度</returns>
        public double GetAngle2XAxis(PointV endtPoint, double tolerance = 1e-6)
        {
            var startPoint = this;
            //防止向量z轴有值造成检测角度不是一个平面
            var pt1 = new PointV(startPoint._X, startPoint._Y, 0, startPoint._S);
            var pt2 = new PointV(endtPoint._X, endtPoint._Y, 0, endtPoint._S);
            return pt1.GetVectorTo(pt2).GetAngle2XAxis(tolerance);
        }

        /// <summary>
        /// X轴到向量的弧度,cad的获取的弧度是1PI,所以转换为2PI(上小,下大)
        /// </summary>
        /// <param name="ve">向量</param>
        /// <returns>X轴到向量的弧度</returns>
        public double GetAngle2XAxis(double tolerance = 1e-6)
        {
            var ve = this;
            //世界重合到用户 Vector3d.XAxis->两点向量
            double al = XAxis.GetAngleTo(ve);
            al = ve._Y > 0 ? al : Tau - al; //逆时针为正,大于0是上半圆,小于则是下半圆,如果-负值控制正反
            al = Math.Abs(Tau - al) <= tolerance ? 0 : al;
            return al;
        }


        /// 原理见:http://www.cnblogs.com/graphics/archive/2012/08/10/2627458.html
        /// 以及:http://www.doc88.com/p-786491590188.html
        /// <summary>
        /// 输入一个法向量(用户Z轴),获取它与世界坐标X轴和Y轴的夹角,
        /// 旋转这两个夹角可以重合世界坐标的Z轴
        /// </summary>
        /// <param name="userZxis">用户坐标系的Z轴</param>
        /// <param name="alx">输出X轴夹角</param>
        /// <param name="aly">输出Y轴夹角</param>
        public void ToWcsZAxisAngles(out double alx, out double aly, double tolerance = 1e-6)
        {
            var userZxis = this;
            //处理精度
            double X = Math.Abs(userZxis._X) <= tolerance ? 0 : userZxis._X;
            double Y = Math.Abs(userZxis._Y) <= tolerance ? 0 : userZxis._Y;
            double Z = Math.Abs(userZxis._Z) <= tolerance ? 0 : userZxis._Z;

            //YOZ面==旋转X轴..这是投影的
            //旋转轴重合原点0,才能令用户 ZY 面就是重合到世界坐标系,从而求出 ZY 面的夹角
            var px = new PointV(0, Y, Z);//旋转
            if (px != PointV.Origin)
            {
                var oq = PointV.Origin.GetVectorTo(px);
                alx    = PointV.ZAxis.GetAngleTo(oq);
                alx    = oq._Y > 0 ? alx : Tau - alx;
                alx    = Math.Abs(Tau - alx) <= tolerance ? 0 : alx;
            }
            else
                alx = 0;

            //XOZ面==旋转Y轴
            var py = new PointV(X, 0, Math.Pow(Y * Y + Z * Z, 0.5));//投影点
            if (py != PointV.Origin)
            {
                var or = PointV.Origin.GetVectorTo(py);
                aly    = PointV.ZAxis.GetAngleTo(or);
                aly    = or._X < 0 ? aly : Tau - aly;
                aly    = Math.Abs(Tau - aly) <= tolerance ? 0 : aly;
            }
            else
                aly = 0;
        }
        #endregion

        #region 坐标系变换
        PointV Wcs2Ucs(CoordinateSystemV ucs)
        {
            var pt = this;
            ucs.ToWcsAngles(out double alx, out double aly, out double alz);

            pt = new PointV(pt._X - ucs.Origin._X,
                            pt._Y - ucs.Origin._Y,
                            pt._Z - ucs.Origin._Z);

            pt = pt.RotateBy(alx, XAxis, PointV.Origin)
                   .RotateBy(aly, YAxis, PointV.Origin)
                   .RotateBy(alz, ZAxis, PointV.Origin);
            return pt;
        }
        PointV Ucs2Wcs(CoordinateSystemV ucs)
        {
            var pt = this;
            ucs.ToWcsAngles(out double alx, out double aly, out double alz);

            //pt数据直接放在世界坐标系上,此时和用户坐标系的pt不重合,
            //进行逆旋转重合到 用户pt 的向量上
            pt = pt.RotateBy(-alz, ZAxis, PointV.Origin)
                   .RotateBy(-aly, YAxis, PointV.Origin)
                   .RotateBy(-alx, XAxis, PointV.Origin);
            //平移就会是世界坐标系的点重合到用户坐标系的点
            pt = new PointV(pt._X + ucs.Origin._X,
                            pt._Y + ucs.Origin._Y,
                            pt._Z + ucs.Origin._Z);
            return pt;
        }
        /// <summary>
        /// 把一个点从一个坐标系统变换到另外一个坐标系统
        /// </summary>
        /// <param name="userPt">来源点</param>
        /// <param name="source">来源坐标系</param>
        /// <param name="target">目标坐标系</param>
        /// <returns>目标坐标系的点</returns>
        public PointV Transform(CoordinateSystemV source, CoordinateSystemV target)
        {
            var userPt = this;
            if (Equals(source, target))
                return userPt;
            //世界坐标是独特的
            var wcs = CoordinateSystemV.WCS;
            PointV pt;
            if (Equals(target, wcs))//目标是wcs,那么前面去掉了相同的,所以来源是ucs
                pt = userPt.Ucs2Wcs(source);
            else if (Equals(source, wcs))//目标是ucs
                pt = userPt.Wcs2Ucs(target);
            else //用户转用户
                pt = userPt.Ucs2Wcs(source).Wcs2Ucs(target);
            return pt;
        }

        /// <summary>
        /// 斜率
        /// </summary>
        /// <param name="p2"></param>
        public double Slope(PointV p2, double tolerance = 1e-6)
        {
            var y = Math.Abs(_Y - p2._Y);
            var x = Math.Abs(_X - p2._X);
            if (y < tolerance || x < tolerance)
                return 0;
            return y / x;
        }

        #endregion


#if true3
        //定比分点公式
        //二维情况：
        //对于线段AB，A(x1,y1)为起点，B(x2,y2)为终点，且AC/AB=t；求线段C(x,y)的坐标。
        //x = (1-t)*x1+t*x2
        //y = (1-t)*y1+t*y2

        //三维情况：
        //对于线段AB，A(x1,y1,z1)为起点，B(x2,y2,z2)为终点，且AC/AB=t；求线段C(x,y,z)的坐标。
        //x = (1-t)*x1+t*x2
        //y = (1-t)*y1+t*y2
        //z = (1-t)*z1+t*z2

        /// <summary>
        /// 定比分点公式
        /// </summary>
        /// <param name="a">A点</param>
        /// <param name="b">B点</param>
        /// <param name="aptToPpt">AC长度,C点在AB之间</param>
        /// <returns>求C点</returns>
        public static PointV ScorePointFormula(PointV a, PointV b, double aptToPpt)
        {
            var length = a.DistanceTo(b);
            var m = aptToPpt;
            var n = length - m;
            //需要交叉相乘
            var nSc = (n / length);
            var mSc = (m / length);
            var x = nSc * a.X + mSc * b.X;
            var y = nSc * a.Y + mSc * b.Y;
            var z = nSc * a.Z + mSc * b.Z;
            return new PointV(x, y, z);
        }
#endif
    }
}