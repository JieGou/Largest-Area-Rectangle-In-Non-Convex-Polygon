using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;

namespace JoinBox
{
    public class CurveSplit
    {
        public List<Curve> Curves { get; set; }

        /// <summary>
        /// 获取定值分割的曲线集合
        /// </summary>
        /// <param name="curve">曲线</param>
        /// <param name="fixedValue">定值分割</param>
        public CurveSplit(Curve curve, double fixedValue)
        {
            Curves = new();

            //算曲线长度
            double curveLength = curve.GetLength();

            //若少于定值,则直接返回这条曲线,表示返回这段长度
            if (curveLength < fixedValue)
            {
                Curves.Add(curve);
                return;
            }

            var pts = new Point3dCollection();
            //用来叠加长度
            double overlyingLength = 0;
            //定值采集点
            while (overlyingLength < curveLength)
            {
                //求起点到长度的点
                pts.Add(curve.GetPointAtDist(overlyingLength));
                overlyingLength += fixedValue;
            }
            //最后没有完全重合,加入尾巴点
            if (overlyingLength - curveLength < 1e-6)
                pts.Add(curve.GetPointAtDist(curveLength));

            //通过点集,分割曲线
            var splits = curve.GetSplitCurves(pts);
            foreach (var item in splits)
            {
                var cuItem = (Curve)item;
                Curves.Add(cuItem);
            }
            pts.Dispose();
            splits.Dispose();//释放
        }

        /// <summary>
        /// 手动释放生成出来的曲线,
        /// 因为cad的Point3d没有继承,所以不能用 <see cref="IDisposable">进行释放</see>
        /// 否则提示:Forgot to call Dispose? (Autodesk.AutoCAD.DatabaseServices.Arc): DisposableWrapper
        /// </summary>
        public void Dispose()
        {
            Curves?.ForEach(cu =>
            {
                cu.Dispose();
            });
        }
    }

    public class CurveSample
    {
        Curve _curve { get; set; }
        int _numSample { get; set; }

        /// <summary>
        /// 曲线采样
        /// </summary>
        /// <param name="curve">曲线</param>
        /// <param name="sampleNum">采样份数</param>
        public CurveSample(Curve curve, int sampleNum = 256)
        {
            _curve = curve;
            _numSample = sampleNum;
        }

