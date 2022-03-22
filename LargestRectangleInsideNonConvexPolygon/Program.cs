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

        [CommandMethod("AddLine")]
        public static void AddLine()
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
                            //转换为自定义的polygon
                            Polygon polygon = CreatePolygon(polyline);
                            //得到自定义的rectangle
                            var rectangle = polygon.GetInsideLargestRectangle();
                            //转换为cad的polygon
                            Polyline cadRect = GetPolyline(rectangle);
                            if (cadRect != null) cadRect.ColorIndex = 1;
                            rectList.Add(cadRect);
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
                polygon.AddPoint(point, number == 0);
            }

            return polygon;
        }
    }
}