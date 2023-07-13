using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TVGL;
using TVGL.PointCloud;

namespace TVGLUnitTestsAndBenchmarking
{
    internal class ICPTesting
    {
        internal static void Test1()
        {
            var myWriter = new ConsoleTraceListener();
            Trace.Listeners.Add(myWriter);
            TVGL.Message.Verbosity = VerbosityLevels.Everything;
            var points = new List<Vector3> {
                new Vector3(3, 1,1),
                new Vector3(2, 6,1),
                new Vector3(5,4,1),
                new Vector3(8,7, 2),
                new Vector3(10,2, 2),
                new Vector3(13,3, 2),
            };
            var quat = new Quaternion(new Vector4(3,2,1,1).Normalize());
            var translate = Matrix4x4.CreateTranslation(1, 2, 3);
            var transfrom = Matrix4x4.CreateFromQuaternion(quat) * translate;
            var targetPoints = points.Select(p => p.Transform(transfrom)).ToList();
            var tPredicted = IterativeClosestPoint3D.Run(points, targetPoints);
            Message.  output(transfrom);
            Message.output(tPredicted);
        }

    }
}
