using System;
using System.Collections.Generic;

namespace JoinBox.BasalMath
{
    //透视矩阵
    //参考视频 https://www.bilibili.com/video/BV1VE411N7fp?from=search&seid=12136077390854982191
    //参考文章 http://www.hanyeah.com/blog/post/%E5%9B%9B%E9%A1%B6%E7%82%B9%E6%A0%A1%E6%AD%A3%E9%80%8F%E8%A7%86%E5%8F%98%E6%8D%A2%E7%9A%84%E7%BA%BF%E6%80%A7%E6%96%B9%E7%A8%8B%E8%A7%A3.html
    public partial class MatrixTransform
    {
        /// <summary>
        /// 透视矩阵
        /// </summary>
        /// <param name="src">来源边界(必须4个点,多于则需要拆解边界为4)</param>
        /// <param name="dst">目标边界(必须4个点,多于则需要拆解边界为4)</param>
        /// <returns>3*3矩阵</returns>
        public static Matrix GetPerspectiveTransform(PointV[] src, PointV[] dst)
        {
            Matrix xMat;
            {
                var a = new double[8, 8];
                var b = new double[8, 1];

                //摘录自openCV的代码段 https://github.com/opencv/opencv/blob/master/modules/imgproc/src/imgwarp.cpp
                //循环四次,每次加4个表示,i+2行放在i+4行,也就是数组一半的位置
                //我把它叫做二分矩阵
                for (int i = 0; i < 4; ++i)
                {
                    var ii   = i + 4;
                    a[i, 0]  = a[ii, 3] = src[i].X;
                    a[i, 1]  = a[ii, 4] = src[i].Y;
                    a[i, 2]  = a[ii, 5] = 1;
                    a[i, 3]  = a[i, 4] = a[i, 5] = a[ii, 0] = a[ii, 1] = a[ii, 2] = 0;
                    a[i, 6]  = -src[i].X * dst[i].X;
                    a[i, 7]  = -src[i].Y * dst[i].X;
                    a[ii, 6] = -src[i].X * dst[i].Y;
                    a[ii, 7] = -src[i].Y * dst[i].Y;
                    b[i, 0]  = dst[i].X;
                    b[ii, 0] = dst[i].Y;
                }

                var aMatrix = new Matrix(a);
                var bMatrix = new Matrix(b);

                //把二分矩阵 转为 错开矩阵
                aMatrix.DetachmentEvenLineOddLine();
                bMatrix.DetachmentEvenLineOddLine();

                //求结果x矩阵8*1,也就是全局变换系数
                xMat = aMatrix.Inverse() * bMatrix;
            }

            Matrix rMat;
            {
                //填充矩阵:将8*1转为3*3矩阵
                var a = xMat[0, 0]; //m0
                var b = xMat[1, 0]; //m1
                var c = xMat[2, 0]; //m2
                var d = xMat[3, 0]; //m3
                var e = xMat[4, 0]; //m4
                var f = xMat[5, 0]; //m5
                var g = xMat[6, 0]; //m6
                var h = xMat[7, 0]; //m7

                var mat3 = new double[3, 3]
                {
                    { a,b,c },
                    { d,e,f },
                    { g,h,1 }
                };
                rMat = new Matrix(mat3);
            }

            return rMat;
        }

        /// <summary>
        /// 应用透视变换矩阵
        /// </summary>
        /// <param name="pts">来源边界内的图形点集</param>
        /// <param name="matrix">3*3矩阵</param>
        /// <returns>变换后的点集</returns>
        public static PointV[] WarpPerspective(PointV[] pts, Matrix matrix)
        {
#if DEBUG
            if (matrix.Rows != matrix.Cols || matrix.Cols != 3)
                throw new ArgumentNullException("WarpPerspective矩阵大小不对");
#endif
            var a = matrix[0, 0]; //m0
            var b = matrix[0, 1]; //m1
            var c = matrix[0, 2]; //m2
            var d = matrix[1, 0]; //m3
            var e = matrix[1, 1]; //m4
            var f = matrix[1, 2]; //m5
            var g = matrix[2, 0]; //m6
            var h = matrix[2, 1]; //m7
            var j = matrix[2, 2]; //m8

            var outPts = new PointV[pts.Length];
            for (int i = 0; i < pts.Length; i++)
            {
                var x     = pts[i].X;
                var y     = pts[i].Y;

                var www   = g * x + h * y + j;
                var u     = (a * x + b * y + c) / www;
                var v     = (d * x + e * y + f) / www;

                outPts[i] = new PointV(u, v);
            }
            return outPts;
        }
    }

    //旋转矩阵
    //代码来自: https://www.cnblogs.com/graphics/archive/2012/08/10/2627458.html
    public partial class MatrixTransform
    {
        /// <summary>
        /// 旋转矩阵
        /// </summary>
        /// <param name="angle">角度</param>
        /// <param name="axis">任意旋转轴</param>
        /// <returns></returns>
        public static Matrix GetRotateTransform(double angle, PointV axis)
        {
            angle = -angle;//保证是逆时针
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);
            var cosMinus = 1 - cos;

            axis = axis.GetUnitNormal();
            var u = axis.X;
            var v = axis.Y;
            var w = axis.Z;

