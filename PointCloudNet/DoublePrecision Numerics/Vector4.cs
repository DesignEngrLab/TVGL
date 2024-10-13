// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using System.Runtime.Intrinsics;

namespace PointCloud.Numerics
{
    /// <summary>Represents a vector with four single-precision floating-point values.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// The <xref:System.Numerics.Vector4> structure provides support for hardware acceleration.
    /// [!INCLUDE[vectors-are-rows-paragraph](~/includes/system-numerics-vectors-are-rows.md)]
    /// ]]></format></remarks>

    public partial struct Vector4 : IEquatable<Vector4>, IFormattable
    {
        /// <summary>The X component of the vector.</summary>
        public double X;

        /// <summary>The Y component of the vector.</summary>
        public double Y;

        /// <summary>The Z component of the vector.</summary>
        public double Z;

        /// <summary>The W component of the vector.</summary>
        public double W;

        internal const int Count = 4;

        /// <summary>Creates a new <see cref="Vector4" /> object whose four elements have the same value.</summary>
        /// <param name="value">The value to assign to all four elements.</param>
    
        public Vector4(double value)
        {
            this = Create(value);
        }

        /// <summary>Creates a   new <see cref="Vector4" /> object from the specified <see cref="Vector2" /> object and a Z and a W component.</summary>
        /// <param name="value">The vector to use for the X and Y components.</param>
        /// <param name="z">The Z component.</param>
        /// <param name="w">The W component.</param>
    
        public Vector4(Vector2 value, double z, double w)
        {
            this = Create(value, z, w);
        }

        /// <summary>Constructs a new <see cref="Vector4" /> object from the specified <see cref="Vector3" /> object and a W component.</summary>
        /// <param name="value">The vector to use for the X, Y, and Z components.</param>
        /// <param name="w">The W component.</param>
    
        public Vector4(Vector3 value, double w)
        {
            this = Create(value, w);
        }

        /// <summary>Creates a vector whose elements have the specified values.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <param name="y">The value to assign to the <see cref="Y" /> field.</param>
        /// <param name="z">The value to assign to the <see cref="Z" /> field.</param>
        /// <param name="w">The value to assign to the <see cref="W" /> field.</param>
    
        public Vector4(double x, double y, double z, double w)
        {
            this = Create(x, y, z, w);
        }

        /// <summary>Constructs a vector from the given <see cref="ReadOnlySpan{Double}" />. The span must contain at least 4 elements.</summary>
        /// <param name="values">The span of elements to assign to the vector.</param>
    
        public Vector4(ReadOnlySpan<double> values)
        {
            this = Create(values);
        }

        /// <summary>Gets a vector whose elements are equal to <see cref="double.E" />.</summary>
        /// <value>A vector whose elements are equal to <see cref="double.E" /> (that is, it returns the vector <c>Create(double.E)</c>).</value>
        public static Vector4 E
        {
        
            get => Create(double.E);
        }

        /// <summary>Gets a vector whose elements are equal to <see cref="double.Epsilon" />.</summary>
        /// <value>A vector whose elements are equal to <see cref="double.Epsilon" /> (that is, it returns the vector <c>Create(double.Epsilon)</c>).</value>
        public static Vector4 Epsilon
        {
        
            get => Create(double.Epsilon);
        }

        /// <summary>Gets a vector whose elements are equal to <see cref="double.NaN" />.</summary>
        /// <value>A vector whose elements are equal to <see cref="double.NaN" /> (that is, it returns the vector <c>Create(double.NaN)</c>).</value>
        public static Vector4 NaN
        {
        
            get => Create(double.NaN);
        }

        /// <summary>Gets a vector whose elements are equal to <see cref="double.NegativeInfinity" />.</summary>
        /// <value>A vector whose elements are equal to <see cref="double.NegativeInfinity" /> (that is, it returns the vector <c>Create(double.NegativeInfinity)</c>).</value>
        public static Vector4 NegativeInfinity
        {
        
            get => Create(double.NegativeInfinity);
        }

