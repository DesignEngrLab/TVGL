using System;
using Xunit;
using TVGL.Numerics;
using TVGL;
using System.Collections.Generic;

namespace TVGLUnitTestsAndBenchmarking
{
    public static partial class TVGLNumericsTests
    {
        static Random random = new Random();
        const double maxError = 5e-7;
        const int numTrials = 100000;
        static void DisplayPolynomialTests(string[] args)
        {
            if (ConicSolveRealCoefficients())
                Console.WriteLine("Successfully solved cubic with real coefficients");
            if (ConicSolveComplex())
                Console.WriteLine("Successfully solved cubic with complex coefficients");
            if (QuarticSolveReal())
                Console.WriteLine("Successfully solved quartic with real coefficients");
            if (QuarticSolveComplex())
                Console.WriteLine("Successfully solved quartic with complex coefficients");
        }
        public static bool ConicSolveRealCoefficients()
        {
            for (int i = 0; i < numTrials; i++)
            {
                var i1 = i % 2 == 0 ? 0 : r100;
                var r1 = r100;
                var origRoots = new List<ComplexNumber>
                {
                    new ComplexNumber(r1,i1),
                    new ComplexNumber( i % 2 == 0 ? r100:r1,-i1),
                    new ComplexNumber(r100),
                };
                var a = r100;
                var b = -a * (origRoots[0] + origRoots[1] + origRoots[2]);
                var c = +a * (origRoots[0] * origRoots[1] + origRoots[0] * origRoots[2] + origRoots[1] * origRoots[2]);
                var d = -a * (origRoots[0] * origRoots[1] * origRoots[2]);

                if (!b.JustRealNumber || !c.JustRealNumber || !d.JustRealNumber)
                    continue;
                foreach (var root in PolynomialSolve.Cubic(a, b.Real, c.Real, d.Real))
                {
                    if (!origRoots.Exists(origRoot => 2 * Math.Abs(root.Real - origRoot.Real) / (root.Real + origRoot.Real + maxError) < maxError &&
                    2 * Math.Abs(root.Imaginary - origRoot.Imaginary) / (root.Imaginary + origRoot.Imaginary + maxError) < maxError))
                        return false;
                }

            }
            return true;
        }
        public static bool ConicSolveComplex()
        {
            for (int i = 0; i < numTrials; i++)
            {
                var r1 = r100;
                var origRoots = new List<ComplexNumber>
                {
                    new ComplexNumber(r100,r100),
                    new ComplexNumber(r100,r100),
                    new ComplexNumber(r100,r100)
                };
                var a = r100;
                var b = -a * (origRoots[0] + origRoots[1] + origRoots[2]);
                var c = +a * (origRoots[0] * origRoots[1] + origRoots[0] * origRoots[2] + origRoots[1] * origRoots[2]);
                var d = -a * (origRoots[0] * origRoots[1] * origRoots[2]);

                foreach (var root in PolynomialSolve.Cubic(new ComplexNumber(a), b, c, d))
                {
                    if (!origRoots.Exists(origRoot => 2 * Math.Abs(root.Real - origRoot.Real) / (root.Real + origRoot.Real + maxError) < maxError &&
                    2 * Math.Abs(root.Imaginary - origRoot.Imaginary) / (root.Imaginary + origRoot.Imaginary + maxError) < maxError))
                        return false;
                }

            }
            return true;
        }
        public static bool QuarticSolveReal()
        {
            for (int i = 0; i < numTrials; i++)
            {
                var origRoots = new List<ComplexNumber>
                {
                    new ComplexNumber(r100),
                    new ComplexNumber(r100),
                    new ComplexNumber(r100),
                    new ComplexNumber(r100)
                };
                var a = r100;
                var b = -a * (origRoots[0] + origRoots[1] + origRoots[2] + origRoots[3]);
                var c = +a * (origRoots[0] * origRoots[1] + origRoots[0] * origRoots[2] + origRoots[1] * origRoots[2] + origRoots[0] * origRoots[3] + origRoots[1] * origRoots[3] + origRoots[2] * origRoots[3]);
                var d = -a * (origRoots[0] * origRoots[1] * origRoots[2] + origRoots[0] * origRoots[1] * origRoots[3] + origRoots[0] * origRoots[2] * origRoots[3] + origRoots[1] * origRoots[2] * origRoots[3]);
                var e = +a * (origRoots[0] * origRoots[1] * origRoots[2] * origRoots[3]);

                if (!b.JustRealNumber || !c.JustRealNumber || !d.JustRealNumber || !e.JustRealNumber)
                    continue;
                //var roots = PolynomialSolve.Quartic(a, b.Real, c.Real, d.Real, e.Real);
                //var rootsArray = new[] { roots.Item1, roots.Item2, roots.Item3, roots.Item4 };
                foreach (var root in PolynomialSolve.Quartic(a, b.Real, c.Real, d.Real, e.Real))
                {
                    if (!origRoots.Exists(origRoot => 2 * Math.Abs(root.Real - origRoot.Real) / (root.Real + origRoot.Real + maxError) < maxError &&
                    2 * Math.Abs(root.Imaginary - origRoot.Imaginary) / (root.Imaginary + origRoot.Imaginary + maxError) < maxError))
                        return false;
                }


            }
            return true;
        }
        public static bool QuarticSolveComplex()
        {
            for (int i = 0; i < numTrials; i++)
            {
                var origRoots = new List<ComplexNumber>
                {
                    new ComplexNumber(r100,r100),
                    new ComplexNumber(r100,r100),
                    new ComplexNumber(r100,r100),
                    new ComplexNumber(r100,r100)
                };
                var a = r100;
                var b = -a * (origRoots[0] + origRoots[1] + origRoots[2] + origRoots[3]);
                var c = +a * (origRoots[0] * origRoots[1] + origRoots[0] * origRoots[2] + origRoots[1] * origRoots[2] + origRoots[0] * origRoots[3] + origRoots[1] * origRoots[3] + origRoots[2] * origRoots[3]);
                var d = -a * (origRoots[0] * origRoots[1] * origRoots[2] + origRoots[0] * origRoots[1] * origRoots[3] + origRoots[0] * origRoots[2] * origRoots[3] + origRoots[1] * origRoots[2] * origRoots[3]);
                var e = +a * (origRoots[0] * origRoots[1] * origRoots[2] * origRoots[3]);

                foreach (var root in PolynomialSolve.Quartic(new ComplexNumber(a), b, c, d, e))
                {
                    if (!origRoots.Exists(origRoot => 2 * Math.Abs(root.Real - origRoot.Real) / (root.Real + origRoot.Real + maxError) < maxError &&
                    2 * Math.Abs(root.Imaginary - origRoot.Imaginary) / (root.Imaginary + origRoot.Imaginary + maxError) < maxError))
                        return false;
                }
            }
            return true;
        }
    }
}