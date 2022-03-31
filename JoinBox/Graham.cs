#if !HC2020
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
#else
using GrxCAD.ApplicationServices;
using GrxCAD.DatabaseServices;
using GrxCAD.EditorInput;
using GrxCAD.Geometry;
using GrxCAD.Runtime;
#endif

using System.Collections.Generic;
using System.Linq;
using static System.Math;
using JoinBox.Extensions;
using JoinBox.BasalMath;

namespace JoinBox
{
    /*
        视频参考: https://www.bilibili.com/video/BV1v741197YM
        相关学习: https://www.cnblogs.com/VividBinGo/p/11637684.html
        ① 找到所有点中最左下角的点_p0(按 x 升序排列，如果 x 相同，则按 y 升序排列)
        ② 以_p0为基准求所有点的极角,并按照极角排序(按极角升序排列,若极角相同,则按距离升序排列),设这些点为p1,p2,……,pn-1
        ③ 建立一个栈,_p0,p1进栈，对于P[2..n-1]的每个点,利用叉积判断,
          若栈顶的两个点与它不构成"向左转(逆时针)"的关系,则将栈顶的点出栈,直至没有点需要出栈以后,将当前点进栈
        ④ 所有点处理完之后栈中保存的点就是凸包了。
    */
    public partial class Graham
    {
        /// <summary>
        /// 最靠近x轴的点(必然是凸包边界的点)
        /// </summary>
        Point3d _p0;

        /// <summary>
        /// 求凸包测试命令
        /// </summary>
        [CommandMethod("test_gra")]
        public void test_gra()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;//当前的数据库
            ed.WriteMessage("\n****{惊惊连盒}求凸包,选择曲线:");

            //定义选择集选项
            var pso = new PromptSelectionOptions
            {
                RejectObjectsOnLockedLayers = true, //不选择锁定图层对象
                AllowDuplicates = true, //不允许重复选择
            };
            var ssPsr = ed.GetSelection(pso);//手选  这里输入al会变成all,无法删除ssget的all关键字
            if (ssPsr.Status != PromptStatus.OK)
                return;

            db.Action(tr =>
            {
                var getPts = new List<Point3d>();
                foreach (ObjectId id in ssPsr.Value.GetObjectIds())
                {
                    var ent = id.ToEntity(tr);
                    if (ent is Curve curve)
                    {
                        var cs = new CurveSample(curve);
                        getPts.AddRange(/*cs.GetSamplePoints*/cs.CollectPoints(tr, curve).Cast<Point3d>());
                    }
                    else if (ent is DBPoint bPoint)
                        getPts.Add(bPoint.Position);
                    else
                    {
                        var entPosition = ent.GetType().GetProperty("Position");//反射获取属性
                        if (entPosition != null)
                        {
                            var pt = (Point3d)entPosition.GetValue(null, null);
                            getPts.Add(pt);
                        }
                    }
                }

                //葛立恒方法
                var pts = GrahamConvexHull(getPts).ToPoint2ds();

                ed.WriteMessage("\n\r凸包对踵点最大距离:" + RotateCalipersMax(pts));
                ed.WriteMessage("\n\r凸包对踵点最小距离:" + RotateCalipersMin(pts));

                //var psd = new PointsDistance<Point2d>();
                //ed.WriteMessage("\n\r凸包点集最大距离:" + psd.Min(pts));
                //ed.WriteMessage("\n\r凸包点集最小距离:" + psd.Max(pts));

                var bv = new List<BulgeVertex>();
                for (int i = 0; i < pts.Count(); i++)
                {
                    bv.Add(new BulgeVertex(pts[i], 0));
                }
                Entity pl = EntityAdd.AddPolyLineToEntity(bv);
                pl.ColorIndex = 1;
                tr.AddEntityToMsPs(db, pl);

#if true3
                var recs = Boundingbox(pts);
                //生成所有的包围盒,每条边生成一个
                int ColorIndex = 0;
                foreach (var rec in recs)
                {
                    bv = new List<BulgeVertex>
                    {
                        new BulgeVertex(rec.R1, 0),
                        new BulgeVertex(rec.R2, 0),
                        new BulgeVertex(rec.R3, 0),
                        new BulgeVertex(rec.R4, 0)
                    };
                    pl = EntityAdd.AddPolyLineToEntity(0, bv);
                    pl.ColorIndex = ++ColorIndex;
                    tr.AddEntityToMsPs(db, pl);
                }
#endif

                //生成计算面积最小的包围盒
                var recAreaMin = BoundingboxAreaMin(pts);
                bv = new List<BulgeVertex>
                    {
                        new BulgeVertex(recAreaMin.R1, 0),
                        new BulgeVertex(recAreaMin.R2, 0),
                        new BulgeVertex(recAreaMin.R3, 0),
                        new BulgeVertex(recAreaMin.R4, 0)
                    };
                pl = EntityAdd.AddPolyLineToEntity(bv);
                pl.ColorIndex = 2;
                tr.AddEntityToMsPs(db, pl);

            });
        }

