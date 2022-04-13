using NewLaserProject.Classes;
using NewLaserProject.Classes.Geometry;
using NUnit.Framework;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace NewLaserTest
{
    public class Tests
    {
        enum Place
        {
            one, two, three
        }

        CoorSystem<Place>.ThreePointCoorSystemBuilder<Place> _builder1;

        [SetUp]
        public void Setup()
        {
            _builder1 = CoorSystem<Place>.GetThreePointSystemBuilder();

            //_builder1.SetFirstPointPair(new PointF(0, 0), new PointF(120, 96))
            //         .SetSecondPointPair(new PointF(0, 48), new PointF(120, 192))
            //         .SetThirdPointPair(new PointF(60, 48), new PointF(223.92304845F, 252.24326448F));

            //_builder1.SetFirstPointPair(new PointF(0, 0), new PointF(0, 0))
            //         .SetSecondPointPair(new PointF(0, 48), new PointF(-24, 41.56921938F))
            //         .SetThirdPointPair(new PointF(60, 48), new PointF(27.96152423F, 71.56921938F));

            //_builder1.SetFirstPointPair(new PointF(0, 0), new PointF(0, 0))
            //         .SetSecondPointPair(new PointF(0, 48), new PointF(27.71281292F, 48))
            //         .SetThirdPointPair(new PointF(60, 48), new PointF(87.71281292F, 48));

            _builder1.SetFirstPointPair(new PointF(0, 48), new PointF(-63.71281292F, 151.42562584F))
                     .SetSecondPointPair(new PointF(60, 48), new PointF(5.56921938F, 220.70765814F))
                     .SetThirdPointPair(new PointF(60, 0), new PointF(101.56921938F, 124.70765814F));
        }

        //[Test]
        public void Test1()
        {
            var stages = new string[]
            {
                "I'm going under camera",
                "I'm going under laser for shooting",
                "I'm going to load point"
            };
            var controlString = string.Empty;
            var cbBuilder = CameraOffsetTeacher.GetBuilder();
            cbBuilder.SetOnGoUnderCameraAction(() => Task.Run(() => { controlString = stages[0]; }));
            cbBuilder.SetOnGoToShotAction(() => Task.Run(() => { controlString = stages[1]; }));
            cbBuilder.SetOnGoLoadPointAction(() => Task.Run(() => { controlString = stages[2]; }));


            var teachCameraBias = cbBuilder.Build();
            teachCameraBias.Accept();
            Assert.That(controlString == stages[2]);
            teachCameraBias.Next();
            Assert.That(controlString == stages[0]);
            teachCameraBias.Next();
            Assert.That(controlString == stages[1]);
            //teachCameraBias.Accept();

        }

        [Test]
        // [TestCase(30,0,60,30)]

        //[TestCase(8.0377905, 38.16272951, 133.922, 180.396)]
        //[TestCase(40.38412066, 24, 189.947, 184.548)]
        //[TestCase(0, 0, 120, 96)]
        //[TestCase(0, 48, 120, 192)]
        //[TestCase(60, 48, 223.92304845, 252.24326448)]
        public void CoorSystemTest(double x1, double y1, double x2, double y2)
        {
            
            var coorSys = _builder1.FormWorkMatrix().Build();

            var initial = (x1, y1);
            var expected = (Math.Round(x2,3), Math.Round(y2,3));

            var result = coorSys.ToGlobal(initial.x1, initial.y1);
            var res = (Math.Round(result[0], 3), Math.Round(result[1], 3));
            Assert.That(res, Is.EqualTo(expected));
        }

        [TestCase(0, 48, 27.71281292, 48)]
        [TestCase(60, 48, 87.71281292, 48)]
        public void CoorSystemSkewTest(double x1, double y1, double x2, double y2)
        {

            var coorSys = _builder1.FormWorkMatrix(CoorSystem<Place>.Transformation.Skew).Build();

            var initial = (x1, y1);
            var expected = (Math.Round(x2, 3), Math.Round(y2, 3));

            var result = coorSys.ToGlobal(initial.x1, initial.y1);
            var res = (Math.Round(result[0], 3), Math.Round(result[1], 3));
            Assert.That(res, Is.EqualTo(expected));
        }

        //[TestCase(0, 48, 120, 144)]
        //[TestCase(60, 48, 180, 144)]
        public void CoorSystemTranslateTest(double x1, double y1, double x2, double y2)
        {

            var coorSys = _builder1.FormWorkMatrix(CoorSystem<Place>.Transformation.Translation).Build();

            var initial = (x1, y1);
            var expected = (Math.Round(x2, 3), Math.Round(y2, 3));

            var result = coorSys.ToGlobal(initial.x1, initial.y1);
            var res = (Math.Round(result[0], 3), Math.Round(result[1], 3));
            Assert.That(res, Is.EqualTo(expected));
        }

        //[TestCase(0, 48, -24, 41.56921938)]
        //[TestCase(60, 48, 27.96152423, 71.56921938)]
        public void CoorSystemRotateTest(double x1, double y1, double x2, double y2)
        {

            var coorSys = _builder1.FormWorkMatrix(CoorSystem<Place>.Transformation.Rotation).Build();

            var initial = (x1, y1);
            var expected = (Math.Round(x2, 3), Math.Round(y2, 3));

            var result = coorSys.ToGlobal(initial.x1, initial.y1);
            var res = (Math.Round(result[0], 3), Math.Round(result[1], 3));
            Assert.That(res, Is.EqualTo(expected));
        }
        
        //[TestCase(0, 48, 0, 96)]
        //[TestCase(60, 48, 120, 96)]
        public void CoorSystemScaleTest(double x1, double y1, double x2, double y2)
        {

            var coorSys = _builder1.FormWorkMatrix(CoorSystem<Place>.Transformation.Scaling).Build();

            var initial = (x1, y1);
            var expected = (Math.Round(x2, 3), Math.Round(y2, 3));

            var result = coorSys.ToGlobal(initial.x1, initial.y1);
            var res = (Math.Round(result[0], 3), Math.Round(result[1], 3));
            Assert.That(res, Is.EqualTo(expected));
        }
    }
}