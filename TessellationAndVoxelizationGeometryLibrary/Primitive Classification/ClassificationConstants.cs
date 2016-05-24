using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TVGL
{
    public static partial class Primitive_Classification
    {
        internal static class ClassificationConstants
        {

            internal const double Classifier_MinAreaForStartFace = 0.0001;

            internal const double AbnLe1 = 0.25;
            internal const double AbnLe2 = 0.5;
            internal const double AbnMs1 = 0.25;
            internal const double AbnMs2 = 0.5;
            internal const double AbnMe1 = 22;
            internal const double AbnMe2 = 25;
            internal const double AbnHs1 = 22;
            internal const double AbnHs2 = 25;

            internal const double McmLe1 = 0.01;
            internal const double McmLe2 = 0.015;
            internal const double McmMs1 = 0.01;
            internal const double McmMs2 = 0.015;
            internal const double McmMe1 = 0.2;
            internal const double McmMe2 = 0.3;
            internal const double McmHs1 = 0.2;
            internal const double McmHs2 = 0.3;

            internal const double SmLe1 = 0.1;
            internal const double SmLe2 = 0.2;
            internal const double SmMs1 = 0.1;
            internal const double SmMs2 = 0.2;
            internal const double SmMe1 = 1.8;
            internal const double SmMe2 = 2;
            internal const double SmHs1 = 1.8;
            internal const double SmHs2 = 2;
            internal const double MinConeGaussPlaneOffset = 0.1; // sine of 1 degrees 0.1

            internal static double[] MakingListOfLimABNbeta2()
            {

                var listOfLimits = new double[]
                {
                    AbnLe1,
                    AbnLe2,
                    AbnMs1,
                    AbnMs2,
                    AbnMe1,
                    AbnMe2,
                    AbnHs1,
                    AbnHs2,
                };
                return listOfLimits;
            }

            internal static double[] MakingListOfLimMCMbeta2()
            {
                var listOfLimits = new double[]
                {
                    McmLe1,
                    McmLe2,
                    McmMs1,
                    McmMs2,
                    McmMe1,
                    McmMe2,
                    McmHs1,
                    McmHs2,
                };
                return listOfLimits;
            }

            internal static double[] MakingListOfLimSMbeta2()
            {
                var listOfLimits = new double[]
                {
                    SmLe1,
                    SmLe2,
                    SmMs1,
                    SmMs2,
                    SmMe1,
                    SmMe2,
                    SmHs1,
                    SmHs2,
                };
                return listOfLimits;
            }

            internal static int[,] readingEdgesRules2()
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "TVGL.Primitive_Classification.NewEdgeRules.csv";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    return ReadIntMatrix(stream, 4);
                }
            }

            internal static int[,] readingFacesRules()
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "TVGL.Primitive_Classification.NewFaRules.csv";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    return ReadIntMatrix(stream, 7);
                }
            }

            private static StreamReader getStreamReader(string filepath)
            {
                //var assembly = typeof(TesselationToPrimitives).GetTypeInfo().Assembly;
                // Once you figure out the name, pass it in as the argument here.
                //Stream stream = assembly.GetManifestResourceStream(@"TVGL.Resources." + filepath);
                //return new StreamReader(stream);
                return null;
            }

            private static int[,] ReadIntMatrix(Stream stream, int numColumns)
            {
                using (var reader = new StreamReader(stream))
                {
                    // First read all lines to know how many there are
                    var lines = new List<int[]>();
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine()
                            .Split(',')
                            .Take(numColumns)
                            .Select((str) => Convert.ToInt32(str))
                            .ToArray();

                        lines.Add(line);
                    }

                    // Now convert to native matrix
                    var result = new int[numColumns, lines.Count];

                    for (var i = 0; i < numColumns; i++)
                    {
                        for (var j = 0; j < lines.Count; j++)
                        {
                            result[i, j] = lines[j][i];
                        }
                    }

                    return result;
                }
            }
        }
    }
}
