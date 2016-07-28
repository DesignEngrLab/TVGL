using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using TVGL;
using TVGL._2D.Clipper;

namespace TVGLTest.Test
{
    using Path = List<IntPoint>;
    using Paths = List<List<IntPoint>>;

    [TestFixture]
    [RequiresSTA]
    class ClipperTest
    {
        Paths subject;
        Paths subject2;
        Paths clip;
        Paths solution;
        PolyTree polytree;
        Clipper clipper;

        [SetUp]
        public void TestSetup()
        {
            subject = new Paths();
            subject2 = new Paths();
            clip = new Paths();
            solution = new Paths();
            polytree = new PolyTree();
            clipper = new Clipper();
        }

        private Path MakePolygonFromInts(int[] ints, double scale = 1.0)
        {
            var polygon = new Path();

            for (var i = 0; i < ints.Length; i += 2)
            {
                polygon.Add(new IntPoint(scale * ints[i], scale * ints[i + 1]));
            }

            return polygon;
        }

        private void MakeSquarePolygons(int size, int totalWidth, int totalHeight, Paths result)
        {
            int cols = totalWidth / size;
            int rows = totalHeight / size;
            Path[] paths = new Path[cols * rows];
            for (int i = 0; i < rows; ++i)
            {
                for (int j = 0; j < cols; ++j)
                {
                    int[] ints = {
                        j * size,
                        i * size,
                        (j+1) * size,
                        i * size,
                        (j+1) * size,
                        (i+1) * size,
                        j * size,
                        (i+1) * size
                    };

                    paths[j * rows + i] = MakePolygonFromInts(ints);
                }
            }

            result.Clear();
            result.AddRange(paths);
        }

        private void MakeDiamondPolygons(int size, int totalWidth, int totalHeight, Paths result)
        {
            int halfSize = size / 2;
            size = halfSize * 2;
            int cols = totalWidth / size;
            int rows = totalHeight * 2 / size;
            Path[] paths = new Path[cols * rows];
            int dx = 0;
            for (int i = 0; i < rows; ++i)
            {
                if (dx == 0) dx = halfSize; else dx = 0;
                for (int j = 0; j < cols; ++j)
                {
                    int[] ints = {
                        dx + j * size,
                        i * halfSize + halfSize,
                        dx + j * size + halfSize,
                        i * halfSize,
                        dx + (j+1) * size,
                        i * halfSize + halfSize,
                        dx + j * size + halfSize,
                        i * halfSize + halfSize *2
                    };

                    paths[j * rows + i] = MakePolygonFromInts(ints);
                }
            }

            result.Clear();
            result.AddRange(paths);
        }

        private static void ShowPaths(IEnumerable<Path> paths, int scalingFactor = 1)
        {
            var pointPaths = new List<List<Point>>();
            foreach (var path in paths)
            {
                var points = new List<Point>();
                if (scalingFactor < 1) scalingFactor = 1;
                points.AddRange(path.Select(intPoint => new Point(new List<double>() { intPoint.X / scalingFactor, intPoint.Y / scalingFactor, 0.0 })));
                pointPaths.Add(points);
            }
            Presenter.ShowAndHang(pointPaths);
        }

        private static void ShowPathListsAsDifferentColors(IEnumerable<IEnumerable<Path>> pathLists, int scalingFactor = 1)
        {
            
            var pointPathLists = new List<List<List<Point>>>();
            foreach (var paths in pathLists)
            {
                var pointPathList = new List<List<Point>>();
                foreach (var path in paths)
                {
                    var points = new List<Point>();
                    if (scalingFactor < 1) scalingFactor = 1;
                    points.AddRange(path.Select(intPoint => new Point(new List<double>() { intPoint.X / scalingFactor, intPoint.Y/ scalingFactor, 0.0 })));
                    pointPathList.Add(points);
                }
                pointPathLists.Add(pointPathList);
            }
            Presenter.ShowAndHang(pointPathLists);
        }

        [Test]
        public void Difference1()
        {
            PolyFillType fillMethod = PolyFillType.Positive;
            //Note: If you do not use a scaling factor for intPoints, 
            //if two points are close together, they will not be distinguished.
            //Clipper was meant to use a scaling factor.
            var scalingFactor = 1000;
            int[] ints1 = { 29, 342, 115, 68, 141, 86 }; //CCW Black
            //int[] ints2 = { 128, 160, 99, 132, 97, 174 }; //CW Teal
            int[] ints2 = { 128, 160,  97, 174, 99, 132}; //CCW Teal
            //int[] ints3 = { 99, 212, 128, 160, 97, 174, 58, 160 }; //CW Magenta
            int[] ints3 = { 99, 212, 58, 160, 97, 174, 128, 160 }; //CCW Magenta
            //int[] ints4 = { 97, 174, 99, 132, 60, 124, 58, 160 }; //CW Red
            int[] ints4 = { 97, 174, 58, 160, 60, 124, 99, 132 }; //CCW Red

            subject.Add(MakePolygonFromInts(ints1, scalingFactor));
            clip.Add(MakePolygonFromInts(ints2, scalingFactor));
            clip.Add(MakePolygonFromInts(ints3, scalingFactor));
            clip.Add(MakePolygonFromInts(ints4, scalingFactor));

            //ShowPathListsAsDifferentColors(new List<List<Path>>() { subject, clip}, scalingFactor);

            clipper.StrictlySimple = true;
            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);

