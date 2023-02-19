using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAPITrainingCreateModel
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            double a = 10000;
            double b = 5000;

            List<Wall> walls = new List<Wall>();
            List<XYZ> wPoints = GetWallPointsByWidthDepth(a, b);
            List<Level> listLevel = GetLevels();
            Level level1 = GetLevelByName(listLevel, "Уровень 1");
            Level level2 = GetLevelByName(listLevel, "Уровень 2");


            //Получение списка всех уровней
            List<Level> GetLevels()
            {
                List<Level> allLevel = new FilteredElementCollector(doc)
               .OfClass(typeof(Level))
               .OfType<Level>()
               .ToList();
                return allLevel;
            }

            //Получение уровня по наименованию
            Level GetLevelByName(List<Level> allLev, string s)
            {
                Level lvl = allLev
                .Where(x => x.Name.Equals(s))
                .FirstOrDefault();
                return lvl;
            }

            // Получение списка точек для построение стен по заданной ширине w и глубине d
            List<XYZ> GetWallPointsByWidthDepth(double w, double d)
            {
                double width = UnitUtils.ConvertToInternalUnits(w, UnitTypeId.Millimeters);
                double depth = UnitUtils.ConvertToInternalUnits(d, UnitTypeId.Millimeters);
                double dx = width / 2;
                double dy = depth / 2;

                List<XYZ> points = new List<XYZ>();
                points.Add(new XYZ(-dx, -dy, 0));
                points.Add(new XYZ(dx, -dy, 0));
                points.Add(new XYZ(dx, dy, 0));
                points.Add(new XYZ(-dx, dy, 0));
                points.Add(new XYZ(-dx, -dy, 0));

                return points;
            }

            // Создание стен по линиям, образованным из списка точек
            Transaction transaction = new Transaction(doc, "Построение стен");
            transaction.Start();
            for (int i = 0; i < (wPoints.Count - 1); i++)
            {
                Line line = Line.CreateBound(wPoints[i], wPoints[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
              
                if (i>0)
                {
                 AddWindows(doc, level1, walls[i]);
                }
                

            }
            AddDoor(doc, level1, walls[0]);
            transaction.Commit();
       
            return Result.Succeeded;
        }

        // Добавление двери
        private void AddDoor(Document doc, Level level1, Wall wall)
        {
            FamilySymbol doorType =new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1+ point2)/2;

            if (!doorType.IsActive)
                doorType.Activate();

            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);
        }


        // Добавление окон
        private void AddWindows(Document doc, Level level1, Wall wall)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 1830 мм"))
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            if (!windowType.IsActive)
                windowType.Activate();

            doc.Create.NewFamilyInstance(point, windowType, wall, level1, StructuralType.NonStructural);
        }
        }
}
