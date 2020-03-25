using System;
using Xunit;
using TVGL.Numerics;

namespace TVGLUnitTestsAndBenchmarking
{
    public class TVGLNumericsTests
    {
        static Random r = new Random();
        static double r100 => 200.0 * r.NextDouble() - 100.0;

        [Fact]
        public void Vector2Length()
        {
            var vectLength5 = new Vector2(3, 4);
            Assert.Equal(5, vectLength5.Length());
        }

        [Fact]
        public void Cross2test1()
        {
            Assert.Equal(1, Vector2.UnitX.Cross(Vector2.UnitY));
        }

        [Fact]
        public void Cross2test2()
        {
            var v1 = new Vector2(1, 2);
            var v2 = new Vector2(2, 1);
            Assert.True(v1.Cross(v2) < 0);
        }
        [Fact]
        public void Normalize2()
        {
            var r = new Random();
            var v1 = new Vector2(r100, r100);
            var v2 = v1.Normalize();
            Assert.Equal(1, v2.Length());
        }
        [Fact]
        public void Matrix3Invert()
        {
            for (int i = 0; i < 5; i++)
            {
                var m = Matrix3x3.Null;
                var mInv = Matrix3x3.Null;
                var v1 = Vector2.Null;
                var v2 = Vector2.Null;
                do
                {
                    m = new Matrix3x3(r100, r100, r100, r100, r100, r100, r100, r100, r100);
                    v1 = new Vector2(r100, r100);
                    v2 = v1.Transform(m);
                }
                while (!Matrix3x3.Invert(m, out mInv));
                Assert.True(v1.IsPracticallySame(v2.Transform(mInv), 1e-11));
            }
        }


    }
}
