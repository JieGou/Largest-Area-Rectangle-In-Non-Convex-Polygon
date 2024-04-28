using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using DotNetARX;

namespace LargestRectangleInsideNonConvexPolygon
{
    public class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Enter number of vertecies:");
            int number = Int32.Parse(Console.ReadLine());
            Console.WriteLine($"Enter {number} vertecies:");
            var polygon = new Polygon();
            while (number-- > 0)
            {
                var line = Console.ReadLine();
                var coords = line.Split(' ');
                var point = new Point(Double.Parse(coords[0].Trim()), Double.Parse(coords[1].Trim()));
                polygon.AddPoint(point, number == 0);
            }
            Console.WriteLine(polygon.GetInsideLargestRectangle());
            Console.ReadKey();
        }
        /// <summary>
        /// 构造注册多边形
        /// </summary>
        [CommandMethod("TestPolygon")]
        public static void TestPolygon()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            Polyline pl = new Polyline
            {
                Closed = true
            };
            Point2d[] point2s = new Point2d[]
                {
                    new Point2d(9,-7),
                    new Point2d(12,-6),
                    new Point2d(8,3),
                    new Point2d(10,6),
                    new Point2d(12,7),
                    new Point2d(1,9),
                    new Point2d(-8,7),
                    new Point2d(-6,6),
                    new Point2d(-4,6),
                    new Point2d(-6,2),
                    new Point2d(-6,0),
                    new Point2d(-7,-5),
                    new Point2d(-2,-7),
                    new Point2d(1,-3),
                    new Point2d(5,-7),
                    new Point2d(8,-4),
                };
            pl.CreatePolyline(point2s);
            db.AddToModelSpace(pl);
        }

        [CommandMethod("LargestInsideRectangle")]
        public void LargestInsideRectangle()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            //选择多段线 polygon
            TypedValue[] acTypValAr = new TypedValue[1];
            acTypValAr.SetValue(new TypedValue((int)DxfCode.Start, "*POLYLINE"), 0);

            SelectionFilter acSelFtr = new SelectionFilter(acTypValAr);

            PromptSelectionResult acSSPrompt = ed.GetSelection(acSelFtr);

            if (acSSPrompt.Status == PromptStatus.OK)
            {
                SelectionSet acSSet = acSSPrompt.Value;

                var rectList = new List<Entity>();
                using (Transaction trans = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt = trans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord btr = trans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    foreach (SelectedObject selectedObject in acSSet)
                    {
                        if (selectedObject != null)
                        {
                            var polyline = trans.GetObject(selectedObject.ObjectId, OpenMode.ForWrite) as Polyline;

                            //获取所有的子线段
                            var splitedLines = new List<Line>();
                            var pts = CollectPoints(trans, polyline).Cast<Point3d>().ToList();
                            //去重
                            var districtPts = new List<Point3d>();
                            foreach (var pt in pts)
                            {
                                if (districtPts.Any(p => p.IsEqualTo(pt)) || districtPts.Contains(pt)) continue;
                                districtPts.Add(pt);
                            }
                            var n = districtPts.Count;
                            for (int i = 0; i < n; i++)
                            {
                                var l = new Line(districtPts[i], districtPts[(i + 1) % n]);
                                splitedLines.Add(l);
                            }
                            //获取所有线段的角度
                            var angleList = new List<double>();
                            splitedLines.ForEach(l => angleList.Add(l.Angle));//角度列表
                            angleList.Sort();//角度从小到大排序
                            // pi/2间隔去重
                            var trimedAngleList = new List<double>();//清理后的角度

                            //!影响速度，谨慎放入
                            ////初始化 以5° 
                            //Enumerable.Range(0, 90).Where(number => number % 5 == 0).ToList()
                            //    .ForEach(num => trimedAngleList.Add(num * Math.PI / 180));

                            foreach (var a in angleList)
                            {
                                if (trimedAngleList.Contains(a)
                                    || trimedAngleList.Any(angle => Math.Sign(Math.Abs(Math.Abs(a - angle) - Math.PI / 2) - 1e-5) <= 0)
                                    || trimedAngleList.Any(angle => Math.Sign(Math.Abs(Math.Abs(a - angle) - Math.PI) - 1e-5) <= 0)
                                    || trimedAngleList.Any(angle => Math.Sign(Math.Abs(a - angle) - 1e-5) <= 0)
                                    || Math.Sign(Math.Abs(a) - 1e-5) <= 0
                                    || Math.Sign(Math.Abs(a - Math.PI / 2) - 1e-5) <= 0
                                    || Math.Sign(Math.Abs(a - Math.PI) - 1e-5) <= 0
                                    || Math.Sign(Math.Abs(a - Math.PI * 1.5) - 1e-5) <= 0
                                    || Math.Sign(Math.Abs(a - Math.PI * 2) - 1e-5) <= 0)//再去除 0 pi/2 pi...整数倍pi/2的角度

                                {
                                    continue;
                                }
                                trimedAngleList.Add(a);
                            }
                            var maxAreaAngle = 0.0;
                            //优先使用当前状态，即不旋转求内接矩形
                            Polygon polygon = CreatePolygon(polyline);
                            var maxRectangle = polygon.GetInsideLargestRectangle();
                            var maxArea = maxRectangle.Square;
                            if (trimedAngleList.Any())
                            {
                                //角度排序
                                trimedAngleList.Sort();
                                foreach (var a in trimedAngleList)
                                {
                                    var clonedPl = polyline.Clone() as Polyline;
                                    //旋转到水平角度
                                    ((Entity)clonedPl).Rotate(polyline.GetPointAtParameter(0), -a);

                                    //转换为自定义的polygon
                                    polygon = CreatePolygon(clonedPl);
                                    //得到自定义的rectangle
                                    var rectangle = polygon.GetInsideLargestRectangle();
                                    double square = rectangle.Square;
                                    if (square > maxArea)
                                    {
                                        maxArea = square;
                                        maxRectangle = rectangle;
                                        maxAreaAngle = a;
                                    }
                                }
                            }
                            if (maxRectangle != null)
                            {
                                //转换为cad的polygon
                                Polyline cadRect = GetPolyline(maxRectangle);
                                cadRect.Rotate(polyline.GetPointAtParameter(0), maxAreaAngle);//再转回去
                                if (cadRect != null) cadRect.ColorIndex = 1;
                                rectList.Add(cadRect);

                            }
                        }
                    }
                    trans.Commit();
                }

                //在图中给出出来
                rectList.ForEach(ent => db.AddToModelSpace(ent));
            }
            else
            {
                Application.ShowAlertDialog("未选择任何对象!");
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
                        Point3d pt = cur.GetPointAtParameter(cur.StartParam + (i * param / (segs - 1)));
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
                        pts.Add(pl.EvaluatePoint(pt.RotateBy(txt.Rotation, Point2d.Origin)));
                    }
                }
            }
        }



        private static Polyline GetPolyline(Rectangle rectangle)
        {
            var pl = new Polyline();
            Point2d pt1 = GetPoint2d(rectangle.LeftBottom);
            Point2d pt2 = GetPoint2d(rectangle.RightTop);
            pl.CreateRectangle(pt1, pt2);

            return pl;
        }

        private static Point2d GetPoint2d(Point pt)
        {
            return new Point2d(pt.X, pt.Y);
        }

        /// <summary>
        /// 从多段线创建自定义多边形
        /// </summary>
        /// <param name="pl"></param>
        /// <returns></returns>
        private static Polygon CreatePolygon(Polyline pl)
        {
            var polygon = new Polygon();
            int number = pl.NumberOfVertices;
            while (number-- > 0)
            {
                var cadPt = pl.GetPoint2dAt(number);
                var point = new Point(cadPt.X, cadPt.Y);
                if (polygon.Contains(point)||polygon.Points.Any(p=>p.GetDistance(point)<10e-5))
                {
                    continue;
                }
                polygon.AddPoint(point, number == 0);
            }

            return polygon;
        }
    }
}