        /// <summary>
        /// 曲线采样(注意尾点是否缺少哦)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Point3d> GetSamplePoints
        {
            get
            {
                if (_numSample == 0)
                    throw new System.Exception("NumSample参数不能为0");

                var length = _curve.GetLength();
                var fixedValue = length / _numSample;
                var cs = new CurveSplit(_curve, fixedValue);
                var curves = cs.Curves;

                var pts = new List<Point3d>();
                pts.Add(curves[0].StartPoint);//起点

                foreach (var item in curves)
                    pts.Add(item.EndPoint);//间隔点,尾点

                //末尾两个点可能一样,需要判断去除
                if (pts[pts.Count - 1] == pts[pts.Count - 2])
                    pts.RemoveAt(pts.Count - 1);

                cs.Dispose();
                return pts;
            }
        }
        /// <summary>
        /// 获取图元的点集合——多段线分解，其中弧线段划分为20段,注意对结果去重；块分解
        /// </summary>
        /// <param name="tr"></param>
        /// <param name="ent"></param>
        /// <returns></returns>
        public Point3dCollection CollectPoints(Transaction tr, Entity ent)
        {
            // The collection of points to populate and return
            Point3dCollection pts = new Point3dCollection();

            // We'll start by checking a block reference for
            // attributes, getting their bounds and adding
            // them to the point list. We'll still explode
            // the BlockReference later, to gather points
            // from other geometry, it's just that approach
            // doesn't work for attributes (we only get the
            // AttributeDefinitions, which don't have bounds)
            BlockReference br = ent as BlockReference;
            if (br != null)
            {
                foreach (ObjectId arId in br.AttributeCollection)
                {
                    DBObject obj = tr.GetObject(arId, OpenMode.ForRead);
                    if (obj is AttributeReference)
                    {
                        AttributeReference ar = (AttributeReference)obj;
                        ExtractBounds(ar, pts);
                    }
                }
            }
            // If we have a curve - other than a polyline, which
            // we will want to explode - we'll get points along
            // its length
            Curve cur = ent as Curve;
            if (cur != null && !(cur is Polyline || cur is Polyline2d || cur is Polyline3d))
            {
                // Two points are enough for a line, we'll go with a higher number for other curves
                int segs = (ent is Line ? 2 : 20);
                double param = cur.EndParam - cur.StartParam;
                for (int i = 0; i < segs; i++)
                {
                    try
                    {
                        Point3d pt = cur.GetPointAtParameter( cur.StartParam + (i * param / (segs - 1)) );
                        pts.Add(pt);
                    }
                    catch { }
                }
            }
            else if (ent is DBPoint)
            {
                // Points are easy
                pts.Add(((DBPoint)ent).Position);
            }
            else if (ent is DBText)
            {
                // For DBText we use the same approach as
                // for AttributeReferences
                ExtractBounds((DBText)ent, pts);
            }
            else if (ent is MText)
            {
                // MText is also easy - you get all four corners
                // returned by a function. That said, the points
                // are of the MText's box, so may well be different
                // from the bounds of the actual contents
                MText txt = (MText)ent;
                Point3dCollection pts2 = txt.GetBoundingPoints();
                foreach (Point3d pt in pts2)
                {
                    pts.Add(pt);
                }
            }
            else if (ent is Face)
            {
                Face f = (Face)ent;
                try
                {
                    for (short i = 0; i < 4; i++)
                    {
                        pts.Add(f.GetVertexAt(i));
                    }
                }
                catch { }
            }
            else if (ent is Solid)
            {
                Solid sol = (Solid)ent;
                try
                {
                    for (short i = 0; i < 4; i++)
                    {
                        pts.Add(sol.GetPointAt(i));
                    }
                }
                catch { }
            }
            else
            {
                // Here's where we attempt to explode other types of object
                DBObjectCollection oc = new DBObjectCollection();
                try
                {
                    ent.Explode(oc);
                    if (oc.Count > 0)
                    {
                        foreach (DBObject obj in oc)
                        {
                            Entity ent2 = obj as Entity;
                            if (ent2 != null && ent2.Visible)
                            {
                                foreach (Point3d pt in CollectPoints(tr, ent2))
                                {
                                    pts.Add(pt);
                                }
                            }
                            obj.Dispose();
                        }
                    }
                }
                catch { }
            }
            return pts;
        }
        private void ExtractBounds(DBText txt, Point3dCollection pts)
        {
            // We have a special approach for DBText and
            // AttributeReference objects, as we want to get
            // all four corners of the bounding box, even
            // when the text or the containing block reference
            // is rotated
            if (txt.Bounds.HasValue && txt.Visible)
            {
                // Create a straight version of the text object
                // and copy across all the relevant properties
                // (stopped copying AlignmentPoint, as it would
                // sometimes cause an eNotApplicable error)
                // We'll create the text at the WCS origin
                // with no rotation, so it's easier to use its
                // extents
                DBText txt2 = new DBText();
                txt2.Normal = Vector3d.ZAxis;
                txt2.Position = Point3d.Origin;
                // Other properties are copied from the original
                txt2.TextString = txt.TextString;
                txt2.TextStyleId = txt.TextStyleId;
                txt2.LineWeight = txt.LineWeight;
                txt2.Thickness = txt2.Thickness;
                txt2.HorizontalMode = txt.HorizontalMode;
                txt2.VerticalMode = txt.VerticalMode;
                txt2.WidthFactor = txt.WidthFactor;
                txt2.Height = txt.Height;
                txt2.IsMirroredInX = txt2.IsMirroredInX;
                txt2.IsMirroredInY = txt2.IsMirroredInY;
                txt2.Oblique = txt.Oblique;
                // Get its bounds if it has them defined (which it should, as the original did)
                if (txt2.Bounds.HasValue)
                {
                    Point3d maxPt = txt2.Bounds.Value.MaxPoint;
                    // Place all four corners of the bounding box in an array
                    Point2d[] bounds = new Point2d[] { Point2d.Origin, new Point2d(0.0, maxPt.Y), new Point2d(maxPt.X, maxPt.Y), new Point2d(maxPt.X, 0.0) };
                    // We're going to get each point's WCS coordinates
                    // using the plane the text is on
                    Plane pl = new Plane(txt.Position, txt.Normal);
                    // Rotate each point and add its WCS location to the
                    // collection
                    foreach (Point2d pt in bounds)
                    {
                        pts.Add( pl.EvaluatePoint( pt.RotateBy(txt.Rotation, Point2d.Origin) ) );
                    }
                }
            }
        }
    }
}