        /// <summary>
        /// 角度p0和pn的角度
        /// </summary>
        /// <param name="pn"></param>
        /// <returns></returns>
        double Cosine(Point3d pn)
        {
            double d = _p0.DistanceTo(pn);

            //距离是0表示是自己和自己的距离,那么0不可以是除数,否则Nan:求角度(高/斜)==sin(角度)
            var angle = d == 0.0 ? 1.0 : (pn.Y - _p0.Y) / d;
            //var angle = d == 0 ? 0 : (pn.Y - _p0.Y) / d; //0度会让点被忽略了
            return angle;
        }

        /// <summary>
        /// 求凸包_葛立恒算法,出来的凸包做的多段线在正交的情况下会多点或者少点
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        Point3d[] GrahamConvexHull(IEnumerable<Point3d> pt2ds)
        {
            //消重,点排序
            var pts = pt2ds.Distinct(new ToleranceDistinct()).OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            //max右上角,因为负角度的问题,所以需要从右上角计算
            _p0 = pts.Last();
            //按角度及距离排序
            pts = pts.OrderByDescending(p => Cosine(p)).ThenBy(p => _p0.DistanceTo(p)).ToList();

            var stack = new Stack<Point3d>();
            stack.Push(_p0);//顶部加入对象
            stack.Push(pts[1]);
            bool tf = true;

            //遍历所有的点,因为已经角度顺序,所有是有序遍历.从第三个点开始
            for (int i = 2; i < pts.Count; i++)
            {
                Point3d qn = pts[i];      //第一次为p2,相当于pn
                Point3d q1 = stack.Pop(); //第一次为p1,相当于前一个点,删除顶部对象(相当于点回退)
                Point3d q0 = stack.Peek();//第一次为_p0,相当于后面一个点,查询顶部对象
                //为真表示要剔除
                while (tf && CrossAclockwise(q1, q0, qn))
                {
                    if (stack.Count > 1)
                    {
                        stack.Pop();//删除顶部对象(相当于删除前一个点进行回退)

                        //前后点交换,用于while循环,
                        //可参考 https://www.bilibili.com/video/BV1v741197YM 04:15
                        //栈顶就是回滚之后的,再次和qn进行向量叉乘,看看是不是叉乘方向,是就继续回滚
                        //否则结束循环后加入栈中.
                        q1 = q0;
                        q0 = stack.Peek();
                    }
                    else
                    {
                        //栈少于1,就不在剔除顶部.结束循环...
                        //保护栈中_p0不剔除
                        tf = false;
                    }
                }
                stack.Push(q1);
                stack.Push(qn);
                tf = true;
            }

            var npts = stack.ToList();
            //过滤凸度过少的话,将点移除,以免凸包有多余的边点.
            for (int i = 0; i < npts.Count() - 2; i++)
            {
                var bu = MathHelper.GetArcBulge(npts[i], npts[i + 1], npts[i + 2]);
                if (Abs(bu) < 1e-6)
                {
                    npts.RemoveAt(i + 1);//移除中间
                    i--;
                }
            }

            return npts.ToArray();
        }


