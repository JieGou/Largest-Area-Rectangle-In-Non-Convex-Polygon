using System;
using System.Runtime.InteropServices;
using System.Text;

namespace JoinBox.BasalMath
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public class CoordinateSystemV : IFormattable, IEquatable<CoordinateSystemV>
    {
        #region 成员
        public PointV _ZAxis;
        public PointV _YAxis;
        public PointV _XAxis;
        public PointV _Origin;

        public PointV ZAxis { get => _ZAxis; }
        public PointV YAxis { get => _YAxis; }
        public PointV XAxis { get => _XAxis; }
        public PointV Origin { get => _Origin; }
        public static CoordinateSystemV WCS = new(PointV.Origin, PointV.XAxis, PointV.YAxis);
        #endregion

        #region 构造
        public CoordinateSystemV(PointV origin, PointV xAxis, PointV yAxis)
        {
            _Origin = origin;
            _XAxis  = xAxis;
            _YAxis  = yAxis;
            _ZAxis  = xAxis.CrossProductNormal(yAxis);
        }
        #endregion

        #region 重载运算符_比较
        public override bool Equals(object? obj)
        {
            return this == obj as CoordinateSystemV;
        }
        public bool Equals(CoordinateSystemV? b)
        {
            return this == b;
        }
        public static bool operator !=(CoordinateSystemV? a, CoordinateSystemV? b)
        {
            return !(a == b);
        }
        public static bool operator ==(CoordinateSystemV? a, CoordinateSystemV? b)
        {
            //此处地方不允许使用==null,因为此处是定义
            if (b is null)
                return a is null;
            else if (a is null)
                return false;
            if (ReferenceEquals(a, b))//同一对象
                return true;

            return a.Equals(b, 0);
        }
        public bool Equals(CoordinateSystemV? b, double tolerance = 0)
        {
            if (b is null)
                return false;
            if (ReferenceEquals(this, b)) //同一对象
                return true;

            return _Origin.Equals(b._Origin, false, tolerance) &&
                   _XAxis.Equals(b._XAxis, false, tolerance) &&
                   _YAxis.Equals(b._YAxis, false, tolerance) &&
                   _ZAxis.Equals(b._ZAxis, false, tolerance);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region 转换类型
#if NET35 || NET40 || NET45 //net50在其他测试
        public static implicit operator CoordinateSystemV(Autodesk.AutoCAD.Geometry.CoordinateSystem3d csd)
        {
            return new CoordinateSystemV(csd.Origin, csd.Xaxis, csd.Yaxis);
        }
#endif
        public CoordinateSystemV Clone()
        {
            return new CoordinateSystemV(_Origin, _XAxis, _YAxis);
        }


        /// <summary>
        /// 格式化调用
        /// </summary>
        /// <param name="format"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
        {
            return ToString(format, formatProvider);
        }

        /// <summary>
        /// 无参数调用
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(null, null);
        }

        /// <summary>
        /// 有参调用
        /// </summary>
        /// <param name="format"></param>
        /// <param name="formatProvider"></param>
        /// <returns></returns>
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            var array = new string[]
            {
                _Origin.ToString(format, formatProvider),
                _XAxis.ToString(format, formatProvider),
                _YAxis.ToString(format, formatProvider),
                _ZAxis.ToString(format, formatProvider),
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
        #endregion

        #region 方法
        /// <summary>
        /// 三个轴角度
        /// 重合两个坐标系的Z轴,求出三个轴旋转角度,
        /// 后续利用旋转角度正负和次序来变换用户和世界坐标系
        /// </summary>
        /// <param name="ucs">用户坐标系</param>
        /// <param name="alx">坐标系间的X轴旋转角度</param>
        /// <param name="aly">坐标系间的Y轴旋转角度</param>
        /// <param name="alz">坐标系间的Z轴旋转角度</param>
        public void ToWcsAngles(out double alx, out double aly, out double alz)
        {
            var ucs = this;
            //ucs可能带有新原点,设置0重合到世界坐标系原点
            var ucs2o = new CoordinateSystemV(PointV.Origin, ucs._XAxis, ucs._YAxis);
            //XY轴通过叉乘求得Z轴
            ucs2o._ZAxis.ToWcsZAxisAngles(out alx, out aly);
            //使用户X轴与世界XOY面共面,求出Z轴旋转角
            var newXa = ucs2o._XAxis.RotateBy(alx, PointV.XAxis)
                                   .RotateBy(aly, PointV.YAxis);
            newXa.S = 0;
            alz = -newXa.GetAngle2XAxis();
        }

        //求旋转矩阵,貌似还是三个夹角比较直观
        //public Matrix Rotation3d()
        //{
        //    ToWcsAngles(out double alx, out double aly, out double alz);
        //    var matX = MatrixTransform.GetRotateTransform(alx, XAxis);
        //    var matY = MatrixTransform.GetRotateTransform(aly, YAxis);
        //    var matZ = MatrixTransform.GetRotateTransform(alz, ZAxis);
        //    return matZ * matY * matX;
        //}

        /// <summary>
        /// 用户坐标系的两点,形成ucs.X轴
        /// 旋转此轴,旋转90度,构成ucs.Y轴
        /// </summary>
        /// <param name="v1">点1,形成ucs.X轴的点</param>
        /// <param name="v2">点2,形成ucs.X轴的点</param>
        public CoordinateSystemV XAxisBulid(PointV v1, PointV v2)
        {
            var vX = v1.GetVectorTo(v2);//形成的是X轴
            var vY = vX.RotateBy(Constant.PiHalf, _ZAxis, PointV.Origin);
            return new CoordinateSystemV(PointV.Origin, vX.GetUnitNormal(), vY.GetUnitNormal());//叉乘求Z轴
        }

        /// <summary>
        /// 从观察方向获取坐标系(就是通过dcs.X轴,求出dcs.Y dcs.Z)
        /// </summary>
        /// <param name="xve">X方向</param>
        /// <returns></returns>
        public static CoordinateSystemV XBuild(PointV xve)
        {
            PointV xAxis, yAxis;//, zAxis;
            xAxis = xve.GetUnitNormal();
            yAxis = PointV.XAxis.CrossProductNormal(xAxis);
            if (!yAxis.IsZeroLength())//不同方向执行(一般是这样),同方向叉乘是0长度
            {
                yAxis = yAxis.GetUnitNormal();
                //zAxis = xAxis.CrossProductNormal(yAxis);
            }
            else if (xAxis.X < 0)//同WCS的
            {
                xAxis = -PointV.XAxis;
                yAxis = PointV.YAxis;
                //zAxis = -PointV.ZAxis;
            }
            else
            {
                xAxis = PointV.XAxis;
                yAxis = PointV.YAxis;
                //zAxis = PointV.ZAxis;
            }
            return new CoordinateSystemV(PointV.Origin, xAxis, yAxis);
        }

        /// <summary>
        /// 从观察方向获取坐标系(就是通过dcs.Z轴,求出dcs.X dcs.Y)
        /// </summary>
        /// <param name="viewDirection">观察方向</param>
        /// <returns>DCS</returns>
        public static CoordinateSystemV ZBuild(PointV viewDirection)
        {
            PointV zAxis, xAxis, yAxis;
            zAxis = viewDirection.GetUnitNormal();
            xAxis = PointV.ZAxis.CrossProductNormal(zAxis);
            if (!xAxis.IsZeroLength())//不同方向执行(一般是这样),同方向叉乘是0长度
            {
                xAxis = xAxis.GetUnitNormal();
                yAxis = zAxis.CrossProductNormal(xAxis);
            }
            else if (zAxis.Z < 0)//同WCS的
            {
                xAxis = -PointV.XAxis;
                yAxis = PointV.YAxis;
                //zAxis = -PointV.ZAxis;
            }
            else
            {
                xAxis = PointV.XAxis;
                yAxis = PointV.YAxis;
                //zAxis = PointV.ZAxis;
            }
            return new CoordinateSystemV(PointV.Origin, xAxis, yAxis);
        }




        #endregion
    }
}