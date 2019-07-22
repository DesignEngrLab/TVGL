// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 05-28-2016
// ***********************************************************************
// <copyright file="TVGLFileData.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using StarMathLib;
using TVGL.Voxelization;

namespace TVGL.IOFunctions
{
    /// <summary>
    /// Class TVGLFileData.
    /// </summary>
    /// <seealso cref="TVGL.IOFunctions.IO" />
    [XmlType("TVGLSolid")]
#if help
    internal class TVGLFileData : IO
#else
    public class TVGLFileData : IO
#endif
    {

        #region Fields and Properties
        #region that match with Solid
        /// <summary>
        ///     Gets the center.
        /// </summary>
        /// <value>The center.</value>
        public double[] Center;

        public double[] ConvexHullCenter;
        /// <summary>
        ///     Gets the z maximum.
        /// </summary>
        /// <value>The z maximum.</value>
        public double ZMax;

        /// <summary>
        ///     Gets the y maximum.
        /// </summary>
        /// <value>The y maximum.</value>
        public double YMax;

        /// <summary>
        ///     Gets the x maximum.
        /// </summary>
        /// <value>The x maximum.</value>
        public double XMax;

        /// <summary>
        ///     Gets the z minimum.
        /// </summary>
        /// <value>The z minimum.</value>
        public double ZMin;

        /// <summary>
        ///     Gets the y minimum.
        /// </summary>
        /// <value>The y minimum.</value>
        public double YMin;

        /// <summary>
        ///     Gets the x minimum.
        /// </summary>
        /// <value>The x minimum.</value>
        public double XMin;


        /// <summary>
        ///     Gets the volume.
        /// </summary>
        /// <value>The volume.</value>
        public double Volume;
        /// <summary>
        /// The convex hull volume
        /// </summary>
        public double ConvexHullVolume;

        /// <summary>
        /// Gets and sets the mass.
        /// </summary>
        /// <value>The mass.</value>
        public double Mass;

        /// <summary>
        ///     Gets the surface area.
        /// </summary>
        /// <value>The surface area.</value>
        public double SurfaceArea;
        /// <summary>
        /// The convex hull area
        /// </summary>
        public double ConvexHullArea;
        /// <summary>
        ///     Gets the convex hull.
        /// </summary>
        /// <value>The convex hull.</value>
        public string ConvexHullVertices;

        /// <summary>
        /// The convex hull faces
        /// </summary>
        public string ConvexHullFaces;
        /// <summary>
        ///     The has uniform color
        /// </summary>
        public bool HasUniformColor;


        /// <summary>
        /// The inertia tensor
        /// </summary>
        public string InertiaTensor;

        /// <summary>
        /// The colors
        /// </summary>
        public string Colors;

        /// <summary>
        ///     Gets or sets the primitive objects that make up the solid
        /// </summary>
        public List<PrimitiveSurface> Primitives;
        #endregion
        #region that match with TessellatedSolid
        /// <summary>
        ///     Gets the faces.
        /// </summary>
        /// <value>The faces.</value>
        public string Faces;

        /// <summary>
        ///     Gets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        public string Vertices;
        /// <summary>
        ///     The tolerance is set during the initiation (constructor phase). This is based on the maximum
        ///     length of the axis-aligned bounding box times Constants.
        /// </summary>
        /// <value>The same tolerance.</value>
        public double SameTolerance;
        #endregion
        #region that match with VoxelizedSolid

        public string[] Voxels { get; set; }
        public int[] VoxelsPerSide { get; set; }
        #endregion


        #endregion


        #region Open Solids

        /// <summary>
        /// Opens the f00.tvgl.xml file as a list of tessellated solids.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="filename">The filename.</param>
        /// <returns>List&lt;TessellatedSolid&gt;.</returns>
        internal static List<Solid> OpenSolids(Stream s, string filename)
        {
            var now = DateTime.Now;
            var solids = new List<Solid>();
            try
            {
                var tvglDeserializer = new XmlSerializer(typeof(TVGLFileData));
                if (tvglDeserializer.CanDeserialize(XmlReader.Create(s)))
                {
                    s.Position = 0;
                    var streamReader = new StreamReader(s);
                    var fileData = (TVGLFileData)tvglDeserializer.Deserialize(streamReader);
                    if (!fileData.Voxels.Any())
                        solids.Add(new TessellatedSolid(fileData, filename));
                    else solids.Add(new VoxelizedSolid(fileData, filename));
                }
                else
                {
                    s.Position = 0;
                    var streamReader = new StreamReader(s);
                    tvglDeserializer = new XmlSerializer(typeof(List<TVGLFileData>));
                    var fileDataList = (List<TVGLFileData>)tvglDeserializer.Deserialize(streamReader);
                    foreach (var fileData in fileDataList)
                        if (!fileData.Voxels.Any())
                            solids.Add(new TessellatedSolid(fileData, filename));
                        else solids.Add(new VoxelizedSolid(fileData, filename));
                }
                Message.output("Successfully read in TVGL file (" + (DateTime.Now - now) + ").", 3);
            }
            catch (Exception exception)
            {
                Message.output("Unable to read in TVGL file (" + (DateTime.Now - now) + ").", 1);
                Message.output("Exception: " + exception.Message, 3);
                return null;
            }
            return solids;
        }

        #endregion
        #region Save Solid



        /// <summary>
        /// Saves the solids.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solids">The solids.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool SaveSolids(Stream stream, IList<Solid> solids)
        {
            try
            {
                var fileDataList = solids.Select(MakeFileData).ToList();
                using (var writer = XmlWriter.Create(stream))
                {
                    writer.WriteComment(tvglDateMarkText);
                    if (!string.IsNullOrWhiteSpace(solids[0].FileName))
                        writer.WriteComment("Originally loaded from " + solids[0].FileName);
                    //writer.WriteStartElement("ListOfTessellatedSolids");
                    var serializer = new XmlSerializer(typeof(List<TVGLFileData>));
                    serializer.Serialize(writer, fileDataList);
                    //foreach (var solid in solids)
                    //    serializer.Serialize(writer, MakeFileData(solid));
                    //writer.WriteEndElement();
                }
                Message.output("Successfully wrote TVGL file to stream.", 3);
                return true;
            }
            catch (Exception exception)
            {
                Message.output("Unable to write in model file.", 1);
                Message.output("Exception: " + exception.Message, 3);
                return false;
            }
        }


        /// <summary>
        /// Saves the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="solid">The solid.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="NotImplementedException"></exception>
        internal static bool SaveSolid(Stream stream, Solid solid)
        {
            try
            {
                using (var writer = XmlWriter.Create(stream))
                {
                    writer.WriteComment(tvglDateMarkText);
                    if (!string.IsNullOrWhiteSpace(solid.FileName))
                        writer.WriteComment("Originally loaded from " + solid.FileName);
                    var serializer = new XmlSerializer(typeof(TVGLFileData));
                    serializer.Serialize(writer, MakeFileData(solid));
                }
                Message.output("Successfully wrote TVGL file to stream.", 3);
                return true;
            }
            catch (Exception exception)
            {
                Message.output("Unable to write in model file.", 1);
                Message.output("Exception: " + exception.Message, 3);
                return false;
            }
        }

        private static TVGLFileData MakeFileData(Solid s)
        {
            if (s is TessellatedSolid) return MakeFileData((TessellatedSolid)s);
            if (s is VoxelizedSolid) return MakeFileData((VoxelizedSolid)s);
            return null;
        }

        private static TVGLFileData MakeFileData(TessellatedSolid ts)
        {
            var result = new TVGLFileData
            {
                Center = ts.Center,
                ConvexHullCenter = ts.ConvexHull.Center,
                ConvexHullArea = ts.ConvexHull.SurfaceArea,
                ConvexHullVolume = ts.ConvexHull.Volume,
                HasUniformColor = ts.HasUniformColor,
                Language = ts.Language,
                Mass = ts.Mass,
                Name = ts.Name,
                Primitives = ts.Primitives,
                SameTolerance = ts.SameTolerance,
                SurfaceArea = ts.SurfaceArea,
                Units = ts.Units,
                Volume = ts.Volume,
                XMax = ts.XMax,
                XMin = ts.XMin,
                YMax = ts.YMax,
                YMin = ts.YMin,
                ZMax = ts.ZMax,
                ZMin = ts.ZMin,
                ConvexHullVertices = string.Join(",", ts.ConvexHull.Vertices.Select(v => v.IndexInList)),
                ConvexHullFaces = string.Join(",",
                ts.ConvexHull.Faces.SelectMany(face => face.Vertices.Select(v => v.IndexInList))),
                Faces = string.Join(",", ts.Faces.SelectMany(face => face.Vertices.Select(v => v.IndexInList))),
                Vertices = string.Join(",", ts.Vertices.SelectMany(v => v.Position))
            };
            result.Colors = result.HasUniformColor ? ts.SolidColor.ToString() : string.Join(",", ts.Faces.Select(f => f.Color));
            result.Comments.AddRange(ts.Comments);
            if (ts._inertiaTensor != null)
            {
                var tensorAsArray = new double[9];
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                        tensorAsArray[3 * i + j] = ts._inertiaTensor[i, j];
                result.InertiaTensor = string.Join(",", tensorAsArray);
            }
            return result;
        }

        private static TVGLFileData MakeFileData(VoxelizedSolid vs)
        {
            double[] ConvexHullCenter;
            double ConvexHullArea;
            double ConvexHullVolume;
            if (vs.ConvexHull is null)
            {
                ConvexHullCenter = null;
                ConvexHullArea = 0;
                ConvexHullVolume = 0;
            }
            else
            {
                ConvexHullCenter = vs.ConvexHull.Center;
                ConvexHullArea = vs.ConvexHull.SurfaceArea;
                ConvexHullVolume = vs.ConvexHull.Volume;
            }

            var result = new TVGLFileData
            {
                Center = vs.Center,
                ConvexHullCenter = ConvexHullCenter,
                ConvexHullArea = ConvexHullArea,
                ConvexHullVolume = ConvexHullVolume,
                HasUniformColor = true,
                Language = vs.Language,
                Mass = vs.Mass,
                Name = vs.Name,
                Primitives = vs.Primitives,
                SurfaceArea = vs.SurfaceArea,
                Units = vs.Units,
                Volume = vs.Volume,
                XMax = vs.XMax,
                XMin = vs.XMin,
                YMax = vs.YMax,
                YMin = vs.YMin,
                ZMax = vs.ZMax,
                ZMin = vs.ZMin,
            };
            result.Voxels = vs.GetVoxelsAsStringArrays();
            result.VoxelsPerSide = vs.VoxelsPerSide;
            result.Colors = vs.SolidColor.ToString();
            result.Comments.AddRange(vs.Comments);
            if (vs._inertiaTensor != null)
            {
                var tensorAsArray = new double[9];
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                        tensorAsArray[3 * i + j] = vs._inertiaTensor[i, j];
                result.InertiaTensor = string.Join(",", tensorAsArray);
            }
            return result;
        }

        #endregion
    }
}