        /// <summary>Gets a vector whose elements are equal to <see cref="double.NegativeZero" />.</summary>
        /// <value>A vector whose elements are equal to <see cref="double.NegativeZero" /> (that is, it returns the vector <c>Create(double.NegativeZero)</c>).</value>
        public static Vector4 NegativeZero
        {
        
            get => Create(double.NegativeZero);
        }

        /// <summary>Gets a vector whose elements are equal to one.</summary>
        /// <value>A vector whose elements are equal to one (that is, it returns the vector <c>Create(1)</c>).</value>
        public static Vector4 One
        {
        
            get => Create(1);
        }

        /// <summary>Gets a vector whose elements are equal to <see cref="double.Pi" />.</summary>
        /// <value>A vector whose elements are equal to <see cref="double.Pi" /> (that is, it returns the vector <c>Create(double.Pi)</c>).</value>
        public static Vector4 Pi
        {
        
            get => Create(double.Pi);
        }

        /// <summary>Gets a vector whose elements are equal to <see cref="double.PositiveInfinity" />.</summary>
        /// <value>A vector whose elements are equal to <see cref="double.PositiveInfinity" /> (that is, it returns the vector <c>Create(double.PositiveInfinity)</c>).</value>
        public static Vector4 PositiveInfinity
        {
        
            get => Create(double.PositiveInfinity);
        }

        /// <summary>Gets a vector whose elements are equal to <see cref="double.Tau" />.</summary>
        /// <value>A vector whose elements are equal to <see cref="double.Tau" /> (that is, it returns the vector <c>Create(double.Tau)</c>).</value>
        public static Vector4 Tau
        {
        
            get => Create(double.Tau);
        }

        /// <summary>Gets the vector (1,0,0,0).</summary>
        /// <value>The vector <c>(1,0,0,0)</c>.</value>
        public static Vector4 UnitX
        {
        
            get => CreateScalar(1.0f);
        }

        /// <summary>Gets the vector (0,1,0,0).</summary>
        /// <value>The vector <c>(0,1,0,0)</c>.</value>
        public static Vector4 UnitY
        {
        
            get => Create(0.0f, 1.0f, 0.0f, 0.0f);
        }

        /// <summary>Gets the vector (0,0,1,0).</summary>
        /// <value>The vector <c>(0,0,1,0)</c>.</value>
        public static Vector4 UnitZ
        {
        
            get => Create(0.0f, 0.0f, 1.0f, 0.0f);
        }

        /// <summary>Gets the vector (0,0,0,1).</summary>
        /// <value>The vector <c>(0,0,0,1)</c>.</value>
        public static Vector4 UnitW
        {
        
            get => Create(0.0f, 0.0f, 0.0f, 1.0f);
        }

        /// <summary>Gets a vector whose elements are equal to zero.</summary>
        /// <value>A vector whose elements are equal to zero (that is, it returns the vector <c>Create(0)</c>).</value>
        public static Vector4 Zero
        {
        
            get => default;
        }

        /// <summary>Gets or sets the element at the specified index.</summary>
        /// <param name="index">The index of the element to get or set.</param>
        /// <returns>The the element at <paramref name="index" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        public double this[int index]
        {
        
            readonly get => this.AsVector256().GetElement(index);

        
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                this = this.AsVector256().WithElement(index, value).AsVector4();
            }
        }

        /// <summary>Adds two vectors together.</summary>
        /// <param name="left">The first vector to add.</param>
        /// <param name="right">The second vector to add.</param>
        /// <returns>The summed vector.</returns>
        /// <remarks>The <see cref="op_Addition" /> method defines the addition operation for <see cref="Vector4" /> objects.</remarks>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator +(Vector4 left, Vector4 right) => (left.AsVector256() + right.AsVector256()).AsVector4();

        /// <summary>Divides the first vector by the second.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The vector that results from dividing <paramref name="left" /> by <paramref name="right" />.</returns>
        /// <remarks>The <see cref="Vector4.op_Division" /> method defines the division operation for <see cref="Vector4" /> objects.</remarks>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator /(Vector4 left, Vector4 right) => (left.AsVector256() / right.AsVector256()).AsVector4();

