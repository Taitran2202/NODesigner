using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Processing
{
    public struct Point
    {
        public double X;
        public double Y;
        public Point(double X,double Y)
        {
            this.X = X;
            this.Y = Y;
        }
    }
    
    public class RANSAC
    {
        static double Distance(Point x,Point y)
        {
            return (x.X - y.X) * (x.X - y.X) + (x.Y - y.Y) * (x.Y - y.Y);
        }
        static IEnumerable<Point> Translate(IEnumerable<Point> input,double tx,double ty)
        {
            return input.Select(x => new Point(x.X + tx, x.Y + ty));
        }
        public  static List<Point> Match(IEnumerable<Point>  modelPoint, IEnumerable<Point> obserPoint)
        {
            var sortedModelPoint=modelPoint;
            var sortedObserPoint = obserPoint.OrderBy(x => x.Y).OrderBy(x => x.X);
            double maxy = modelPoint.Max(x => x.Y);
            var firstPoint = modelPoint.OrderBy(x=>x.X+ maxy-x.Y).First();
            double min_error = 999999999;
            List<Point> BestMatchedPoint=null;
            foreach (var item in sortedObserPoint)
            {
                //get translate
                double tx = item.X - firstPoint.X;
                double ty = item.Y - firstPoint.Y;
                //translate
                var translatedModelPoint = Translate(sortedModelPoint,tx,ty);
                List<Point> MatchedPoint = new List<Point>();
                double error = 0;
                var sortedObserPointCopy = new List<Point>(sortedObserPoint);
                foreach (var item2  in translatedModelPoint)
                {
                    if (sortedObserPointCopy.Count == 0)
                    {
                        break;
                    }
                    var clostedPoint = sortedObserPointCopy.OrderBy(x => Distance(item2, x)).First();
                    MatchedPoint.Add(clostedPoint);
                    error += Distance(item2, clostedPoint);
                    sortedObserPointCopy.Remove(clostedPoint);
                }
                if (error < min_error)
                {
                    min_error = error;
                    BestMatchedPoint = MatchedPoint;
                }

            }
            List<Point> BestMatchedPointFiltered = new List<Point>();
            var distances = new List<double>();
            for(int i=0;i<BestMatchedPoint.Count;i++)
            {
                distances.Add(Distance(BestMatchedPoint[i],sortedModelPoint.ElementAt(i)));
            }
            double average = distances.Average();
            var distancesNorm = distances.Select(x => Math.Abs(x - average)).ToList();
            for(int i = 0; i < distancesNorm.Count; i++)
            {
                if (distancesNorm[i] < 20000)
                {
                    BestMatchedPointFiltered.Add(BestMatchedPoint[i]);
                }
            }
            return BestMatchedPointFiltered;

            
        }
    }
}
