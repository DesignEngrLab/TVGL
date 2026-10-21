// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 10-21-2026
//
// Decoder for PRC_TYPE_TESS_3D_Compressed, the highly compressed tessellation of PRC
// (ISO 14739-1 §7.8.9). It follows the standard's traversal, point prediction and normal coding,
// as corrected by the PDF Association's errata, which are needed to read real files:
//   pdf-issues #727 (CR-14): edge_status_array holds T or 3T entries; only the first T are used,
//     masked to 2 bits (1 = right neighbour, 2 = left). When both neighbours are pushed, the right
//     edge is pushed first, so the left one is treated first. A traversal whose stack drains
//     restarts with a seed triangle that reads three point_is_reference entries.
//   pdf-issues #773 (CR-18): the apex frame is that of Formulas (1)-(3) turned 180° about Z.
//   pdf-issues #774/#775 (CR-19/20): a mesh may hold several components, each seeded afresh;
//     decoding continues until there are as many triangles as triangle_face_array entries.
//   pdf-issues #772/#750 (CR-15): normal_is_reversed holds one flag per triangle.
//   pdf-issues #436: normal_binary_data holds has_multiple_normal only at a vertex's first normal.
// Two details the standard leaves open were settled against sample files: a planar face's single
// normal is coded as a normal of the first vertex of the face's first triangle, and a reference to
// one of a vertex's n stored normals is coded, counted from the last, in
// GetNumberOfBitsUsedToStoreUnsignedInteger(n - 1) bits.
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.IO;

namespace TVGL
{
    /// <summary> Decodes the points and triangles of a PRC_TYPE_TESS_3D_Compressed entity. </summary>
    internal sealed class PrcCompressedTessellation
    {
        private const double FloatEpsilon = 1.1920928955078125e-07;

        private readonly double tolerance;
        private readonly Vec origin;
        private readonly int[] pointArray;
        private readonly bool[] pointIsReference;
        private readonly int[] pointReferences;
        private readonly sbyte[] edgeStatus;
        private readonly int[] triangleFaces;
        private readonly bool[] normalIsReversed;
        private readonly NormalStream normals;

        private readonly List<Vec> points = [];
        private int nextTriple, nextIsReference, nextReference;

        internal PrcCompressedTessellation(PrcNode tessellation)
        {
            tolerance = tessellation.Double("tolerance");
            var originArray = tessellation.Get<float[]>("origin_array");
            origin = originArray == null ? default : new Vec(originArray[0], originArray[1], originArray[2]);
            pointArray = tessellation.Get<int[]>("point_array") ?? [];
            pointIsReference = tessellation.Get<bool[]>("point_is_reference_array") ?? [];
            pointReferences = tessellation.Get<int[]>("point_reference_array") ?? [];
            edgeStatus = tessellation.Get<sbyte[]>("edge_status_array") ?? [];
            triangleFaces = tessellation.Get<int[]>("triangle_face_array") ?? [];
            if (tessellation.Bool("must_recalculate_normals"))
                normalIsReversed = tessellation.Get<bool[]>("normal_is_reversed") ?? [];
            else
                normals = new NormalStream(tessellation.Get<bool[]>("normal_binary_data") ?? [],
                    tessellation.Get<short[]>("normal_angle_array") ?? [],
                    tessellation.Get<sbyte>("normal_angle_number_of_bits"),
                    tessellation.Get<bool[]>("is_face_planar") ?? []);
        }

