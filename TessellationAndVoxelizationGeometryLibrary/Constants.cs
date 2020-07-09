// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 04-18-2016
// ***********************************************************************
// <copyright file="Constants.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    ///     Class Constants.
    /// </summary>
    public static class Constants
    {
        internal const int MaxNumberFacesDefaultFullTS = 50000;
        internal const double TwoPi = 2 * Math.PI;
        internal const double HalfPi = Math.PI / 2;
        internal const long SquareRootOfLongMaxValue = 3037000499; // 3 billion
        internal const long CubeRootOfLongMaxValue = 2097151; //2 million
        /// <summary>
        /// VertexCheckSumMultiplier is the checksum multiplier to be used for face and edge references.
        /// Since the edges connect two vertices the maximum value this can be is
        /// the square root of the max. value of a long (see above). However, during
        /// debugging, it is nice to see the digits of the vertex indices embedded in 
        /// check, so when debugging, this is reducing to 1 billion instead of 3 billion.
        /// This way if you are connecting vertex 1234 with 5678, you will get a checksum = 5678000001234
        /// </summary>
#if DEBUG
        public const long VertexCheckSumMultiplier = 1000000000;
#else
        public const long VertexCheckSumMultiplier = SquareRootOfLongMaxValue;
#endif


        /// <summary>The conversion from double to IntPoint as is used in the Clipper polygon functions. 
        /// See: https://github.com/DesignEngrLab/TVGL/wiki/Determining-the-Double-to-Long-Dimension-Multiplier
        /// for how this number is established.</summary>
        internal const int DoubleToIntPointMultipler = 365760000;
        internal const double IntPointToDoubleMultipler = 1.0 / 365760000.0;


        /// <summary>
        ///     The default color
        /// </summary>
        public const KnownColors DefaultColor = KnownColors.LightGray;

        /// <summary>
        ///     The error ratio used as a base for determining a good tolerance within a given tessellated solid.
        /// </summary>
        public const double BaseTolerance = 1E-9;

        /// <summary>
        ///     The tolerance used for simplifying polygons by joining to similary sloped lines.
        /// </summary>
        public const double SimplifyDefaultDeltaArea = 0.0003;

        /// <summary>
        ///     The angle tolerance used in the Oriented Bounding Box calculations
        /// </summary>
        public const double OBBTolerance = 1e-5;

        /// <summary>
        ///     The error for face in surface
        /// </summary>
        public const double ErrorForFaceInSurface = 0.002;

        /// <summary>
        ///     The tolerance for the same normal of a face when two are dot-producted.
        /// </summary>
        public const double SameFaceNormalDotTolerance = 1e-2;
        /// <summary>
        /// The maximum allowable edge similarity score. This is used when trying to match stray edges when loading in 
        /// a tessellated model.
        /// </summary>
        internal const double MaxAllowableEdgeSimilarityScore = 0.2;

        /// <summary>
        /// A high confidence percentage of 0.997 (3 sigma). This is used in some boolean "is" checks,
        /// like Polygon.IsRectangular
        /// </summary>
        public const double HighConfidence = 0.997;

        /// <summary>
        /// A medium confidence percentage of 0.95 (2 sigma). This is used in some boolean "is" checks,
        /// like Polygon.IsRectangular
        /// </summary>
        public const double MediumConfidence = 0.95;

        /// <summary>
        /// A low confidence percentage of 0.68 (1 sigma). This is used in some boolean "is" checks,
        /// like Polygon.IsRectangular
        /// </summary>
        public const double LowConfidence = 0.68;

        /// <summary>
        /// This is used to set the amount that polygon segments search outward to define the grid
        /// points that they affect.
        /// </summary>
        internal const int MarchingCubesBufferFactor = 5;
        internal const int MarchingCubesMissedFactor = 4;

        /// <summary>
        /// The tessellation to voxelization intersection combinations. This is used in the unction that
        /// produces voxels on the edges and faces of a tesselated shape.
        /// </summary>
        internal static readonly List<int[]> TessellationToVoxelizationIntersectionCombinations = new List<int[]>()
        {
            new []{ 0, 0, 0},
            new []{ -1, 0, 0},
            new []{ 0, -1, 0},
            new []{ 0, 0, -1},
            new []{ -1, -1, 0},
            new []{ -1, 0, -1},
            new []{ 0, -1, -1},
            new []{ -1, -1, -1},
        };


        /// <summary>
        ///     Finds the index.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The items.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>System.Int32.</returns>
        internal static int FindIndex<T>(this IEnumerable<T> items, Predicate<T> predicate)
        {
            var numItems = items.Count();
            if (numItems == 0) return -1;
            var index = 0;
            foreach (var item in items)
            {
                if (predicate(item)) return index;
                index++;
            }
            return -1;
        }

        /// <summary>
        ///     Finds the index.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The items.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>System.Int32.</returns>
        internal static int FindIndex<T>(this IEnumerable<T> items, T predicate)
        {
            var numItems = items.Count();
            if (numItems == 0) return -1;
            var index = 0;
            foreach (var item in items)
            {
                if (predicate.Equals(item)) return index;
                index++;
            }
            return -1;
        }

        internal static PolygonRelationship Converse(this PolygonRelationship relationship)
        {
            if (((byte)relationship & 0b1100) == 0)  // flags "4" and "8" indicate that A>B or B>A
                                                     // so if both are zero, then nothing to flip
                return relationship;
            var firstTwoBits = (byte)relationship & 0b11;
            if (((byte)relationship & 0b100) != 0) // the "2" flag means that boundaries touch.
                return (PolygonRelationship)(firstTwoBits + 8);
            return (PolygonRelationship)(firstTwoBits + 4);
        }

        #region new known colors

        /// <summary>
        /// The new known colors are taken from http://www.workwithcolor.com/
        /// </summary>
        public static Dictionary<ColorFamily, Dictionary<string, Color>> ColorDictionary
            = new Dictionary<ColorFamily, Dictionary<string, Color>>()
            {
                { ColorFamily.Red, new Dictionary<string, Color>()
                    {
                        {"Snow", new Color(255, 250, 250)},
                        {"Baby Pink", new Color(244, 194, 194)},
                        {"Pastel Red", new Color(255, 105, 97)},
                        {"Indian Red", new Color(255, 92, 92) },
                        { "Ferrari Red", new Color(255, 28, 0)},
                        { "Candy Apple Red", new Color(255, 8, 0)},
                        { "Red", new Color(255, 0, 0)},
                        { "Chestnut", new Color(205, 92, 92)},
                        { "Cinnabar", new Color(227, 66, 52)},
                        { "Jasper", new Color(215, 59, 62)},
                        { "Fire Engine Red", new Color(206, 22, 32)},
                        { "Boston University Red", new Color(204, 0, 0)},
                        { "Firebrick", new Color(178, 34, 34)},
                        { "Cornell Red", new Color(179, 27, 27)},
                        { "Dark Candy Apple Red", new Color(164, 0, 0)},
                        { "Maroon", new Color(128, 0, 0)},
                        { "Prune", new Color(112, 28, 28)},
                        { "Dark Sienna", new Color(60, 20, 20)},
                        { "Seal Brown", new Color(50, 20, 20) } } },
                { ColorFamily.Pink, new Dictionary<string, Color>()
                    {
                        { "Bubble Gum", new Color(255, 193, 204)},
                        {"Amaranth", new Color(229, 43, 80)},
                        {"Dark Terra Cotta", new Color(204, 78, 92)},
                        {"Bazaar", new Color(152, 119, 123)},
                        {"Alizarin", new Color(227, 38, 54)},
                        {"Crimson", new Color(220, 20, 60)},
                        {"Cadmium Red", new Color(227, 0, 34)},
                        {"Cardinal", new Color(196, 30, 58)},
                        {"Cordovan", new Color(137, 63, 69)},
                        {"Carmine", new Color(150, 0, 24)},
                        {"Rosewood", new Color(101, 0, 11)},
                        {"Pastel Pink", new Color(255, 209, 220)},
                        {"Pink", new Color(255, 192, 203)},
                        {"Cherry Blossom Pink", new Color(255, 183, 197)},
                        {"Flamingo Pink", new Color(252, 142, 172)},
                        {"Dark Pink", new Color(231, 84, 128)},
                        {"Blush", new Color(222, 93, 131)},
                        {"Cherry", new Color(222, 49, 99)},
                        {"Raspberry", new Color(227, 11, 93)},
                        {"Ruby", new Color(224, 17, 95)},
                        {"Bright Maroon", new Color(195, 33, 72)},
                        {"Burgundy", new Color(128, 0, 32)},
                        {"Dark Scarlet", new Color(86, 3, 25) }
                    } },
                { ColorFamily.Magenta, new Dictionary<string, Color>()
                    {
                        { "Lavender Blush", new Color(255, 240, 245)},
                        {"Classic Rose", new Color(251, 204, 231)},
                        { "Pastel Magenta", new Color(244, 154, 194)},
                        { "Sky Magenta", new Color(207, 113, 175)},
                        { "Bright Pink", new Color(255, 0, 127)},
                        { "Dark Raspberry", new Color(135, 38, 87)},
                        { "Eggplant", new Color(97, 64, 81)},
                        { "Bright Ube", new Color(209, 159, 232)},
                        { "Magenta", new Color(255, 0, 255)},
                        { "Pastel Violet", new Color(203, 153, 201)},
                        { "Pastel Purple", new Color(179, 158, 181)},
                        { "Byzantine", new Color(189, 51, 164)},
                        { "Dark Violet", new Color(148, 0, 211)},
                        { "Plum", new Color(142, 69, 133)},
                        { "Dark Magenta", new Color(139, 0, 139)},
                        { "Byzantium", new Color(112, 41, 99)},
                        { "Dark Byzantium", new Color(93, 57, 84)},
                        { "Bright Lavender", new Color(191, 148, 228)},
                        { "Lavender", new Color(181, 126, 220)},
                        { "Dark Pastel Purple", new Color(150, 111, 214)},
                        { "Violet", new Color(143, 0, 255)},
                        { "Dark Lavender", new Color(115, 79, 150)},
                        { "Purple Heart", new Color(105, 53, 156) }
                    } },
                { ColorFamily.Blue, new Dictionary<string, Color>()
                    {
                        { "Ghost White", new Color(248, 248, 255)},
                        { "Lavender Blue", new Color(204, 204, 255)},
                        { "Lavender Gray", new Color(196, 195, 208)},
                        { "Ceil", new Color(146, 161, 207)},
                        { "Cool Grey", new Color(140, 146, 172)},
                        { "Blue", new Color(0, 0, 255)},
                        { "Cerulean Blue", new Color(42, 82, 190)},
                        { "International Klein Blue", new Color(0, 47, 167)},
                        { "Dark Powder Blue", new Color(0, 51, 153)},
                        { "Duke Blue", new Color(0, 0, 156)},
                        { "Ultramarine", new Color(18, 10, 143)},
                        { "Dark Blue", new Color(0, 0, 139)},
                        { "Navy Blue", new Color(0, 0, 128)},
                        { "Midnight Blue", new Color(25, 25, 112)},
                        { "Sapphire", new Color(8, 37, 103)},
                        { "Royal Blue", new Color(0, 35, 102) }
                    } },
                { ColorFamily.CyanBlue, new Dictionary<string, Color>()
                    {
                        {"Ghost White", new Color(248, 248, 255)},
                        { "Lavender Blue", new Color(204, 204, 255)},
                        { "Lavender Gray", new Color(196, 195, 208)},
                        { "Ceil", new Color(146, 161, 207)},
                        { "Cool Grey", new Color(140, 146, 172)},
                        { "Blue", new Color(0, 0, 255)},
                        { "Cerulean Blue", new Color(42, 82, 190)},
                        { "International Klein Blue", new Color(0, 47, 167)},
                        { "Dark Powder Blue", new Color(0, 51, 153)},
                        { "Duke Blue", new Color(0, 0, 156)},
                        { "Ultramarine", new Color(18, 10, 143)},
                        { "Dark Blue", new Color(0, 0, 139)},
                        { "Navy Blue", new Color(0, 0, 128)},
                        { "Midnight Blue", new Color(25, 25, 112)},
                        { "Sapphire", new Color(8, 37, 103)},
                        { "Royal Blue", new Color(0, 35, 102) }
                    } },
                { ColorFamily.Cyan, new Dictionary<string, Color>()
                    {
                        { "Bubbles", new Color(231, 254, 255)},
                        { "Cyan", new Color(0, 255, 255)},
                        { "Columbia Blue", new Color(155, 221, 255)},
                        { "Bright Turquoise", new Color(8, 232, 222)},
                        { "Baby Blue", new Color(137, 207, 240)},
                        { "Sky Blue", new Color(135, 206, 235)},
                        { "Pastel Blue", new Color(174, 198, 207)},
                        { "Turquoise", new Color(48, 213, 200)},
                        { "Dark Cyan", new Color(0, 139, 139)},
                        { "Cerulean", new Color(0, 123, 167)},
                        { "Teal", new Color(0, 128, 128)},
                        { "Pine Green", new Color(1, 121, 111)},
                        { "Dark Slate Gray", new Color(47, 79, 79) }
                    } },
                { ColorFamily.GreenCyan, new Dictionary<string, Color>()
                    {
                        {"Aquamarine", new Color(127, 255, 212)},
                        {"Clover", new Color(0, 255, 111)},
                        {"Ash Grey", new Color(178, 190, 181)},
                        {"Cambridge Blue", new Color(163, 193, 173)},
                        {"Caribbean Green", new Color(0, 204, 153)},
                        {"Emerald", new Color(80, 200, 120)},
                        {"Mint", new Color(62, 180, 137)},
                        {"Dark Pastel Green", new Color(3, 192, 60)},
                        {"Jade", new Color(0, 168, 107)},
                        {"Xanadu", new Color(115, 134, 120)},
                        {"Tropical Rain Forest", new Color(0, 117, 94)},
                        {"Dark Spring Green", new Color(23, 114, 69)},
                        {"Cadmium Green", new Color(0, 107, 60)},
                        {"Forest Green", new Color(1, 68, 33)},
                        {"British Racing Green", new Color(0, 66, 37)},
                        {"Dark Green", new Color(1, 50, 32)},
                        {"Dark Jungle Green", new Color(26, 36, 33) }
                    } },
                { ColorFamily.Green, new Dictionary<string, Color>()
                    {
                        { "Inchworm", new Color(178, 236, 93)},
                        {"Lawn Green", new Color(124, 252, 0)},
                        {"Bright Green", new Color(102, 255, 0)},
                        {"Celadon", new Color(172, 225, 175)},
                        {"Pastel Green", new Color(119, 221, 119)},
                        {"Pistachio", new Color(147, 197, 114)},
                        {"Dollar Bill", new Color(133, 187, 101)},
                        {"Asparagus", new Color(135, 169, 107)},
                        {"Dark Pastel Green", new Color(3, 192, 60)},
                        {"Camouflage Green", new Color(120, 134, 107)},
                        {"India Green", new Color(19, 136, 8)},
                        {"Green", new Color(0, 128, 0)},
                        {"Dark Olive Green", new Color(85, 107, 47)},
                        {"Rifle Green", new Color(65, 72, 51)},
                        {"Chartreuse", new Color(223, 255, 0)},
                        {"Lime", new Color(191, 255, 0)},
                        {"Spring Bud", new Color(167, 252, 0)},
                        {"Pear", new Color(209, 226, 49)},
                        {"Android Green", new Color(164, 198, 57)},
                        {"Apple Green", new Color(141, 182, 0)},
                        {"Battleship Grey", new Color(132, 132, 130)},
                        {"Olive", new Color(128, 128, 0)},
                        {"Army Green", new Color(75, 83, 32) }
                    } },
                { ColorFamily.Yellow, new Dictionary<string, Color>()
                    {
                        { "Ivory", new Color(255, 255, 240)},
                        {"Cream", new Color(255, 253, 208)},
                        {"Pastel Yellow", new Color(253, 253, 150)},
                        {"Beige", new Color(245, 245, 220)},
                        {"Daffodil", new Color(255, 255, 49)},
                        {"Yellow", new Color(255, 255, 0)},
                        {"Icterine", new Color(252, 247, 94)},
                        {"Lemon", new Color(255, 247, 0)},
                        {"Canary Yellow", new Color(255, 239, 0)},
                        {"Flavescent", new Color(247, 233, 142)},
                        {"Corn", new Color(251, 236, 93)},
                        {"Golden Yellow", new Color(255, 223, 0)},
                        {"Titanium Yellow", new Color(238, 230, 0)},
                        {"Dandelion", new Color(240, 225, 48)},
                        {"Peridot", new Color(230, 226, 0)},
                        {"Straw", new Color(228, 217, 111)},
                        {"Sandstorm", new Color(236, 213, 64)},
                        {"Pastel Gray", new Color(207, 207, 196)},
                        {"Citrine", new Color(228, 208, 10)},
                        {"Dark Khaki", new Color(189, 183, 107)},
                        {"Brass", new Color(181, 166, 66) }
                    } },
                { ColorFamily.OrangeYellow, new Dictionary<string, Color>()
                    {
                        { "Cornsilk", new Color(255, 248, 220)},
                        { "Blond", new Color(250, 240, 190)},
                        { "Pearl", new Color(240, 234, 214)},
                        { "Platinum", new Color(229, 228, 226)},
                        { "Vanilla", new Color(243, 229, 171)},
                        { "Mustard", new Color(255, 219, 88)},
                        { "Buff", new Color(240, 220, 130)},
                        { "Banana Yellow", new Color(255, 209, 42)},
                        { "Arylide Yellow", new Color(233, 214, 107)},
                        { "Saffron", new Color(244, 196, 48)},
                        { "Amber", new Color(255, 191, 0)},
                        { "Sand", new Color(194, 178, 128)},
                        { "Goldenrod", new Color(218, 165, 32)},
                        { "Dark Goldenrod", new Color(184, 134, 11)},
                        { "Dark Tan", new Color(145, 129, 81)},
                        { "Sand Dune", new Color(150, 113, 23) }
                    } },
                { ColorFamily.OrangeBrown, new Dictionary<string, Color>()
                    {
                        { "Seashell", new Color(255, 245, 238)},
                        { "Champagne", new Color(247, 231, 206)},
                        { "Peach", new Color(255, 229, 180)},
                        { "Wheat", new Color(245, 222, 179)},
                        { "Sunset", new Color(250, 214, 165)},
                        { "Apricot", new Color(251, 206, 177)},
                        { "Desert Sand", new Color(237, 201, 175)},
                        { "Pastel Orange", new Color(255, 179, 71)},
                        { "Burlywood", new Color(222, 184, 135)},
                        { "Dark Tangerine", new Color(255, 168, 18)},
                        { "Chrome Yellow", new Color(255, 167, 0)},
                        { "Fawn", new Color(229, 170, 112)},
                        { "Earth Yellow", new Color(225, 169, 95)},
                        { "Indian Yellow", new Color(227, 168, 87)},
                        { "Khaki", new Color(195, 176, 145)},
                        { "Dark Orange", new Color(255, 140, 0)},
                        { "Gamboge", new Color(228, 155, 15)},
                        { "Carrot Orange", new Color(237, 145, 33)},
                        { "Orange", new Color(255, 127, 0)},
                        { "Tangerine", new Color(242, 133, 0)},
                        { "Cadmium Orange", new Color(237, 135, 45)},
                        { "Pumpkin", new Color(255, 117, 24)},
                        { "Desert", new Color(193, 154, 107)},
                        { "Safety Orange", new Color(255, 103, 0)},
                        { "Bronze", new Color(205, 127, 50)},
                        { "Ochre", new Color(204, 119, 34)},
                        { "Cinnamon", new Color(210, 105, 30)},
                        { "Copper", new Color(184, 115, 51)},
                        { "Chamoisee", new Color(160, 120, 90)},
                        { "Burnt Orange", new Color(204, 85, 0)},
                        { "Shadow", new Color(138, 121, 93)},
                        { "Golden Brown", new Color(153, 101, 21)},
                        { "Pastel Brown", new Color(131, 105, 83)},
                        { "Raw Umber", new Color(130, 102, 68)},
                        { "Brown", new Color(150, 75, 0)},
                        { "Chocolate", new Color(123, 63, 0)},
                        { "Sepia", new Color(112, 66, 20)},
                        { "Dark Brown", new Color(101, 67, 33)},
                        { "Dark Lava", new Color(72, 60, 50)},
                        { "Bistre", new Color(61, 43, 31) }
                    } },
                { ColorFamily.RedOrange, new Dictionary<string, Color>()
                    {
                        {"Atomic Tangerine", new Color(255, 153, 102)},
                        { "Salmon", new Color(255, 140, 105)},
                        { "Dark Salmon", new Color(233, 150, 122)},
                        { "Coral", new Color(255, 127, 80)},
                        { "Burnt Sienna", new Color(233, 116, 81)},
                        { "Portland Orange", new Color(255, 90, 54)},
                        { "Terra Cotta", new Color(226, 114, 91)},
                        { "Coquelicot", new Color(255, 56, 0)},
                        { "Flame", new Color(226, 88, 34)},
                        { "Cinereous", new Color(152, 129, 123)},
                        { "Dark Coral", new Color(205, 91, 69)},
                        { "Dark Chestnut", new Color(152, 105, 96)},
                        { "Sinopia", new Color(203, 65, 11)},
                        { "Dark Pastel Red", new Color(194, 59, 34)},
                        { "Mahogany", new Color(192, 64, 0)},
                        { "Rust", new Color(183, 65, 14)},
                        { "Bole", new Color(121, 68, 59)},
                        { "Burnt Umber", new Color(138, 51, 36)},
                        { "Auburn", new Color(109, 53, 26) }
                    } }
            };

        internal const double DegreesToRadiansFactor = Math.PI / 180.0;
        internal const double DefaultRoundOffsetDeltaAngle = Math.PI / 180.0; // which is also one degree or 360 in a circle
        #endregion
    }


    /// <summary>
    /// Units of a specified coordinates within the shape or set of shapes.
    /// </summary>
    public enum UnitType
    {
        /// <summary>
        /// the unspecified state
        /// </summary>
        unspecified = 0,
        /// <summary>
        ///     The millimeter
        /// </summary>
        millimeter = 11,

        /// <summary>
        ///     The micron
        /// </summary>
        micron = 8,


        /// <summary>
        ///     The centimeter
        /// </summary>
        centimeter = 1,

        /// <summary>
        ///     The inch
        /// </summary>
        inch = 4,

        /// <summary>
        ///     The foot
        /// </summary>
        foot = 3,

        /// <summary>
        ///     The meter
        /// </summary>
        meter = 6
    }


    /// <summary>
    ///     Enum CurvatureType
    /// </summary>
    public enum CurvatureType
    {
        /// <summary>
        ///     The concave
        /// </summary>
        Concave = -1,

        /// <summary>
        ///     The saddle or flat
        /// </summary>
        SaddleOrFlat = 0,

        /// <summary>
        ///     The convex
        /// </summary>
        Convex = 1,

        /// <summary>
        ///     The undefined
        /// </summary>
        Undefined
    }

    /// <summary>
    ///     Enum FileType
    /// </summary>
    public enum FileType
    {
        /// <summary>
        /// represents an unspecified state
        /// </summary>
        unspecified,
        /// <summary>
        ///     Stereolithography (STL) American Standard Code for Information Interchange (ASCII)
        /// </summary>
        // ReSharper disable once InconsistentNaming
        STL_ASCII,

        /// <summary>
        ///     Stereolithography (STL) Binary
        /// </summary>
        // ReSharper disable once InconsistentNaming
        STL_Binary,

        /// <summary>
        ///     Mobile MultiModal Framework
        /// </summary>
        ThreeMF,

        /// <summary>
        ///     Mobile MultiModal Framework
        /// </summary>
        Model3MF,

        /// <summary>
        ///     Additive Manufacturing File Format
        /// </summary>
        AMF,

        /// <summary>
        ///     Object File Format
        /// </summary>
        OFF,

        /// <summary>
        ///     Polygon File Format as ASCII
        /// </summary>
        PLY_ASCII,
        /// <summary>
        ///     Polygon File Format as Binary
        /// </summary>
        PLY_Binary,
        /// <summary>
        ///     Shell file...I think this was created as part of collaboration with an Oregon-based EDA company
        /// </summary>
        SHELL,
        /// <summary>
        ///     A serialized version of the TessellatedSolid object
        /// </summary>
        TVGL
    }

    internal enum FormatEndiannessType
    {
        ascii,
        binary_little_endian,
        binary_big_endian
    }
    /// <summary>
    ///     Enum ShapeElement
    /// </summary>
    internal enum ShapeElement
    {
        /// <summary>
        ///     The vertex
        /// </summary>
        Vertex,
        Edge,
        Face,
        Uniform_Color
    }

    /// <summary>
    ///     Enum ColorElements
    /// </summary>
    internal enum ColorElements
    {
        Red,
        Green,
        Blue,
        Opacity
    }

    /// <summary>
    /// CartesianDirections: just the six cardinal directions for the voxelized box around the solid
    /// </summary>
    public enum CartesianDirections
    {
        /// <summary>
        /// <summary>
        /// Enum VoxelDirections
        /// </summary>
        /// Negative X Direction
        /// </summary>
        /// <summary>
        /// The x negative
        /// </summary>
        XNegative = -1,

        /// <summary>
        /// Negative Y Direction
        /// <summary>
        /// The x negative
        /// </summary>
        /// </summary>
        /// <summary>
        /// The y negative
        /// </summary>
        YNegative = -2,

        /// <summary>
        /// Negative Z Direction
        /// <summary>
        /// The y negative
        /// </summary>
        /// </summary>
        /// <summary>
        /// The z negative
        /// </summary>
        ZNegative = -3,

        /// <summary>
        /// Positive X Direction
        /// <summary>
        /// The z negative
        /// </summary>
        /// </summary>
        /// <summary>
        /// The x positive
        /// </summary>
        XPositive = 1,

        /// <summary>
        /// Positive Y Direction
        /// <summary>
        /// The x positive
        /// </summary>
        /// </summary>
        /// <summary>
        /// The y positive
        /// </summary>
        YPositive = 2,

        /// <summary>
        /// Positive Z Direction
        /// <summary>
        /// The y positive
        /// </summary>
        /// </summary>
        /// <summary>
        /// The z positive
        /// </summary>
        ZPositive = 3
    }

    /// <summary>
    /// Enum PolygonRelationship
    /// </summary>
    [Flags]
    public enum PolygonRelationship : byte
    {
        // byte 0(1): 1 if intersecting
        // byte 1(2): 1 if borders touch but not intersecting
        // byte 2(4): 1 if inside a hole of the other (not touching or intersecting)
        // byte 3(8): 1 if A is inside B
        // byte 4(16): 1 if B is inside A
        Separated = 0, //xb0000 0000
        Intersect = 1, //xb0000 0001
        SeparatedButBordersTouch = 2, //xb0000 0010

        AIsCompletelyInsideB = 8, //xb0000 1000
        //AVerticesInsideBButLinesIntersect = 9, //xb0000 1001
        AInsideBButBordersTouch = 10, //xb000 1010
        AIsInsideHoleOfB = 12,  //xb0000 1100

        BIsCompletelyInsideA = 16, //xb0001 0000
        //BVerticesInsideAButLinesIntersect = 17, //xb0001 0001
        BInsideAButBordersTouch = 18,  //xb0001 0010
        BIsInsideHoleOfA = 20,  //xb0001 0100


    }

    /// <summary>
    /// Enum PolygonRelationship
    /// </summary>
    [Flags]
    public enum PolygonSegmentRelationship
    {
        Unknown = 0,
        AtStartOfA = 1, // byte 0(1): the intersection is at the from point for line A (T joint)
        AtStartOfB = 2, // byte 1(2) the intersection is at the from  point for line B (T joint)
        // therefore the value is zero when at an intermediate point 
        // for both line segments (this is like 99% of the time). 
        LinesSharePoint = AtStartOfA | AtStartOfB, // 0b11: at the from points for both lineA and lineB 

        AEncompassesB = 4, //if polygonA encompasses polygonB at this intersection
        BEncompassesA = 8, // if polygonB encompasses polygonA at this intersection
        Overlapping = AEncompassesB | BEncompassesA, // normally there is some encompasses of the other for both
        CoincidentLines = 16, // the lines before and/or after the point are on top of each other - this may make it 
        // is impossible to tell if one encompasses the other. 
        // some details in the combinations:
        // 0b100,yy: the interaction is unknown 
        //           For this to be the case, bytes 0 & 1 can be 11, 10, or 01 but not 00; and bytes 3 & 4
        //           should both be 0
        // 0b000,yy: "glance": it is known that the insides of A & B do not overlap at this intersection.
        //                    Instead, they glance off of one another
        //           For this to be the case, bytes 0 & 1 can be 11, 10, or 01 but not 00
        //         Technically, you can have CoincidentLines and still have it glance
        // 0bx01,yy: "AEncompassB": it is known that the insides of A fully encompass B at this intersection.
        //           For this to be the case, bytes 0 & 1 can be 11, 10, or 01 but not 00
        // 0bx10,yy: "BEncompassA": it is known that the insides of B fully encompass A at this intersection.
        //           For this to be the case, bytes 0 & 1 can be 11, 10, or 01 but not 00
        // 0bx11,yy: "Overlap" (proper intersection): it is known that A encloses part of B and B encloses are of A
        //           For this case, bytes 0 & 1 can have all four values

        // these final three are rare. They indicate more detail is CoincidentLines is true. Otherwise they 
        // should be left as zero (and ignored)
        // the lines merge into the same line after the point (for the A direction).
        SameLineAfterPoint = 32,
        // the lines are the same before the point (for the A direction)
        SameLineBeforePoint = 64,
        // if the lines are moving in opposite directions, set the last bit to true
        OppositeDirections = 128,
    }

    /// <summary>
    ///     A comparer for optimization that can be used for either
    ///     minimization or maximization.
    /// </summary>
    internal class NoEqualSort : IComparer<double>
    {
        readonly int direction;
        internal NoEqualSort(bool minimize = true)
        {
            direction = minimize ? -1 : 1;
        }
        /// <summary>
        ///     Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>
        ///     A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as
        ///     shown in the following table.Value Meaning Less than zero<paramref name="x" /> is less than <paramref name="y" />
        ///     .Zero<paramref name="x" /> equals <paramref name="y" />.Greater than zero<paramref name="x" /> is greater than
        ///     <paramref name="y" />.
        /// </returns>
        public int Compare(double x, double y)
        {
            if (x < y) return direction;
            return -direction;
        }
    }
}