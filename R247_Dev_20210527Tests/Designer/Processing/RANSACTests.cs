using Microsoft.VisualStudio.TestTools.UnitTesting;
using NOVisionDesigner.Designer.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NOVisionDesigner.Designer.Processing.Tests
{
    [TestClass()]
    public class RANSACTests
    {
        [TestMethod()]
        public void MatchTest()
        {
            RANSAC test = new RANSAC();
            List<Point> model = new List<Point>()
            {
                new Point(1,2),new Point(2,3),new Point(4,5),new Point(6,2),new Point(7,9)

            };
            List<Point> obser = new List<Point>()
            {
                new Point(1,2),new Point(2,3),new Point(4,5),new Point(6,2),new Point(7,9),new Point(8,9), new Point(7,20),
                 new Point(7,5), new Point(9,11), new Point(4,14), new Point(8,19)

            };
            Random rnd = new Random();
            //translate obser
            obser = obser.Select(x => new Point(x.X + 10, x.Y + 10)).OrderBy(x=>rnd.Next(obser.Count)).ToList();
            //test.Match(model, obser);
        }
    }
}