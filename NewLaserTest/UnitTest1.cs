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

        CoorSystem<Place>.ThreePointCoorSystemBuilder<Place> _builder;
        CoorSystem<Place>.ThreePointCoorSystemBuilder<Place> _builderPureDeformation;


        [SetUp]
        public void Setup()
        {

            _builder = CoorSystem<Place>.GetThreePointSystemBuilder();

            _builder.SetFirstPointPair(new PointF(0, 0), new PointF(120, 96))
                     .SetSecondPointPair(new PointF(60, 0), new PointF(235.91109915F, 127.05828541F))
                     .SetThirdPointPair(new PointF(60, 48), new PointF(153.68364061F, 219.78716474F));

            _builderPureDeformation = CoorSystem<Place>.GetThreePointSystemBuilder();

            _builderPureDeformation.SetFirstPointPair(new PointF(0, 0), new PointF(120, 96))
                                   .SetSecondPointPair(new PointF(120, 96), new PointF(132.900991F, 158.833326F))
                                   .SetThirdPointPair(new PointF(120, 0), new PointF(176.859624F, 115.156805F));
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

        [TestCase(0, 0, 120, 96)]
        [TestCase(60, 0, 235.91109915F, 127.05828541F)]
        [TestCase(60, 48, 153.68364061F, 219.78716474F)]
        [TestCase(270, 50, 555.94634354F, 332.35486698F)]
        [TestCase(-180, -80, -90.68753322F, -151.72298844F)]

        public void CoorSystemTestMainTransformation(double x1, double y1, double x2, double y2)
        {

            var coorSys = _builder.FormWorkMatrix(2, 2, false).Build();

            var initial = (x1, y1);
            var expected = (Math.Round(x2, 3), Math.Round(y2, 3));

            var result = coorSys.ToGlobal(initial.x1, initial.y1);
            var res = (Math.Round(result[0], 3), Math.Round(result[1], 3));
            Assert.That(res, Is.EqualTo(expected));
        }

        [TestCase(0, 96, -55.42562584F, 110.85125168F)]
        [TestCase(120, 96, 64.57437416F, 110.85125168F)]
        public void CoorSystemTestPureDeformation(double x1, double y1, double x2, double y2)
        {

            var coorSys = _builderPureDeformation.FormWorkMatrix(0.5, 0.5, true).Build();

            var initial = (x1, y1);
            var expected = (Math.Round(x2, 3), Math.Round(y2, 3));

            var result = coorSys.ToGlobal(initial.x1, initial.y1);
            var res = (Math.Round(result[0], 3), Math.Round(result[1], 3));
            Assert.That(res, Is.EqualTo(expected));
        }


        [TestCase(0, 96, -55.42562584F, 110.85125168F)]
        [TestCase(120, 96, 64.57437416F, 110.85125168F)]
        [TestCase(-180, -80, -133.81197846F, -92.37604307F)]

        public void CoorSystemTestPureDeformation2(double x1, double y1, double x2, double y2)
        {

            var coorSys = _builder.FormWorkMatrix(2, 2, true).Build();

            var initial = (x1, y1);
            var expected = (Math.Round(x2, 3), Math.Round(y2, 3));

            var result = coorSys.ToGlobal(initial.x1, initial.y1);
            var res = (Math.Round(result[0], 3), Math.Round(result[1], 3));
            Assert.That(res, Is.EqualTo(expected));
        }

        [TestCase(0, 96, -82.22745855F, 92.72887932F)]
        [TestCase(120, 96, 33.68364061F, 123.78716474F)]
        [TestCase(120, 0, 115.91109915F, 31.05828541F)]

        public void CoorSystemTestRotatePureDeformation(double x1, double y1, double x2, double y2)
        {

            var coorSys = _builder.FormWorkMatrix(2, 2, true).Build();

            coorSys.BuildRelatedSystem()
                   .Rotate(15 * Math.PI / 180)
                   .Build(Place.one);

            var initial = (x1, y1);
            var expected = (Math.Round(x2, 3), Math.Round(y2, 3));

            var result = coorSys.ToSub(Place.one, initial.x1, initial.y1);
            var res = (Math.Round(result[0], 3), Math.Round(result[1], 3));
            Assert.That(res, Is.EqualTo(expected));
        }
    }
}