        /// <summary>
        /// The decoded coordinates (x, y, z per point) and triangles (3 point indices each, wound
        /// counter-clockwise about the surface normal). Throws InvalidDataException when the data
        /// is inconsistent, including when any of it is left unused.
        /// </summary>
        internal (double[] Coordinates, int[] Triangles) Decode()
        {
            var triangleCount = triangleFaces.Length;
            if (triangleCount == 0) return ([], []);
            if (!(tolerance > 0) || double.IsInfinity(tolerance)) throw Invalid($"tolerance {tolerance}");
            if (edgeStatus.Length < triangleCount) throw Invalid("edge_status_array is too short");
            var triangles = new int[3 * triangleCount];
            var stack = new List<(int Low, int High, int Far)>();
            var edgeUse = new Dictionary<long, int>();

            for (var t = 0; t < triangleCount; t++)
            {
                (int Low, int High, int Far)? gate = null;
                while (stack.Count > 0)
                {
                    var edge = stack[^1];
                    stack.RemoveAt(stack.Count - 1);
                    // An edge whose far side was reached another way since it was pushed is skipped.
                    if (edgeUse.GetValueOrDefault(EdgeKey(edge.Low, edge.High)) < 2)
                    {
                        gate = edge;
                        break;
                    }
                }

                int i0, i1, i2;
                if (gate is { } g)
                {
                    i0 = g.Low;
                    i1 = g.High;
                    i2 = NextIsReference()
                        ? NextReference()
                        : AddPoint(ApexFromEdge(points[g.Low], points[g.High], points[g.Far], NextOffsets()));
                }
                else
                {
                    // Seed triangle: V0 from the origin, V1 from V0, V2 from the middle of [V0 V1],
                    // each either a reference to an earlier point or a new point.
                    i0 = NextIsReference() ? NextReference() : AddPoint(origin + NextOffsets());
                    i1 = NextIsReference() ? NextReference() : AddPoint(points[i0] + NextOffsets());
                    i2 = NextIsReference()
                        ? NextReference()
                        : AddPoint((points[i0] + points[i1]) * 0.5 + NextOffsets());
                    if (i0 > i1) (i0, i1) = (i1, i0);
                }
                if (i0 == i1 || i1 == i2 || i0 == i2) throw Invalid($"triangle {t} repeats a point");
                foreach (var (a, b) in new[] { (i0, i1), (i1, i2), (i0, i2) })
                {
                    var key = EdgeKey(a, b);
                    edgeUse[key] = edgeUse.GetValueOrDefault(key) + 1;
                }

                // Whether [V0 V1 V2] turns counter-clockwise about the surface normal; it decides
                // which forward edge is the right one and which the left.
                var alongNormal = TurnsAlongNormal(t, i0, i1, i2);
                triangles[3 * t] = i0;
                triangles[3 * t + 1] = alongNormal ? i1 : i2;
                triangles[3 * t + 2] = alongNormal ? i2 : i1;

                // Push the forward edges with untreated neighbours: right [V1 V2] then left [V0 V2],
                // exchanged when the triangle turns along the normal.
                var right = (Low: Math.Min(i1, i2), High: Math.Max(i1, i2), Far: i0);
                var left = (Low: Math.Min(i0, i2), High: Math.Max(i0, i2), Far: i1);
                if (alongNormal) (right, left) = (left, right);
                var status = edgeStatus[t] & 0x03;
                if ((status & 0x01) != 0) stack.Add(right);
                if ((status & 0x02) != 0) stack.Add(left);
            }

            if (nextTriple * 3 != pointArray.Length || nextIsReference != pointIsReference.Length ||
                nextReference != pointReferences.Length || (normals != null && !normals.IsFullyRead))
                throw Invalid("the mesh decoded without using all of its data");

            var coordinates = new double[3 * points.Count];
            for (var i = 0; i < points.Count; i++)
            {
                coordinates[3 * i] = points[i].X;
                coordinates[3 * i + 1] = points[i].Y;
                coordinates[3 * i + 2] = points[i].Z;
            }
            return (coordinates, triangles);
        }

        /// <summary>
        /// Whether triangle <paramref name="t"/> turns counter-clockwise about its surface normal: from
        /// normal_is_reversed when normals are recalculated; otherwise from the stored normals, either
        /// the triangle_normal_reversed bit of the first normal coded on this triangle or, when all of
        /// its normals were coded before, the side of the first vertex's normal.
        /// </summary>
        private bool TurnsAlongNormal(int t, int i0, int i1, int i2)
        {
            if (normalIsReversed != null)
                return t < normalIsReversed.Length && normalIsReversed[t];
            var normal = normals.Restore(triangleFaces[t], [i0, i1, i2], [points[i0], points[i1], points[i2]],
                out var reversedBit);
            if (reversedBit is { } reversed) return !reversed;
            return Vec.Dot(Vec.Cross(points[i1] - points[i0], points[i2] - points[i0]), normal) >= 0;
        }

        /// <summary>
        /// The apex of a triangle entered through [Va Vb] (Va treated first), in the frame of the
        /// neighbouring triangle with far vertex Vfar: origin at the edge middle, X along the edge, Z
        /// normal to the neighbour, and Y and Z pointing away from it (ISO 14739-1 §7.8.9.3, CR-18).
        /// </summary>
        private static Vec ApexFromEdge(Vec a, Vec b, Vec far, Vec offsets)
        {
            var o = (a + b) * 0.5;
            var x = (a - b).Normalized(out _);
            var z = Vec.Cross(far - o, x).Normalized(out var zLength);
            Vec y;
            if (zLength < FloatEpsilon)
                (y, z) = OrthonormalComplement(x);
            else
            {
                y = Vec.Cross(z, x).Normalized(out var yLength);
                if (yLength < FloatEpsilon) (y, z) = OrthonormalComplement(x);
            }
            if (Vec.Dot(y, o - far) > 0)
            {
                y = -y;
                z = -z;
            }
            return o + x * offsets.X + y * offsets.Y + z * offsets.Z;
        }

