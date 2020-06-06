using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {

        #region Clipper Offset

        /// <summary>
        /// Offets the given path by the given offset, rounding corners.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="offset"></param>
        /// <param name="minLength"></param>
        /// <returns></returns>
        public static List<List<Vector2>> OffsetRound(this IEnumerable<Vector2> path, double offset,
            double minLength = 0.0)
        {
            return Offset(new List<IEnumerable<Vector2>> {path}, offset, JoinType.jtRound, minLength);
        }


        /// <summary>
        /// Offsets all paths by the given offset value. Rounds the corners.
        /// Offest value may be positive or negative.
        /// Loops must be ordered CCW positive.
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="offset"></param>
        /// <param name="minLength"></param>
        /// <returns></returns>
        public static List<List<Vector2>> OffsetRound(this IEnumerable<IEnumerable<Vector2>> paths, double offset,
            double minLength = 0.0)
        {
            return Offset(paths, offset, JoinType.jtRound, minLength);
        }

        /// <summary>
        /// Offsets all paths by the given offset value. Squares the corners.
        /// Offest value may be positive or negative. 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="offset"></param>
        /// <param name="minLength"></param>
        /// <returns></returns>
        public static List<List<Vector2>> OffsetMiter(this IEnumerable<Vector2> path, double offset,
            double minLength = 0.0)
        {
            return Offset(new List<IEnumerable<Vector2>> {path}, offset, JoinType.jtMiter, minLength);
        }

        /// <summary>
        /// Offsets all paths by the given offset value. Miters the corners.
        /// Offest value may be positive or negative.
        /// Loops must be ordered CCW positive.
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="minLength"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static List<List<Vector2>> OffsetMiter(this IEnumerable<IEnumerable<Vector2>> paths, double offset,
            double minLength = 0.0)
        {
            return Offset(paths, offset, JoinType.jtMiter, minLength);
        }

        /// <summary>
        /// Offsets all paths by the given offset value. Squares the corners.
        /// Offest value may be positive or negative. 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="offset"></param>
        /// <param name="minLength"></param>
        /// <returns></returns>
        public static List<List<Vector2>> OffsetSquare(this IEnumerable<Vector2> path, double offset,
            double minLength = 0.0)
        {
            return Offset(new List<IEnumerable<Vector2>> {path}, offset, JoinType.jtSquare, minLength);
        }

        /// <summary>
        /// Offsets all paths by the given offset value. Squares the corners.
        /// Offest value may be positive or negative.
        /// Loops must be ordered CCW positive.
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="minLength"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static List<List<Vector2>> OffsetSquare(this IEnumerable<IEnumerable<Vector2>> paths, double offset,
            double minLength = 0.0)
        {
            return Offset(paths, offset, JoinType.jtSquare, minLength);
        }

        private static List<List<Vector2>> Offset(IEnumerable<IEnumerable<Vector2>> paths, double offset,
            JoinType joinType,
            double minLength = 0.0)
        {
            //Convert Points (TVGL) to IntPoints (Clipper)
            var clipperSubject =
                paths.Select(loop => loop.Select(point =>
                        new IntPoint(point.X * Constants.DoubleToIntPointMultipler,
                            point.Y * Constants.DoubleToIntPointMultipler))
                    .ToList()).ToList();
            if (minLength.IsNegligible())
            {
                var totalLength = clipperSubject.Sum(Perimeter);
                minLength = totalLength * 0.001;
            }

            //Setup Clipper
            var clip = new ClipperOffset(2, minLength * Constants.DoubleToIntPointMultipler);
            clip.AddPaths(clipperSubject, joinType, EndType.etClosedPolygon);

            //Begin an evaluation
            var clipperSolution = new List<List<IntPoint>>();
            clip.Execute(clipperSolution, offset * Constants.DoubleToIntPointMultipler);

            //Convert back to points and return solution
            return clipperSolution.Select(clipperPath => clipperPath.Select(point
                => new Vector2(point.X * Constants.IntPointToDoubleMultipler,
                    point.Y * Constants.IntPointToDoubleMultipler)).ToList()).ToList();
        }
        #endregion

        #region next TVGL Offsetting
    }
}