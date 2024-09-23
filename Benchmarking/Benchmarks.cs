using BenchmarkDotNet;
using BenchmarkDotNet.Attributes;
using StarMathLib;
using System;
using TVGL;

namespace Benchmarking
{
    public class Benchmarks
    {
        static Random r = new Random();
        static double r1 => 2.0 * r.NextDouble() - 1.0;
        static double r100 => 200.0 * r.NextDouble() - 100.0;
        private Vector3 q;
        private double[,] matrix;

        [GlobalSetup]
        public void Setup()
        {
            q = new Vector3(r1, r1, r1); //.Normalize();
            matrix = new double[,] { { r100, r100, r100, r100 }, { r100, r100, r100, r100 },
                { r100, r100, r100, r100 }, { r100, r100, r100, r100 } };
        }

        [Benchmark]
        public ComplexNumber[] OLD() =>  StarMath.GetEigenValuesAndVectors(matrix, out var eVOld);

        [Benchmark]
        public ComplexNumber[] NEW() => StarMath.GetEigenValuesAndVectors4(matrix[0, 0], matrix[0, 1], matrix[0, 2], matrix[0, 3],
            matrix[1, 0], matrix[1, 1], matrix[1, 2], matrix[1, 3],
            matrix[2, 0], matrix[2, 1], matrix[2, 2], matrix[2, 3],
            matrix[3, 0], matrix[3, 1], matrix[3, 2], matrix[3, 3], out var eVNew);



    }

}