        /// <summary>
        /// 有向包围盒
        /// </summary>
        /// <param name="pts">点集</param>
        /// <returns>返回每条边的包围盒</returns>
        List<Rectangular> Boundingbox(List<Point2d> pts)
        {
            /*
                  最小包围盒(外接矩形)
                  重要的条件:凸多边形的最小周长(最小面积)外接矩形存在一条边和多边形的一条边重合

                  角点r1==a->c叉乘a->b得到r1角点,再a->d叉乘a->b...轮询,如果r1与a点距离开始进行缩小(叉乘距离回归),
                         那么表示r1确定,以及得到末尾点ptLast图上位c点,
                         最后明确得到:a->ptLast叉乘a->b就是最近角点r1

                  角点r2,r3,r4需要的仅仅是通过a->b向量与X轴角度绕r1旋转得到
                */

            var recs = new List<Rectangular>();

            //因为利用三点获取第一个交点,
            //所以为了最后的边界,需要加入集合前面,以使得成为一个环
            pts.Add(pts[0]);
            pts.Add(pts[1]);
            pts.Add(pts[2]);
            pts.Add(pts[3]);

            //此处循环是作为边,下次则是ab..bc..cd..de..循环下去
            for (int i = 0; i < pts.Count() - 1; i++)
            {
                //矩形的点
                Point2d r1;
                Point2d r2;
                Point2d r3;
                Point2d r4;

                //最后一次的点
                Point2d c_point;

                //上一次长度
                double ac_lengthLast = -1;

                Point2d a_point = pts[i];
                Point2d b_point = pts[i + 1];

                //此处循环是求长度求r1点,如果出现点乘距离收缩,就结束
                for (int c_nmuber = i + 2; c_nmuber < pts.Count(); c_nmuber++)
                {
                    //ac->ab点乘求矩形的一个角点
                    //角点距离如果出现缩小,就确定了这个点是j
                    var ac_length = a_point.GetDistanceTo(DotProduct(a_point, pts[c_nmuber], b_point));

                    double d = a_point.GetDistanceTo(b_point);


                    //第一次赋值
                    if (ac_lengthLast == -1)
                    {
                        ac_lengthLast = ac_length;
                        //2022-03-31 add By JieGou。
                        //第一次进行比较时，投影边应该和边长作一次比较
                        if (ac_length < d)
                        {
                            ac_length = d;
                            r1 = b_point;

                            //根据角度旋转所有的点
                            var v1 = r1.GetVectorTo(a_point);
                            var angle1 = v1.GetAngleTo(Vector2d.XAxis);
                            var angle = v1.GetAngle2XAxis();

                            //此处循环是求r2,r3,r4的点
                            //后面的点旋转之后加入集合,再利用linq判断Y轴X轴最大的
                            var houmiandesuoyoudian = new List<Point2d>();
                            foreach (var pt in pts)
                            {
                                houmiandesuoyoudian.Add(pt.RotateBy(-angle, r1));
                            }

                            var maxY = houmiandesuoyoudian.OrderByDescending(pt => pt.Y).ToList()[0].Y;
                            var maxX = houmiandesuoyoudian.OrderByDescending(pt => pt.X).ToList()[0].X;

                            //通过最大,计算角点,然后逆旋转,回到原始图形
                            r2 = new Point2d(r1.X, maxY).RotateBy(angle, r1);
                            r3 = new Point2d(maxX, maxY).RotateBy(angle, r1);
                            r4 = new Point2d(maxX, r1.Y).RotateBy(angle, r1);
                            recs.Add(new Rectangular(r1, r2, r3, r4));

                            break;
                        }
                    }
                    else if (ac_lengthLast < ac_length)
                    {
                        ac_lengthLast = ac_length;
                    }
                    else
                    {
                        //此处条件是点乘距离已经收缩,求得r1点,最后会break结束循环.
                        c_point = pts[c_nmuber - 1];               //边界点-1就是上次的
                        r1 = DotProduct(a_point, c_point, b_point);//角点计算


                        //根据角度旋转所有的点
                        var v1 = r1.GetVectorTo(a_point);
                        var angle1 = v1.GetAngleTo(Vector2d.XAxis);
                        var angle = v1.GetAngle2XAxis();

                        //此处循环是求r2,r3,r4的点
                        //后面的点旋转之后加入集合,再利用linq判断Y轴X轴最大的
                        var houmiandesuoyoudian = new List<Point2d>();
                        foreach (var pt in pts)
                        {
                            houmiandesuoyoudian.Add(pt.RotateBy(-angle, r1));
                        }

                        var maxY = houmiandesuoyoudian.OrderByDescending(pt => pt.Y).ToList()[0].Y;
                        var maxX = houmiandesuoyoudian.OrderByDescending(pt => pt.X).ToList()[0].X;

                        //通过最大,计算角点,然后逆旋转,回到原始图形
                        r2 = new Point2d(r1.X, maxY).RotateBy(angle, r1);
                        r3 = new Point2d(maxX, maxY).RotateBy(angle, r1);
                        r4 = new Point2d(maxX, r1.Y).RotateBy(angle, r1);
                        recs.Add(new Rectangular(r1, r2, r3, r4));

                        break;
                    }
                }
            }
            return recs;
        }

        /// <summary>
        /// 面积最小包围盒
        /// </summary>
        /// <param name="pts">点集</param>
        /// <returns></returns>
        Rectangular BoundingboxAreaMin(List<Point2d> pts)
        {
            var recs = Boundingbox(pts);
            return recs.OrderBy(rec => rec.Area).ToArray()[0];
        }