        /// <summary>Divides the specified vector by a specified scalar value.</summary>
        /// <param name="value1">The vector.</param>
        /// <param name="value2">The scalar value.</param>
        /// <returns>The result of the division.</returns>
        /// <remarks>The <see cref="Vector4.op_Division" /> method defines the division operation for <see cref="Vector4" /> objects.</remarks>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator /(Vector4 value1, double value2) => (value1.AsVector256() / value2).AsVector4();

        /// <summary>Returns a value that indicates whether each pair of elements in two specified vectors is equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two <see cref="Vector4" /> objects are equal if each element in <paramref name="left" /> is equal to the corresponding element in <paramref name="right" />.</remarks>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector4 left, Vector4 right) => left.AsVector256() == right.AsVector256();

        /// <summary>Returns a value that indicates whether two specified vectors are not equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, <see langword="false" />.</returns>
    
        public static bool operator !=(Vector4 left, Vector4 right) => !(left == right);

        /// <summary>Returns a new vector whose values are the product of each pair of elements in two specified vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The element-wise product vector.</returns>
        /// <remarks>The <see cref="Vector4.op_Multiply" /> method defines the multiplication operation for <see cref="Vector4" /> objects.</remarks>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator *(Vector4 left, Vector4 right) => (left.AsVector256() * right.AsVector256()).AsVector4();

        /// <summary>Multiplies the specified vector by the specified scalar value.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        /// <remarks>The <see cref="Vector4.op_Multiply" /> method defines the multiplication operation for <see cref="Vector4" /> objects.</remarks>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator *(Vector4 left, double right) => (left.AsVector256() * right).AsVector4();

        /// <summary>Multiplies the scalar value by the specified vector.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        /// <remarks>The <see cref="Vector4.op_Multiply" /> method defines the multiplication operation for <see cref="Vector4" /> objects.</remarks>
    
        public static Vector4 operator *(double left, Vector4 right) => right * left;

        /// <summary>Subtracts the second vector from the first.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The vector that results from subtracting <paramref name="right" /> from <paramref name="left" />.</returns>
        /// <remarks>The <see cref="op_Subtraction" /> method defines the subtraction operation for <see cref="Vector4" /> objects.</remarks>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator -(Vector4 left, Vector4 right) => (left.AsVector256() - right.AsVector256()).AsVector4();

        /// <summary>Negates the specified vector.</summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>The negated vector.</returns>
        /// <remarks>The <see cref="op_UnaryNegation" /> method defines the unary negation operation for <see cref="Vector4" /> objects.</remarks>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator -(Vector4 value) => (-value.AsVector256()).AsVector4();

        /// <summary>Returns a vector whose elements are the absolute values of each of the specified vector's elements.</summary>
        /// <param name="value">A vector.</param>
        /// <returns>The absolute value vector.</returns>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Abs(Vector4 value) => Vector256.Abs(value.AsVector256()).AsVector4();

        /// <summary>Adds two vectors together.</summary>
        /// <param name="left">The first vector to add.</param>
        /// <param name="right">The second vector to add.</param>
        /// <returns>The summed vector.</returns>
    
