// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;


namespace PointCloud.Numerics
{
    /// <summary>Represents a 3x2 matrix.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// [!INCLUDE[vectors-are-rows-paragraph](~/includes/system-numerics-vectors-are-rows.md)]
    /// ]]></format></remarks>

    public partial struct Matrix3x3 : IEquatable<Matrix3x3>
    {
        // In an ideal world, we'd have 3x Vector2 fields. However, Matrix3x3 was shipped with
        // 6x public double fields and as such we cannot change the "backing" fields without it being
        // a breaking change. Likewise, we cannot switch to using something like ExplicitLayout
        // without it pessimizing other parts of the JIT and still preventing things like field promotion.
        //
        // This nested Impl struct works around this problem by relying on the JIT treating same sizeof
        // value type bitcasts as a no-op. Effectively the entire implementation is here in this type
        // and the public facing Matrix3x3 just defers to it with simple reinterpret casts inserted
        // at the relevant points.

        #region Public Fields
        /// <summary>
        /// The first element of the first row. This is the x-scaling term.
        /// </summary>
        /// <value>The M11.</value>
        public double M11 { get; }
        /// <summary>
        /// The second element of the first row. This is the y-skewing term.
        /// </summary>
        /// <value>The M12.</value>
        public double M12 { get; }
        /// <summary>
        /// The first element of the second row. This is the x-skewing term.
        /// </summary>
        /// <value>The M21.</value>
        public double M21 { get; }
        /// <summary>
        /// The second element of the second row. This is the y-scaling term.
        /// </summary>
        /// <value>The M22.</value>
        public double M22 { get; }
        /// <summary>
        /// The first element of the third row. This is the x-translation term.
        /// </summary>
        /// <value>The M31.</value>
        public double M31 { get; }
        /// <summary>
        /// The second element of the third row. This is the y-translation term.
        /// </summary>
        /// <value>The M32.</value>
        public double M32 { get; }

        // Now the Projective Transform terms
        /// <summary>
        /// Gets a value indicating whether this instance is projective transform. This means that the third
        /// column has non-trivia values MX3 are nonzero or M33 is not unity (1).
        /// </summary>
        /// <value><c>true</c> if this instance is projective transform; otherwise, <c>false</c>.</value>
        public bool IsProjectiveTransform { get; }
        /// <summary>
        /// The third element of the first row. This is the x-projective term.
        /// </summary>
        /// <value>The M13.</value>
        public double M13 { get; }
        /// <summary>
        /// The third element of the second row. This is the y-projective term.
        /// </summary>
        /// <value>The M23.</value>
        public double M23 { get; }
        /// <summary>
        /// The third element of the third row. This is the global scaling term.
        /// </summary>
        /// <value>The M33.</value>
        public double M33 { get; }
        #endregion Public Fields

        /// <summary>Creates a 3x2 matrix from the specified components.</summary>
        /// <param name="m11">The value to assign to the first element in the first row.</param>
        /// <param name="m12">The value to assign to the second element in the first row.</param>
        /// <param name="m21">The value to assign to the first element in the second row.</param>
        /// <param name="m22">The value to assign to the second element in the second row.</param>
        /// <param name="m31">The value to assign to the first element in the third row.</param>
        /// <param name="m32">The value to assign to the second element in the third row.</param>
        public Matrix3x3(double m11, double m12,
                         double m21, double m22,
                         double m31, double m32)
        {
            Unsafe.SkipInit(out this);

            AsImpl().Init(
                m11, m12,
                m21, m22,
                m31, m32
            );
        }

        /// <summary>
        /// Constructs a Matrix3x3 from the given components.
        /// </summary>
        /// <param name="m11">The M11.</param>
        /// <param name="m12">The M12.</param>
        /// <param name="m13">The M13.</param>
        /// <param name="m21">The M21.</param>
        /// <param name="m22">The M22.</param>
        /// <param name="m23">The M23.</param>
        /// <param name="m31">The M31.</param>
        /// <param name="m32">The M32.</param>
        /// <param name="m33">The M33.</param>
        public Matrix3x3(double m11, double m12, double m13,
                         double m21, double m22, double m23,
                         double m31, double m32, double m33)
        {
            this.M11 = m11;
            this.M12 = m12;
            this.M21 = m21;
            this.M22 = m22;
            this.M31 = m31;
            this.M32 = m32;

            if (m13.IsNegligible() && m23.IsNegligible() && m33.IsPracticallySame(1.0))
            {
                IsProjectiveTransform = false;
                this.M13 = 0.0;
                this.M23 = 0.0;
                this.M33 = 1.0;
            }
            else
            {
                IsProjectiveTransform = true;
                this.M13 = m13;
                this.M23 = m23;
                this.M33 = m33;
            }
        }