        /// <summary> Two unit axes completing a unit X (the role of MakeOrthoRep, which the standard does not list). </summary>
        private static (Vec Y, Vec Z) OrthonormalComplement(Vec x)
        {
            var helper = Math.Abs(x.X) < 0.6 ? new Vec(1, 0, 0) : Math.Abs(x.Y) < 0.6 ? new Vec(0, 1, 0) : new Vec(0, 0, 1);
            var y = Vec.Cross(helper, x).Normalized(out _);
            return (y, Vec.Cross(x, y));
        }

        private bool NextIsReference()
        {
            if (nextIsReference >= pointIsReference.Length) throw Invalid("point_is_reference_array is too short");
            return pointIsReference[nextIsReference++];
        }

        private int NextReference()
        {
            if (nextReference >= pointReferences.Length) throw Invalid("point_reference_array is too short");
            var index = pointReferences[nextReference++];
            if (index < 0 || index >= points.Count) throw Invalid($"reference to point {index} of {points.Count}");
            return index;
        }

        /// <summary> The next point_array triple, in units of the tolerance. </summary>
        private Vec NextOffsets()
        {
            if (3 * nextTriple + 2 >= pointArray.Length) throw Invalid("point_array is too short");
            var i = 3 * nextTriple++;
            return new Vec(pointArray[i] * tolerance, pointArray[i + 1] * tolerance, pointArray[i + 2] * tolerance);
        }

        private int AddPoint(Vec point)
        {
            points.Add(point);
            return points.Count - 1;
        }

        private static long EdgeKey(int a, int b) => a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;

        private static InvalidDataException Invalid(string message)
            => new($"Invalid PRC compressed tessellation: {message}.");

        /// <summary>
        /// The stored normals (ISO 14739-1 §7.8.9.4, read as clarified in pdf-issues #436). Each vertex
        /// keeps a list of normals. Its first normal is preceded by has_multiple_normal; after that it
        /// is reused unless has_multiple_normal was set, when a bit says whether the normal is a
        /// reference to the list or a new one. A new normal is three orientation bits and two angles
        /// in a frame built from the triangle. A planar face has one normal, coded at the first vertex
        /// of its first triangle and reused for the rest of the face.
        /// </summary>
        private sealed class NormalStream
        {
            private readonly bool[] bits;
            private readonly short[] angles;
            private readonly int angleBits;
            private readonly bool[] facePlanar;
            private readonly Dictionary<int, (bool HasMultiple, List<Vec> Normals)> vertexNormals = [];
            private readonly Dictionary<int, Vec> faceNormals = [];
            private int bitsRead, anglesRead;
            private bool? reversedBit;

            internal bool IsFullyRead => bitsRead == bits.Length && anglesRead == angles.Length;

            internal NormalStream(bool[] bits, short[] angles, int angleBits, bool[] facePlanar)
            {
                if (angleBits < 1 || angleBits > 16) throw Invalid($"normal angles of {angleBits} bits");
                this.bits = bits;
                this.angles = angles;
                this.angleBits = angleBits;
                this.facePlanar = facePlanar;
            }

            /// <summary>
            /// Restores the triangle's normals and returns that of its first vertex (or of its planar
            /// face), with the triangle_normal_reversed bit of the first normal coded here, if any.
            /// </summary>
            internal Vec Restore(int face, int[] vertices, Vec[] triangle, out bool? firstReversedBit)
            {
                reversedBit = null;
                Vec first;
                if (face >= 0 && face < facePlanar.Length && facePlanar[face])
                {
                    if (!faceNormals.TryGetValue(face, out first))
                        faceNormals[face] = first = RestoreVertexNormal(vertices[0], triangle);
                }
                else
                {
                    first = RestoreVertexNormal(vertices[0], triangle);
                    RestoreVertexNormal(vertices[1], triangle);
                    RestoreVertexNormal(vertices[2], triangle);
                }
                firstReversedBit = reversedBit;
                return first;
            }