        public static Vector4 Add(Vector4 left, Vector4 right) => left + right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Clamp(TSelf, TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Clamp(Vector4 value1, Vector4 min, Vector4 max) => Vector256.Clamp(value1.AsVector256(), min.AsVector256(), max.AsVector256()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ClampNative(TSelf, TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ClampNative(Vector4 value1, Vector4 min, Vector4 max) => Vector256.ClampNative(value1.AsVector256(), min.AsVector256(), max.AsVector256()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopySign(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 CopySign(Vector4 value, Vector4 sign) => Vector256.CopySign(value.AsVector256(), sign.AsVector256()).AsVector4();

        /// <inheritdoc cref="Vector256.Cos(Vector256{double})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Cos(Vector4 vector) => Vector256.Cos(vector.AsVector256()).AsVector4();

        /// <summary>Creates a new <see cref="Vector4" /> object whose four elements have the same value.</summary>
        /// <param name="value">The value to assign to all four elements.</param>
        /// <returns>A new <see cref="Vector4" /> whose four elements have the same value.</returns>
    
        public static Vector4 Create(double value) => Vector256.Create(value).AsVector4();

        /// <summary>Creates a new <see cref="Vector4" /> object from the specified <see cref="Vector2" /> object and a Z and a W component.</summary>
        /// <param name="vector">The vector to use for the X and Y components.</param>
        /// <param name="z">The Z component.</param>
        /// <param name="w">The W component.</param>
        /// <returns>A new <see cref="Vector4" /> from the specified <see cref="Vector2" /> object and a Z and a W component.</returns>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Create(Vector2 vector, double z, double w)
        {
            return vector.AsVector256Unsafe()
                         .WithElement(2, z)
                         .WithElement(3, w)
                         .AsVector4();
        }

        /// <summary>Constructs a new <see cref="Vector4" /> object from the specified <see cref="Vector3" /> object and a W component.</summary>
        /// <param name="vector">The vector to use for the X, Y, and Z components.</param>
        /// <param name="w">The W component.</param>
        /// <returns>A new <see cref="Vector4" /> from the specified <see cref="Vector3" /> object and a W component.</returns>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Create(Vector3 vector, double w)
        {
            return vector.AsVector256Unsafe()
                         .WithElement(3, w)
                         .AsVector4();
        }

        /// <summary>Creates a vector whose elements have the specified values.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <param name="y">The value to assign to the <see cref="Y" /> field.</param>
        /// <param name="z">The value to assign to the <see cref="Z" /> field.</param>
        /// <param name="w">The value to assign to the <see cref="W" /> field.</param>
        /// <returns>A new <see cref="Vector4" /> whose elements have the specified values.</returns>
    
        public static Vector4 Create(double x, double y, double z, double w) => Vector256.Create(x, y, z, w).AsVector4();

        /// <summary>Constructs a vector from the given <see cref="ReadOnlySpan{Double}" />. The span must contain at least 4 elements.</summary>
        /// <param name="values">The span of elements to assign to the vector.</param>
        /// <returns>A new <see cref="Vector4" /> whose elements have the specified values.</returns>
    
        public static Vector4 Create(ReadOnlySpan<double> values) => Vector256.Create(values).AsVector4();

        /// <summary>Creates a vector with <see cref="X" /> initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <returns>A new <see cref="Vector4" /> with <see cref="X" /> initialized <paramref name="x" /> and the remaining elements initialized to zero.</returns>
    
        internal static Vector4 CreateScalar(double x) => Vector256.CreateScalar(x).AsVector4();

        /// <summary>Creates a vector with <see cref="X" /> initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <returns>A new <see cref="Vector4" /> with <see cref="X" /> initialized <paramref name="x" /> and the remaining elements left uninitialized.</returns>
    
        internal static Vector4 CreateScalarUnsafe(double x) => Vector256.CreateScalarUnsafe(x).AsVector4();

        /// <inheritdoc cref="Vector256.DegreesToRadians(Vector256{double})" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 DegreesToRadians(Vector4 degrees) => Vector256.DegreesToRadians(degrees.AsVector256()).AsVector4();

        /// <summary>Computes the Euclidean distance between the two given points.</summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance.</returns>
    
        public static double Distance(Vector4 value1, Vector4 value2) => double.Sqrt(DistanceSquared(value1, value2));

        /// <summary>Returns the Euclidean distance squared between two specified points.</summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance squared.</returns>
    
        public static double DistanceSquared(Vector4 value1, Vector4 value2) => (value1 - value2).LengthSquared();

        /// <summary>Divides the first vector by the second.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The vector resulting from the division.</returns>
    
        public static Vector4 Divide(Vector4 left, Vector4 right) => left / right;

        /// <summary>Divides the specified vector by a specified scalar value.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="divisor">The scalar value.</param>
        /// <returns>The vector that results from the division.</returns>
    
        public static Vector4 Divide(Vector4 left, double divisor) => left / divisor;

        /// <summary>Returns the dot product of two vectors.</summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The dot product.</returns>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(Vector4 vector1, Vector4 vector2) => Vector256.Dot(vector1.AsVector256(), vector2.AsVector256());

        /// <inheritdoc cref="Vector256.Exp(Vector256{double})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Exp(Vector4 vector) => Vector256.Exp(vector.AsVector256()).AsVector4();

        /// <inheritdoc cref="Vector256.MultiplyAddEstimate(Vector256{double}, Vector256{double}, Vector256{double})" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 FusedMultiplyAdd(Vector4 left, Vector4 right, Vector4 addend) => Vector256.FusedMultiplyAdd(left.AsVector256(), right.AsVector256(), addend.AsVector256()).AsVector4();

        /// <inheritdoc cref="Vector256.Hypot(Vector256{double}, Vector256{double})" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Hypot(Vector4 x, Vector4 y) => Vector256.Hypot(x.AsVector256(), y.AsVector256()).AsVector4();

        /// <inheritdoc cref="Lerp(Vector4, Vector4, Vector4)" />
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The behavior of this method changed in .NET 5.0. For more information, see [Behavior change for Vector2.Lerp and Vector4.Lerp](/dotnet/core/compatibility/3.1-5.0#behavior-change-for-vector2lerp-and-vector4lerp).
        /// ]]></format></remarks>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Lerp(Vector4 value1, Vector4 value2, double amount) => Lerp(value1, value2, Create(amount));

        /// <inheritdoc cref="Vector256.Lerp(Vector256{double}, Vector256{double}, Vector256{double})" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Lerp(Vector4 value1, Vector4 value2, Vector4 amount) => Vector256.Lerp(value1.AsVector256(), value2.AsVector256(), amount.AsVector256()).AsVector4();

        /// <inheritdoc cref="Vector256.Log(Vector256{double})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Log(Vector4 vector) => Vector256.Log(vector.AsVector256()).AsVector4();

        /// <inheritdoc cref="Vector256.Log2(Vector256{double})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Log2(Vector4 vector) => Vector256.Log2(vector.AsVector256()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Max(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Max(Vector4 value1, Vector4 value2) => Vector256.Max(value1.AsVector256(), value2.AsVector256()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxMagnitude(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MaxMagnitude(Vector4 value1, Vector4 value2) => Vector256.MaxMagnitude(value1.AsVector256(), value2.AsVector256()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxMagnitudeNumber(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MaxMagnitudeNumber(Vector4 value1, Vector4 value2) => Vector256.MaxMagnitudeNumber(value1.AsVector256(), value2.AsVector256()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxNative(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MaxNative(Vector4 value1, Vector4 value2) => Vector256.MaxNative(value1.AsVector256(), value2.AsVector256()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxNumber(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MaxNumber(Vector4 value1, Vector4 value2) => Vector256.MaxNumber(value1.AsVector256(), value2.AsVector256()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Min(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Min(Vector4 value1, Vector4 value2) => Vector256.Min(value1.AsVector256(), value2.AsVector256()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinMagnitude(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MinMagnitude(Vector4 value1, Vector4 value2) => Vector256.MinMagnitude(value1.AsVector256(), value2.AsVector256()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinMagnitudeNumber(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MinMagnitudeNumber(Vector4 value1, Vector4 value2) => Vector256.MinMagnitudeNumber(value1.AsVector256(), value2.AsVector256()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinNative(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MinNative(Vector4 value1, Vector4 value2) => Vector256.MinNative(value1.AsVector256(), value2.AsVector256()).AsVector4();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinNumber(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MinNumber(Vector4 value1, Vector4 value2) => Vector256.MinNumber(value1.AsVector256(), value2.AsVector256()).AsVector4();

        /// <summary>Returns a new vector whose values are the product of each pair of elements in two specified vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The element-wise product vector.</returns>
    
        public static Vector4 Multiply(Vector4 left, Vector4 right) => left * right;

        /// <summary>Multiplies a vector by a specified scalar.</summary>
        /// <param name="left">The vector to multiply.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
    
        public static Vector4 Multiply(Vector4 left, double right) => left * right;

        /// <summary>Multiplies a scalar value by a specified vector.</summary>
        /// <param name="left">The scaled value.</param>
        /// <param name="right">The vector.</param>
        /// <returns>The scaled vector.</returns>
    
        public static Vector4 Multiply(double left, Vector4 right) => left * right;

        /// <inheritdoc cref="Vector256.MultiplyAddEstimate(Vector256{double}, Vector256{double}, Vector256{double})" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 MultiplyAddEstimate(Vector4 left, Vector4 right, Vector4 addend) => Vector256.MultiplyAddEstimate(left.AsVector256(), right.AsVector256(), addend.AsVector256()).AsVector4();

        /// <summary>Negates a specified vector.</summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>The negated vector.</returns>
    
        public static Vector4 Negate(Vector4 value) => -value;

        /// <summary>Returns a vector with the same direction as the specified vector, but with a length of one.</summary>
        /// <param name="vector">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
    
        public static Vector4 Normalize(Vector4 vector) => vector / vector.Length();

        /// <inheritdoc cref="Vector256.RadiansToDegrees(Vector256{double})" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 RadiansToDegrees(Vector4 radians) => Vector256.RadiansToDegrees(radians.AsVector256()).AsVector4();

        /// <inheritdoc cref="Vector256.Round(Vector256{double})" />
    
        public static Vector4 Round(Vector4 vector) => Vector256.Round(vector.AsVector256()).AsVector4();

        /// <inheritdoc cref="Vector256.Round(Vector256{double}, MidpointRounding)" />
    
        public static Vector4 Round(Vector4 vector, MidpointRounding mode) => Vector256.Round(vector.AsVector256(), mode).AsVector4();

        /// <inheritdoc cref="Vector256.Sin(Vector256{double})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Sin(Vector4 vector) => Vector256.Sin(vector.AsVector256()).AsVector4();

        /// <inheritdoc cref="Vector256.SinCos(Vector256{double})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector4 Sin, Vector4 Cos) SinCos(Vector4 vector)
        {
            (Vector256<double> sin, Vector256<double> cos) = Vector256.SinCos(vector.AsVector256());
            return (sin.AsVector4(), cos.AsVector4());
        }

        /// <summary>Returns a vector whose elements are the square root of each of a specified vector's elements.</summary>
        /// <param name="value">A vector.</param>
        /// <returns>The square root vector.</returns>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 SquareRoot(Vector4 value) => Vector256.Sqrt(value.AsVector256()).AsVector4();

        /// <summary>Subtracts the second vector from the first.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The difference vector.</returns>
    
        public static Vector4 Subtract(Vector4 left, Vector4 right) => left - right;

        /// <summary>Transforms a two-dimensional vector by a specified 4x4 matrix.</summary>
        /// <param name="position">The vector to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector4 Transform(Vector2 position, Matrix4x4 matrix) => Transform(position, in matrix.AsImpl());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector4 Transform(Vector2 position, in Matrix4x4.Impl matrix)
        {
            // This implementation is based on the DirectX Math Library XMVector2Transform method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            Vector4 result = matrix.X * position.X;
            result = MultiplyAddEstimate(matrix.Y, Create(position.Y), result);
            return result + matrix.W;
        }

        /// <summary>Transforms a two-dimensional vector by the specified Quaternion rotation value.</summary>
        /// <param name="value">The vector to rotate.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Transform(Vector2 value, Quaternion rotation) => Transform(Create(value, 0.0f, 1.0f), rotation);

        /// <summary>Transforms a three-dimensional vector by a specified 4x4 matrix.</summary>
        /// <param name="position">The vector to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector4 Transform(Vector3 position, Matrix4x4 matrix) => Transform(position, in matrix.AsImpl());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector4 Transform(Vector3 position, in Matrix4x4.Impl matrix)
        {
            // This implementation is based on the DirectX Math Library XMVector3Transform method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            Vector4 result = matrix.X * position.X;
            result = MultiplyAddEstimate(matrix.Y, Create(position.Y), result);
            result = MultiplyAddEstimate(matrix.Z, Create(position.Z), result);
            return result + matrix.W;
        }

        /// <summary>Transforms a three-dimensional vector by the specified Quaternion rotation value.</summary>
        /// <param name="value">The vector to rotate.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Transform(Vector3 value, Quaternion rotation) => Transform(Create(value, 1.0f), rotation);

        /// <summary>Transforms a four-dimensional vector by a specified 4x4 matrix.</summary>
        /// <param name="vector">The vector to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector4 Transform(Vector4 vector, Matrix4x4 matrix) => Transform(vector, in matrix.AsImpl());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector4 Transform(Vector4 vector, in Matrix4x4.Impl matrix)
        {
            // This implementation is based on the DirectX Math Library XMVector4Transform method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            Vector4 result = matrix.X * vector.X;
            result = MultiplyAddEstimate(matrix.Y, Create(vector.Y), result);
            result = MultiplyAddEstimate(matrix.Z, Create(vector.Z), result);
            result = MultiplyAddEstimate(matrix.W, Create(vector.W), result);
            return result;
        }

        /// <summary>Transforms a four-dimensional vector by the specified Quaternion rotation value.</summary>
        /// <param name="value">The vector to rotate.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Transform(Vector4 value, Quaternion rotation)
        {
            // This implementation is based on the DirectX Math Library XMVector3Rotate method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            Quaternion conjuagate = Quaternion.Conjugate(rotation);
            Quaternion temp = Quaternion.Concatenate(conjuagate, value.AsQuaternion());
            return Quaternion.Concatenate(temp, rotation).AsVector4();
        }

        /// <inheritdoc cref="Vector256.Truncate(Vector256{double})" />
    
        public static Vector4 Truncate(Vector4 vector) => Vector256.Truncate(vector.AsVector256()).AsVector4();

        /// <summary>Copies the elements of the vector to a specified array.</summary>
        /// <param name="array">The destination array.</param>
        /// <remarks><paramref name="array" /> must have at least four elements. The method copies the vector's elements starting at index 0.</remarks>
        /// <exception cref="NullReferenceException"><paramref name="array" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">The number of elements in the current instance is greater than in the array.</exception>
        /// <exception cref="RankException"><paramref name="array" /> is multidimensional.</exception>
        public readonly void CopyTo(double[] array) => this.AsVector256().CopyTo(array);

        /// <summary>Copies the elements of the vector to a specified array starting at a specified index position.</summary>
        /// <param name="array">The destination array.</param>
        /// <param name="index">The index at which to copy the first element of the vector.</param>
        /// <remarks><paramref name="array" /> must have a sufficient number of elements to accommodate the four vector elements. In other words, elements <paramref name="index" /> through <paramref name="index" /> + 3 must already exist in <paramref name="array" />.</remarks>
        /// <exception cref="NullReferenceException"><paramref name="array" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">The number of elements in the current instance is greater than in the array.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is less than zero.
        /// -or-
        /// <paramref name="index" /> is greater than or equal to the array length.</exception>
        /// <exception cref="RankException"><paramref name="array" /> is multidimensional.</exception>
        public readonly void CopyTo(double[] array, int index) => this.AsVector256().CopyTo(array, index);

        /// <summary>Copies the vector to the given <see cref="Span{T}" />. The length of the destination span must be at least 4.</summary>
        /// <param name="destination">The destination span which the values are copied into.</param>
        /// <exception cref="ArgumentException">If number of elements in source vector is greater than those available in destination span.</exception>
        public readonly void CopyTo(Span<double> destination) => this.AsVector256().CopyTo(destination);

        /// <summary>Attempts to copy the vector to the given <see cref="Span{Double}" />. The length of the destination span must be at least 4.</summary>
        /// <param name="destination">The destination span which the values are copied into.</param>
        /// <returns><see langword="true" /> if the source vector was successfully copied to <paramref name="destination" />. <see langword="false" /> if <paramref name="destination" /> is not large enough to hold the source vector.</returns>
        public readonly bool TryCopyTo(Span<double> destination) => this.AsVector256().TryCopyTo(destination);

        /// <summary>Returns a value that indicates whether this instance and another vector are equal.</summary>
        /// <param name="other">The other vector.</param>
        /// <returns><see langword="true" /> if the two vectors are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two vectors are equal if their <see cref="X" />, <see cref="Y" />, <see cref="Z" />, and <see cref="W" /> elements are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector4 other) => this.AsVector256().Equals(other.AsVector256());

        /// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true" /> if the current instance and <paramref name="obj" /> are equal; otherwise, <see langword="false" />. If <paramref name="obj" /> is <see langword="null" />, the method returns <see langword="false" />.</returns>
        /// <remarks>The current instance and <paramref name="obj" /> are equal if <paramref name="obj" /> is a <see cref="Vector4" /> object and their corresponding elements are equal.</remarks>
        public override readonly bool Equals([NotNullWhen(true)] object? obj) => (obj is Vector4 other) && Equals(other);

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>The hash code.</returns>
        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z, W);

        /// <summary>Returns the length of this vector object.</summary>
        /// <returns>The vector's length.</returns>
        /// <altmember cref="LengthSquared" />
    
        public readonly double Length() => double.Sqrt(LengthSquared());

        /// <summary>Returns the length of the vector squared.</summary>
        /// <returns>The vector's length squared.</returns>
        /// <remarks>This operation offers better performance than a call to the <see cref="Length" /> method.</remarks>
        /// <altmember cref="Length" />
    
        public readonly double LengthSquared() => Dot(this, this);

        /// <summary>Returns the string representation of the current instance using default formatting.</summary>
        /// <returns>The string representation of the current instance.</returns>
        /// <remarks>This method returns a string in which each element of the vector is formatted using the "G" (general) format string and the formatting conventions of the current thread culture. The "&lt;" and "&gt;" characters are used to begin and end the string, and the current culture's <see cref="NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
        public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);

        /// <summary>Returns the string representation of the current instance using the specified format string to format individual elements.</summary>
        /// <param name="format">A standard or custom numeric format string that defines the format of individual elements.</param>
        /// <returns>The string representation of the current instance.</returns>
        /// <remarks>This method returns a string in which each element of the vector is formatted using <paramref name="format" /> and the current culture's formatting conventions. The "&lt;" and "&gt;" characters are used to begin and end the string, and the current culture's <see cref="NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
        /// <related type="Article" href="/dotnet/standard/base-types/standard-numeric-format-strings">Standard Numeric Format Strings</related>
        /// <related type="Article" href="/dotnet/standard/base-types/custom-numeric-format-strings">Custom Numeric Format Strings</related>
        public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ToString(format, CultureInfo.CurrentCulture);

        /// <summary>Returns the string representation of the current instance using the specified format string to format individual elements and the specified format provider to define culture-specific formatting.</summary>
        /// <param name="format">A standard or custom numeric format string that defines the format of individual elements.</param>
        /// <param name="formatProvider">A format provider that supplies culture-specific formatting information.</param>
        /// <returns>The string representation of the current instance.</returns>
        /// <remarks>This method returns a string in which each element of the vector is formatted using <paramref name="format" /> and <paramref name="formatProvider" />. The "&lt;" and "&gt;" characters are used to begin and end the string, and the format provider's <see cref="NumberFormatInfo.NumberGroupSeparator" /> property followed by a space is used to separate each element.</remarks>
        /// <related type="Article" href="/dotnet/standard/base-types/standard-numeric-format-strings">Standard Numeric Format Strings</related>
        /// <related type="Article" href="/dotnet/standard/base-types/custom-numeric-format-strings">Custom Numeric Format Strings</related>
        public readonly string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
        {
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

            return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}{separator} {Z.ToString(format, formatProvider)}{separator} {W.ToString(format, formatProvider)}>";
        }
    }
}