        /// <summary>Gets the multiplicative identity matrix.</summary>
        /// <value>The multiplicative identify matrix.</value>
        public static Matrix3x3 Identity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Impl.Identity.AsM3x2();
        }

        /// <summary>Gets or sets the element at the specified indices.</summary>
        /// <param name="row">The index of the row containing the element to get or set.</param>
        /// <param name="column">The index of the column containing the element to get or set.</param>
        /// <returns>The element at [<paramref name="row" />][<paramref name="column" />].</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="row" /> was less than zero or greater than the number of rows.
        /// -or-
        /// <paramref name="column" /> was less than zero or greater than the number of columns.
        /// </exception>
        public double this[int row, int column]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => AsROImpl()[row, column];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => AsImpl()[row, column] = value;
        }

        /// <summary>Gets a value that indicates whether the current matrix is the identity matrix.</summary>
        /// <value><see langword="true" /> if the current matrix is the identity matrix; otherwise, <see langword="false" />.</value>
        public readonly bool IsIdentity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => AsROImpl().IsIdentity;
        }

        /// <summary>Gets or sets the translation component of this matrix.</summary>
        /// <value>The translation component of the current instance.</value>
        public Vector2 Translation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => AsROImpl().Translation;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => AsImpl().Translation = value;
        }

        /// <summary>Adds each element in one matrix with its corresponding element in a second matrix.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The matrix that contains the summed values.</returns>
        /// <remarks>The <see cref="op_Addition" /> method defines the operation of the addition operator for <see cref="Matrix3x3" /> objects.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 operator +(Matrix3x3 value1, Matrix3x3 value2)
            => (value1.AsImpl() + value2.AsImpl()).AsM3x2();

        /// <summary>Returns a value that indicates whether the specified matrices are equal.</summary>
        /// <param name="value1">The first matrix to compare.</param>
        /// <param name="value2">The second matrix to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="value1" /> and <paramref name="value2" /> are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two matrices are equal if all their corresponding elements are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Matrix3x3 value1, Matrix3x3 value2)
            => value1.AsImpl() == value2.AsImpl();

        /// <summary>Returns a value that indicates whether the specified matrices are not equal.</summary>
        /// <param name="value1">The first matrix to compare.</param>
        /// <param name="value2">The second matrix to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="value1" /> and <paramref name="value2" /> are not equal; otherwise, <see langword="false" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Matrix3x3 value1, Matrix3x3 value2)
            => value1.AsImpl() != value2.AsImpl();

        /// <summary>Multiplies two matrices together to compute the product.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The product matrix.</returns>
        /// <remarks>The <see cref="Matrix3x3.op_Multiply" /> method defines the operation of the multiplication operator for <see cref="Matrix3x3" /> objects.</remarks>
        public static Matrix3x3 operator *(Matrix3x3 value1, Matrix3x3 value2)
            => (value1.AsImpl() * value2.AsImpl()).AsM3x2();

        /// <summary>Multiplies a matrix by a double to compute the product.</summary>
        /// <param name="value1">The matrix to scale.</param>
        /// <param name="value2">The scaling value to use.</param>
        /// <returns>The scaled matrix.</returns>
        /// <remarks>The <see cref="Matrix3x3.op_Multiply" /> method defines the operation of the multiplication operator for <see cref="Matrix3x3" /> objects.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 operator *(Matrix3x3 value1, double value2)
            => (value1.AsImpl() * value2).AsM3x2();

        /// <summary>Subtracts each element in a second matrix from its corresponding element in a first matrix.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The matrix containing the values that result from subtracting each element in <paramref name="value2" /> from its corresponding element in <paramref name="value1" />.</returns>
        /// <remarks>The <see cref="Subtract" /> method defines the operation of the subtraction operator for <see cref="Matrix3x3" /> objects.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 operator -(Matrix3x3 value1, Matrix3x3 value2)
            => (value1.AsImpl() - value2.AsImpl()).AsM3x2();

        /// <summary>Negates the specified matrix by multiplying all its values by -1.</summary>
        /// <param name="value">The matrix to negate.</param>
        /// <returns>The negated matrix.</returns>
        /// <altmember cref="Negate(Matrix3x3)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 operator -(Matrix3x3 value)
            => (-value.AsImpl()).AsM3x2();

        /// <summary>Adds each element in one matrix with its corresponding element in a second matrix.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The matrix that contains the summed values of <paramref name="value1" /> and <paramref name="value2" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 Add(Matrix3x3 value1, Matrix3x3 value2)
            => (value1.AsImpl() + value2.AsImpl()).AsM3x2();

        /// <summary>Creates a rotation matrix using the given rotation in radians.</summary>
        /// <param name="radians">The amount of rotation, in radians.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix3x3 CreateRotation(double radians)
            => Impl.CreateRotation(radians).AsM3x2();

        /// <summary>Creates a rotation matrix using the specified rotation in radians and a center point.</summary>
        /// <param name="radians">The amount of rotation, in radians.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix3x3 CreateRotation(double radians, Vector2 centerPoint)
            => Impl.CreateRotation(radians, centerPoint).AsM3x2();

        /// <summary>Creates a scaling matrix from the specified vector scale.</summary>
        /// <param name="scales">The scale to use.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix3x3 CreateScale(Vector2 scales)
            => Impl.CreateScale(scales).AsM3x2();

        /// <summary>Creates a scaling matrix from the specified X and Y components.</summary>
        /// <param name="xScale">The value to scale by on the X axis.</param>
        /// <param name="yScale">The value to scale by on the Y axis.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix3x3 CreateScale(double xScale, double yScale)
            => Impl.CreateScale(xScale, yScale).AsM3x2();

        /// <summary>Creates a scaling matrix that is offset by a given center point.</summary>
        /// <param name="xScale">The value to scale by on the X axis.</param>
        /// <param name="yScale">The value to scale by on the Y axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix3x3 CreateScale(double xScale, double yScale, Vector2 centerPoint)
            => Impl.CreateScale(xScale, yScale, centerPoint).AsM3x2();

        /// <summary>Creates a scaling matrix from the specified vector scale with an offset from the specified center point.</summary>
        /// <param name="scales">The scale to use.</param>
        /// <param name="centerPoint">The center offset.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix3x3 CreateScale(Vector2 scales, Vector2 centerPoint)
            => Impl.CreateScale(scales, centerPoint).AsM3x2();

        /// <summary>Creates a scaling matrix that scales uniformly with the given scale.</summary>
        /// <param name="scale">The uniform scale to use.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix3x3 CreateScale(double scale)
            => Impl.CreateScale(scale).AsM3x2();

        /// <summary>Creates a scaling matrix that scales uniformly with the specified scale with an offset from the specified center.</summary>
        /// <param name="scale">The uniform scale to use.</param>
        /// <param name="centerPoint">The center offset.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix3x3 CreateScale(double scale, Vector2 centerPoint)
            => Impl.CreateScale(scale, centerPoint).AsM3x2();

        /// <summary>Creates a skew matrix from the specified angles in radians.</summary>
        /// <param name="radiansX">The X angle, in radians.</param>
        /// <param name="radiansY">The Y angle, in radians.</param>
        /// <returns>The skew matrix.</returns>
        public static Matrix3x3 CreateSkew(double radiansX, double radiansY)
            => Impl.CreateSkew(radiansX, radiansY).AsM3x2();

        /// <summary>Creates a skew matrix from the specified angles in radians and a center point.</summary>
        /// <param name="radiansX">The X angle, in radians.</param>
        /// <param name="radiansY">The Y angle, in radians.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The skew matrix.</returns>
        public static Matrix3x3 CreateSkew(double radiansX, double radiansY, Vector2 centerPoint)
            => Impl.CreateSkew(radiansX, radiansY, centerPoint).AsM3x2();

        /// <summary>Creates a translation matrix from the specified 2-dimensional vector.</summary>
        /// <param name="position">The translation position.</param>
        /// <returns>The translation matrix.</returns>
        public static Matrix3x3 CreateTranslation(Vector2 position)
            => Impl.CreateTranslation(position).AsM3x2();

        /// <summary>Creates a translation matrix from the specified X and Y components.</summary>
        /// <param name="xPosition">The X position.</param>
        /// <param name="yPosition">The Y position.</param>
        /// <returns>The translation matrix.</returns>
        public static Matrix3x3 CreateTranslation(double xPosition, double yPosition)
            => Impl.CreateTranslation(xPosition, yPosition).AsM3x2();

        /// <summary>Tries to invert the specified matrix. The return value indicates whether the operation succeeded.</summary>
        /// <param name="matrix">The matrix to invert.</param>
        /// <param name="result">When this method returns, contains the inverted matrix if the operation succeeded.</param>
        /// <returns><see langword="true" /> if <paramref name="matrix" /> was converted successfully; otherwise,  <see langword="false" />.</returns>
        public static bool Invert(Matrix3x3 matrix, out Matrix3x3 result)
        {
            Unsafe.SkipInit(out result);
            return Impl.Invert(in matrix.AsImpl(), out result.AsImpl());
        }

        /// <summary>Performs a linear interpolation from one matrix to a second matrix based on a value that specifies the weighting of the second matrix.</summary>
        /// <param name="matrix1">The first matrix.</param>
        /// <param name="matrix2">The second matrix.</param>
        /// <param name="amount">The relative weighting of <paramref name="matrix2" />.</param>
        /// <returns>The interpolated matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 Lerp(Matrix3x3 matrix1, Matrix3x3 matrix2, double amount)
            => Impl.Lerp(in matrix1.AsImpl(), in matrix2.AsImpl(), amount).AsM3x2();

        /// <summary>Multiplies two matrices together to compute the product.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The product matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 Multiply(Matrix3x3 value1, Matrix3x3 value2)
            => (value1.AsImpl() * value2.AsImpl()).AsM3x2();

        /// <summary>Multiplies a matrix by a double to compute the product.</summary>
        /// <param name="value1">The matrix to scale.</param>
        /// <param name="value2">The scaling value to use.</param>
        /// <returns>The scaled matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 Multiply(Matrix3x3 value1, double value2)
            => (value1.AsImpl() * value2).AsM3x2();

        /// <summary>Negates the specified matrix by multiplying all its values by -1.</summary>
        /// <param name="value">The matrix to negate.</param>
        /// <returns>The negated matrix.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 Negate(Matrix3x3 value)
            => (-value.AsImpl()).AsM3x2();

        /// <summary>Subtracts each element in a second matrix from its corresponding element in a first matrix.</summary>
        /// <param name="value1">The first matrix.</param>
        /// <param name="value2">The second matrix.</param>
        /// <returns>The matrix containing the values that result from subtracting each element in <paramref name="value2" /> from its corresponding element in <paramref name="value1" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 Subtract(Matrix3x3 value1, Matrix3x3 value2)
            => (value1.AsImpl() - value2.AsImpl()).AsM3x2();

        /// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true" /> if the current instance and <paramref name="obj" /> are equal; otherwise, <see langword="false" />. If <paramref name="obj" /> is <see langword="null" />, the method returns <see langword="false" />.</returns>
        /// <remarks>The current instance and <paramref name="obj" /> are equal if <paramref name="obj" /> is a <see cref="Matrix3x3" /> object and the corresponding elements of each matrix are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals([NotNullWhen(true)] object? obj)
            => AsROImpl().Equals(obj);

        /// <summary>Returns a value that indicates whether this instance and another 3x2 matrix are equal.</summary>
        /// <param name="other">The other matrix.</param>
        /// <returns><see langword="true" /> if the two matrices are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two matrices are equal if all their corresponding elements are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Matrix3x3 other)
            => AsROImpl().Equals(in other.AsImpl());

        /// <summary>Calculates the determinant for this matrix.</summary>
        /// <returns>The determinant.</returns>
        /// <remarks>The determinant is calculated by expanding the matrix with a third column whose values are (0,0,1).</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly double GetDeterminant()
            => AsROImpl().GetDeterminant();

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>The hash code.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly int GetHashCode()
            => AsROImpl().GetHashCode();

        /// <summary>Returns a string that represents this matrix.</summary>
        /// <returns>The string representation of this matrix.</returns>
        /// <remarks>The numeric values in the returned string are formatted by using the conventions of the current culture. For example, for the en-US culture, the returned string might appear as <c>{ {M11:1.1 M12:1.2} {M21:2.1 M22:2.2} {M31:3.1 M32:3.2} }</c>.</remarks>
        public override readonly string ToString()
            => $"{{ {{M11:{M11} M12:{M12}}} {{M21:{M21} M22:{M22}}} {{M31:{M31} M32:{M32}}} }}";


        #region Solve Ax=b
        /// <summary>
        /// Solves for the value x in Ax=b. This is also represented as the
        /// backslash operation
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <param name="b">The b.</param>
        /// <returns>Vector4.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Solve(Vector3 b)
        {
            if (!Invert(this, out var invert))
                return Vector3.NaN;
            return invert.Multiply(b);
        }
        #endregion


    }
}
