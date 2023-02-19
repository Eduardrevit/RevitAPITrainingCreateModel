using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;



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
            double c = 1500; // Высота крыши
            double e = 400; // Свес крыши

            List<Wall> walls = new List<Wall>();
            List<XYZ> wPoints = GetWallPointsByWidthDepth(a, b);
            List<Level> listLevel = GetLevels();
            Level level1 = GetLevelByName(listLevel, "Уровень 1");
            Level level2 = GetLevelByName(listLevel, "Уровень 2");
            double zz = level2.Elevation - level1.Elevation; 


            //Получение списка всех уровней
            List <Level> GetLevels()
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

                if (i > 0)
                {
                    AddWindows(doc, level1, walls[i]);
                }


            }
            AddDoor(doc, level1, walls[0]);
            AddRoof(doc, level2, a, b, c, e, zz);

            transaction.Commit();

            return Result.Succeeded;
        }

        // Добавление двери
        private void AddDoor(Document doc, Level level1, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

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

      
        private void AddRoof(Document doc, Level lvl1, double x, double y, double z, double r, double dl)
        {

            double x1=UnitUtils.ConvertToInternalUnits(x, UnitTypeId.Millimeters);
            double y1 = UnitUtils.ConvertToInternalUnits(y, UnitTypeId.Millimeters);
            double z1 = UnitUtils.ConvertToInternalUnits(z, UnitTypeId.Millimeters);
            double r1 = UnitUtils.ConvertToInternalUnits(r, UnitTypeId.Millimeters);
          

            // Фильтр RoofType
            RoofType roofType = new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType))
                .OfType<RoofType>()
                .Where(n => n.Name.Equals("Типовой - 400мм"))
                .Where(n => n.FamilyName.Equals("Базовая крыша"))
                .FirstOrDefault();
           

            // Создание профиля
            CurveArray profile = new CurveArray();
            profile.Append(Line.CreateBound(new XYZ(0, -y1/2-r1, dl), new XYZ(0, 0, z1+ dl)));
            profile.Append(Line.CreateBound(new XYZ(0, 0, z1 + dl), new XYZ(0, y1/2+r1, dl)));

            ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(-x1/2, -y1/2, 0), new XYZ(-x1/2, -y1/2, z1), new XYZ(0, y1/2, 0), doc.ActiveView);
            doc.Create.NewExtrusionRoof(profile, plane, lvl1, roofType, 0, x1);

        }

    }
}
