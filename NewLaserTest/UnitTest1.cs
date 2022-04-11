using NewLaserProject.Classes;
using NewLaserProject.Classes.Geometry;
using NUnit.Framework;
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
        [SetUp]
        public void Setup()
        {
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
        [TestCase(30,0,60,30)]
        public void CoorSystemTest(double x1, double y1, double x2, double y2)
        {

            var coorSys = new CoorSystem<Place>(first:(new PointF(0,0),new PointF(0,0)),
                second: (new PointF(0, 20), new PointF(20, 20)),
                third: (new PointF(30, 20), new PointF(80, 50)));

            var initial = (x1, y1);
            var expected = (x2, y2);

            var result = coorSys.ToGlobal(initial.x1, initial.y1);
            Assert.That((result[0],result[1]), Is.EqualTo(expected));
        }
    }
}