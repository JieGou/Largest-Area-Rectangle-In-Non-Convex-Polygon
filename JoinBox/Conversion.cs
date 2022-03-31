using System;

namespace JoinBox.BasalMath
{
    public static partial class Conversion
    {
        public enum AngleUnit
        {
            Angle,
            Degree,
        }

        /// <summary>
        /// 角度转弧度
        /// </summary>
        /// <param name="degree">角度</param>
        /// <returns>弧度</returns>
        public static double DegreeToAngle(double degree)
        {
            return degree * Math.PI / 180;
        }

        /// <summary>
        /// 弧度转角度
        /// </summary>
        /// <param name="angle">弧度</param>
        /// <returns>角度</returns>
        public static double AngleToDegree(double angle)
        {
            return angle * 180 / Math.PI;
        }


        /// <summary>
        /// 若数值大于PI2(一个圆),则把数值设置回一个圆内
        /// </summary>
        /// <param name="angle">弧度</param>
        /// <returns>一个圆内的弧度</returns>
        public static double SetInsideScopeTwoPi(double angle)
        {
            int tmp = (int)(angle / Constant.PiHalf);
            return Math.Abs(angle - (tmp * Constant.Tau));
        }
    }
}