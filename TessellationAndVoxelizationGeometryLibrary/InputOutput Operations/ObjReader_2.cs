using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace TVGL.IOFunctions
{


    /// <summary>
    /// A Wavefront .obj file reader.
    /// </summary>
    public class ObjReader
    {
        /// <summary>
        /// Gets or sets the normal vectors.
        /// </summary>
        private IList<double[]> Normals { get; set; }

        /// <summary>
        /// Gets or sets the points.
        /// </summary>
        private IList<double[]> Vertices { get; set; }


        /// <summary>
        /// Reads the model from the specified stream.
        /// </summary>
        /// <param name="s">The stream.</param>
        /// <returns>The model.</returns>
        public override Model3DGroup Read(Stream s)
        {
            using (this.Reader = new StreamReader(s))
            {
                this.currentLineNo = 0;
                while (!this.Reader.EndOfStream)
                {
                    this.currentLineNo++;
                    var line = this.Reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    line = line.Trim();
                    if (line.StartsWith("#") || line.Length == 0)
                    {
                        continue;
                    }

                    string keyword, values;
                    SplitLine(line, out keyword, out values);

                    switch (keyword.ToLower())
                    {
                        // Vertex data
                        case "v": // geometric vertices
                            this.AddVertex(values);
                            break;
                        case "vn": // vertex normals
                            this.AddNormal(values);
                            break;
                        case "f": // face
                            this.AddFace(values);
                            break;
                        case "vp": // parameter space vertices
                        case "cstype":
                        // rational or non-rational forms of curve or surface type: 
                        //   basis matrix, Bezier, B-spline, Cardinal, Taylor
                        case "degree": // degree
                        case "bmat": // basis matrix
                        case "step": // step size
                        // Elements
                        case "p": // point
                        case "l": // line
                        case "curv": // curve
                        case "curv2": // 2D curve
                        case "surf": // surface
                        // Free-form curve/surface body statements
                        case "parm": // parameter name
                        case "trim": // outer trimming loop (trim)
                        case "hole": // inner trimming loop (hole)
                        case "scrv": // special curve (scrv)
                        case "sp": // special point (sp)
                        case "end": // end statement (end)
                        // Connectivity between free-form surfaces
                        case "con": // connect
                        // Grouping
                        case "g": // group name
                        case "s": // smoothing group
                        case "mg": // merging group
                        case "o": // object name
                        // Display/render attributes
                        case "mtllib": // material library
                        case "usemtl": // material name
                        case "usemap": // texture map name
                        case "bevel": // bevel interpolation
                        case "c_interp": // color interpolation
                        case "d_interp": // dissolve interpolation
                        case "lod": // level of detail
                        case "shadow_obj": // shadow casting
                        case "trace_obj": // ray tracing
                        case "ctech": // curve approximation technique
                        case "stech": // surface approximation technique
                            // not supported
                            break;
                    }
                }
            }

            return this.BuildModel();
        }

        /// <summary>
        /// Reads a GZipStream compressed OBJ file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>A Model3D object containing the model.</returns>
        /// <remarks>This is a file format used by Helix Toolkit only.
        /// Use the GZipHelper class to compress an .obj file.</remarks>
        public Model3DGroup ReadZ(string path)
        {
            this.TexturePath = Path.GetDirectoryName(path);
            using (var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var deflateStream = new GZipStream(s, CompressionMode.Decompress, true);
                return this.Read(deflateStream);
            }
        }
        private static Color ColorParse(string values)
        {
            var fields = Split(values);
            return new Color().FromRgb((byte)(fields[0] * 255), (byte)(fields[1] * 255), (byte)(fields[2] * 255));
        }

        /// <summary>
        /// Parse a string containing a double value.
        /// </summary>
        /// <param name="input">
        /// The input string.
        /// </param>
        /// <returns>
        /// The value.
        /// </returns>
        private static double DoubleParse(string input)
        {
            return double.Parse(input, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Splits the specified string using whitespace(input) as separators.
        /// </summary>
        /// <param name="input">
        /// The input string.
        /// </param>
        /// <returns>
        /// List of input.
        /// </returns>
        private static IList<double> Split(string input)
        {
            input = input.Trim();
            var fields = input.Split(' ');
            var result = new double[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                result[i] = DoubleParse(fields[i]);
            }

            return result;
        }

        /// <summary>
        /// Splits a line in keyword and arguments.
        /// </summary>
        /// <param name="line">
        /// The line.
        /// </param>
        /// <param name="keyword">
        /// The keyword.
        /// </param>
        /// <param name="arguments">
        /// The arguments.
        /// </param>
        private static void SplitLine(string line, out string keyword, out string arguments)
        {
            int idx = line.IndexOf(' ');
            if (idx < 0)
            {
                keyword = line;
                arguments = null;
                return;
            }

            keyword = line.Substring(0, idx);
            arguments = line.Substring(idx + 1);
        }

        /// <summary>
        /// Adds a face.
        /// </summary>
        /// <param name="values">
        /// The input values.
        /// </param>
        /// <remarks>
        /// Adds a polygonal face. The numbers are indexes into the arrays of vertex positions,
        /// texture coordinates, and normal vectors respectively. A number may be omitted if,
        /// for example, texture coordinates are not being defined in the model.
        /// There is no maximum number of vertices that a single polygon may contain.
        /// The .obj file specification says that each face must be flat and convex.
        /// </remarks>
        private void AddFace(string values)
        {   
            var fields = values.SplitOnWhitespace();
            var faceIndices = new List<int>();
            foreach (var field in fields)
            {
                if (string.IsNullOrEmpty(field))
                {
                    continue;
                }

                var ff = field.Split('/');
                int vi = int.Parse(ff[0]);
                int vti = ff.Length > 1 && ff[1].Length > 0 ? int.Parse(ff[1]) : int.MaxValue;
                int vni = ff.Length > 2 && ff[2].Length > 0 ? int.Parse(ff[2]) : int.MaxValue;

                // Handle relative indices (negative numbers)
                if (vi < 0)
                {
                    vi = this.Vertices.Count + vi;
                }

                if (vti < 0)
                {
                    vti = this.TextureCoordinates.Count + vti;
                }

                if (vni < 0)
                {
                    vni = this.Normals.Count + vni;
                }

                // Check if the indices are valid
                if (vi - 1 >= this.Vertices.Count)
                {
                    if (this.IgnoreErrors)
                    {
                        return;
                    }

                    throw new FileFormatException(string.Format("Invalid vertex index ({0}) on line {1}.", vi,
                        this.currentLineNo));
                }

                if (vti == int.MaxValue)
                {
                    // turn off texture coordinates in the builder
                    builder.CreateTextureCoordinates = false;
                }

                if (vni == int.MaxValue)
                {
                    // turn off normals in the builder
                    builder.CreateNormals = false;
                }

                // check if the texture coordinate index is valid
                if (builder.CreateTextureCoordinates && vti - 1 >= this.TextureCoordinates.Count)
                {
                    if (this.IgnoreErrors)
                    {
                        return;
                    }

                    throw new FileFormatException(
                        string.Format(
                            "Invalid texture coordinate index ({0}) on line {1}.", vti, this.currentLineNo));
                }

                // check if the normal index is valid
                if (builder.CreateNormals && vni - 1 >= this.Normals.Count)
                {
                    if (this.IgnoreErrors)
                    {
                        return;
                    }

                    throw new FileFormatException(
                        string.Format("Invalid normal index ({0}) on line {1}.", vni, this.currentLineNo));
                }

                bool addVertex = true;

                if (smoothingGroupMap != null)
                {
                    int vix;
                    if (smoothingGroupMap.TryGetValue(vi, out vix))
                    {
                        // use the index of a previously defined vertex
                        addVertex = false;
                    }
                    else
                    {
                        // add a new vertex
                        vix = positions.Count;
                        smoothingGroupMap.Add(vi, vix);
                    }

                    faceIndices.Add(vix);
                }
                else
                {
                    // if smoothing is off, always add a new vertex
                    faceIndices.Add(positions.Count);
                }

                if (addVertex)
                {
                    // add vertex
                    positions.Add(this.Vertices[vi - 1]);

                    // add texture coordinate (if enabled)
                    if (builder.CreateTextureCoordinates)
                    {
                        textureCoordinates.Add(this.TextureCoordinates[vti - 1]);
                    }

                    // add normal (if enabled)
                    if (builder.CreateNormals)
                    {
                        normals.Add(this.Normals[vni - 1]);
                    }
                }
            }

            if (faceIndices.Count <= 4)
            {
                // add triangles or quads
                builder.AddPolygon(faceIndices);
            }
            else
            {
                // add triangles by cutting ears algorithm
                // this algorithm is quite expensive...
                builder.AddPolygonByCuttingEars(faceIndices);
            }
        }

        /// <summary>
        /// Adds a normal.
        /// </summary>
        /// <param name="values">
        /// The input values.
        /// </param>
        private void AddNormal(string values)
        {
            var fields = Split(values);
            this.Normals.Add(new []{fields[0], fields[1], fields[2]});
        }


        /// <summary>
        /// Adds a vertex.
        /// </summary>
        /// <param name="values">
        /// The input values.
        /// </param>
        private void AddVertex(string values)
        {
            var fields = Split(values);
            this.Vertices.Add(new []{fields[0], fields[1], fields[2]});
        }

    }
}