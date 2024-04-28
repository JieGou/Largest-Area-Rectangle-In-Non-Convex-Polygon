using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LargestRectangleInsideNonConvexPolygon
{
    /// <summary>
    /// 多边形扩展方法类
    /// </summary>
    public static class PolygonExtenssion
    {
        //2022-04-02备注 在需要时进行优化
        //有不稳定的地方，表现在针对同一个多边形摆放的角度不同，会出现不同的结果；同一个多边形，多次运行会出现不同的结果
        /// <summary>
        /// 计算最大内接矩形
        /// </summary>
        /// <param name="polygon">多边形</param>
        /// <returns></returns>
        public static Rectangle GetInsideLargestRectangle(this Polygon polygon)
        {
            var rectangles = polygon.Rectangulate();
            var matrix = polygon.GetBaseMatrix();
            int width = rectangles.GetUpperBound(1) + 1;
            var answerRectangle = new Rectangle
            {
                LeftBottom = new Point(0, 0),
                RightTop = new Point(0, 0)
            };
            //TODO 使用力扣85题 最大矩阵算法进行效验
            var prefSum = new int[matrix.Length / width, width];
            for (int i = 0; i < matrix.Length / width; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    prefSum[i, j] = (!matrix[i, j] ? 1 : 0);
                    if (i > 0)
                        prefSum[i, j] += prefSum[i - 1, j];
                    if (j > 0)
                        prefSum[i, j] += prefSum[i, j - 1];
                    if (i * j > 0)
                        prefSum[i, j] -= prefSum[i - 1, j - 1];
                }
            }
            for (int i1 = 0; i1 < matrix.Length / width; i1++)
            {
                for (int j1 = 0; j1 < width; j1++)
                {
                    for (int i2 = i1; i2 < matrix.Length / width; i2++)
                    {
                        for (int j2 = j1; j2 < width; j2++)
                        {
                            var elCount = prefSum[i2, j2];
                            if (j1 > 0)
                                elCount -= prefSum[i2, j1 - 1];
                            if (i1 > 0)
                                elCount -= prefSum[i1 - 1, j2];
                            if (i1 * j1 > 0)
                                elCount += prefSum[i1 - 1, j1 - 1];
                            if ((i2 - i1 + 1) * (j2 - j1 + 1) == elCount)
                            {
                                var tempRect = new Rectangle
                                {
                                    LeftBottom = rectangles[i1, j1].LeftBottom,
                                    RightTop = rectangles[i2, j2].RightTop
                                };
                                if (tempRect.Square > answerRectangle.Square)
                                {
                                    answerRectangle = tempRect;
                                }
                            }
                        }
                    }
                }
            }
            return answerRectangle;
        }
        //Done 重点检查该函数，对水平及垂直边界是否有问题
        /// <summary>
        /// 获取多边形矩形划分基础二维数组，矩形在外部true，内部false
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        private static bool[,] GetBaseMatrix(this Polygon polygon)
        {
            var rectangles = polygon.Rectangulate();
            int width = rectangles.GetUpperBound(1) + 1;
            var matrix = new bool[rectangles.Length / width, width];
            for (int i = 0; i < rectangles.Length / width; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    foreach (var edge in polygon.Edges)
                    {
                        if (edge.NearArea.Contains(rectangles[i, j]))
                        {
                            var point = rectangles[i, j].GetPointByEdge(edge);
                            var vector = Vector.GetNormalVectorFromPoint(point, edge);
                            if (!vector.HasSameDirection(edge.NormalVector))
                            {
                                matrix[i, j] = true;
                            }
                        }
                    }
                }
            }
            for (int i = 0; i < rectangles.Length / width; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Point lbPt = rectangles[i, j].LeftBottom;//左下角点
                    Point rtPt = rectangles[i, j].RightTop;//右上角点
                    //增加对左上角点以及 右下角点的处理
                    Point ltPt = new Point(lbPt.X, rtPt.Y);//左上角点
                    Point rbPt = new Point(rtPt.X, lbPt.Y);//右下角点
                    if (lbPt.OutsideOf(polygon) || rtPt.OutsideOf(polygon) ||
                        ltPt.OutsideOf(polygon) || rbPt.OutsideOf(polygon))
                    {
                        matrix[i, j] = true;
                    }
                    else
                    {
                        //<image url="$(ProjectDir)\DocumentImages\外部特殊情况.png" scale="0.4"/>
                        //2022-04-02 Add By JieGou。特殊情况处理
                        if (CheckInterpolationPointOut(polygon, lbPt, rtPt) || CheckInterpolationPointOut(polygon, ltPt, rbPt))
                        {
                            matrix[i, j] = true;
                        }
                    }
                }
            }
            return matrix;
        }
        /// <summary>
        /// 给定两点，检测是否有插值点在给定多边形外部。两点构成直线水平或垂直的可能不适用
        /// </summary>
        /// <param name="polygon">多边形</param>
        /// <param name="matrix"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="pt1">第一点</param>
        /// <param name="pt2">第二点</param>
        /// <param name="n">默认50等分</param>
        private static bool CheckInterpolationPointOut(Polygon polygon, Point pt1, Point pt2, int n = 50)
        {
            var a = Math.Atan((pt2.Y - pt1.Y) / (pt2.X - pt1.X));//线性插值角度
            double l = pt1.GetDistance(pt2);//对角线长度
            for (int idx = 1; idx < n - 1; idx++)
            {
                var indexPt = new Point(pt1.X + Math.Cos(a) * idx * l / n, pt1.Y + Math.Sin(a) * idx * l / n);
                if (indexPt.OutsideOf(polygon))
                {
                    return true;//发现一点在多边形外部即判断true
                }
            }
            return false;
        }

        private static IEnumerable<Rectangle> GetRectangles(this Polygon polygon)
        {
            var rectangles = polygon.Rectangulate();
            int width = rectangles.GetUpperBound(1) + 1;
            for (int i = 0; i < rectangles.Length / width; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    yield return rectangles[i, j];
                }
            }
        }
        /// <summary>
        /// 多边形矩阵单元格划分
        /// </summary>
        /// <param name="polygon">多边形</param>
        /// <returns></returns>
        private static Rectangle[,] Rectangulate(this Polygon polygon)
        {
            var xCoordinates = polygon.Points
                .Select(p => p.X)
                .OrderBy(x => x)
                .Distinct();
            var yCoordinates = polygon.Points
                .Select(p => p.Y)
                .OrderBy(y => y)
                .Distinct();
            var resultXCoords = new List<double>(xCoordinates);
            foreach (var y in yCoordinates)
            {
                foreach (var edge in polygon.Edges)
                {
                    if (y <= Math.Max(edge.Start.Y, edge.End.Y)
                        && y >= Math.Min(edge.Start.Y, edge.End.Y)
                        && edge.Start.Y != edge.End.Y)
                    {
                        //线性插入X
                        var x = (y - edge.Start.Y) * (edge.End.X - edge.Start.X) / (edge.End.Y - edge.Start.Y) + edge.Start.X;
                        if (!resultXCoords.Contains(x))
                            resultXCoords.Add(x);
                    }
                }
            }
            var resultYCoords = new List<double>(yCoordinates);
            foreach (var x in xCoordinates)
            {
                foreach (var edge in polygon.Edges)
                {
                    if (x <= Math.Max(edge.Start.X, edge.End.X)
                        && x >= Math.Min(edge.Start.X, edge.End.X)
                        && edge.Start.X != edge.End.X)
                    {
                        //线性插入Y
                        var y = (x - edge.Start.X) * (edge.End.Y - edge.Start.Y) / (edge.End.X - edge.Start.X) + edge.Start.Y;
                        if (!resultYCoords.Contains(y))
                            resultYCoords.Add(y);
                    }
                }
            }
            resultXCoords.Sort();
          resultXCoords=  resultXCoords.Distinct(new LambdaComparer<double>((x, y) => Math.Abs(x - y) < 10e-5)).ToList();
            resultYCoords.Sort();
          resultYCoords=  resultYCoords.Distinct(new LambdaComparer<double>((x, y) => Math.Abs(x - y) < 10e-5)).ToList();
            var rectangles = new Rectangle[resultXCoords.Count - 1, resultYCoords.Count - 1];
            for (int i = 1; i < resultXCoords.Count; i++)
            {
                for (int j = 1; j < resultYCoords.Count; j++)
                {
                    var leftBottom = new Point(resultXCoords[i - 1], resultYCoords[j - 1]);
                    var rightTop = new Point(resultXCoords[i], resultYCoords[j]);
                    rectangles[i - 1, j - 1] = new Rectangle
                    {
                        LeftBottom = leftBottom,
                        RightTop = rightTop
                    };
                }
            }
            return rectangles;
        }
    }

    public class LambdaComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _lambdaComparer;
        private readonly Func<T, int> _lambdaHash;

        public LambdaComparer(Func<T, T, bool> lambdaComparer) :
            this(lambdaComparer, o => 0)
        {
        }

        public LambdaComparer(Func<T, T, bool> lambdaComparer, Func<T, int> lambdaHash)
        {
            if (lambdaComparer == null)
                throw new ArgumentNullException("lambdaComparer");
            if (lambdaHash == null)
                throw new ArgumentNullException("lambdaHash");

            _lambdaComparer = lambdaComparer;
            _lambdaHash = lambdaHash;
        }

        public bool Equals(T x, T y)
        {
            return _lambdaComparer(x, y);
        }

        public int GetHashCode(T obj)
        {
            return _lambdaHash(obj);
        }
    }
}