            private Vec RestoreVertexNormal(int vertex, Vec[] triangle)
            {
                if (!vertexNormals.TryGetValue(vertex, out var entry))
                {
                    entry = (ReadBit(), [NextNormal(triangle)]);
                    vertexNormals[vertex] = entry;
                    return entry.Normals[0];
                }
                if (!entry.HasMultiple) return entry.Normals[0];
                if (ReadBit())
                {
                    var last = entry.Normals.Count - 1;
                    var bitCount = Math.Max(1, BitLength(last));
                    var read = 0;
                    for (var i = 0; i < bitCount; i++)
                        if (ReadBit()) read |= 1 << i;
                    var index = last - read;
                    if (index < 0) throw Invalid($"normal reference {read} of {entry.Normals.Count}");
                    return entry.Normals[index];
                }
                var added = NextNormal(triangle);
                entry.Normals.Add(added);
                return added;
            }

            private static int BitLength(int value)
            {
                var length = 0;
                for (; value > 0; value >>= 1) length++;
                return length;
            }

            /// <summary> A new normal from its two angles in the triangle's frame (§7.8.9.4). </summary>
            private Vec NextNormal(Vec[] tri)
            {
                var reversedTriangle = ReadBit();
                reversedBit ??= reversedTriangle;
                var reversedX = ReadBit();
                var reversedY = ReadBit();
                var v1 = (tri[1] - tri[0]).Normalized(out _);
                var v2 = (tri[2] - tri[0]).Normalized(out _);
                var v3 = (tri[2] - tri[1]).Normalized(out _);
                var theta1 = Math.Abs(Math.Acos(Clamp(Vec.Dot(v1, v2))) - Math.PI / 2);
                var theta2 = Math.Abs(Math.Acos(Clamp(Vec.Dot(v3, -v1))) - Math.PI / 2);
                var theta3 = Math.Abs(Math.Acos(Clamp(Vec.Dot(-v2, -v3))) - Math.PI / 2);
                Vec x, z;
                if (theta1 < theta2 && theta1 < theta3)
                {
                    x = v1;
                    z = Vec.Cross(v1, v2);
                }
                else if (theta1 < theta3)
                {
                    x = v3;
                    z = Vec.Cross(-v3, v1);
                }
                else
                {
                    x = -v2;
                    z = Vec.Cross(v2, v3);
                }
                z = z.Normalized(out _);
                if (reversedTriangle) z = -z;
                var y = Vec.Cross(z, x).Normalized(out _);
                if (reversedX) x = -x;
                if (reversedY) y = -y;

                var scale = Math.PI / 2 / ((1 << angleBits) - 1);
                var (sinTheta, cosTheta) = SinCos(NextAngle() * scale);
                var (sinPhi, cosPhi) = SinCos(NextAngle() * scale);
                return x * (cosTheta * cosPhi) + y * (sinTheta * cosPhi) + z * sinPhi;
            }

            private static double Clamp(double cosine) => Math.Max(-1.0, Math.Min(1.0, cosine));

            /// <summary> Sine and cosine by the standard's four-term Taylor expansion, reduced to [0, π/4]. </summary>
            private static (double Sin, double Cos) SinCos(double angle)
            {
                if (angle > Math.PI / 4)
                {
                    var (s, c) = SinCos(Math.PI / 2 - angle);
                    return (c, s);
                }
                var x2 = angle * angle;
                var sin = angle * (1.0 - x2 * (1.0 / 6.0 - x2 * (1.0 / 120.0 - x2 * (1.0 / 5040.0))));
                var cos = 1.0 - x2 * (1.0 / 2.0 - x2 * (1.0 / 24.0 - x2 * (1.0 / 720.0)));
                return (sin, cos);
            }

            private int NextAngle()
            {
                if (anglesRead >= angles.Length) throw Invalid("normal_angle_array is too short");
                return angles[anglesRead++] & ((1 << angleBits) - 1);
            }

            private bool ReadBit()
            {
                if (bitsRead >= bits.Length) throw Invalid("normal_binary_data is too short");
                return bits[bitsRead++];
            }
        }

        /// <summary> A double-precision 3D vector for the decoder's arithmetic. </summary>
        private readonly record struct Vec(double X, double Y, double Z)
        {
            public static Vec operator +(Vec a, Vec b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
            public static Vec operator -(Vec a, Vec b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
            public static Vec operator -(Vec a) => new(-a.X, -a.Y, -a.Z);
            public static Vec operator *(Vec a, double s) => new(a.X * s, a.Y * s, a.Z * s);
            public static double Dot(Vec a, Vec b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
            public static Vec Cross(Vec a, Vec b) => new(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);

            public Vec Normalized(out double length)
            {
                length = Math.Sqrt(Dot(this, this));
                return length > 0 ? this * (1.0 / length) : this;
            }
        }
    }
}
