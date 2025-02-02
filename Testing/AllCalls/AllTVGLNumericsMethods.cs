using System;
using System.Collections.Generic;
using TVGL;

namespace TVGLUnitTestsAndBenchmarking
{
    internal class AllTVGLNumericsMethods
    {
        private static void Run()
        {
            #region Near Equality Methods
            var x = 1.0;
            var y = 2.0;
            bool isItTrueThat = x.IsPracticallySame(y);
            isItTrueThat = x.IsPracticallySame(y);
            Vector2 v2_1 = new Vector2(1.0, 2.0);
            Vector2 v2_2 = new Vector2(1.00000000001, 2.000000000002);
            isItTrueThat = v2_1.IsPracticallySame(v2_2);
            Vector3 v3_1 = new Vector3(1.0, 2.0, 3.0);
            Vector3 v3_2 = new Vector3(1.00000000001, 2.000000000002, 3.0);
            isItTrueThat = v3_1.IsPracticallySame(v3_2);
            isItTrueThat = x.IsNegligible();
            isItTrueThat = v2_1.IsNegligible();
            isItTrueThat = v3_1.IsNegligible();
            isItTrueThat = x.IsGreaterThanNonNegligible(y);
            isItTrueThat = x.IsLessThanNonNegligible(y);

            #endregion

            #region All Vector2 Methods
            v2_1 = new Vector2();
            Vector2 nullVector2 = Vector2.Null;
            Vector2 zeroVector2 = Vector2.Zero;
            Vector2 oneVector2 = Vector2.One;
            Vector2 unitVector2X = Vector2.UnitX;
            Vector2 unitVector2Y = Vector2.UnitY;
            Vector2 copyVector = v2_1.Copy();
            x = v2_1.X;
            x = v2_1[0];
            y = v2_1.Y;
            y = v2_1[1];
            double length = v2_1.Length();
            double lengthSquared = v2_1.LengthSquared();
            double distance = v2_1.Distance(zeroVector2);
            double distanceSquared = v2_1.DistanceSquared(zeroVector2);
            Vector2 normal = v2_1.Normalize();
            Vector2 reflect = v2_1.Reflect(unitVector2Y);
            Vector2 clamp = v2_1.Clamp(zeroVector2, oneVector2);
            Vector2 lerp = v2_1.Lerp(oneVector2, 0.5);

            v2_2 = v2_1 + v2_1;
            v2_2 = v2_1 - v2_1;
            //component to component product vector whos terms sum to dot product
            v2_2 = v2_1 - v2_1;
            v2_2 = v2_1 / v2_1;
            v2_2 = v2_1 / new double();
            v2_2 = -v2_1;
            isItTrueThat = v2_1.IsNull();
            isItTrueThat = v2_1.IsNegligible();
            isItTrueThat = v2_1 == v2_2;
            isItTrueThat = v2_1 != v2_2;
            double dot = v2_1.Dot(v2_2);
            double cross = v2_1.Cross(v2_2);
            Vector2 minVector = Vector2.Min(v2_1, v2_2);
            Vector2 maxVector = Vector2.Max(v2_1, v2_2);
            Vector2 absVector = Vector2.Abs(v2_1);
            Vector2 sqrtVector = Vector2.SquareRoot(v2_1);

            Matrix3x3 m3x3 = new Matrix3x3();
            v2_1 = v2_1.Transform(m3x3);
            v2_1 = v2_1.TransformNoTranslate(m3x3);

            Matrix4x4 m4x4 = new Matrix4x4();
            v2_1 = v2_1.Transform(m4x4);
            v2_1 = v2_1.TransformNoTranslate(m4x4);
            v2_1 = v2_1.Transform(new Quaternion());
            #endregion

            #region All Matrix3x3 Methods
            isItTrueThat = m3x3.IsProjectiveTransform;
            double value = m3x3.M11;
            value = m3x3.M12;
            value = m3x3.M13;
            value = m3x3.M21;
            value = m3x3.M22;
            value = m3x3.M23;
            value = m3x3.M31;
            value = m3x3.M32;
            value = m3x3.M33;
            m3x3 = Matrix3x3.Identity;
            m3x3 = Matrix3x3.Null;
            isItTrueThat = m3x3.IsIdentity();
            isItTrueThat = m3x3.IsNull();
            Vector2 t = m3x3.Translation;
            m3x3 = Matrix3x3.CreateTranslation(t);
            m3x3 = Matrix3x3.CreateTranslation(x, y);
            m3x3 = Matrix3x3.CreateScale(1.0);
            m3x3 = Matrix3x3.CreateScale(x, y);
            m3x3 = Matrix3x3.CreateScale(Vector2.One);
            m3x3 = Matrix3x3.CreateScale(x, y, v2_2); //vResult is the center of scaling
            m3x3 = Matrix3x3.CreateSkew(2.0, 2.0);
            m3x3 = Matrix3x3.CreateSkew(2.0, 2.0, v2_2);//vResult is the center of skewing
            m3x3 = Matrix3x3.CreateRotation(1.0); //in radians
            m3x3 = Matrix3x3.CreateRotation(1.0, v2_2); //vResult is the center of rotate
            m3x3 = m3x3.Transpose();
            isItTrueThat = Matrix3x3.Invert(m3x3, out Matrix3x3 invM3x3);
            var d = m3x3.GetDeterminant();
            m3x3 = Matrix3x3.Lerp(m3x3, m3x3, 0.5);
            m3x3 = -m3x3;
            var m3x3Another = 4.0 * m3x3;
            m3x3 = m3x3 + m3x3;
            m3x3 = m3x3 - m3x3;
            m3x3 = m3x3 * m3x3;

            isItTrueThat = m3x3 == m3x3Another;
            isItTrueThat = m3x3 != m3x3Another;
            #endregion

            #region All Vector3 Methods

            v3_1 = new Vector3();
            v3_1 = new Vector3(v2_1, 0.0);
            Vector3 nullVector3 = Vector3.Null;
            Vector3 zeroVector3 = Vector3.Zero;
            Vector3 oneVector3 = Vector3.One;
            Vector3 unitVector3X = Vector3.UnitX;
            Vector3 unitVector3Y = Vector3.UnitY;
            Vector3 unitVector3Z = Vector3.UnitZ;
            unitVector3X = Vector3.UnitVector(CartesianDirections.XNegative);
            unitVector3X = Vector3.UnitVector(0);

            Vector3 copyVector3 = v3_1.Copy();
            x = v3_1.X;
            x = v3_1[0];
            y = v3_1.Y;
            y = v3_1[1];
            double z = v3_1.Z;
            y = v3_1[2];
            length = v3_1.Length();
            lengthSquared = v3_1.LengthSquared();
            distance = v3_1.Distance(zeroVector3);
            distanceSquared = v3_1.DistanceSquared(zeroVector3);
            Vector3 normal3 = v3_1.Normalize();
            Vector3 reflect3 = v3_1.Reflect(unitVector3Y);
            Vector3 clamp3 = v3_1.Clamp(zeroVector3, oneVector3);
            Vector3 lerp3 = v3_1.Lerp(oneVector3, 0.5);

            v3_2 = v3_1 + v3_1;
            v3_2 = v3_1 - v3_1;
            v3_2 = v3_1 * v3_1; //not dot or cross - basically a 
            //component to component product vector whos terms sum to dot product
            v3_2 = v3_1 - v3_1;
            v3_2 = v3_1 / v3_1;
            v3_2 = v3_1 / new double();
            v3_2 = -v3_1;
            isItTrueThat = v3_1.IsNull();
            isItTrueThat = v3_1.IsNegligible();
            isItTrueThat = v3_1 == v3_2;
            isItTrueThat = v3_1 != v3_2;
            double dot3 = v3_1.Dot(v3_2);
            Vector3 cross3 = v3_1.Cross(v3_2);
            Vector3 minVector3 = Vector3.Min(v3_1, v3_2);
            Vector3 maxVector3 = Vector3.Max(v3_1, v3_2);
            Vector3 absVector3 = Vector3.Abs(v3_1);
            Vector3 sqrtVector3 = Vector3.SquareRoot(v3_1);

            m3x3 = new Matrix3x3();
            v3_1 = v3_1.Multiply(m3x3);

            m4x4 = new Matrix4x4();
            v3_1 = v3_1.Multiply(m4x4);
            v3_1 = v3_1.TransformNoTranslate(m4x4);
            v3_1 = v3_1.Transform(new Quaternion());
            #endregion

            #region All Matrix4x4 Methods
            isItTrueThat = m4x4.IsProjectiveTransform;
            value = m4x4.M11;
            value = m4x4.M12;
            value = m4x4.M13;
            value = m4x4.M14;
            value = m4x4.M21;
            value = m4x4.M22;
            value = m4x4.M23;
            value = m4x4.M24;
            value = m4x4.M31;
            value = m4x4.M32;
            value = m4x4.M33;
            value = m4x4.M34;
            value = m4x4.M41;
            value = m4x4.M42;
            value = m4x4.M43;
            value = m4x4.M44;
            m4x4 = Matrix4x4.Identity;
            m4x4 = Matrix4x4.Null;
            m4x4 = new Matrix4x4(m3x3);
            isItTrueThat = m4x4.IsIdentity();
            isItTrueThat = m4x4.IsNull();
            Vector3 t3 = m4x4.TranslationAsVector;
            m4x4 = Matrix4x4.CreateBillboard(v3_1, v3_1, v3_1, v3_1);
            m4x4 = Matrix4x4.CreateConstrainedBillboard(v3_1, v3_1, v3_1, v3_1, v3_1);


            m4x4 = Matrix4x4.CreateTranslation(t3);
            m4x4 = Matrix4x4.CreateTranslation(x, y, z);
            m4x4 = Matrix4x4.CreateScale(1.0);
            m4x4 = Matrix4x4.CreateScale(x, y, z);
            m4x4 = Matrix4x4.CreateScale(v3_1);
            m4x4 = Matrix4x4.CreateScale(v3_1, v3_2); //vOther is the center of scaling
            m4x4 = Matrix4x4.CreateScale(x, y, z, v3_2); //vOther is the center of scaling
            m4x4 = Matrix4x4.CreateRotationX(1.0); //in radians
            m4x4 = Matrix4x4.CreateRotationX(1.0, v3_2); //vOther is the center of rotate
            m4x4 = Matrix4x4.CreateRotationY(1.0); //in radians
            m4x4 = Matrix4x4.CreateRotationY(1.0, v3_2); //vOther is the center of rotate
            m4x4 = Matrix4x4.CreateRotationZ(1.0); //in radians
            m4x4 = Matrix4x4.CreateRotationZ(1.0, v3_2); //vOther is the center of rotate
            m4x4 = Matrix4x4.CreateFromAxisAngle(v3_2, 1.0); //vOther is the center of rotate
            m4x4 = Matrix4x4.CreatePerspectiveFieldOfView(1.0, 2.0, 3.0, 4.0);
            m4x4 = Matrix4x4.CreatePerspective(1.0, 1.0, 1.0, 1.0);
            m4x4 = Matrix4x4.CreatePerspectiveOffCenter(1.0, 2.0, 3.0, 4.0, 5.0, 6.0);
            m4x4 = Matrix4x4.CreateOrthographic(1.0, 2.0, 3.0, 4.0);
            m4x4 = Matrix4x4.CreateOrthographicOffCenter(1.0, 2.0, 3.0, 4.0, 5.0, 6.0);
            m4x4 = Matrix4x4.CreateLookAt(v3_1, v3_1, v3_2);
            m4x4 = Matrix4x4.CreateWorld(v3_1, v3_1, v3_2);
            m4x4 = Matrix4x4.CreateFromYawPitchRoll(1.0, 2.0, 3.0);
            m4x4 = Matrix4x4.CreateFromQuaternion(new Quaternion());
            m4x4 = Matrix4x4.CreateShadow(v3_1, new Plane(d, v3_2));
            m4x4 = Matrix4x4.CreateReflection(new Plane(d, v3_2));

            m4x4 = m4x4.Transpose();
            isItTrueThat = Matrix4x4.Invert(m4x4, out Matrix4x4 invm4x4);
            d = m4x4.GetDeterminant();
            isItTrueThat = m4x4.Decompose(out var scale, out var rotQ, out var trans3);

            m4x4 = m4x4.Transform(rotQ);

            m4x4 = Matrix4x4.Lerp(m4x4, m4x4, 0.5);
            m4x4 = -m4x4;
            var m4x4Another = 4.0 * m4x4;
            m4x4 = m4x4 + m4x4;
            m4x4 = m4x4 - m4x4;
            m4x4 = m4x4 * m4x4;

            isItTrueThat = m4x4 == m4x4Another;
            isItTrueThat = m4x4 != m4x4Another;
            #endregion

            #region All Quaternion Methods
            var quat = new Quaternion();
            x = quat.X;
            y = quat.Y;
            z = quat.Z;
            var w = quat.W;
            var quatOther = new Quaternion(v3_1, w);
            quat = new Quaternion(x, y, z, w);
            quat = Quaternion.Identity;
            isItTrueThat = quat.IsIdentity();
            quat = Quaternion.Null;
            isItTrueThat = quat.IsNull();
            length = quat.Length();
            length = quat.LengthSquared();
            quat = quat.Normalize();
            quat = quat.Conjugate();
            quat = quat.Inverse();
            quat = Quaternion.CreateFromAxisAngle(v3_1, d);
            quat = Quaternion.CreateFromYawPitchRoll(1.0, 2.0, 3.0);
            quat = Quaternion.CreateFromRotationMatrix(m4x4);
            dot = quat.Dot(quat);
            quat = Quaternion.Lerp(quat, quat, 0.5);
            quat = Quaternion.Slerp(quat, quat, 0.5);
            quat = -quat;
            quat = quat + quat;
            quat = quat - quat;
            quat = quat * quat;
            quat = 4.0 * quat;
            quat = quat / quat;
            isItTrueThat = quat == quatOther;
            isItTrueThat = quat != quatOther;
            #endregion

            #region All Plane Methods
            var plane = new Plane();
            plane = new Plane(d, v3_1);
            var planeOther = new Plane(d, new Vector3(x, y, z));
            v3_1 = plane.Normal;
            d = plane.DistanceToOrigin;
            plane = Plane.CreateFromVertices(v3_1, unitVector3X, v3_2);
             plane.Normalize();
            plane.Transform(m4x4);
            plane.Transform(quat);
            dot = plane.DotCoordinate(v3_1);
            dot = plane.DotNormal(v3_1);
            isItTrueThat = plane == planeOther;
            isItTrueThat = plane != planeOther;
            #endregion

            #region IEnumerable<double> Statistics
            IEnumerable<double> numbers = new[] { 1.1, 2.2, 3.3 };
            var mean = numbers.Mean();
            var median = numbers.Median();
            var nrmse = numbers.NormalizedRootMeanSquareError();
            var nthMedian = numbers.NthOrderStatistic(3);
            var varMean = numbers.VarianceFromMean(mean);
            var varMedian = numbers.VarianceFromMedian(median);
            #endregion
        }
    }
}