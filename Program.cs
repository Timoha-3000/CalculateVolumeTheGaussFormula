using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CalculateVolumeTheGaussFormula
{
    internal class Program
    {
        static void Main(string[] args)
        {
        }

        /// <summary>
        /// Считаем объём построения
        /// </summary>
        /// <param name="progress"> поле прогрессБара</param>
        /// <param name="isProgressOn"> нужно ли отображать прогресс или нет</param>
        /// <param name="token"> токен-уведомление об отмене действия</param>
        /// <param name="calculatedTime"></param>
        public List<double> CalculateVolume(IProgress<int> progress, bool isProgressOn)
        {
            List<double> calculatedTime = new List<double>();
            if (_repositoryLayer.Layers == null || _repositoryLayer.Count == 0)
            {
                return new List<double>();
            }
            else
            {
                int layerNumber = 0;

                if (isProgressOn)
                {
                    foreach (var layer in _repositoryLayer.Layers)
                    {
                        calculatedTime.Add(CalculateVolumeOnLayer(layer));

                        progress.Report(++layerNumber / _repositoryLayer.Count * 100);
                    }
                    progress.Report(100);
                }
                else
                {
                    foreach (var layer in _repositoryLayer.Layers)
                        calculatedTime.Add(CalculateVolumeOnLayer(layer));
                }
                _endOperationReached?.Invoke(false, new EventArgs());
            }
            return calculatedTime;
        }

        /// <summary>
        /// Подсчёт площади одного слоя
        /// </summary>
        /// <param name="layer"> слой</param>
        /// <returns>площадь одного слоя в миллиметрах</returns>
        private double CalculateVolumeOnLayer(Layer layer)
        {
            double area = 0;

            foreach (var geom in layer.Geometry)
            {
                List<IList<Point_2d>> realSortBounds = geom.RealBoundaries.ToList();
                realSortBounds.Sort((list1, list2) =>
                {
                    Point_2d topRight1 = FindTopRightMostPoint(list1);
                    Point_2d topRight2 = FindTopRightMostPoint(list2);
                    if (topRight1.X == topRight2.X)
                        return topRight1.Y.CompareTo(topRight2.Y);
                    return topRight2.X.CompareTo(topRight1.X);
                });

                foreach (var realBoundaries in realSortBounds)
                {
                    if (layer.Id == 4)
                        area = area;
                    if (CheckingIfIsItHole(realBoundaries, geom.RealBoundaries))
                        area += CalculateTheGaussFormula(realBoundaries.ToList());
                    else
                        area -= CalculateTheGaussFormula(realBoundaries.ToList());
                }
            }

            return area; // перевожу из миллиметров в метры
        }

        /// <summary>
        /// Метод для нахождения самой правой верхней точки в списке точек
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private Point_2d FindTopRightMostPoint(IList<Point_2d> points)
        {
            if (points == null || points.Count == 0)
                throw new ArgumentException("Список точек не может быть пустым");

            return points.Aggregate((maxPoint, next) => next.X > maxPoint.X || (next.X == maxPoint.X && next.Y > maxPoint.Y) ? next : maxPoint);
        }

        /// <summary>
        /// Подсчёт площади одной детали, по формуле Гаусса
        /// </summary>
        /// <param name="bound"> реальная граница изделия</param>
        /// <returns></returns>
        private double CalculateTheGaussFormula(List<Point_2d> bound)
        {
            int j = bound.Count - 1;
            double area = 0;

            for (int i = 0; i < bound.Count; i++)
            {
                area += (bound[j].X + bound[i].X) * (bound[j].Y - bound[i].Y);
                j = i;  // j следует за i
            }

            return Math.Abs(area / 2);
        }

        private bool CheckingIfIsItHole(IList<Point_2d> pointForCheck, IList<IList<Point_2d>> realBounds)
        {
            foreach (var bound in realBounds)
                if (pointForCheck == bound)
                    return true;
                else
                    foreach (var point in pointForCheck)
                        if (!IsPointInPolygon(bound, point))
                            return false;

            return true;
        }

        public bool IsPointInPolygon(IList<Point_2d> polygon, Point_2d testPoint)
        {
            bool result = false;
            int n = polygon.Count;

            for (int i = 0, j = n - 2; i < n; j = i++)
            {
                // Проверяем, лежит ли точка на отрезке между polygon[i] и polygon[j]
                if (IsPointOnLine(polygon[i], polygon[j], testPoint))
                    return true; // Если точка на отрезке, считаем, что она находится на внешнем контуре

                // Проверяем пересекает ли горизонтальный луч из testPoint ребро многоугольника
                if (((polygon[i].Y > testPoint.Y) != (polygon[j].Y > testPoint.Y)) &&
                    (testPoint.X < (polygon[j].X - polygon[i].X) * (testPoint.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X))
                {
                    result = !result;
                }
            }

            if (!result)
                return false;

            return result;
        }

        /// <summary>
        /// Метод для проверки нахождения точки на прямой, образованной двумя точками
        /// </summary>
        /// <param name="p1">точка 1 образующая прямую</param>
        /// <param name="p2">точка 1 образующая прямую</param>
        /// <param name="testPoint">точка которую необходимо проверить</param>
        /// <returns></returns>
        private bool IsPointOnLine(Point_2d p1, Point_2d p2, Point_2d testPoint)
        {
            // Вычисляем определитель для проверки коллинеарности
            return (p2.Y - p1.Y) * (testPoint.X - p1.X) - (p2.X - p1.X) * (testPoint.Y - p1.Y) == 0;
        }

    }
}