        // 概念参考了这里,但是它代码好像有点问题  http://www.cppblog.com/staryjy/archive/2009/11/19/101412.html
        /// <summary>
        /// 凸包对踵点最大的距离_旋转卡壳
        /// </summary>
        /// <returns></returns>
        double RotateCalipersMax(IEnumerable<Point2d> pt2ds)
        {
            var pts = pt2ds.ToList();
            if (pts.Count == 0 || pts.Count == 1)
            {
                return 0;
            }
            else if (pts.Count == 2)
            {
                return pts[0].GetDistanceTo(pts[1]);
            }

            //返回的长度
            double ans = 0;

            int ps = 2;     //2是从下下一个点开始,因为1开始会导致[0]和[0]判断距离
            pts.Add(pts[0]);//但是下下一个点开始就表示没有判断到0->1线段,必须加入尾部判断
            int p = pts.Count - ps;
            for (int i = 0; i < p; i++) //点序是叉乘方向的.
            {
                //叉乘求面积,面积呈现为单峰函数(函数图像中间大两边小,函数从递增到递减),
                //满足结束条件:面积最高峰的时候下一次判断即为>前者(函数递减) || 取余为0(即为遍历到最后了)

                //叉乘求面积,A<B,表示求后者面积数大,直到后者面积数小,结束循环,求最大值长度即可
                while (Abs(Cross(pts[i], pts[i + 1], pts[ps])) < Abs(Cross(pts[i], pts[i + 1], pts[ps + 1])))
                {
                    ps = (ps + 1) % p;//X%Y,X<Y返回X.取余为0(即为遍历到最后了)
                }
                //峰值时候求的三点距离
                //第一次3->0和3->1
                ans = Max(ans, Max(pts[ps].GetDistanceTo(pts[i]), pts[ps].GetDistanceTo(pts[i + 1])));
            }
            return ans;
        }

        private double Cross(Point2d point2d1, Point2d point2d2, Point2d point2d3)
        {
            return ((PointV)point2d1).Cross((PointV)point2d2, (PointV)point2d3);
        }

        private Point2d DotProduct(Point2d a_point, Point2d c_point, Point2d b_point)
        {
            PointV pointV = ((PointV)a_point).DotProduct((PointV)c_point, (PointV)b_point);
            return new Point2d(pointV.X, pointV.Y);
        }


        private bool CrossAclockwise(Point3d q1, Point3d q0, Point3d qn)
        {
            return ((PointV)q1).CrossAclockwise((PointV)q0, (PointV)qn);
        }



        /// <summary>
        /// 凸包对踵点最小的距离_旋转卡壳
        /// </summary>
        /// <returns></returns>
        double RotateCalipersMin(IEnumerable<Point2d> pt2ds)
        {
            var pts = pt2ds.ToList();
            if (pts.Count == 0 || pts.Count == 1)
            {
                return 0;
            }
            else if (pts.Count == 2)
            {
                return pts[0].GetDistanceTo(pts[1]);
            }

            var lstmin = new List<double>();

            //点集顺序是叉乘方向的.
            //凸包对踵点最小的距离==非邻点的最小点距,邻边点不计算
            //计算方式是通过获取循环数字,确定执行次数.
            //从i从i+2点开始递增,但是循环数字是递减的,只因不需要重复计算0-2,2-0的情况.
            //所以时间复杂度是常数项

            var last = pts.Count - 1;
            for (int i = 0; i < last; i++)
            {
                //循环次数 = 总数 - 1(前一个点邻边) - i(循环递减)
                int fornum = last - 1 - i;
                //如果i==0,减多1次
                if (i == 0)
                {
                    fornum--;
                }

                int inumq = i + 1;//前一个点(邻边)
                for (int j = 0; j < fornum; j++)
                {
                    inumq++;//前前一个点
                    var dis = pts[i].GetDistanceTo(pts[inumq]);
                    lstmin.Add(dis);
                }
            }
            return lstmin.Min();  //返回的长度
        }
    }

    /// <summary>
    /// Linq Distinct 消重比较两点在容差范围内就去除
    /// </summary>
    public class ToleranceDistinctV : IEqualityComparer<PointV>
    {
        public bool Equals(PointV? a, PointV? b)//Point3d是struct不会为null
        {
            if (b is null)
                return a is null;
            else if (a is null)
                return false;
            if (ReferenceEquals(a, b))//同一对象
                return true;
#if true
            // 方形限定
            // 在 0~1e-6 范围实现 圆形限定 则计算部分在浮点数6位后,没有啥意义
            // 在 0~1e-6 范围实现 从时间和CPU消耗来说,圆形限定 都没有 方形限定 的好
            return a.Equals(b, default, Tolerance.Distinct);
#else
            // 圆形限定
            // DistanceTo 分别对XYZ进行了一次乘法,也是总数3次乘法,然后求了一次平方根
            // (X86.CPU.FSQRT指令用的牛顿迭代法/软件层面可以使用快速平方根....我还以为CPU会采取快速平方根这样的取表操作)
            return a.DistanceTo(b) <= Tolerance.Distinct;
#endif
        }

        public int GetHashCode(PointV obj)
        {
            //结构体直接返回 obj.GetHashCode(); Point3d ToleranceDistinct3d
            //因为结构体是用可值叠加来判断?或者因为结构体兼备了一些享元模式的状态?
            //而类是构造的指针,所以取哈希值要改成x+y+z..s给Equals判断用,+是会溢出,所以用^
            return (int)obj.X ^ (int)obj.Y ^ (int)obj.Z;
        }
    }
}