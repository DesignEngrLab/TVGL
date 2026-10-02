using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TVGL.IOFunctions.Step
{
    /// <summary>
    /// 
    /// </summary>
	internal static class ParseHelper
    {
        internal static readonly IDictionary<string, Type> EntityTypes = new Dictionary<string, Type>()
        {
            {"CARTESIAN_POINT", typeof(CartesianPoint)},
            {"DIRECTION", typeof(DirectionPoint)},
            {"VERTEX_POINT", typeof(VertexPoint)},
            {"VECTOR", typeof(VectorPoint)},
            {"AXIS2_PLACEMENT_3D", typeof(Axis2Placement3D)},
            {"ORIENTED_EDGE", typeof(OrientedEdge)},
            {"FACE_BOUND", typeof(FaceBound)},
            {"CLOSED_SHELL", typeof(ClosedShell)},
            {"ADVANCED_FACE", typeof(AdvancedFace)},
            {"B_SPLINE_CURVE_WITH_KNOTS", typeof(BSplineCurveWithKnots)},
            {"CIRCLE", typeof(Circle)},
            {"CONICAL_SURFACE", typeof(ConicalSurface)},
            {"CYLINDRICAL_SURFACE", typeof(CylindricalSurface)},
            {"TOROIDAL_SURFACE", typeof(ToroidalSurface)},
            {"EDGE_CURVE", typeof(EdgeCurve)},
            {"EDGE_LOOP", typeof(EdgeLoop)},
            {"FACE_OUTER_BOUND", typeof(FaceOuterBound)},
            {"LINE", typeof(Line)},
            {"PLANE", typeof(Plane)},
            //{"SOLID_MODEL", typeof(StepSolid) }, //abstract and can't be instantiated
            {"CSG_SOLID", typeof(CSGStepSolid) },
            {"MANIFOLD_SOLID_BREP", typeof(BREPStepSolid) },
        };

        internal static Stream FindSection(Stream stream, string start, string end)
        {
            stream.Position = 0;
            var ms = new MemoryStream();
            var sw = new StreamWriter(ms);
            var reader = new StreamReader(stream);
            string line;
            var inSection = false;

            while ((line = reader.ReadLine()) != null)
            {
                if (line.Equals(start))
                {
                    inSection = true;
                    continue;
                }
                if (line.Equals(end))
                {
                    inSection = false;
                    continue;
                }
                if (inSection)
                {
                    sw.WriteLine(line);
                }
            }
            sw.Flush();

            return ms;
        }

        internal static IEnumerable<string> GetEntityStrings(Stream dataStream)
        {
            dataStream.Position = 0;
            IList<string> entityStrings = new List<string>();

            using (StreamReader sr = new StreamReader(dataStream))
            {
                while (sr.Peek() >= 0)
                {
                    var entityStr = "";
                    char ch = char.MinValue;
                    do
                    {
                        if (!char.IsControl(ch) && !char.IsSeparator(ch))
                            entityStr += ch;
                        ch = (char)sr.Read();
                    } while (ch != ';');
                    entityStrings.Add(entityStr);
                }
            }
            return entityStrings;
        }

        internal static IList<string> ParseList(string listString)
        {
            return ParseList<string>(listString);
        }

        internal static T[] ParseList<T>(string listString)
        {
            return Regex.Split(listString, @",(?![^\(]*\))").Select(x => (T)Convert.ChangeType(x.Trim(), typeof(T),
                CultureInfo.InvariantCulture)).ToArray();
        }



        internal static Entity ParseBodyLine(string line)
        {
            var splitted = line.Split('=');
            // remove = from id
            var id = ParseId(splitted[0]);

            var rightPart = splitted[1].Trim();
            var positionOfList = rightPart.IndexOf('(');
            var type = rightPart.Substring(0, positionOfList);

            var list = ParseList(rightPart.Substring(positionOfList));

            return Entity.CreateEntity(type, id, list);
        }

        internal static long ParseId(string id)
        {
            if (id == "$" || id == "*")
            {
                return 0;
            }
            return long.Parse(id.Substring(1));
        }


        internal static string ParseString(string data)
        {
            return data.Trim('\'');
        }

        internal static bool ParseBool(string data)
        {
            return data == ".T.";
        }

        internal static T Parse<T>(String data)
        {
            var type = typeof(T);
            if (type == typeof(bool))
            {
                return (T)Convert.ChangeType(ParseBool(data), typeof(T), CultureInfo.InvariantCulture);
            }
            else if (type == typeof(string))
            {
                return (T)Convert.ChangeType(ParseString(data), typeof(T), CultureInfo.InvariantCulture);
            }
            else
            {
                return (T)Convert.ChangeType(data, typeof(T), CultureInfo.InvariantCulture);
            }
        }
    }

}

