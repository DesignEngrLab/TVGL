using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVGL
{
    public static class PlatonicConstants
    {
        readonly static double phi0 = (Math.Sqrt(5) - 1) / 2;
        readonly static double phi1 = (Math.Sqrt(5) + 1) / 2;
        readonly static Vector3 octahe1 = new Vector3(+1, +1, +1).Normalize();  //these eight of the octahedronare also the faces of a two tetrahedra
        readonly static Vector3 octahe2 = new Vector3(+1, -1, -1).Normalize();
        readonly static Vector3 octahe3 = new Vector3(-1, +1, -1).Normalize();
        readonly static Vector3 octahe4 = new Vector3(-1, -1, +1).Normalize();
        readonly static Vector3 octahe5 = new Vector3(-1, -1, -1).Normalize();
        readonly static Vector3 octahe6 = new Vector3(-1, +1, +1).Normalize();
        readonly static Vector3 octahe7 = new Vector3(+1, -1, +1).Normalize();
        readonly static Vector3 octahe8 = new Vector3(+1, +1, -1).Normalize();

        readonly static Vector3 icosa9 = new Vector3(0, +phi0, +phi1).Normalize();
        readonly static Vector3 icosa10 = new Vector3(0, +phi0, -phi1).Normalize();
        readonly static Vector3 icosa11 = new Vector3(0, -phi0, +phi1).Normalize();
        readonly static Vector3 icosa12 = new Vector3(0, -phi0, -phi1).Normalize();

        readonly static Vector3 icosa13 = new Vector3(+phi1, 0.0, +phi0).Normalize();
        readonly static Vector3 icosa14 = new Vector3(+phi1, 0.0, -phi0).Normalize();
        readonly static Vector3 icosa15 = new Vector3(-phi1, 0.0, +phi0).Normalize();
        readonly static Vector3 icosa16 = new Vector3(-phi1, 0.0, -phi0).Normalize();

        readonly static Vector3 icosa17 = new Vector3(+phi0, +phi1, 0).Normalize();
        readonly static Vector3 icosa18 = new Vector3(+phi0, -phi1, 0).Normalize();
        readonly static Vector3 icosa19 = new Vector3(-phi0, +phi1, 0).Normalize();
        readonly static Vector3 icosa20 = new Vector3(-phi0, -phi1, 0).Normalize();

        readonly static Vector3 dodecA = new Vector3(0, +phi1, +1).Normalize();  //0
        readonly static Vector3 dodecB = new Vector3(0, -phi1, +1).Normalize();  //1
        readonly static Vector3 dodecC = new Vector3(0, -phi1, -1).Normalize();  //2
        readonly static Vector3 dodecD = new Vector3(0, +phi1, -1).Normalize();  //3

        readonly static Vector3 dodecE = new Vector3(+1, 0, +phi1).Normalize();  //4
        readonly static Vector3 dodecF = new Vector3(+1, 0, -phi1).Normalize();  //5
        readonly static Vector3 dodecG = new Vector3(-1, 0, -phi1).Normalize();  //6
        readonly static Vector3 dodecH = new Vector3(-1, 0, +phi1).Normalize();  //7

        readonly static Vector3 dodecI = new Vector3(+phi1, +1, 0).Normalize();  //8
        readonly static Vector3 dodecJ = new Vector3(-phi1, +1, 0).Normalize();  //9
        readonly static Vector3 dodecK = new Vector3(-phi1, -1, 0).Normalize();  //10
        readonly static Vector3 dodecL = new Vector3(+phi1, -1, 0).Normalize();  //11

        // thirty edges of dodecahedron or icosahedron as faces define the triaconthedron
        // six of these are the same as the cube
        // #0 -Vector3.UnitX (J-K), #1 -Vector3.UnitY (B-C), #2 -Vector3.UnitZ (F-G), #3 Vector3.UnitX (I-L), #4 Vector3.UnitY (A-D), #5 Vector3.UnitZ (E-H)
        readonly static Vector3 triaco7 = new Vector3(1, +phi1, 1 + phi1).Normalize(); // A-E edge
        readonly static Vector3 triaco8 = new Vector3(1 + phi1, 1, +phi1).Normalize(); // E-I edge
        readonly static Vector3 triaco9 = new Vector3(+phi1, 1 + phi1, 1).Normalize(); // A-I edge

        readonly static Vector3 triaco10 = new Vector3(+phi1, -phi1 - 1, +1).Normalize(); // B-L edge
        readonly static Vector3 triaco11 = new Vector3(1 + phi1, -1, +phi1).Normalize(); // L-E edge
        readonly static Vector3 triaco12 = new Vector3(1, -phi1, phi1 + 1).Normalize(); // E-B edge

        readonly static Vector3 triaco13 = new Vector3(1, -phi1, -phi1 - 1).Normalize(); // C-F edge
        readonly static Vector3 triaco14 = new Vector3(1 + phi1, -1, -phi1).Normalize(); // F-L edge
        readonly static Vector3 triaco15 = new Vector3(+phi1, -phi1 - 1, -1).Normalize(); // L-C edge

        readonly static Vector3 triaco16 = new Vector3(+phi1, 1 + phi1, -1).Normalize(); // D-I edge
        readonly static Vector3 triaco17 = new Vector3(1 + phi1, 1, -phi1).Normalize(); // I-F edge
        readonly static Vector3 triaco18 = new Vector3(1, phi1, -phi1 - 1).Normalize(); // F-D edge

        readonly static Vector3 triaco19 = new Vector3(-1, +phi1, 1 + phi1).Normalize(); //A-H
        readonly static Vector3 triaco20 = new Vector3(-1 - phi1, 1, +phi1).Normalize(); //J-H
        readonly static Vector3 triaco21 = new Vector3(-phi1, 1 + phi1, 1).Normalize(); //A-J

        readonly static Vector3 triaco22 = new Vector3(-phi1, -phi1 - 1, +1).Normalize(); //B-K
        readonly static Vector3 triaco23 = new Vector3(-1 - phi1, -1, +phi1).Normalize(); //H-K
        readonly static Vector3 triaco24 = new Vector3(-1, -phi1, phi1 + 1).Normalize();  //B-H

        readonly static Vector3 triaco25 = new Vector3(-1, -phi1, -phi1 - 1).Normalize(); // C-G
        readonly static Vector3 triaco26 = new Vector3(-1 - phi1, -1, -phi1).Normalize(); // G-K
        readonly static Vector3 triaco27 = new Vector3(-phi1, -phi1 - 1, -1).Normalize(); // C-K

        readonly static Vector3 triaco28 = new Vector3(-phi1, 1 + phi1, -1).Normalize(); // D-J
        readonly static Vector3 triaco29 = new Vector3(-1 - phi1, 1, -phi1).Normalize(); // J-G
        readonly static Vector3 triaco30 = new Vector3(-1, phi1, -phi1 - 1).Normalize(); // D-G

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
        /// Gets the tetrahedron directions.
        /// </summary>
        /// <value>The tetrahedron directions.</value>
        public static Vector3[] TetrahedronDirections
        {
            get
            {
                if (_tetrahedronDirections == null)
                {
                    _tetrahedronDirections = new[]
                    {
                        octahe1,octahe2,octahe3,octahe4
                    };
                }
                return _tetrahedronDirections;
            }
        }
        private static Vector3[] _tetrahedronDirections;


        /// <summary>
        /// Gets the cube directions.
        /// </summary>
        /// <value>The cube directions.</value>
        public static Vector3[] CubeDirections
        {
            get
            {
                if (_cubeDirections == null)
                {
                    _cubeDirections = new[]
                    {
                        -Vector3.UnitX,
                        -Vector3.UnitY,
                        -Vector3.UnitZ,
                        Vector3.UnitX,
                        Vector3.UnitY,
                        Vector3.UnitZ
                    };
                }
                return _cubeDirections;
            }
        }
        private static Vector3[] _cubeDirections;


        /// <summary>
        /// Gets the octahedron directions.
        /// </summary>
        /// <value>The octahedron directions.</value>
        public static Vector3[] OctahedronDirections
        {
            get
            {
                if (_octahedronDirections == null)
                {
                    _octahedronDirections = new[]
                    {
                        octahe1,
                        octahe2,
                        octahe3,
                        octahe4,
                        octahe5,
                        octahe6,
                        octahe7,
                        octahe8
                    };
                }
                return _octahedronDirections;
            }
        }
        private static Vector3[] _octahedronDirections;

        /// <summary>
        /// Gets the dodechedron directions.
        /// </summary>
        /// <value>The dodechedron directions.</value>
        public static Vector3[] DodechedronDirections
        {
            get
            {
                if (_dodechedronDirections == null)
                {
                    _dodechedronDirections = new[] {
                        dodecA,
                        dodecB,
                        dodecC,
                        dodecD,
                        dodecE,
                        dodecF,
                        dodecG,
                        dodecH,
                        dodecI,
                        dodecJ,
                        dodecK,
                        dodecL
                    };
                }
                return _dodechedronDirections;
            }
        }
        private static Vector3[] _dodechedronDirections;

        /// <summary>
        /// Gets the icasohedron directions.
        /// </summary>
        /// <value>The icasohedron directions.</value>
        public static Vector3[] IcosahedronDirections
        {
            get
            {
                if (_icosahedronDirections == null)
                {
                    _icosahedronDirections = new[]
                    {
                        octahe1, octahe2, octahe3, octahe4,
                        octahe5, octahe6, octahe7, octahe8,
                        icosa9, icosa10, icosa11, icosa12,
                        icosa13, icosa14, icosa15, icosa16,
                        icosa17, icosa18, icosa19, icosa20
                    };
                }
                return _icosahedronDirections;
            }
        }
        private static Vector3[] _icosahedronDirections;

        /// <summary>
        /// Gets the triacontahedron directions.
        /// </summary>
        /// <value>The triacontahedron directions.</value>
        public static Vector3[] TriacontahedronDirections
        {
            get
            {
                if (_triacontahedronDirections == null)
                {
                    _triacontahedronDirections = new Vector3[]
                    {
                        -Vector3.UnitX, -Vector3.UnitY, -Vector3.UnitZ,
                        Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ,
                        triaco7, triaco8,  triaco9,
                        triaco10,  triaco11, triaco12,
                        triaco13, triaco14, triaco15,
                        triaco16, triaco17, triaco18,
                        triaco19,  triaco20,  triaco21,
                        triaco22, triaco23, triaco24,
                        triaco25, triaco26, triaco27,
                        triaco28,  triaco29,  triaco30
                    };
                }
                return _triacontahedronDirections;
            }
        }
        private static Vector3[] _triacontahedronDirections;
        public static Vector3[] AllDirections
        {
            get
            {
                if (_allDirections == null)
                {
                    _allDirections = new Vector3[]
                    {
                        -Vector3.UnitX, -Vector3.UnitY, -Vector3.UnitZ,
                        Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ,
                        octahe1, octahe2, octahe3, octahe4,
                        octahe5, octahe6, octahe7, octahe8,

                        dodecA, dodecB, dodecC, dodecD,
                        dodecE, dodecF, dodecG, dodecH,
                        dodecI, dodecJ, dodecK, dodecL,

                        icosa9, icosa10, icosa11, icosa12,
                        icosa13, icosa14, icosa15, icosa16,
                        icosa17, icosa18, icosa19, icosa20,

                        triaco7, triaco8,  triaco9,
                        triaco10,  triaco11, triaco12,
                        triaco13, triaco14, triaco15,
                        triaco16, triaco17, triaco18,
                        triaco19,  triaco20,  triaco21,
                        triaco22, triaco23, triaco24,
                        triaco25, triaco26, triaco27,
                        triaco28,  triaco29,  triaco30
                    };
                }
                return _allDirections;
            }
        }
        private static Vector3[] _allDirections;
    }
}
