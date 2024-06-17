using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using MachineClassLibrary.GeometryUtility;
using MachineClassLibrary.Laser.Entities;
using NewLaserProject.Classes;
using Stateless;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NewLaserTest
{

    public class Tests
    {
        enum Place
        {
            one, two, three
        }

        CoorSystem<Place>.ThreePointCoorSystemBuilder _builder;
        CoorSystem<Place>.ThreePointCoorSystemBuilder _builder2;
        CoorSystem<Place>.ThreePointCoorSystemBuilder _builder3;


        CoorSystem<Place>.ThreePointCoorSystemBuilder _builderPureDeformation;
        //MainCoorSystem<Place>.ThreePointCoorSystemBuilder _builder;

        [SetUp]
        public void Setup()
        {

            _builder = CoorSystem<Place>.GetThreePointSystemBuilder();
            _builder2 = CoorSystem<Place>.GetThreePointSystemBuilder();
            _builder3 = CoorSystem<Place>.GetThreePointSystemBuilder();



            //_builder = MainCoorSystem<Place>.GetThreePointSystemBuilder();

            _builder.SetFirstPointPair(new PointF(0, 0), new PointF(120, 96))
                     .SetSecondPointPair(new PointF(60, 0), new PointF(235.91109915F, 127.05828541F))
                     .SetThirdPointPair(new PointF(60, 48), new PointF(153.68364061F, 219.78716474F));

            _builder2.SetFirstPointPair(new(0f, 10000f), new(32.483199f, 38.69009f))
                .SetSecondPointPair(new(20000f, 10000f), new(54.34078f, 42.780718f))
                .SetThirdPointPair(new(20000f, 0f), new(49.948233f, 31.181165f));

            _builder3.SetFirstPointPair(new(0f, 0f), new(7f, 16f))
                .SetSecondPointPair(new(48000f, 0f), new(99.728879f, 40.846628f))
                .SetThirdPointPair(new(48000f, 60000f), new(99.728879f, 165.07977f));
                


            _builderPureDeformation = CoorSystem<Place>.GetThreePointSystemBuilder();

            _builderPureDeformation.SetFirstPointPair(new PointF(0, 0), new PointF(120, 96))
                                   .SetSecondPointPair(new PointF(120, 96), new PointF(132.900991F, 158.833326F))
                                   .SetThirdPointPair(new PointF(120, 0), new PointF(176.859624F, 115.156805F));
        }

        [TestCase(0f, 0f, 7f, 16f)]
        [TestCase(48000f, 0f,99.728879f, 40.846628f)]
        [TestCase(48000f, 60000f, 99.728879f, 165.07977f)]
        
        [TestCase(11000f, 10000f, 28.250368f, 42.399543f)]
        [TestCase(11000f, 20000f, 28.250368f, 63.105066f)]
        [TestCase(11000f, 33000f, 28.250368f, 90.022247f)]
        [TestCase(11000f, 50000f, 28.250368f, 125.221637f)]


        public void CoorSystemTestMainTransformation3(double x1, double y1, double x2, double y2)
        {
            var coorSys = _builder3.FormWorkMatrix(0.001, 0.001).Build();
            TestAsertion(coorSys.ToGlobal, x1, y1, x2, y2);
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

        [TestCase(814.666, 2057.855, 29.884906, 29.644182)]
        [TestCase(6187.603, 6483.988, 37.701076, 35.877232)]
        [TestCase(17560.537, 8666.873, 51.08916, 40.735404)]
        public void CoorSystemTestMainTransformation2(double x1, double y1, double x2, double y2)
        {
            var coorSys = _builder2.FormWorkMatrix(0.001, 0.001).Build();
            var pureCoorSys = _builder2.BuildPure();
            var theMatrix = pureCoorSys.GetMainMatrixElements().GetMatrix3();


            var pureCoorSys2 = new PureCoorSystem<Place>(theMatrix);

            var coorSys1 = pureCoorSys2.GetTwoPointSystemBuilder()
                .SetFirstPointPair(new(5010.381f, 1679.401f), new(34.304077f, 30.063347f))
                .SetSecondPointPair(new(11488f, 4737.625f), new(42.726671f, 34.935628f))
                .FormWorkMatrix(0.001, 0.001)
                .Build();

            TestAsertion(coorSys1.ToGlobal, x1, y1, x2, y2);
        }

        [TestCase(0, 0, 120, 96)]
        [TestCase(60, 0, 235.91109915F, 127.05828541F)]
        [TestCase(60, 48, 153.68364061F, 219.78716474F)]
        [TestCase(270, 50, 555.94634354F, 332.35486698F)]
        [TestCase(-180, -80, -90.68753322F, -151.72298844F)]

        public void CoorSystemTestMainTransformation(double x1, double y1, double x2, double y2)
        {
            var coorSys = _builder.FormWorkMatrix(2, 2).Build();

            TestAsertion(coorSys.ToGlobal, x1, y1, x2, y2);
        }

        [TestCase(0, 96, -55.42562584F, 110.85125168F)]
        [TestCase(120, 96, 64.57437416F, 110.85125168F)]
        public void CoorSystemTestPureDeformation(double x1, double y1, double x2, double y2)
        {
            var coorSys = _builderPureDeformation.FormWorkMatrix(0.5, 0.5).BuildPure();

            TestAsertion(coorSys.ToGlobal, x1, y1, x2, y2);
        }


        [TestCase(120, 0, 125.91109915F, 41.05828541F)]
        [TestCase(120, 96, 43.68364061F, 133.78716474F)]
        public void CoorSystemTestToSubPureDeformation(double x1, double y1, double x2, double y2)
        {
            var coorSys = _builderPureDeformation.FormWorkMatrix(0.5, 0.5).BuildPure();
            coorSys.BuildRelatedSystem()
                   .Rotate(15 * Math.PI / 180)
                   .Translate(10, 10)
                   .Build(Place.one);

            TestAsertion((a, b) => coorSys.ToSub(Place.one, a, b), x1, y1, x2, y2);
        }


        [TestCase(0, 96, -55.42562584F, 110.85125168F)]
        [TestCase(120, 96, 64.57437416F, 110.85125168F)]
        [TestCase(-180, -80, -133.81197846F, -92.37604307F)]

        public void CoorSystemTestPureDeformation2(double x1, double y1, double x2, double y2)
        {
            var coorSys = _builder.FormWorkMatrix(2, 2).BuildPure();

            TestAsertion((a, b) => coorSys.ToGlobal(a, b), x1, y1, x2, y2);
        }

        [TestCase(0, 96, -82.22745855F, 92.72887932F)]
        [TestCase(120, 96, 33.68364061F, 123.78716474F)]
        [TestCase(120, 0, 115.91109915F, 31.05828541F)]

        public void CoorSystemTestRotatePureDeformation(double x1, double y1, double x2, double y2)
        {
            var coorSys = _builder.FormWorkMatrix(2, 2).BuildPure();

            coorSys.BuildRelatedSystem()
                   .Rotate(15 * Math.PI / 180)
                   .Build(Place.one);

            TestAsertion((a, b) => coorSys.ToSub(Place.one, a, b), x1, y1, x2, y2);
        }


        [TestCase(0, 96, -35.42562584F, 140.85125168F)]
        [TestCase(120, 96, 84.57437416F, 140.85125168F)]
        [TestCase(120, 0, 140F, 30F)]
        public void CoorSystemTestTranslatePureDeformation(double x1, double y1, double x2, double y2)
        {
            var coorSys = _builder.FormWorkMatrix(2, 2).BuildPure();

            coorSys.BuildRelatedSystem()
                   .Translate(20, 30)
                   .Build(Place.one);

            TestAsertion((a, b) => coorSys.ToSub(Place.one, a, b), x1, y1, x2, y2);
        }

        [TestCase(0, 96, -70.67351338F, 126.88303501F)]
        [TestCase(120, 96, 45.23758578F, 157.94132043F)]
        [TestCase(120, 0, 127.46504433F, 65.2124411F)]
        public void CoorSystemTestTranslateRotatePureDeformation(double x1, double y1, double x2, double y2)
        {
            var coorSys = _builder.FormWorkMatrix(2, 2).BuildPure();

            coorSys.BuildRelatedSystem()
                   .Translate(20, 30)
                   .Rotate(15 * Math.PI / 180)
                   .Build(Place.one);

            TestAsertion((a, b) => coorSys.ToSub(Place.one, a, b), x1, y1, x2, y2);
        }

        [TestCase(0, 96, -62.22745855F, 122.72887932F)]
        [TestCase(120, 96, 53.68364061F, 153.78716474F)]
        [TestCase(120, 0, 135.91109915F, 61.05828541F)]
        public void CoorSystemTestRotateTranslatePureDeformation(double x1, double y1, double x2, double y2)
        {
            var coorSys = _builder.FormWorkMatrix(2, 2).BuildPure();

            coorSys.BuildRelatedSystem()
                   .Rotate(15 * Math.PI / 180)
                   .Translate(20, 30)
                   .Build(Place.one);

            TestAsertion((a, b) => coorSys.ToSub(Place.one, a, b), x1, y1, x2, y2);
        }


        private void TestAsertion(Func<double, double, double[]> func, double x1, double y1, double x2, double y2)
        {
            var expected = (Math.Round(x2, 3), Math.Round(y2, 3));

            var result = func(x1, y1);
            var res = (Math.Round(result[0], 3), Math.Round(result[1], 3));
            Assert.That(res, Is.EqualTo(expected));
        }

        [TestCase(new[] { Transformation.Turn90 }, 1, 60, 48, 59, 1, 47, 59)]
        [TestCase(new[] { Transformation.MirrorX }, 1, 60, 48, 59, 1, 1, 1)]
        [TestCase(new[] { Transformation.MirrorX, Transformation.Scale }, 0.001F, 60000, 48000, 59000, 1000, 1, 1)]
        [TestCase(new[] { Transformation.MirrorY }, 1, 60, 48, 59, 1, 59, 47)]
        [TestCase(new[] { Transformation.Turn90, Transformation.Turn90 }, 1, 60, 48, 59, 1, 59, 1)]
        [TestCase(new[] { Transformation.Turn90, Transformation.MirrorX }, 1, 60, 48, 59, 1, 47, 1)]
        [TestCase(new[] { Transformation.Turn90, Transformation.MirrorY }, 1, 60, 48, 59, 1, 1, 59)]
        [TestCase(new[] { Transformation.Turn90, Transformation.MirrorY, Transformation.MirrorX }, 1, 60, 48, 59, 1, 1, 1)]
        [TestCase(new[] { Transformation.MirrorY, Transformation.Turn90, Transformation.MirrorX }, 1, 60, 48, 59, 1, 1, 1)]
        [TestCase(new[] { Transformation.MirrorX, Transformation.Scale }, 10, 60, 48, 59, 1, 10, 10)]
        [TestCase(new[] { Transformation.MirrorX, Transformation.Scale, Transformation.Scale }, 10, 60, 48, 59, 1, 100, 100)]
        [TestCase(new[] { Transformation.MirrorX, Transformation.Scale, Transformation.Translte }, 10, 60, 48, 59, 1, 15, 16, 5, 6)]
        [TestCase(new[] { Transformation.Scale, Transformation.Turn90 }, 0.001F, 30000, 48000, 1000, 1000, 47, 1)]
        [TestCase(new[] { Transformation.Scale, Transformation.Turn90 }, 0.001F, 30000, 48000, 29075, 42425, 5.575, 29.075)]
        [TestCase(new[] { Transformation.Scale, Transformation.Turn90 }, 0.001F, 30000, 48000, 925, 5575, 42.425, 0.925)]
        [TestCase(new[] { Transformation.Scale, Transformation.Turn90 }, 0.001F, 30000, 48000, 29075, 5575, 42.425, 29.075)]

        [TestCase(new[] { Transformation.Scale, Transformation.Turn90, Transformation.MirrorX, Transformation.Translte }, 0.001F, 60000, 48000, 1000, 1000, 29, 59, -18)]
        [TestCase(new[] { Transformation.Scale, Transformation.Turn90, Transformation.MirrorX, Transformation.Translte }, 0.001F, 60000, 48000, 14500, 24000, 6, 45.5, -18)]
        [TestCase(new[] { Transformation.Scale, Transformation.Turn90, Transformation.MirrorX, Transformation.Translte }, 0.001F, 60000, 48000, 45500, 24000, 6, 14.5, -18)]
        [TestCase(new[] { Transformation.Scale, Transformation.Turn90, Transformation.Translte }, 0.001F, 60000, 48000, 3250, 4100, 34.9, -11.75, -9, -15)]

        public void TestLaserWaferTransformations(Transformation[] trSequence, float scale, double sizeX,
            double sizeY, double x1, double y1, double x2, double y2, float offsetX = 0, float offsetY = 0)
        {
            var wafer = new LaserWafer/*<MachineClassLibrary.Laser.Entities.Point>*/(new[] { new PPoint(x1, y1, 0, new MachineClassLibrary.Laser.Entities.Point(), "", 0) }, (sizeX, sizeY));
            foreach (var tr in trSequence)
            {
                switch (tr)
                {
                    case Transformation.MirrorX:
                        wafer.MirrorX();
                        break;
                    case Transformation.MirrorY:
                        wafer.MirrorY();
                        break;
                    case Transformation.Turn90:
                        wafer.Turn90();
                        break;
                    case Transformation.Scale:
                        wafer.Scale(scale);
                        break;
                    case Transformation.Translte:
                        wafer.OffsetX(offsetX);
                        wafer.OffsetY(offsetY);
                        break;
                }
            }

            var point = wafer[0];
            ClassicAssert.AreEqual(point.X, x2, 0.0001);
            ClassicAssert.AreEqual(point.Y, y2, 0.0001);
            
        }
        public enum Transformation
        {
            MirrorX,
            MirrorY,
            Turn90,
            Scale,
            Translte
        }


        [TestCaseSource(nameof(TestCaseSourceData))]
        public void TestPCurveTransformation(IEnumerable<(double X, double Y, double Bulge)> vertices)
        {
            var points = new[] { (-45, 28, 0), (1, 11, 1) };
            var curve = new Curve(vertices, true);
            var pcurve = new PCurve(0, 0, 0, curve, "layer", 1);
            pcurve.SetMirrorX(true);
            var transCurve = pcurve.PObject;
            Assert.That(true);
        }

        public static IEnumerable<TestCaseData> TestCaseSourceData()
        {
            yield return new TestCaseData(new List<(double,double,double)> { (-45, 28, 0), (25, 36, 0), (43,-12,0),(2,-30,0),(-20,6,0) });
        }

        //[Test]
        //public void TestOfTree()
        //{
        //    var expectedStr = "ch1ch2t1bbch1ch2t1bb";
        //    var sb = new StringBuilder();
        //    var tree1 = ActionTree.SetAction(() => { sb.Append("t1"); });
        //    var mainTree = ActionTree.StartLoop(2)
        //                 .AddChild(ActionTree.SetAction(() => { sb.Append("ch1"); }))
        //                 .AddChild(ActionTree.SetAction(() => { sb.Append("ch2"); }))
        //                 .AddChild(tree1)
        //                 .AddChild(
        //                            ActionTree.StartLoop(2)
        //                                .AddChild(ActionTree.SetAction(() => { sb.Append("b"); }))
        //                                .EndLoop
        //                           )
        //                 .EndLoop;
        //    mainTree.DoAction();
        //    Assert.That(sb.ToString() == expectedStr);
        //}
        //[TestCase("CircleListing.json", "t1z2d3t4z5z5z5d6z7z7d6z7z7t4z5z5z5d6z7z7d6z7z7t8")]
        //public void TestTreeBuilder(string filePath, string expectedResult)
        //{
        //    var workingDirectory = Environment.CurrentDirectory;

        //    var directory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;
        //    var json = File.ReadAllText($"{directory}/Files/{filePath}");
        //    var btb = new BTBuilderY(json);
        //    var sb = new StringBuilder();
        //    var tree = btb.SetModuleAction(typeof(TaperBlock), new FuncProxy<Action<double>>(tapper => { sb.Append($"t{tapper}"); }))
        //                  .SetModuleAction(typeof(AddZBlock), new FuncProxy<Action<double>>(z => { sb.Append($"z{z}"); }))
        //                  .SetModuleAction(typeof(PierceBlock), new FuncProxy<Action<MarkLaserParams>>(mlp => { sb.Append("mpl"); }))
        //                  .SetModuleAction(typeof(DelayBlock), new FuncProxy<Action<int>>(delay => { sb.Append($"d{delay}"); }))
        //                  .GetTree();
        //    tree.DoAction();
        //    Assert.That(sb.ToString() == expectedResult);
        //}

        //[TestCase("CircleListing.json", "t1z2d3t4z5z5z5d6z7z7d6z7z7t4z5z5z5d6z7z7d6z7z7t8")]
        //public async Task TestTreeBuilderAsync(string filePath, string expectedResult)
        //{
        //    var workingDirectory = Environment.CurrentDirectory;
        //    var directory = Directory.GetParent(workingDirectory).Parent.Parent.FullName;
        //    var json = File.ReadAllText($"{directory}/Files/{filePath}");


        //    var btb = new BTBuilderZ(json);
        //    var sb = new StringBuilder();
        //    var tree = btb.SetModuleFunction<TaperBlock,double>( new FuncProxy<double>(tapper => { sb.Append($"t{tapper}"); }))
        //                  .SetModuleFunction<AddZBlock,double>(new FuncProxy<double>(z => { sb.Append($"z{z}"); }))
        //                  .SetModuleFunction<PierceBlock,MarkLaserParams>(new FuncProxy<MarkLaserParams>(mlp => { sb.Append("mpl"); }))
        //                  .SetModuleFunction<DelayBlock,int>(new FuncProxy<int>(delay => { sb.Append($"d{delay}"); }))
        //                  .GetTree();
        //    await tree.DoFuncAsync();
        //    Assert.That(sb.ToString() == expectedResult);
        //}
        ////[Test]
        public void StateMachineTest()
        {
            var stateMachine = new StateMachine<State, Trigger>(State.Started);


            var n = 0;
            var count = 3;

            stateMachine.Configure(State.Started)
                .OnActivate(() => { Debug.WriteLine("1"); })
                .Permit(Trigger.Pause, State.Paused)
                .Ignore(Trigger.Deny);

            stateMachine.Configure(State.Paused)
                .OnEntry(() => { Debug.WriteLine($"2 n = {n}"); })
                .OnEntry(() => { Debug.WriteLine($"3 n = {n}"); })
                .OnExit(() => { n++; })
                .PermitReentryIf(Trigger.Pause, () => n < count);

            stateMachine.Activate();
            do
            {
                stateMachine.Fire(Trigger.Pause);
            } while (n < count);
        }
        enum State
        {
            Started,
            Paused,
            Denied
        }
        enum Trigger
        {
            Pause,
            Deny
        }


    }

}