            var pOut = new double[4, 4];
            {
                pOut[0, 0] = cos + u * u * cosMinus;
                pOut[0, 1] = u * v * cosMinus + w * sin;
                pOut[0, 2] = u * w * cosMinus - v * sin;
                pOut[0, 3] = 0;

                pOut[1, 0] = u * v * cosMinus - w * sin;
                pOut[1, 1] = cos + v * v * cosMinus;
                pOut[1, 2] = w * v * cosMinus + u * sin;
                pOut[1, 3] = 0;

                pOut[2, 0] = u * w * cosMinus + v * sin;
                pOut[2, 1] = v * w * cosMinus - u * sin;
                pOut[2, 2] = cos + w * w * cosMinus;
                pOut[2, 3] = 0;

                pOut[3, 0] = 0;
                pOut[3, 1] = 0;
                pOut[3, 2] = 0;
                pOut[3, 3] = 1;
            }
            return new Matrix(pOut);
        }

        /// <summary>
        /// 应用旋转矩阵
        /// </summary>
        /// <param name="pts">点集</param>
        /// <param name="rotateMat">旋转矩阵</param>
        /// <returns></returns>
        public static PointV[] WarpRotate(PointV[] pts, Matrix rotateMat)
        {
#if DEBUG
            if (rotateMat.Rows != rotateMat.Cols || rotateMat.Cols != 4)
                throw new ArgumentNullException("WarpRotate矩阵大小不对");
#endif
            var outPts = new PointV[pts.Length];
            for (int i = 0; i < pts.Length; i++)
                outPts[i] = SetRotateMat(pts[i], rotateMat);
            return outPts;
        }

        /// <summary>
        /// 应用旋转矩阵
        /// </summary>
        /// <param name="ptItem">点</param>
        /// <param name="rotateMat">旋转矩阵</param>
        /// <returns></returns>
        public static PointV WarpRotate(PointV ptItem, Matrix rotateMat)
        {
#if DEBUG
            if (rotateMat.Rows != rotateMat.Cols || rotateMat.Cols != 4)
                throw new ArgumentNullException("WarpRotate矩阵大小不对");
#endif
            return SetRotateMat(ptItem, rotateMat);
        }

        static PointV SetRotateMat(PointV ptItem, Matrix rotateMat)
        {
            //数学矩阵末尾是1,但是向量末尾0旋转也不会出错,测试通过
            var ptRo = rotateMat * new Matrix(ptItem.ToArray());
            return new PointV(ptRo.ToArray());
        }
    }


    //平移矩阵
    public partial class MatrixTransform
    {
        /// <summary>
        /// 平移矩阵
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Matrix GetDisplacementTransform(PointV vector)
        {
            double tx = vector.X;
            double ty = vector.Y;
            double tz = vector.Z;

            var arr = new double[4, 4];
            {
                arr[0, 0] = 1;
                arr[0, 1] = 0;
                arr[0, 2] = 0;
                arr[0, 3] = tx;

                arr[1, 0] = 0;
                arr[1, 1] = 1;
                arr[1, 2] = 0;
                arr[1, 3] = ty;

                arr[2, 0] = 0;
                arr[2, 1] = 0;
                arr[2, 2] = 1;
                arr[2, 3] = tz;

                arr[3, 0] = 0;
                arr[3, 1] = 0;
                arr[3, 2] = 0;
                arr[3, 3] = 1;
            }
            return new Matrix(arr);
        }

        /// <summary>
        /// 应用平移矩阵
        /// </summary>
        /// <param name="ptItem">点</param>
        /// <param name="displacementMat">旋转矩阵</param>
        /// <returns></returns>
        public static PointV WarpDisplacement(PointV ptItem, Matrix displacementMat)
        {
#if DEBUG
            if (displacementMat.Rows != displacementMat.Cols || displacementMat.Cols != 4)
                throw new ArgumentNullException("WarpRotate矩阵大小不对");
#endif
            var ptRo = displacementMat * new Matrix(ptItem.ToArray());
            return new PointV(ptRo.ToArray());
        }
    }

    //对齐矩阵
    public partial class MatrixTransform
    {
        /// <summary>
        /// 对齐矩阵_没验证
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static Matrix AlignCoordinateSystem(CoordinateSystemV source, CoordinateSystemV target)
        {
            //平移到世界0
            var matMoveToWCS0 = GetDisplacementTransform(-source.Origin);

            //基变换 source平移到世界0,然后旋转alz,aly,alx
            Matrix mat1;
            {
                source.ToWcsAngles(out double alx, out double aly, out double alz);
                var matrixRoX = GetRotateTransform(alx, PointV.XAxis);
                var matrixRoY = GetRotateTransform(aly, PointV.YAxis);
                var matrixRoZ = GetRotateTransform(alz, PointV.ZAxis);
                mat1 = matrixRoX * matrixRoY * matrixRoZ; //这里 顺序 和 角度正负 没有验证
            }
            //此时重合wcs,再从wcs旋转目标坐标系三个夹角,平移到目标原点
            Matrix mat2;
            {
                target.ToWcsAngles(out double alx, out double aly, out double alz);
                var matrixRoX = GetRotateTransform(-alx, PointV.XAxis);
                var matrixRoY = GetRotateTransform(-aly, PointV.YAxis);
                var matrixRoZ = GetRotateTransform(-alz, PointV.ZAxis);
                mat2 = matrixRoZ * matrixRoY * matrixRoX;
            }
            //世界0平移出去
            var matWCS0To = GetDisplacementTransform(target.Origin);

            return matWCS0To * mat2 * mat1 * matMoveToWCS0;//左乘
        }

        //应用对齐矩阵
    }

    //其他cad矩阵
    //https://www.cnblogs.com/rf8862/p/15236609.html
}
