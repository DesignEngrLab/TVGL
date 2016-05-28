// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 05-24-2016
//
// Last Modified By : Design Engineering Lab
// Last Modified On : 05-26-2016
// ***********************************************************************
// <copyright file="ClassificationConstants.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TVGL
{
    /// <summary>
    ///     Class Primitive_Classification.
    /// </summary>
    public static partial class Primitive_Classification
    {
        /// <summary>
        ///     Class ClassificationConstants.
        /// </summary>
        internal static class ClassificationConstants
        {
            /// <summary>
            ///     The classifier_ minimum area for start face
            /// </summary>
            internal const double Classifier_MinAreaForStartFace = 0.0001;

            /// <summary>
            ///     The abn le1
            /// </summary>
            internal const double AbnLe1 = 0.25;

            /// <summary>
            ///     The abn le2
            /// </summary>
            internal const double AbnLe2 = 0.5;

            /// <summary>
            ///     The abn MS1
            /// </summary>
            internal const double AbnMs1 = 0.25;

            /// <summary>
            ///     The abn MS2
            /// </summary>
            internal const double AbnMs2 = 0.5;

            /// <summary>
            ///     The abn me1
            /// </summary>
            internal const double AbnMe1 = 22;

            /// <summary>
            ///     The abn me2
            /// </summary>
            internal const double AbnMe2 = 25;

            /// <summary>
            ///     The abn HS1
            /// </summary>
            internal const double AbnHs1 = 22;

            /// <summary>
            ///     The abn HS2
            /// </summary>
            internal const double AbnHs2 = 25;

            /// <summary>
            ///     The MCM le1
            /// </summary>
            internal const double McmLe1 = 0.01;

            /// <summary>
            ///     The MCM le2
            /// </summary>
            internal const double McmLe2 = 0.015;

            /// <summary>
            ///     The MCM MS1
            /// </summary>
            internal const double McmMs1 = 0.01;

            /// <summary>
            ///     The MCM MS2
            /// </summary>
            internal const double McmMs2 = 0.015;

            /// <summary>
            ///     The MCM me1
            /// </summary>
            internal const double McmMe1 = 0.2;

            /// <summary>
            ///     The MCM me2
            /// </summary>
            internal const double McmMe2 = 0.3;

            /// <summary>
            ///     The MCM HS1
            /// </summary>
            internal const double McmHs1 = 0.2;

            /// <summary>
            ///     The MCM HS2
            /// </summary>
            internal const double McmHs2 = 0.3;

            /// <summary>
            ///     The sm le1
            /// </summary>
            internal const double SmLe1 = 0.1;

            /// <summary>
            ///     The sm le2
            /// </summary>
            internal const double SmLe2 = 0.2;

            /// <summary>
            ///     The sm MS1
            /// </summary>
            internal const double SmMs1 = 0.1;

            /// <summary>
            ///     The sm MS2
            /// </summary>
            internal const double SmMs2 = 0.2;

            /// <summary>
            ///     The sm me1
            /// </summary>
            internal const double SmMe1 = 1.8;

            /// <summary>
            ///     The sm me2
            /// </summary>
            internal const double SmMe2 = 2;

            /// <summary>
            ///     The sm HS1
            /// </summary>
            internal const double SmHs1 = 1.8;

            /// <summary>
            ///     The sm HS2
            /// </summary>
            internal const double SmHs2 = 2;

            /// <summary>
            ///     The minimum cone gauss plane offset
            /// </summary>
            internal const double MinConeGaussPlaneOffset = 0.1; // sine of 1 degrees 0.1

            /// <summary>
            ///     Makings the list of lim ab nbeta2.
            /// </summary>
            /// <returns>System.Double[].</returns>
            internal static double[] MakingListOfLimABNbeta2()
            {
                var listOfLimits = new[]
                {
                    AbnLe1,
                    AbnLe2,
                    AbnMs1,
                    AbnMs2,
                    AbnMe1,
                    AbnMe2,
                    AbnHs1,
                    AbnHs2
                };
                return listOfLimits;
            }

            /// <summary>
            ///     Makings the list of lim mc mbeta2.
            /// </summary>
            /// <returns>System.Double[].</returns>
            internal static double[] MakingListOfLimMCMbeta2()
            {
                var listOfLimits = new[]
                {
                    McmLe1,
                    McmLe2,
                    McmMs1,
                    McmMs2,
                    McmMe1,
                    McmMe2,
                    McmHs1,
                    McmHs2
                };
                return listOfLimits;
            }

            /// <summary>
            ///     Makings the list of lim s mbeta2.
            /// </summary>
            /// <returns>System.Double[].</returns>
            internal static double[] MakingListOfLimSMbeta2()
            {
                var listOfLimits = new[]
                {
                    SmLe1,
                    SmLe2,
                    SmMs1,
                    SmMs2,
                    SmMe1,
                    SmMe2,
                    SmHs1,
                    SmHs2
                };
                return listOfLimits;
            }

            /// <summary>
            ///     Readings the edges rules2.
            /// </summary>
            /// <returns>System.Int32[].</returns>
            internal static int[,] readingEdgesRules2()
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "TVGL.Primitive_Classification.NewEdgeRules.csv";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    return ReadIntMatrix(stream, 4);
                }
            }

            /// <summary>
            ///     Readings the faces rules.
            /// </summary>
            /// <returns>System.Int32[].</returns>
            internal static int[,] readingFacesRules()
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "TVGL.Primitive_Classification.NewFaRules.csv";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    return ReadIntMatrix(stream, 7);
                }
            }

            /// <summary>
            ///     Gets the stream reader.
            /// </summary>
            /// <param name="filepath">The filepath.</param>
            /// <returns>StreamReader.</returns>
            private static StreamReader getStreamReader(string filepath)
            {
                //var assembly = typeof(TesselationToPrimitives).GetTypeInfo().Assembly;
                // Once you figure out the name, pass it in as the argument here.
                //Stream stream = assembly.GetManifestResourceStream(@"TVGL.Resources." + filepath);
                //return new StreamReader(stream);
                return null;
            }

            /// <summary>
            ///     Reads the int matrix.
            /// </summary>
            /// <param name="stream">The stream.</param>
            /// <param name="numColumns">The number columns.</param>
            /// <returns>System.Int32[].</returns>
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
                            .Select(str => Convert.ToInt32(str))
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