            var result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);
            //ShowPaths(solution, scalingFactor);
            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(1));

            result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);
            //ShowPaths(solution, scalingFactor);
            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.True);
            Assert.That(Clipper.Orientation(solution[1]), Is.True);

            result = clipper.Execute(ClipType.Intersection, solution, fillMethod, fillMethod);
            //ShowPaths(solution, scalingFactor);
            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(1));

            result = clipper.Execute(ClipType.Xor, solution, fillMethod, fillMethod);
            //ShowPaths(solution, scalingFactor);
            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(4));
        }

        [Test]
        public void Difference2()
        {
            PolyFillType fillMethod = PolyFillType.Positive;
            const int scalingFactor = 1000;
            int[] ints1 = { -103, -219, -103, -136, -115, -136 }; //CCW
            int[] ints2 = { -110, -155, -110, -174, -70, -174  }; //CCW

            subject.Add(MakePolygonFromInts(ints1, scalingFactor));
            clip.Add(MakePolygonFromInts(ints2, scalingFactor));

            //ShowPathListsAsDifferentColors(new List<List<Path>>() { subject, clip }, scalingFactor);

            clipper.StrictlySimple = true;
            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);

            var result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);
            //ShowPaths(solution, scalingFactor);
            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(1));

            result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);
            //ShowPaths(solution, scalingFactor);
            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));

            result = clipper.Execute(ClipType.Intersection, solution, fillMethod, fillMethod);
            //ShowPaths(solution, scalingFactor);
            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(1));

            result = clipper.Execute(ClipType.Xor, solution, fillMethod, fillMethod);
            //ShowPaths(solution, scalingFactor);
            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(4));
        }

        [Test]
        public void Horz1()
        {
            //Must use EvenOdd with self intersecting polygons.
            PolyFillType fillMethod = PolyFillType.EvenOdd;
            const int scalingFactor = 1000;
            int[] ints1 = { 450, 260, 320, 200, 490, 540, 130, 400, 450, 280, 380, 280};//CCW
            int[] ints2 = { 350, 260, 520, 600, 100, 300}; //CCW

            subject.Add(MakePolygonFromInts(ints1, scalingFactor));
            clip.Add(MakePolygonFromInts(ints2, scalingFactor));

            //ShowPathListsAsDifferentColors(new List<List<Path>>() { subject, clip }, scalingFactor);

            clipper.StrictlySimple = true;
            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);

            bool result = clipper.Execute(ClipType.Intersection, solution, fillMethod, fillMethod);
            //ShowPaths(solution, scalingFactor);
            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(1));

            result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);
            //ShowPaths(solution, scalingFactor);
            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(1));

            result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);
            //ShowPaths(solution, scalingFactor);
            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));

            result = clipper.Execute(ClipType.Xor, solution, fillMethod, fillMethod);
            //ShowPaths(solution, scalingFactor);
            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(4));
        }

        [Test]
        public void Horz2()
        {
            PolyFillType fillMethod = PolyFillType.EvenOdd;

            int[] ints1 = { 120, 400, 350, 380, 340, 140 };
            int[] ints2 = { 350, 370, 150, 370, 560, 20, 350, 390, 340, 150, 570, 230, 390, 40 };

            subject.Add(MakePolygonFromInts(ints1));

            clip.Add(MakePolygonFromInts(ints2));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Intersection, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
        }

        [Test]
        public void Horz3()
        {
            PolyFillType fillMethod = PolyFillType.EvenOdd;

            int[] ints1 = { 470, 190, 100, 520, 280, 270, 380, 270, 460, 170 };
            int[] ints2 = { 170, 70, 500, 350, 110, 90 };

            subject.Add(MakePolygonFromInts(ints1));

            clip.Add(MakePolygonFromInts(ints2));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Intersection, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(1));
        }

        [Test]
        public void Horz4()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;
            int[] ints1 = { 904, 901, 1801, 901, 1801, 1801, 902, 1803 };
            int[] ints2 = { 2, 1800, 902, 1800, 902, 2704, 4, 2701 };
            int[] ints3 = { 902, 1802, 902, 2704, 1804, 2703, 1801, 1804 };

            subject.Add(MakePolygonFromInts(ints1));
            subject.Add(MakePolygonFromInts(ints2));
            subject.Add(MakePolygonFromInts(ints3));

            clipper.AddPaths(subject, PolyType.Subject, true);
            bool result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.True);
            Assert.That(Clipper.Orientation(solution[1]), Is.False);
        }

        [Test]
        public void Horz5()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = { 93, 92, 183, 93, 184, 184, 94, 183 };
            int[] ints2 = { 184, 1, 270, 2, 272, 91, 183, 94 };
            int[] ints3 = { 92, 2, 91, 91, 184, 91, 184, 0 };
            int[] ints4 = { 183, 93, 184, 184, 271, 182, 274, 94 };

            subject.Add(MakePolygonFromInts(ints1));
            subject.Add(MakePolygonFromInts(ints2));

            clip.Add(MakePolygonFromInts(ints3));
            clip.Add(MakePolygonFromInts(ints4));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
        }

        [Test]
        public void Horz6()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = { 14, 15, 16, 12, 10, 12 };
            int[] ints2 = { 15, 14, 11, 14, 13, 16, 17, 10, 10, 17, 18, 13 };

            subject.Add(MakePolygonFromInts(ints1));

            clip.Add(MakePolygonFromInts(ints2));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Intersection, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(1));
        }

        [Test]
        public void Horz7()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = { 11, 19, 19, 15, 15, 12, 13, 19, 15, 13, 10, 14, 13, 18, 16, 13 };
            int[] ints2 = { 16, 10, 14, 17, 18, 10, 15, 18, 14, 14, 15, 14, 11, 16 };

            subject.Add(MakePolygonFromInts(ints1));

            clip.Add(MakePolygonFromInts(ints2));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Intersection, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
        }

        [Test]
        public void Horz8()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = { 12, 11, 15, 15, 18, 16, 16, 18, 15, 14, 14, 14, 19, 15 };
            int[] ints2 = { 13, 12, 17, 17, 19, 15 };

            subject.Add(MakePolygonFromInts(ints1));

            clip.Add(MakePolygonFromInts(ints2));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Intersection, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
        }

        [Test]
        public void Horz9()
        {
            PolyFillType fillMethod = PolyFillType.EvenOdd;

            int[] ints1 = { 380, 140, 430, 120, 180, 120, 430, 120, 190, 150 };
            int[] ints2 = { 430, 130, 210, 70, 20, 260 };

            subject.Add(MakePolygonFromInts(ints1));

            clip.Add(MakePolygonFromInts(ints2));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Intersection, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
        }

        [Test]
        public void Horz10()
        {
            PolyFillType fillMethod = PolyFillType.EvenOdd;

            int[] ints1 = { 40, 310, 410, 110, 460, 110, 260, 200 };
            int[] ints2 = { 120, 260, 450, 220, 330, 220, 240, 220, 50, 380 };

            subject.Add(MakePolygonFromInts(ints1));

            clip.Add(MakePolygonFromInts(ints2));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Intersection, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
        }

        [Test]
        public void Orientation1()
        {
            PolyFillType fillMethod = PolyFillType.EvenOdd;

            int[] ints1 = {470, 130, 330, 10, 370, 10,
                290, 190, 290, 280, 190, 10, 70, 370, 10, 400, 310, 10, 490, 220,
                130, 10, 150, 400, 490, 150, 250, 60, 410, 320, 430, 410,
                470, 10, 10, 10, 250, 220, 10, 180, 250, 160, 490, 130, 190, 320,
                170, 240, 290, 280, 370, 240, 350, 90, 450, 190, 10, 370,
                110, 180, 290, 160, 190, 350, 490, 360, 190, 190, 370, 230,
                90, 220, 270, 10, 70, 190, 10, 270, 430, 100, 190, 140, 370, 80,
                10, 40, 250, 260, 430, 40, 130, 350, 190, 420, 10, 10, 130, 50,
                90, 400, 530, 50, 150, 90, 250, 150, 390, 310, 250, 180,
                310, 220, 350, 280, 30, 140, 430, 260, 130, 10, 430, 310,
                10, 60, 190, 60, 490, 320, 190, 360, 430, 130, 210, 220,
                270, 190, 10, 10, 510, 10, 150, 210, 90, 400, 110, 10, 130, 110,
                130, 80, 130, 30, 430, 190, 190, 380, 90, 300, 10, 340, 10, 70,
                250, 380, 310, 370, 370, 240, 190, 130, 490, 100, 470, 70,
                10, 420, 190, 20, 430, 290, 430, 10, 330, 70, 450, 140, 430, 40,
                150, 220, 170, 190, 10, 110, 470, 310, 510, 160, 10, 200
            };
            int[] ints2 = {50, 420, 10, 180, 190, 160,
                50, 40, 490, 40, 450, 130, 450, 290, 290, 310, 430, 110,
                370, 250, 490, 220, 430, 230, 410, 220, 10, 200, 530, 130,
                50, 350, 370, 290, 130, 130, 110, 390, 10, 350, 210, 340,
                370, 220, 530, 280, 370, 170, 190, 370, 330, 310, 510, 280,
                90, 10, 50, 250, 170, 100, 110, 40, 310, 370, 430, 80, 390, 40,
                250, 360, 350, 150, 130, 310, 10, 260, 390, 90, 370, 280,
                70, 100, 530, 190, 10, 250, 470, 340, 110, 180, 10, 10, 70, 380,
                370, 60, 190, 290, 250, 70, 10, 150, 70, 120, 490, 340, 330, 40,
                90, 10, 210, 40, 50, 10, 450, 370, 310, 390, 10, 10, 10, 270,
                250, 180, 130, 120, 10, 150, 10, 220, 150, 280, 490, 10,
                150, 370, 370, 220, 10, 310, 10, 330, 450, 150, 310, 80,
                410, 40, 530, 290, 110, 240, 70, 140, 190, 410, 10, 250,
                270, 230, 370, 380, 270, 280, 230, 220, 430, 110, 10, 290,
                130, 250, 190, 40, 170, 320, 210, 220, 290, 40, 370, 380,
                30, 380, 130, 50, 370, 340, 130, 190, 70, 250, 310, 270,
                250, 290, 310, 280, 230, 150
            };

            subject.Add(MakePolygonFromInts(ints1));

            clip.Add(MakePolygonFromInts(ints2));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Intersection, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            for (var i = 0; i < solution.Count; ++i)
            {
                Assert.That(Clipper.Orientation(solution[i]), Is.True);
            }
        }

        [Test]
        public void Orientation2()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = {370, 150, 130, 400, 490, 290,
                490, 400, 170, 10, 130, 130, 270, 90, 430, 230, 310, 230,
                10, 80, 390, 110, 370, 20, 190, 210, 370, 410, 110, 100,
                410, 230, 370, 290, 350, 190, 350, 100, 230, 290
            };
            int[] ints2 = {510, 400, 250, 100, 410, 410,
                170, 210, 390, 100, 10, 100, 10, 250, 10, 220, 130, 90, 410, 330,
                450, 160, 50, 180, 110, 100, 210, 320, 410, 220, 190, 30,
                370, 70, 270, 260, 450, 250, 90, 280
            };

            subject.Add(MakePolygonFromInts(ints1));

            clip.Add(MakePolygonFromInts(ints2));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Intersection, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            int count = 0;
            for (var i = 0; i < solution.Count; ++i)
            {
                if (!Clipper.Orientation(solution[i])) count++;
            }
            Assert.That(count, Is.EqualTo(4));
        }

        [Test]
        public void Orientation3()
        {
            PolyFillType fillMethod = PolyFillType.EvenOdd;

            int[] ints1 = { 70, 290, 10, 410, 10, 220 };
            int[] ints2 = { 430, 20, 10, 30, 10, 370, 250, 300, 190, 10, 10, 370, 30, 220, 490, 100, 10, 370 };

            subject.Add(MakePolygonFromInts(ints1));

            clip.Add(MakePolygonFromInts(ints2));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Intersection, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            for (var i = 0; i < solution.Count; ++i)
            {
                Assert.That(Clipper.Orientation(solution[i]), Is.True);
            }
        }

        [Test]
        public void Orientation4()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = {40, 190, 400, 10, 510, 450,
                300, 50, 440, 230, 340, 290, 260, 510, 110, 50, 500, 90,
                450, 410, 550, 70, 70, 130, 410, 110, 130, 130, 470, 50,
                410, 10, 360, 50, 460, 90, 170, 270, 400, 210, 240, 370,
                50, 370, 350, 270, 530, 330, 170, 250, 440, 170, 40, 430,
                410, 90, 170, 510, 470, 130, 290, 390, 510, 410, 500, 230,
                490, 490, 430, 430, 10, 250, 240, 190, 80, 370, 60, 190,
                570, 490, 110, 270, 550, 290, 90, 10, 200, 10, 580, 450,
                500, 450, 370, 210, 10, 250, 60, 70, 220, 10, 530, 130, 190, 10,
                350, 170, 440, 330, 260, 50, 320, 10, 570, 10, 350, 170,
                130, 470, 350, 370, 40, 130, 540, 50, 10, 50, 320, 450, 270, 470,
                460, 10, 60, 110, 280, 170, 300, 410, 300, 370, 520, 170,
                460, 410, 180, 270, 270, 450, 50, 110, 490, 490, 10, 150,
                240, 490, 200, 190, 10, 10, 30, 370, 170, 410, 560, 290,
                140, 10, 350, 190, 290, 10, 460, 210, 70, 290, 300, 270,
                570, 450, 250, 330, 250, 290, 300, 410, 210, 330, 320, 390,
                160, 290, 70, 190, 40, 170, 490, 70, 70, 50
            };
            int[] ints2 = {160, 510, 440, 90, 400, 510,
                220, 250, 480, 210, 80, 410, 530, 170, 10, 50, 220, 290,
                110, 490, 110, 10, 350, 130, 510, 330, 10, 410, 190, 30,
                90, 10, 380, 270, 50, 250, 510, 50, 580, 10, 50, 130, 540, 330,
                120, 250, 440, 250, 10, 430, 10, 410, 150, 190, 510, 490,
                400, 170, 200, 10, 170, 470, 300, 10, 130, 130, 190, 10,
                500, 350, 40, 10, 400, 230, 20, 370, 230, 510, 140, 10, 220, 490,
                90, 370, 490, 190, 520, 210, 180, 70, 440, 490, 510, 10,
                420, 210, 340, 410, 80, 10, 100, 190, 100, 250, 340, 390,
                360, 10, 170, 70, 300, 290, 110, 370, 160, 330, 210, 10,
                300, 10, 540, 410, 380, 490, 550, 290, 170, 450, 580, 390,
                360, 10, 450, 370, 520, 330, 100, 30, 160, 450, 160, 190,
                300, 90, 400, 270, 40, 170, 40, 90, 210, 330, 450, 50, 430, 370,
                290, 370, 150, 10, 340, 170, 10, 90, 180, 150, 530, 450,
                310, 490, 400, 450, 340, 10, 420, 210, 500, 70, 100, 10,
                400, 470, 40, 490, 550, 190, 30, 90, 100, 130, 70, 490, 20, 270,
                490, 410, 570, 370, 220, 90
            };

            subject.Add(MakePolygonFromInts(ints1));

            clip.Add(MakePolygonFromInts(ints2));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            int count = 0;
            for (var i = 0; i < solution.Count; ++i)
            {
                if (!Clipper.Orientation(solution[i])) count++;
            }
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void Orientation5()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = {5237, 5237, 68632, 5164, 10315, 61247,
    10315, 20643, 16045, 29877, 24374, 11012, 10359, 19690, 10315, 20643,
    10315, 67660};

            subject.Add(MakePolygonFromInts(ints1));

            clipper.AddPaths(subject, PolyType.Subject, true);
            bool result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[1]), Is.False);
        }

        [Test]
        public void Orientation6()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = { 0, 0, 100, 0, 101, 116, 0, 109 };
            int[] ints2 = { 110, 112, 200, 106, 200, 200, 111, 200 };
            int[] ints3 = { 0, 106, 101, 114, 107, 200, 0, 200 };
            int[] ints4 = { 117, 0, 200, 0, 200, 110, 115, 102 };

            subject.Add(MakePolygonFromInts(ints1));
            subject.Add(MakePolygonFromInts(ints2));

            clip.Add(MakePolygonFromInts(ints3));
            clip.Add(MakePolygonFromInts(ints4));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.True);
            Assert.That(Clipper.Orientation(solution[1]), Is.True);
        }

        [Test]
        public void Orientation7()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = { 0, 0, 100, 0, 104, 116, 0, 118 };
            int[] ints2 = { 111, 115, 200, 103, 200, 200, 105, 200 };
            int[] ints3 = { 0, 103, 112, 111, 105, 200, 0, 200 };
            int[] ints4 = { 116, 0, 200, 0, 200, 113, 101, 110 };

            subject.Add(MakePolygonFromInts(ints1));
            subject.Add(MakePolygonFromInts(ints2));

            clip.Add(MakePolygonFromInts(ints3));
            clip.Add(MakePolygonFromInts(ints4));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.True);
            Assert.That(Clipper.Orientation(solution[1]), Is.True);
        }

        [Test]
        public void Orientation8()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = { 0, 0, 112, 0, 111, 116, 0, 108 };
            int[] ints2 = { 112, 114, 200, 108, 200, 200, 116, 200 };
            int[] ints3 = { 0, 102, 118, 111, 117, 200, 0, 200 };
            int[] ints4 = { 109, 0, 200, 0, 200, 117, 105, 110 };

            subject.Add(MakePolygonFromInts(ints1));
            subject.Add(MakePolygonFromInts(ints2));

            clip.Add(MakePolygonFromInts(ints3));
            clip.Add(MakePolygonFromInts(ints4));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.True);
            Assert.That(Clipper.Orientation(solution[1]), Is.True);
        }

        [Test]
        public void Orientation9()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = { 0, 0, 114, 0, 113, 110, 0, 117 };
            int[] ints2 = { 109, 114, 200, 106, 200, 200, 104, 200 };
            int[] ints3 = { 0, 100, 118, 106, 103, 200, 0, 200 };
            int[] ints4 = { 110, 0, 200, 0, 200, 116, 101, 105 };

            subject.Add(MakePolygonFromInts(ints1));
            subject.Add(MakePolygonFromInts(ints2));

            clip.Add(MakePolygonFromInts(ints3));
            clip.Add(MakePolygonFromInts(ints4));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.True);
            Assert.That(Clipper.Orientation(solution[1]), Is.True);
        }

        [Test]
        public void Orientation10()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = { 0, 0, 102, 0, 103, 118, 0, 106 };
            int[] ints2 = { 110, 115, 200, 108, 200, 200, 113, 200 };
            int[] ints3 = { 0, 110, 103, 117, 109, 200, 0, 200 };
            int[] ints4 = { 118, 0, 200, 0, 200, 108, 116, 101 };

            subject.Add(MakePolygonFromInts(ints1));
            subject.Add(MakePolygonFromInts(ints2));

            clip.Add(MakePolygonFromInts(ints3));
            clip.Add(MakePolygonFromInts(ints4));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.True);
            Assert.That(Clipper.Orientation(solution[1]), Is.True);
        }

        [Test]
        public void Orientation11()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = { 0, 0, 100, 0, 107, 116, 0, 104 };
            int[] ints2 = { 116, 100, 200, 115, 200, 200, 118, 200 };
            int[] ints3 = { 0, 115, 107, 115, 115, 200, 0, 200 };
            int[] ints4 = { 101, 0, 200, 0, 200, 100, 100, 100 };

            subject.Add(MakePolygonFromInts(ints1));
            subject.Add(MakePolygonFromInts(ints2));

            clip.Add(MakePolygonFromInts(ints3));
            clip.Add(MakePolygonFromInts(ints4));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.True);
            Assert.That(Clipper.Orientation(solution[1]), Is.True);
        }

        [Test]
        public void Orientation12()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = { 0, 0, 119, 0, 113, 105, 0, 100 };
            int[] ints2 = { 117, 103, 200, 105, 200, 200, 106, 200 };
            int[] ints3 = { 0, 112, 116, 104, 108, 200, 0, 200 };
            int[] ints4 = { 101, 0, 200, 0, 200, 117, 104, 112 };

            subject.Add(MakePolygonFromInts(ints1));
            subject.Add(MakePolygonFromInts(ints2));

            clip.Add(MakePolygonFromInts(ints3));
            clip.Add(MakePolygonFromInts(ints4));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.True);
            Assert.That(Clipper.Orientation(solution[1]), Is.True);
        }

        [Test]
        public void Orientation13()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = { 0, 0, 119, 0, 109, 108, 0, 101 };
            int[] ints2 = { 115, 100, 200, 103, 200, 200, 101, 200 };
            int[] ints3 = { 0, 117, 110, 100, 103, 200, 0, 200 };
            int[] ints4 = { 115, 0, 200, 0, 200, 109, 119, 102 };

            subject.Add(MakePolygonFromInts(ints1));
            subject.Add(MakePolygonFromInts(ints2));

            clip.Add(MakePolygonFromInts(ints3));
            clip.Add(MakePolygonFromInts(ints4));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.True);
            Assert.That(Clipper.Orientation(solution[1]), Is.True);
        }

        [Test]
        public void Orientation14()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = { 0, 0, 102, 0, 119, 107, 0, 101 };
            int[] ints2 = { 116, 110, 200, 114, 200, 200, 107, 200 };
            int[] ints3 = { 0, 108, 117, 106, 111, 200, 0, 200 };
            int[] ints4 = { 112, 0, 200, 0, 200, 117, 101, 112 };

            subject.Add(MakePolygonFromInts(ints1));
            subject.Add(MakePolygonFromInts(ints2));

            clip.Add(MakePolygonFromInts(ints3));
            clip.Add(MakePolygonFromInts(ints4));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.True);
            Assert.That(Clipper.Orientation(solution[1]), Is.True);
        }

        [Test]
        public void Orientation15()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = { 0, 0, 106, 0, 107, 111, 0, 102 };
            int[] ints2 = { 119, 116, 200, 118, 200, 200, 117, 200 };
            int[] ints3 = { 0, 101, 107, 106, 111, 200, 0, 200 };
            int[] ints4 = { 113, 0, 200, 0, 200, 114, 117, 117 };

            subject.Add(MakePolygonFromInts(ints1));
            subject.Add(MakePolygonFromInts(ints2));

            clip.Add(MakePolygonFromInts(ints3));
            clip.Add(MakePolygonFromInts(ints4));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.True);
            Assert.That(Clipper.Orientation(solution[1]), Is.True);
        }

        [Test]
        public void SelfInt1()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = { 0, 0, 201, 0, 203, 217, 0, 207 };
            int[] ints2 = { 204, 214, 400, 217, 400, 400, 205, 400 };
            int[] ints3 = { 0, 211, 203, 214, 208, 400, 0, 400 };
            int[] ints4 = { 207, 0, 400, 0, 400, 208, 218, 200 };

            subject.Add(MakePolygonFromInts(ints1));
            subject.Add(MakePolygonFromInts(ints2));

            clip.Add(MakePolygonFromInts(ints3));
            clip.Add(MakePolygonFromInts(ints4));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.True);
            Assert.That(Clipper.Orientation(solution[1]), Is.True);
        }

        [Test]
        public void SelfInt2()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = { 0, 0, 200, 0, 219, 207, 0, 200 };
            int[] ints2 = { 201, 207, 400, 200, 400, 400, 200, 400 };
            int[] ints3 = { 0, 200, 214, 207, 200, 400, 0, 400 };
            int[] ints4 = { 200, 0, 400, 0, 400, 200, 209, 215 };

            subject.Add(MakePolygonFromInts(ints1));
            subject.Add(MakePolygonFromInts(ints2));

            clip.Add(MakePolygonFromInts(ints3));
            clip.Add(MakePolygonFromInts(ints4));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.True);
            Assert.That(Clipper.Orientation(solution[1]), Is.True);
        }

        [Test]
        public void SelfInt3()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = { 0, 0, 201, 0, 207, 214, 0, 207 };
            int[] ints2 = { 209, 211, 400, 206, 400, 400, 214, 400 };
            int[] ints3 = { 0, 211, 207, 208, 213, 400, 0, 400 };
            int[] ints4 = { 213, 0, 400, 0, 400, 210, 213, 200 };

            subject.Add(MakePolygonFromInts(ints1));
            subject.Add(MakePolygonFromInts(ints2));

            clip.Add(MakePolygonFromInts(ints3));
            clip.Add(MakePolygonFromInts(ints4));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.True);
            Assert.That(Clipper.Orientation(solution[1]), Is.True);
        }

        [Test]
        public void SelfInt4()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = { 0, 0, 214, 0, 209, 206, 0, 201 };
            int[] ints2 = { 205, 208, 400, 207, 400, 400, 200, 400 };
            int[] ints3 = { 201, 0, 400, 0, 400, 217, 205, 217 };
            int[] ints4 = { 0, 205, 215, 206, 217, 400, 0, 400 };

            subject.Add(MakePolygonFromInts(ints1));
            subject.Add(MakePolygonFromInts(ints2));

            clip.Add(MakePolygonFromInts(ints3));
            clip.Add(MakePolygonFromInts(ints4));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.True);
            Assert.That(Clipper.Orientation(solution[1]), Is.True);
        }

        [Test]
        public void SelfInt5()
        {
            PolyFillType fillMethod = PolyFillType.EvenOdd;

            int[] ints1 = { 0, 0, 219, 0, 217, 217, 0, 200 };
            int[] ints2 = { 214, 219, 400, 200, 400, 400, 219, 400 };
            int[] ints3 = { 0, 207, 205, 211, 214, 400, 0, 400 };
            int[] ints4 = { 202, 0, 400, 0, 400, 217, 205, 217 };

            subject.Add(MakePolygonFromInts(ints1));
            subject.Add(MakePolygonFromInts(ints2));

            clip.Add(MakePolygonFromInts(ints3));
            clip.Add(MakePolygonFromInts(ints4));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Difference, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.True);
            Assert.That(Clipper.Orientation(solution[1]), Is.True);

        }

        [Test]
        public void SelfInt6()
        {
            PolyFillType fillMethod = PolyFillType.EvenOdd;

            int[] ints = { 182, 179, 477, 123, 25, 55 };
            int[] ints2 = { 477, 122, 485, 103, 122, 265, 55, 207 };

            subject.Add(MakePolygonFromInts(ints));

            clip.Add(MakePolygonFromInts(ints2));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Intersection, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(Clipper.Orientation(solution[0]), Is.True);
        }

        [Test]
        public void Union1()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;

            int[] ints1 = { 1026, 1126, 1026, 235, 4505, 401, 4522, 1145, 4503, 1162, 2280, 1129 };
            int[] ints2 = { 4501, 1100, 4501, 866, 1146, 462, 1071, 1067, 4469, 1000 };
            int[] ints3 = { 4499, 1135, 3360, 1050, 3302, 1107 };
            int[] ints4 = { 3360, 1050, 3291, 1118, 4512, 1136 };

            subject.Add(MakePolygonFromInts(ints1));
            subject.Add(MakePolygonFromInts(ints2));
            subject.Add(MakePolygonFromInts(ints3));

            clip.Add(MakePolygonFromInts(ints4));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.True);
            Assert.That(Clipper.Orientation(solution[1]), Is.False);
        }

        [Test]
        public void Union2()
        {
            PolyFillType fillMethod = PolyFillType.EvenOdd;
            const int scalingFactor = 1000;
            int[][] ints = {
                new int[] {10, 10, 20, 10, 20, 20, 10, 20},
                new int[] {20, 10, 30, 10, 30, 20, 20, 20},
                new int[] {30, 10, 40, 10, 40, 20, 30, 20},
                new int[] {40, 10, 50, 10, 50, 20, 40, 20},
                new int[] {50, 10, 60, 10, 60, 20, 50, 20},
                new int[] {10, 20, 20, 20, 20, 30, 10, 30},
                new int[] {30, 20, 40, 20, 40, 30, 30, 30},
                new int[] {10, 30, 20, 30, 20, 40, 10, 40},
                new int[] {20, 30, 30, 30, 30, 40, 20, 40},
                new int[] {30, 30, 40, 30, 40, 40, 30, 40},
                new int[] {40, 30, 50, 30, 50, 40, 40, 40}
            };

            for (var i = 0; i < 11; ++i)
            {
                subject.Add(MakePolygonFromInts(ints[i], scalingFactor));
            }

            //ShowPathListsAsDifferentColors(new List<List<Path>>() { subject}, scalingFactor);

            clipper.AddPaths(subject, PolyType.Subject, true);
            bool result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);
            //ShowPaths(solution, scalingFactor);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
        }

        [Test]
        public void Union3()
        {
            PolyFillType fillMethod = PolyFillType.EvenOdd;
            int[][] ints = {
                new int[] { 1, 3, 2, 4, 2, 5 },
                new int[] { 1, 3, 3, 3, 2, 4 }
            };

            for (var i = 0; i < 2; ++i)
            {
                subject.Add(MakePolygonFromInts(ints[i]));
            }

            clipper.AddPaths(subject, PolyType.Subject, true);
            bool result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(1));
        }

        [Test]
        public void AddPath1()
        {

            int[] ints = { 480, 20, 480, 110, 320, 30, 480, 30, 250, 250, 480, 30 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void AddPath2()
        {

            int[] ints = { 60, 320, 390, 320, 100, 320, 220, 120, 120, 10, 20, 380, 120, 20, 280, 20, 480, 20 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void AddPath3()
        {
            int[] ints = { 320, 70, 420, 370, 250, 170, 60, 290, 10, 290, 210, 290, 400, 150, 410, 340 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void AddPath4()
        {

            int[] ints = { 300, 80, 280, 220, 180, 220, 170, 220, 290, 220, 40, 180 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void AddPath5()
        {

            int[] ints = { 170, 340, 280, 230, 160, 50, 430, 370, 280, 230 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void AddPath6()
        {

            int[] ints = { 30, 380, 70, 160, 170, 220, 70, 160, 240, 160 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void AddPath7()
        {

            int[] ints = { 440, 300, 40, 40, 440, 300, 80, 360 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void AddPath8()
        {

            int[] ints = { 260, 10, 260, 240, 190, 100, 260, 10, 420, 120 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void AddPath9()
        {

            int[] ints = { 60, 240, 30, 10, 460, 170, 110, 280, 30, 10 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void AddPath10()
        {

            int[] ints = { 430, 270, 440, 260, 470, 30, 280, 30, 430, 270, 450, 40 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void AddPath11()
        {

            int[] ints = { 320, 10, 240, 300, 260, 140, 320, 10, 240, 300 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void AddPath12()
        {

            int[] ints = { 270, 340, 130, 50, 50, 350, 270, 340, 290, 40 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void AddPath13()
        {

            int[] ints = { 430, 330, 280, 10, 210, 280, 430, 330, 280, 10 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void AddPath14()
        {
            int[] ints = { 50, 30, 410, 330, 50, 30, 310, 50 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void AddPath15()
        {

            int[] ints = { 230, 50, 10, 50, 110, 50 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void AddPath16()
        {

            int[] ints = { 260, 320, 40, 130, 100, 30, 80, 360, 260, 320, 40, 50 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void AddPath17()
        {

            int[] ints = { 190, 170, 350, 290, 110, 290, 250, 290, 430, 90 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void AddPath18()
        {

            int[] ints = { 150, 330, 210, 70, 90, 70, 210, 70, 150, 330 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void AddPath19()
        {

            int[] ints = { 170, 290, 50, 290, 170, 290, 410, 310, 170, 290 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void AddPath20()
        {

            int[] ints = { 430, 10, 150, 110, 430, 10, 230, 50 };

            subject2.Add(MakePolygonFromInts(ints));

            clipper.AddPaths(subject2, PolyType.Subject, false);
        }

        [Test]
        public void OpenPath1()
        {
            PolyFillType fillMethod = PolyFillType.EvenOdd;

            int[] ints = { 290, 370, 160, 150, 230, 150, 160, 150, 250, 280 };

            subject2.Add(MakePolygonFromInts(ints));
            int[] ints2 = { 150, 10, 160, 290, 200, 80, 50, 340 };

            clip.Add(MakePolygonFromInts(ints2));

            clipper.AddPaths(subject2, PolyType.Subject, false);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Intersection, polytree, fillMethod, fillMethod);

            Assert.That(result, Is.True);
        }

        [Test]
        public void OpenPath2()
        {
            PolyFillType fillMethod = PolyFillType.EvenOdd;

            int[] ints = { 50, 310, 210, 110, 260, 110, 170, 110, 350, 200 };

            subject2.Add(MakePolygonFromInts(ints));
            int[] ints2 = { 310, 30, 90, 90, 370, 130 };

            clip.Add(MakePolygonFromInts(ints2));

            clipper.AddPaths(subject2, PolyType.Subject, false);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Intersection, polytree, fillMethod, fillMethod);

            Assert.That(result, Is.True);
        }

        [Test]
        public void OpenPath3()
        {
            PolyFillType fillMethod = PolyFillType.EvenOdd;

            int[] ints = { 40, 360, 260, 50, 180, 270, 180, 250, 410, 250, 140, 250, 350, 380 };

            subject2.Add(MakePolygonFromInts(ints));
            int[] ints2 = { 30, 110, 330, 90, 20, 370 };

            clip.Add(MakePolygonFromInts(ints2));

            clipper.AddPaths(subject2, PolyType.Subject, false);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Intersection, polytree, fillMethod, fillMethod);

            Assert.That(result, Is.True);
        }

        [Test]
        public void OpenPath4()
        {
            PolyFillType fillMethod = PolyFillType.EvenOdd;

            int[] ints = { 10, 50, 200, 50 };

            subject2.Add(MakePolygonFromInts(ints));
            int[] ints2 = { 50, 10, 150, 10, 150, 100, 50, 100 };

            clip.Add(MakePolygonFromInts(ints2));

            clipper.AddPaths(subject2, PolyType.Subject, false);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Intersection, polytree, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(polytree.ChildCount, Is.EqualTo(1));
        }

        [Test]
        public void Simplify1()
        {
            PolyFillType fillMethod = PolyFillType.EvenOdd;
            int[] ints = { 5048400, 1719180, 5050250, 1717630, 5049070, 1717320, 5049150, 1717200, 5049350, 1717570 };

            subject.Add(MakePolygonFromInts(ints));

            clipper.StrictlySimple = true;
            clipper.AddPaths(subject, PolyType.Subject, true);
            bool result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
        }

        [Test]
        public void Simplify2()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;
            int[] ints = { 220, 720, 420, 720, 420, 520, 320, 520, 320, 480, 480, 480, 480, 800, 180, 800, 180, 480, 320, 480, 320, 520, 220, 520 };
            int[] ints2 = { 440, 520, 620, 520, 620, 420, 440, 420 };

            subject.Add(MakePolygonFromInts(ints));
            subject.Add(MakePolygonFromInts(ints2));

            clipper.AddPaths(subject, PolyType.Subject, true);
            bool result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(3));
        }

        [Test]
        public void Joins1()
        {
            PolyFillType fillMethod = PolyFillType.EvenOdd;
            int[][] ints = {
                new int[] {0, 0, 0, 32, 32, 32, 32, 0},
                new int[] {32, 0, 32, 32, 64, 32, 64, 0},
                new int[] {64, 0, 64, 32, 96, 32, 96, 0},
                new int[] {96, 0, 96, 32, 128, 32, 128, 0},
                new int[] {0, 32, 0, 64, 32, 64, 32, 32},
                new int[] {64, 32, 64, 64, 96, 64, 96, 32},
                new int[] {0, 64, 0, 96, 32, 96, 32, 64},
                new int[] {32, 64, 32, 96, 64, 96, 64, 64}
            };

            for (var i = 0; i < 8; ++i)
            {
                subject.Add(MakePolygonFromInts(ints[i]));
            }

            clipper.AddPaths(subject, PolyType.Subject, true);
            bool result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(1));
        }

        [Test]
        public void Joins2()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;
            int[][] ints = {
                new int[] {100, 100, 100, 91, 200, 91, 200, 100},
                new int[] {200, 91, 209, 91, 209, 250, 200, 250},
                new int[] {209, 250, 209, 259, 100, 259, 100, 250},
                new int[] {100, 250, 109, 250, 109, 300, 100, 300},
                new int[] {109, 300, 109, 309, 50, 309, 50, 300},
                new int[] {50, 309, 41, 309, 41, 250, 50, 250},
                new int[] {50, 250, 50, 259, 0, 259, 0, 250},
                new int[] {0, 259, -9, 259, -9, 100, 0, 100},
                new int[] {-9, 100, -9, 91, 50, 91, 50, 100},
                new int[] {50, 100, 41, 100, 41, 50, 50, 50},
                new int[] {41, 50, 41, 41, 100, 41, 100, 50},
                new int[] {100, 41, 109, 41, 109, 100, 100, 100}
            };

            for (var i = 0; i < 12; ++i)
            {
                subject.Add(MakePolygonFromInts(ints[i]));
            }

            clipper.AddPaths(subject, PolyType.Subject, true);
            bool result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.Not.EqualTo(Clipper.Orientation(solution[1])));
        }

        [Test]
        public void Joins3()
        {
            PolyFillType fillMethod = PolyFillType.NonZero;
            int[] ints = { 220, 720, 420, 720, 420, 520, 320, 520, 320, 480, 480, 480, 480, 800, 180, 800, 180, 480, 320, 480, 320, 520, 220, 520 };

            subject.Add(MakePolygonFromInts(ints));
            int[] ints2 = { 440, 520, 620, 520, 620, 420, 440, 420 };

            clip.Add(MakePolygonFromInts(ints2));

            clipper.AddPaths(subject, PolyType.Subject, true);
            clipper.AddPaths(clip, PolyType.Clip, true);
            bool result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(2));
            Assert.That(Clipper.Orientation(solution[0]), Is.Not.EqualTo(Clipper.Orientation(solution[1])));
        }

        [Test]
        public void Joins4()
        {
            PolyFillType fillMethod = PolyFillType.EvenOdd;
            int[] ints = {
                1172, 318, 337, 1066, 154, 639, 479, 448, 1197, 545, 1041, 773, 30, 888,
                444, 308, 1051, 552, 1109, 102, 658, 683, 394, 596, 972, 1145, 442, 179,
                470, 441, 227, 564, 1179, 1037, 213, 379, 1072, 872, 587, 171, 723, 329,
                272, 242, 952, 1121, 714, 1148, 91, 217, 735, 561, 903, 1009, 664, 1168,
                1160, 847, 9, 7, 619, 142, 1139, 1116, 1134, 369, 760, 647, 372, 134,
                1106, 183, 311, 103, 265, 185, 1062, 856, 453, 944, 44, 653, 766, 527,
                334, 965, 443, 971, 474, 36, 397, 1138, 901, 841, 775, 612, 222, 465,
                148, 955, 417, 540, 997, 472, 666, 802, 754, 32, 907, 638, 927, 42, 990,
                406, 99, 682, 17, 281, 106, 848
            };
            MakeDiamondPolygons(20, 600, 400, subject);
            for (int i = 0; i < 120; ++i) subject[ints[i]].Clear();

            clipper.AddPaths(subject, PolyType.Subject, true);
            bool result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(69));
        }

        [Test]
        public void Joins5()
        {
            PolyFillType fillMethod = PolyFillType.EvenOdd;
            int[] ints = {
                553, 388, 574, 20, 191, 26, 461, 258, 509, 19, 466, 257, 90, 269, 373, 516,
                350, 333, 288, 141, 47, 217, 247, 519, 535, 336, 504, 497, 344, 341, 293,
                177, 558, 598, 399, 286, 482, 185, 266, 24, 27, 118, 338, 413, 514, 510,
                366, 46, 593, 465, 405, 32, 449, 6, 326, 59, 75, 173, 127, 130
            };
            MakeSquarePolygons(20, 600, 400, subject);
            for (int i = 0; i < 60; ++i) subject[ints[i]].Clear();

            clipper.AddPaths(subject, PolyType.Subject, true);
            bool result = clipper.Execute(ClipType.Union, solution, fillMethod, fillMethod);

            Assert.That(result, Is.True);
            Assert.That(solution.Count, Is.EqualTo(37));
        }

        [Test]
        public void OffsetPoly1()
        {
            const double scale = 10;
            int[] ints2 = { 348, 257, 364, 148, 362, 148, 326, 241, 295, 219, 258, 88, 440, 129, 370, 196, 372, 275 };

            subject.Add(MakePolygonFromInts(ints2, scale));
            ClipperOffset clipperOffset = new ClipperOffset();
            clipperOffset.AddPaths(subject, JoinType.Round, EndType.ClosedPolygon);
            clipperOffset.Execute(ref solution, -7.0 * scale);

            Assert.That(solution.Count, Is.EqualTo(2));
        }
    }
}