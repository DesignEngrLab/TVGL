using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVGL.Miscellaneous_Functions
{
    public static class PlatonicConstants
    {
        readonly static double phi0 = (Math.Sqrt(5) - 1) / 2;
        readonly static double phi1 = (Math.Sqrt(5) + 1) / 2;
        readonly static double[] cubeNX = new double[] { -1, 0, 0 };
        readonly static double[] cubePX = new double[] { 1, 0, 0 };
        readonly static double[] cubeNY = new double[] { 0, -1, 0 };
        readonly static double[] cubePY = new double[] { 0, 1, 0 };
        readonly static double[] cubeNZ = new double[] { 0, 0, -1 };
        readonly static double[] cubePZ = new double[] { 0, 0, 1 };

        readonly static double[] octahe1 = new double[] { +1, +1, +1 };  //these eight of the octahedronare also the faces of a two tetrahedra
        readonly static double[] octahe2 = new double[] { +1, +1, -1 };
        readonly static double[] octahe3 = new double[] { +1, -1, +1 };
        readonly static double[] octahe4 = new double[] { +1, -1, -1 };
        readonly static double[] octahe5 = new double[] { -1, +1, +1 };
        readonly static double[] octahe6 = new double[] { -1, +1, -1 };
        readonly static double[] octahe7 = new double[] { -1, -1, +1 };
        readonly static double[] octahe8 = new double[] { -1, -1, -1 };

        readonly static double[] icosa9 = new double[] { 0, +phi0, +phi1 };
        readonly static double[] icosa10 = new double[] { 0, +phi0, -phi1 };
        readonly static double[] icosa11 = new double[] { 0, -phi0, +phi1 };
        readonly static double[] icosa12 = new double[] { 0, -phi0, -phi1 };

        readonly static double[] icosa13 = new double[] { +phi1, 0.0, +phi0 };
        readonly static double[] icosa14 = new double[] { +phi1, 0.0, -phi0 };
        readonly static double[] icosa15 = new double[] { -phi1, 0.0, +phi0 };
        readonly static double[] icosa16 = new double[] { -phi1, 0.0, -phi0 };

        readonly static double[] icosa17 = new double[] { +phi0, +phi1, 0 };
        readonly static double[] icosa18 = new double[] { +phi0, -phi1, 0 };
        readonly static double[] icosa19 = new double[] { -phi0, +phi1, 0 };
        readonly static double[] icosa20 = new double[] { -phi0, -phi1, 0 };

        readonly static double[] dodecA = new double[] { 0, +phi1, +1 };  //0
        readonly static double[] dodecB = new double[] { 0, -phi1, +1 };  //1
        readonly static double[] dodecC = new double[] { 0, -phi1, -1 };  //2
        readonly static double[] dodecD = new double[] { 0, +phi1, -1 };  //3

        readonly static double[] dodecE = new double[] { +1, 0, +phi1 };  //4
        readonly static double[] dodecF = new double[] { +1, 0, -phi1 };  //5
        readonly static double[] dodecG = new double[] { -1, 0, -phi1 };  //6
        readonly static double[] dodecH = new double[] { -1, 0, +phi1 };  //7

        readonly static double[] dodecI = new double[] { +phi1, +1, 0 };  //8
        readonly static double[] dodecJ = new double[] { -phi1, +1, 0 };  //9
        readonly static double[] dodecK = new double[] { -phi1, -1, 0 };  //10
        readonly static double[] dodecL = new double[] { +phi1, -1, 0 };  //11

        // thirty edges of dodecahedron or icosahedron as faces define the triaconthedron
        // six of these are the same as the cube
        // #0 cubeNX (J-K), #1 cubeNY (B-C), #2 cubeNZ (F-G), #3 cubePX (I-L), #4 cubePY (A-D), #5 cubePZ (E-H)
        readonly static double[] triaco7 = new double[] { 1, +phi1, 1 + phi1 }; // A-E edge
        readonly static double[] triaco8 = new double[] { 1 + phi1, 1, +phi1 }; // E-I edge
        readonly static double[] triaco9 = new double[] { +phi1, 1 + phi1, 1 }; // A-I edge

        readonly static double[] triaco10 = new double[] { +phi1, -phi1 - 1, +1 }; // B-L edge
        readonly static double[] triaco11 = new double[] { 1 + phi1, -1, +phi1 }; // L-E edge
        readonly static double[] triaco12 = new double[] { 1, -phi1, phi1 + 1 }; // E-B edge

        readonly static double[] triaco13 = new double[] { 1, -phi1, -phi1 - 1 }; // C-F edge
        readonly static double[] triaco14 = new double[] { 1 + phi1, -1, -phi1 }; // F-L edge
        readonly static double[] triaco15 = new double[] { +phi1, -phi1 - 1, -1 }; // L-C edge

        readonly static double[] triaco16 = new double[] { +phi1, 1 + phi1, -1 }; // D-I edge
        readonly static double[] triaco17 = new double[] { 1 + phi1, 1, -phi1 }; // I-F edge
        readonly static double[] triaco18 = new double[] { 1, phi1, -phi1 - 1 }; // F-D edge

        readonly static double[] triaco19 = new double[] { -1, +phi1, 1 + phi1 }; //A-H
        readonly static double[] triaco20 = new double[] { -1 - phi1, 1, +phi1 }; //J-H
        readonly static double[] triaco21 = new double[] { -phi1, 1 + phi1, 1 }; //A-J

        readonly static double[] triaco22 = new double[] { -phi1, -phi1 - 1, +1 }; //B-K
        readonly static double[] triaco23 = new double[] { -1 - phi1, -1, +phi1 }; //H-K
        readonly static double[] triaco24 = new double[] { -1, -phi1, phi1 + 1 };  //B-H

        readonly static double[] triaco25 = new double[] { -1, -phi1, -phi1 - 1 }; // C-G
        readonly static double[] triaco26 = new double[] { -1 - phi1, -1, -phi1 }; // G-K
        readonly static double[] triaco27 = new double[] { -phi1, -phi1 - 1, -1 }; // C-K

        readonly static double[] triaco28 = new double[] { -phi1, 1 + phi1, -1 }; // D-J
        readonly static double[] triaco29 = new double[] { -1 - phi1, 1, -phi1 }; // J-G
        readonly static double[] triaco30 = new double[] { -1, phi1, -phi1 - 1 }; // D-G

        // these are indices to the dodecahedron vertices
        /// <summary>
        /// The indicies of the icosahedron vertices and the faces of the dodecahedron
        /// </summary>
        public readonly static int[][] IcosaTriangleVertices = new[]
          {
            new[]{0, 4, 8}, //icosa1: A, E, I
            new[]{3, 8, 5}, //icosa2: D, I, F
            new[]{1, 11, 4}, //icosa3: B, L, E
            new[]{2, 5, 11}, //icosa4: C, F, L
            new[]{0, 9, 7}, //icosa5: A, J, H
            new[]{3, 6, 9}, //icosa6: D, G, J
            new[]{1, 7, 10}, //icosa7: B, H, K
            new[]{2, 10, 6}, //icosa8: C, K, G
            new[]{0, 7, 4}, //icosa9: A, H, E
            new[]{3, 5, 6}, //icosa10: D, F, G
            new[]{1, 4, 7}, //icosa11: B, E, H
            new[]{2, 6, 5}, //icosa12: C, G, F
            new[]{4, 11, 8}, //icosa13: E, L, I
            new[]{5, 8, 11}, //icosa14: F, I, L
            new[]{7, 9, 10}, //icosa15: H, J, K
            new[]{6, 10, 9}, //icosa16: G, K J
            new[]{0, 8, 3}, //icosa17: A, I, D
            new[]{1, 2, 11}, //icosa18: B, C, L
            new[]{0, 3, 9}, //icosa19: A, D, J
            new[]{1, 10, 2}, //icosa20: B, K, C
        };

        // these are indices to the triacontahedron faces which are located at the midpoint of the icasohedron edges
        public readonly static int[][] IcosaTriangleEdges = new[]
         {
            new[]{6,7,8}, //octahe1: A, E, I
            new[]{15,16,17}, //octahe2: D, I, F
            new[]{9,10,11}, //octahe3: B, L, E
            new[]{12,13,14}, //octahe4: C, F, L
            new[]{20,19,18}, //octahe5: A, J, H
            new[]{29,28,27}, //octahe6: D, G, J
            new[]{23, 22, 21}, //octahe7: B, H, K
            new[]{26, 25, 24}, //octahe8: C, K, G
            new[]{18, 5, 6 }, //icosa9: A, H, E
            new[]{17, 2, 29}, //icosa10: D, F, G
            new[]{11, 5, 23}, //icosa11: B, E, H
            new[]{24, 2, 12 }, //icosa12: C, G, F
            new[]{10, 3, 7}, //icosa13: E, L, I
            new[]{16, 3, 13 }, //icosa14: F, I, L
            new[]{19, 0, 22}, //icosa15: H, J, K
            new[]{25, 0, 28}, //icosa16: G, K J
            new[]{8, 15, 4 }, //icosa17: A, I, D
            new[]{1, 14, 9}, //icosa18: B, C, L
            new[]{4, 27, 20}, //icosa19: A, D, J
            new[]{21, 26, 1 }, //icosa20: B, K, C
        };

        public readonly static int[][] IcosaEdgeToFaces = new[]
        {
            new[]{ 0,  8 }, new[]{ 0,  12 }, new[]{ 0,  16 },
            new[]{ 1,  16 }, new[]{ 1,  13 }, new[]{ 1,  9 },
            new[]{ 2,  17 }, new[]{ 2,  12 }, new[]{ 2,  10 },
            new[]{ 3,  11 }, new[]{ 3,  13 }, new[]{ 3,  17 },
            new[]{ 4,  8 }, new[]{ 4,  14 }, new[]{ 4,  18 },
            new[]{ 5,  18 }, new[]{ 5,  15 }, new[]{ 5,  9 },
            new[]{ 6,  19 }, new[]{ 6,  14 }, new[]{ 6,  10 },
            new[]{ 7,  11 }, new[]{ 7,  15 }, new[]{ 7,  19 },
            new[]{ 8,  10 }, new[]{ 9,  11 }, new[]{ 12,  13 },
            new[]{ 14,  15 },  new[]{ 16,  18 }, new[]{ 17,  19 }
        };
        /// <summary>
        /// Gets the cube directions.
        /// </summary>
        /// <value>The cube directions.</value>
        public static double[][] CubeDirections
        {
            get
            {
                if (_cubeDirections == null)
                {
                    _cubeDirections = new[]
                    {
                        cubeNX,
                        cubeNY,
                        cubeNZ,
                        cubePX,
                        cubePY,
                        cubePZ
                    };
                }
                return _cubeDirections;
            }
        }
        private static double[][] _cubeDirections;


        /// <summary>
        /// Gets the octahedron directions.
        /// </summary>
        /// <value>The octahedron directions.</value>
        public static double[][] OctahedronDirections
        {
            get
            {
                if (_octahedronDirections == null)
                {
                    _octahedronDirections = new[]
                    {
                        octahe1.Normalize(),
                        octahe2.Normalize(),
                        octahe3.Normalize(),
                        octahe4.Normalize(),
                        octahe5.Normalize(),
                        octahe6.Normalize(),
                        octahe7.Normalize(),
                        octahe8.Normalize()
                    };
                }
                return _octahedronDirections;
            }
        }
        private static double[][] _octahedronDirections;

        /// <summary>
        /// Gets the dodechedron directions.
        /// </summary>
        /// <value>The dodechedron directions.</value>
        public static double[][] DodechedronDirections
        {
            get
            {
                if (_dodechedronDirections == null)
                {
                    _dodechedronDirections = new[] {
                        dodecA.Normalize(),
                        dodecB.Normalize(),
                        dodecC.Normalize(),
                        dodecD.Normalize(),
                        dodecE.Normalize(),
                        dodecF.Normalize(),
                        dodecG.Normalize(),
                        dodecH.Normalize(),
                        dodecI.Normalize(),
                        dodecJ.Normalize(),
                        dodecK.Normalize(),
                        dodecL.Normalize()
                    };
                }
                return _dodechedronDirections;
            }
        }
        private static double[][] _dodechedronDirections;

        /// <summary>
        /// Gets the icasohedron directions.
        /// </summary>
        /// <value>The icasohedron directions.</value>
        public static double[][] IcosahedronDirections
        {
            get
            {
                if (_icosahedronDirections == null)
                {
                    _icosahedronDirections = new[]
                    {
                        octahe1.Normalize(), octahe2.Normalize(), octahe3.Normalize(), octahe4.Normalize(),
                        octahe5.Normalize(), octahe6.Normalize(), octahe7.Normalize(), octahe8.Normalize(),
                        icosa9.Normalize(), icosa10.Normalize(), icosa11.Normalize(), icosa12.Normalize(),
                        icosa13.Normalize(), icosa14.Normalize(), icosa15.Normalize(), icosa16.Normalize(),
                        icosa17.Normalize(), icosa18.Normalize(), icosa19.Normalize(), icosa20.Normalize()
                    };
                }
                return _icosahedronDirections;
            }
        }
        private static double[][] _icosahedronDirections;

        /// <summary>
        /// Gets the triacontahedron directions.
        /// </summary>
        /// <value>The triacontahedron directions.</value>
        public static double[][] TriacontahedronDirections
        {
            get
            {
                if (_triacontahedronDirections == null)
                {
                    _triacontahedronDirections = new double[][]
                    {
                        Normalize(cubeNX), cubeNY.Normalize(), cubeNZ.Normalize(),
                        cubePX.Normalize(), cubePY.Normalize(), cubePZ.Normalize(),
                        triaco7.Normalize(), triaco8.Normalize(),  triaco9.Normalize(),
                        triaco10.Normalize(),  triaco11.Normalize(), triaco12.Normalize(),
                        triaco13.Normalize(), triaco14.Normalize(), triaco15.Normalize(),
                        triaco16.Normalize(), triaco17.Normalize(), triaco18.Normalize(),
                        triaco19.Normalize(),  triaco20.Normalize(),  triaco21.Normalize(),
                        triaco22.Normalize(), triaco23.Normalize(), triaco24.Normalize(),
                        triaco25.Normalize(), triaco26.Normalize(), triaco27.Normalize(),
                        triaco28.Normalize(),  triaco29.Normalize(),  triaco30.Normalize()
                    };
                }
                return _triacontahedronDirections;
            }
        }
        private static double[][] _triacontahedronDirections;
        public static double[][] AllDirections
        {
            get
            {
                if (_allDirections == null)
                {
                    _allDirections = new double[][]
                    {
                        cubeNX.Normalize(), cubeNY.Normalize(), cubeNZ.Normalize(),
                        cubePX.Normalize(), cubePY.Normalize(), cubePZ.Normalize(),
                        octahe1.Normalize(), octahe2.Normalize(), octahe3.Normalize(), octahe4.Normalize(),
                        octahe5.Normalize(), octahe6.Normalize(), octahe7.Normalize(), octahe8.Normalize(),

                        dodecA.Normalize(), dodecB.Normalize(), dodecC.Normalize(), dodecD.Normalize(),
                        dodecE.Normalize(), dodecF.Normalize(), dodecG.Normalize(), dodecH.Normalize(),
                        dodecI.Normalize(), dodecJ.Normalize(), dodecK.Normalize(), dodecL.Normalize(),

                        icosa9.Normalize(), icosa10.Normalize(), icosa11.Normalize(), icosa12.Normalize(),
                        icosa13.Normalize(), icosa14.Normalize(), icosa15.Normalize(), icosa16.Normalize(),
                        icosa17.Normalize(), icosa18.Normalize(), icosa19.Normalize(), icosa20.Normalize(),

                        triaco7.Normalize(), triaco8.Normalize(),  triaco9.Normalize(),
                        triaco10.Normalize(),  triaco11.Normalize(), triaco12.Normalize(),
                        triaco13.Normalize(), triaco14.Normalize(), triaco15.Normalize(),
                        triaco16.Normalize(), triaco17.Normalize(), triaco18.Normalize(),
                        triaco19.Normalize(),  triaco20.Normalize(),  triaco21.Normalize(),
                        triaco22.Normalize(), triaco23.Normalize(), triaco24.Normalize(),
                        triaco25.Normalize(), triaco26.Normalize(), triaco27.Normalize(),
                        triaco28.Normalize(),  triaco29.Normalize(),  triaco30.Normalize()
                    };
                }
                return _allDirections;
            }
        }
        private static double[][] _allDirections;

        public static double[] Normalize(this double[] vector)
        {
            var mag = Math.Sqrt(vector[0] * vector[0] + vector[1] * vector[1] + vector[2] * vector[2]);
            vector[0] /= mag;
            vector[1] /= mag;
            vector[2] /= mag;
            return vector;
        }
    }
}
