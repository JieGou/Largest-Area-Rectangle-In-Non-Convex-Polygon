using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

// 矩阵乘法 https://www.bilibili.com/video/BV1QU4y1V7PJ?from=search&seid=4674962908957462089
// 矩阵转置 https://www.bilibili.com/video/BV1jh411e7Vb?from=search&seid=4674962908957462089
// 矩阵求逆 https://www.bilibili.com/video/BV1iE411w7vt
// 本代码来自 https://www.cnblogs.com/wzxwhd/p/5877624.html

namespace JoinBox.BasalMath
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public class Matrix : IFormattable, IEquatable<Matrix>
    {
        #region 成员
        /// <summary>
        /// 储存位置
        /// </summary>
        double[] _element;
        /// <summary>
        /// 获取矩阵行数
        /// </summary>
        public int Rows { get; private set; } = 0;
        /// <summary>
        /// 获取矩阵列数
        /// </summary>
        public int Cols { get; private set; } = 0;
        #endregion

        #region 构造
        /// <summary>
        /// 矩阵
        /// </summary>
        /// <param name="m">二维数组</param>
        public Matrix(double[][] m)
        {
            Rows = m.GetLength(0);
            Cols = m.GetLength(1);
            _element = new double[Rows * Cols];
            int count = 0;
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    _element[count++] = m[i][j];
                }
            }
        }
        /// <summary>
        /// 矩阵
        /// </summary>
        /// <param name="m">生成来源</param>
        public Matrix(double[,] m)
        {
            Rows = m.GetLength(0);
            Cols = m.GetLength(1);
            _element = new double[Rows * Cols];
            int count = 0;

            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                {
                    _element[count++] = m[i, j];
                }
        }
        public Matrix(List<List<double>> m)
        {
            Rows = m.Count;
            Cols = m[0].Count;
            _element = new double[Rows * Cols];
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    this[i, j] = m[i][j];
                }
            }
        }
        /// <summary>
        /// 矩阵
        /// </summary>
        /// <param name="m">生成来源</param>
        /// <param name="fill">填充</param>
        public Matrix(double[,] m, Matrix fill) : this(m)
        {
            //多维数组转为一维数组
            var lst = new List<double>();
            for (int i = 0; i < fill.Rows; i++)
                for (int j = 0; j < fill.Cols; j++)
                    lst.Add(fill[i, j]);
            //如果是5列,那么3/5就是[0行,3列]
            for (int i = 0; i < lst.Count; i++)
                this[i / Cols, i % Cols] = lst[i];
        }
        public Matrix(double[] m)
        {
            Rows     = m.Length;
            Cols     = 1;
            _element = m;
        }
        #endregion

        #region 重载运算符
        public static Matrix MAbs(Matrix a)
        {
            var a2 = a.Clone();
            for (int i = 0; i < a.Rows; i++)
                for (int j = 0; j < a.Cols; j++)
                    a2[i, j] = Math.Abs(a[i, j]);
            return a2;
        }
        /// <summary>
        /// 矩阵相加
        /// </summary>
        /// <param name="a">第一个矩阵,和b矩阵必须同等大小</param>
        /// <param name="b">第二个矩阵</param>
        /// <returns>返回矩阵相加后的结果</returns>
        public static Matrix operator +(Matrix a, Matrix b)
        {
            if (a.Cols == b.Cols && a.Rows == b.Rows)
            {
                double[,] res = new double[a.Rows, a.Cols];
                for (int i = 0; i < a.Rows; i++)
                {
                    for (int j = 0; j < a.Cols; j++)
                    {
                        res[i, j] = a[i, j] + b[i, j];
                    }
                }
                return new Matrix(res);
            }
            else
            {
                throw new Exception("两个矩阵行列不相等");
            }
        }
        /// <summary>
        /// 矩阵相减
        /// </summary>
        /// <param name="a">第一个矩阵,和b矩阵必须同等大小</param>
        /// <param name="b">第二个矩阵</param>
        /// <returns>返回矩阵相减后的结果</returns>
        public static Matrix operator -(Matrix a, Matrix b)
        {
            if (a.Cols == b.Cols && a.Rows == b.Rows)
            {
                double[,] res = new double[a.Rows, a.Cols];
                for (int i = 0; i < a.Rows; i++)
                {
                    for (int j = 0; j < a.Cols; j++)
                    {
                        res[i, j] = a[i, j] - b[i, j];
                    }
                }
                return new Matrix(res);
            }
            else
            {
                throw new Exception("两个矩阵行列不相等");
            }
        }
        /// <summary>
        /// 对矩阵每个元素取相反数
        /// </summary>
        /// <param name="a">二维矩阵</param>
        /// <returns>得到矩阵的相反数</returns>
        public static Matrix operator -(Matrix a)
        {
            Matrix res = a;
            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < a.Cols; j++)
                {
                    res._element[i * a.Cols + j] = -res._element[i * a.Cols + j];
                }
            }
            return res;
        }
        /// <summary>
        /// 矩阵相乘
        /// </summary>
        /// <param name="a">第一个矩阵</param>
        /// <param name="b">第二个矩阵,这个矩阵的行要与第一个矩阵的列相等</param>
        /// <returns>返回相乘后的一个新的矩阵</returns>
        public static Matrix operator *(Matrix a, Matrix b)
        {
            if (a.Cols != b.Rows)
                throw new Exception("两个矩阵行和列不等");

            double[,] res = new double[a.Rows, b.Cols];
            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < b.Cols; j++)
                {
                    for (int k = 0; k < a.Cols; k++)
                    {
                        res[i, j] += a[i, k] * b[k, j];
                    }
                }
            }
            return new Matrix(res);
        }
        /// <summary>
        /// 数乘
        /// </summary>
        /// <param name="a">第一个矩阵</param>
        /// <param name="num">一个实数</param>
        /// <returns>返回相乘后的新的矩阵</returns>
        public static Matrix operator *(Matrix a, double num)
        {
            Matrix res = a;
            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < a.Cols; j++)
                {
                    res._element[i * a.Cols + j] *= num;
                }
            }
            return res;
        }

        public static Matrix operator /(Matrix a, double num)
        {
            Matrix res = a;
            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < a.Cols; j++)
                {
                    res._element[i * a.Cols + j] /= num;
                }
            }
            return res;
        }

        /// <summary>
        /// 矩阵转置
        /// </summary>
        /// <returns>返回当前矩阵转置后的新矩阵</returns>
        public Matrix Transpose()
        {
            double[,] res = new double[Cols, Rows];
            {
                for (int i = 0; i < Cols; i++)
                {
                    for (int j = 0; j < Rows; j++)
                    {
                        res[i, j] = this[j, i];
                    }
                }
            }
            return new Matrix(res);
        }
        /// <summary>
        /// 矩阵求逆
        /// </summary>
        /// <returns>返回求逆后的新的矩阵</returns>
        public Matrix Inverse()
        {
            //保证原始矩阵不变,所以克隆一份
            var thisClone = this.Clone();
            if (Cols != Rows || this.Determinant() == 0)
                throw new Exception("矩阵不是方阵无法求逆");

            //初始化一个同等大小的单位阵
            var res = thisClone.EMatrix();
            for (int i = 0; i < Rows; i++)
            {
                //首先找到第i列的绝对值最大的数,并将该行和第i行互换
                int rowMax = i;
                double max = Math.Abs(thisClone[i, i]);
                for (int j = i; j < Rows; j++)
                    if (Math.Abs(thisClone[j, i]) > max)
                    {
                        rowMax = j;
                        max    = Math.Abs(thisClone[j, i]);
                    }
                //将第i行和找到最大数那一行rowMax交换
                if (rowMax != i)
                {
                    thisClone.Exchange(i, rowMax);
                    res.Exchange(i, rowMax);
                }
                //将第i行做初等行变换,将第一个非0元素化为1
                double r = 1.0 / thisClone[i, i];
                thisClone.Exchange(i, -1, r);
                res.Exchange(i, -1, r);
                //消元
                for (int j = 0; j < Rows; j++)
                {
                    //到本行后跳过
                    if (j == i)
                        continue;
                    else
                    {
                        r = -thisClone[j, i];
                        thisClone.Exchange(i, j, r);
                        res.Exchange(i, j, r);
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// 行列式
        /// </summary>
        /// <returns></returns>
        public double Determinant()
        {
            if (Cols != Rows)
                throw new Exception("数量不等同,不是行列式");

            var thisClone = this.Clone();
            //行列式每次交换行,都需要乘以-1
            double res = 1;
            for (int i = 0; i < Rows; i++)
            {
                //首先找到第i列的绝对值最大的数
                int rowMax = i;
                double max = Math.Abs(thisClone[i, i]);
                for (int j = i; j < Rows; j++)
                {
                    if (Math.Abs(thisClone[j, i]) > max)
                    {
                        rowMax = j;
                        max    = Math.Abs(thisClone[j, i]);
                    }
                }
                //将第i行和找到最大数那一行rowMax交换,同时将单位阵做相同初等变换
                if (rowMax != i)
                {
                    thisClone.Exchange(i, rowMax);
                    res *= -1;
                }
                //消元
                for (int j = i + 1; j < Rows; j++)
                {
                    double r = -thisClone[j, i] / thisClone[i, i];
                    thisClone.Exchange(i, j, r);
                }
            }
            //计算对角线乘积
            for (int i = 0; i < Rows; i++)
            {
                res *= thisClone[i, i];
            }
            return res;
        }
        #endregion

        #region 重载运算符_比较
        public override bool Equals(object? obj)
        {
            return this == obj as Matrix;
        }

        public bool Equals(Matrix? other)
        {
            return this == other;
        }

        public static bool operator !=(Matrix? a, Matrix? b)
        {
            return !(a == b);
        }

        public static bool operator ==(Matrix? a, Matrix? b)
        {
            //此处地方不允许使用==null,因为此处是定义
            if (b is null)
                return a is null;
            else if (a is null)
                return false;
            if (ReferenceEquals(a, b))//同一对象
                return true;

            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < a.Cols; j++)
                    if (a[i, j] != b[i, j])
                        return false;
            }
            return true;
        }


        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion

        #region 转换对象
        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns>返回深拷贝后的新对象</returns>
        public Matrix Clone()
        {
            double[,] ele = new double[Rows, Cols];
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                    ele[i, j] = this[i, j];
            return new Matrix(ele);

            //以下不能用
            //因为构造函数只是拿数据头指针(构造函数应该保持最小操作)
            //如果构造函数每次都自己克隆一个数组,那么共享数组内容就不能实现了
            //return new Matrix(_element);
        }
        public double[] ToArray()
        {
            var d = new double[_element.Length];
            _element.CopyTo(d, 0);
            return d;
        }

        public sealed override string ToString()
        {
            return ToString(null, null);
        }
        public string ToString(IFormatProvider? provider)
        {
            return ToString(null, provider);
        }

        public string ToString(string? format = null, IFormatProvider? formatProvider = null)
        {
            var str = new StringBuilder();

            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Cols; j++)
                {
                    str.Append(this[i, j].ToString(format, formatProvider));
                    if (j != Cols - 1)
                        str.Append("\t");
                    else if (i != Rows - 1)
                        str.Append(Environment.NewLine);
                }

            return str.ToString();
        }

        // 隐式转换(相当于是重载赋值运算符)
        public static implicit operator Matrix(double[] array)
        {
            return new Matrix(array);
        }
        #endregion

        #region 初等变换
        /// <summary>
        /// 初等变换：交换第r1和第r2行
        /// </summary>
        /// <param name="r1">第r1行</param>
        /// <param name="r2">第r2行</param>
        /// <returns>返回交换两行后的新的矩阵</returns>
        public void Exchange(int r1, int r2)
        {
            if (Math.Min(r2, r1) >= 0 && Math.Max(r1, r2) < Rows)
            {
                for (int j = 0; j < Cols; j++)
                {
                    double temp = this[r1, j];
                    this[r1, j] = this[r2, j];
                    this[r2, j] = temp;
                }
            }
            else
            {
                throw new Exception("超出索引");
            }
        }
        /// <summary>
        /// 初等变换：将r1行乘以某个数加到r2行
        /// </summary>
        /// <param name="r1">第r1行乘以num</param>
        /// <param name="r2">加到第r2行,若第r2行为负,则直接将r1乘以num并返回</param>
        /// <param name="num">某行放大的倍数</param>
        /// <returns></returns>
        public void Exchange(int r1, int r2, double num)
        {
            if (Math.Min(r2, r1) >= 0 && Math.Max(r1, r2) < Rows)
            {
                for (int j = 0; j < Cols; j++)
                {
                    this[r2, j] += this[r1, j] * num;
                }
            }
            else if (r2 < 0)
            {
                for (int j = 0; j < Cols; j++)
                {
                    this[r1, j] *= num;
                }
            }
            else
            {
                throw new Exception("超出索引");
            }
        }
        /// <summary>
        /// 得到一个同等大小的单位矩阵
        /// </summary>
        /// <returns>返回一个同等大小的单位矩阵</returns>
        public Matrix EMatrix()
        {
            if (Rows == Cols)
            {
                double[,] res = new double[Rows, Cols];
                for (int i = 0; i < Rows; i++)
                {
                    for (int j = 0; j < Cols; j++)
                    {
                        if (i == j)
                            res[i, j] = 1;
                        else
                            res[i, j] = 0;
                    }
                }
                return new Matrix(res);
            }
            else
                throw new Exception("不是方阵,无法得到单位矩阵");
        }
        #endregion

        #region 方法
        /// <summary>
        /// 设置行数据
        /// </summary>
        /// <param name="row">行</param>
        /// <param name="data">数据</param>
        public void SetRow(int row, double[] data)
        {
            for (int i       = 0; i < data.Length; i++)
                this[row, i] = data[i];
        }

        /// <summary>
        /// 设置列数据
        /// </summary>
        /// <param name="col">列</param>
        /// <param name="data">数据</param>
        public void SetCol(int col, double[] data)
        {
            for (int i       = 0; i < data.Length; i++)
                this[i, col] = data[i];
        }

        /// <summary>
        /// 获取行
        /// </summary>
        /// <param name="row">行</param>
        /// <returns></returns>
        public double[] GetRows(int row)
        {
            var arr = new double[Cols];
            //遍历列
            for (int i = 0; i < Cols; i++)
                arr[i] = this[row, i];
            return arr;
        }

        /// <summary>
        /// 获取列
        /// </summary>
        /// <param name="col">列</param>
        /// <returns></returns>
        public double[] GetCols(int col)
        {
            var arr = new double[Rows];
            //遍历行
            for (int i = 0; i < Rows; i++)
                arr[i] = this[i, col];
            return arr;
        }

        /// <summary>
        /// 获取或设置第i行第j列的元素值
        /// </summary>
        /// <param name="i">行</param>
        /// <param name="j">列</param>
        /// <returns>返回第i行第j列的元素值</returns>
        public double this[int i, int j]
        {
            get
            {
                if (i < Rows && j < Cols)
                    return _element[i * Cols + j];
                else
                    throw new Exception("索引越界");
            }
            set
            {
                _element[i * Cols + j] = value;
            }
        }

        /// <summary>
        /// 隔行赋值
        /// </summary>
        public void DetachmentEvenLineOddLine()
        {
            //拆离奇数行和偶数行
            var thisClone = this.Clone();
            //将数组一分为二,然后上半部分隔行分配
            for (int i = 0; i < thisClone.Rows / 2; i++)//0~3
            {
                this.SetRow(i * 2, thisClone.GetRows(i));//覆盖此行
            }
            //下半部分插入上半部分的奇数位置
            int v = 0;
            for (int i = thisClone.Rows / 2; i < thisClone.Rows; i++)//4~7
            {
                this.SetRow(v * 2 + 1, thisClone.GetRows(i));//覆盖此行 将4写入到1
                ++v;
            }
        }
        #endregion

#if true3
        /// <summary>
        /// 相当于是矩阵的除法
        /// A*X=B,已知A,B求X.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public Matrix Solve(Matrix b)
        {
            if (Cols != Rows || this.Determinant() == 0)
            {
                throw new Exception("矩阵不是方阵无法运算");
            }
            return this.Inverse() * b;
        }

        /// <summary>
        /// 交换行
        /// </summary>
        /// <param name="a">行1</param>
        /// <param name="b">行2</param>
        public void ExchangeRow(int a, int b)
        {
            //先获取第a行和第b行
            int mCols = this.Cols;//列
            var colList1 = new List<double>();

            for (int i = 0; i < mCols; i++)//循环列
            {
                colList1.Add(this[a, i]);
            }
            for (int i = 0; i < mCols; i++)//循环列
            {
                this[a, i] = this[b, i];
                this[b, i] = colList1[i];
            }
        }
#endif
    }
}
