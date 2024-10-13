// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace PointCloud.Numerics
{
    /// <summary>Represents a vector with three  single-precision floating-point values.</summary>
    /// <remarks><format type="text/markdown"><![CDATA[
    /// The <xref:System.Numerics.Vector3> structure provides support for hardware acceleration.
    /// [!INCLUDE[vectors-are-rows-paragraph](~/includes/system-numerics-vectors-are-rows.md)]
    /// ]]></format></remarks>

    public partial struct Vector3 : IEquatable<Vector3>, IFormattable
    {
        /// <summary>The X component of the vector.</summary>
        public double X;

        /// <summary>The Y component of the vector.</summary>
        public double Y;

        /// <summary>The Z component of the vector.</summary>
        public double Z;

        internal const int Count = 3;

        /// <summary>Creates a new <see cref="Vector3" /> object whose three elements have the same value.</summary>
        /// <param name="value">The value to assign to all three elements.</param>
    
        public Vector3(double value)
        {
            this = Create(value);
        }

        /// <summary>Creates a   new <see cref="Vector3" /> object from the specified <see cref="Vector2" /> object and the specified value.</summary>
        /// <param name="value">The vector with two elements.</param>
        /// <param name="z">The additional value to assign to the <see cref="Z" /> field.</param>
    
        public Vector3(Vector2 value, double z)
        {
            this = Create(value, z);
        }

        /// <summary>Creates a vector whose elements have the specified values.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <param name="y">The value to assign to the <see cref="Y" /> field.</param>
        /// <param name="z">The value to assign to the <see cref="Z" /> field.</param>
    
        public Vector3(double x, double y, double z)
        {
            this = Create(x, y, z);
        }

        /// <summary>Constructs a vector from the given <see cref="ReadOnlySpan{Double}" />. The span must contain at least 3 elements.</summary>
        /// <param name="values">The span of elements to assign to the vector.</param>
    
        public Vector3(ReadOnlySpan<double> values)
        {
            this = Create(values);
        }

        /// <inheritdoc cref="Vector4.E" />
        public static Vector3 E
        {
        
            get => Create(double.E);
        }

        /// <inheritdoc cref="Vector4.Epsilon" />
        public static Vector3 Epsilon
        {
        
            get => Create(double.Epsilon);
        }

        /// <inheritdoc cref="Vector4.NaN" />
        public static Vector3 NaN
        {
        
            get => Create(double.NaN);
        }

        /// <inheritdoc cref="Vector4.NegativeInfinity" />
        public static Vector3 NegativeInfinity
        {
        
            get => Create(double.NegativeInfinity);
        }

        /// <inheritdoc cref="Vector4.NegativeZero" />
        public static Vector3 NegativeZero
        {
        
            get => Create(double.NegativeZero);
        }

        /// <inheritdoc cref="Vector4.One" />
        public static Vector3 One
        {
        
            get => Create(1.0f);
        }

        /// <inheritdoc cref="Vector4.Pi" />
        public static Vector3 Pi
        {
        
            get => Create(double.Pi);
        }

        /// <inheritdoc cref="Vector4.PositiveInfinity" />
        public static Vector3 PositiveInfinity
        {
        
            get => Create(double.PositiveInfinity);
        }

        /// <inheritdoc cref="Vector4.Tau" />
        public static Vector3 Tau
        {
        
            get => Create(double.Tau);
        }

        /// <summary>Gets the vector (1,0,0).</summary>
        /// <value>The vector <c>(1,0,0)</c>.</value>
        public static Vector3 UnitX
        {
        
            get => CreateScalar(1.0f);
        }

        /// <summary>Gets the vector (0,1,0).</summary>
        /// <value>The vector <c>(0,1,0)</c>.</value>
        public static Vector3 UnitY
        {
        
            get => Create(0.0f, 1.0f, 0.0f);
        }

        /// <summary>Gets the vector (0,0,1).</summary>
        /// <value>The vector <c>(0,0,1)</c>.</value>
        public static Vector3 UnitZ
        {
        
            get => Create(0.0f, 0.0f, 1.0f);
        }

        /// <inheritdoc cref="Vector4.Zero" />
        public static Vector3 Zero
        {
        
            get => default;
        }

        /// <summary>Gets or sets the element at the specified index.</summary>
        /// <param name="index">The index of the element to get or set.</param>
        /// <returns>The the element at <paramref name="index" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        public double this[int index]
        {
        
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                if ((uint)index >= Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                    //ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
                }
                return this.AsVector256Unsafe().GetElement(index);
            }

        
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if ((uint)index >= Count)
                {
                    throw new ArgumentOutOfRangeException("index");
                    //ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
                }
                this = this.AsVector256Unsafe().WithElement(index, value).AsVector3();
            }
        }

        /// <summary>Adds two vectors together.</summary>
        /// <param name="left">The first vector to add.</param>
        /// <param name="right">The second vector to add.</param>
        /// <returns>The summed vector.</returns>
        /// <remarks>The <see cref="op_Addition" /> method defines the addition operation for <see cref="Vector3" /> objects.</remarks>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator +(Vector3 left, Vector3 right) => (left.AsVector256Unsafe() + right.AsVector256Unsafe()).AsVector3();

        /// <summary>Divides the first vector by the second.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The vector that results from dividing <paramref name="left" /> by <paramref name="right" />.</returns>
        /// <remarks>The <see cref="Vector3.op_Division" /> method defines the division operation for <see cref="Vector3" /> objects.</remarks>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator /(Vector3 left, Vector3 right) => (left.AsVector256Unsafe() / right.AsVector256Unsafe()).AsVector3();

        /// <summary>Divides the specified vector by a specified scalar value.</summary>
        /// <param name="value1">The vector.</param>
        /// <param name="value2">The scalar value.</param>
        /// <returns>The result of the division.</returns>
        /// <remarks>The <see cref="Vector3.op_Division" /> method defines the division operation for <see cref="Vector3" /> objects.</remarks>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator /(Vector3 value1, double value2) => (value1.AsVector256Unsafe() / value2).AsVector3();

        /// <summary>Returns a value that indicates whether each pair of elements in two specified vectors is equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two <see cref="Vector3" /> objects are equal if each element in <paramref name="left" /> is equal to the corresponding element in <paramref name="right" />.</remarks>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector3 left, Vector3 right) => left.AsVector256() == right.AsVector256();

        /// <summary>Returns a value that indicates whether two specified vectors are not equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, <see langword="false" />.</returns>
    
        public static bool operator !=(Vector3 left, Vector3 right) => !(left == right);

        /// <summary>Returns a new vector whose values are the product of each pair of elements in two specified vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The element-wise product vector.</returns>
        /// <remarks>The <see cref="Vector3.op_Multiply" /> method defines the multiplication operation for <see cref="Vector3" /> objects.</remarks>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(Vector3 left, Vector3 right) => (left.AsVector256Unsafe() * right.AsVector256Unsafe()).AsVector3();

        /// <summary>Multiplies the specified vector by the specified scalar value.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        /// <remarks>The <see cref="Vector3.op_Multiply" /> method defines the multiplication operation for <see cref="Vector3" /> objects.</remarks>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(Vector3 left, double right) => (left.AsVector256Unsafe() * right).AsVector3();

        /// <summary>Multiplies the scalar value by the specified vector.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        /// <remarks>The <see cref="Vector3.op_Multiply" /> method defines the multiplication operation for <see cref="Vector3" /> objects.</remarks>
    
        public static Vector3 operator *(double left, Vector3 right) => right * left;

        /// <summary>Subtracts the second vector from the first.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The vector that results from subtracting <paramref name="right" /> from <paramref name="left" />.</returns>
        /// <remarks>The <see cref="op_Subtraction" /> method defines the subtraction operation for <see cref="Vector3" /> objects.</remarks>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator -(Vector3 left, Vector3 right) => (left.AsVector256Unsafe() - right.AsVector256Unsafe()).AsVector3();

        /// <summary>Negates the specified vector.</summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>The negated vector.</returns>
        /// <remarks>The <see cref="op_UnaryNegation" /> method defines the unary negation operation for <see cref="Vector3" /> objects.</remarks>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator -(Vector3 value) => (-value.AsVector256Unsafe()).AsVector3();

        /// <summary>Returns a vector whose elements are the absolute values of each of the specified vector's elements.</summary>
        /// <param name="value">A vector.</param>
        /// <returns>The absolute value vector.</returns>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Abs(Vector3 value) => Vector256.Abs(value.AsVector256Unsafe()).AsVector3();

        /// <summary>Adds two vectors together.</summary>
        /// <param name="left">The first vector to add.</param>
        /// <param name="right">The second vector to add.</param>
        /// <returns>The summed vector.</returns>
    
        public static Vector3 Add(Vector3 left, Vector3 right) => left + right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Clamp(TSelf, TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Clamp(Vector3 value1, Vector3 min, Vector3 max) => Vector256.Clamp(value1.AsVector256Unsafe(), min.AsVector256Unsafe(), max.AsVector256Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ClampNative(TSelf, TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ClampNative(Vector3 value1, Vector3 min, Vector3 max) => Vector256.ClampNative(value1.AsVector256Unsafe(), min.AsVector256Unsafe(), max.AsVector256Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopySign(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 CopySign(Vector3 value, Vector3 sign) => Vector256.CopySign(value.AsVector256Unsafe(), sign.AsVector256Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.Cos(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Cos(Vector3 vector) => Vector256.Cos(vector.AsVector256()).AsVector3();

        /// <summary>Creates a new <see cref="Vector3" /> object whose three elements have the same value.</summary>
        /// <param name="value">The value to assign to all three elements.</param>
        /// <returns>A new <see cref="Vector3" /> whose three elements have the same value.</returns>
    
        public static Vector3 Create(double value) => Vector256.Create(value).AsVector3();

        /// <summary>Creates a new <see cref="Vector3" /> object from the specified <see cref="Vector2" /> object and a Z and a W component.</summary>
        /// <param name="vector">The vector to use for the X and Y components.</param>
        /// <param name="z">The Z component.</param>
        /// <returns>A new <see cref="Vector3" /> from the specified <see cref="Vector2" /> object and a Z and a W component.</returns>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Create(Vector2 vector, double z)
        {
            return vector.AsVector256Unsafe()
                         .WithElement(2, z)
                         .AsVector3();
        }

        /// <summary>Creates a vector whose elements have the specified values.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <param name="y">The value to assign to the <see cref="Y" /> field.</param>
        /// <param name="z">The value to assign to the <see cref="Z" /> field.</param>
        /// <returns>A new <see cref="Vector3" /> whose elements have the specified values.</returns>
    
        public static Vector3 Create(double x, double y, double z) => Vector256.Create(x, y, z, 0).AsVector3();

        /// <summary>Constructs a vector from the given <see cref="ReadOnlySpan{Double}" />. The span must contain at least 3 elements.</summary>
        /// <param name="values">The span of elements to assign to the vector.</param>
        /// <returns>A new <see cref="Vector3" /> whose elements have the specified values.</returns>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Create(ReadOnlySpan<double> values)
        {
            if (values.Length < Count)
            {
                throw new ArgumentOutOfRangeException("values");
                //ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.values);
            }
            return Unsafe.ReadUnaligned<Vector3>(ref Unsafe.As<double, byte>(ref MemoryMarshal.GetReference(values)));
        }

        /// <summary>Creates a vector with <see cref="X" /> initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <returns>A new <see cref="Vector3" /> with <see cref="X" /> initialized <paramref name="x" /> and the remaining elements initialized to zero.</returns>
    
        internal static Vector3 CreateScalar(double x) => Vector256.CreateScalar(x).AsVector3();

        /// <summary>Creates a vector with <see cref="X" /> initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="x">The value to assign to the <see cref="X" /> field.</param>
        /// <returns>A new <see cref="Vector3" /> with <see cref="X" /> initialized <paramref name="x" /> and the remaining elements left uninitialized.</returns>
    
        internal static Vector3 CreateScalarUnsafe(double x) => Vector256.CreateScalarUnsafe(x).AsVector3();

        /// <summary>Computes the cross product of two vectors.</summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The cross product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Cross(Vector3 vector1, Vector3 vector2)
        {
            // This implementation is based on the DirectX Math Library XMVector3Cross method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            Vector256<double> v1 = vector1.AsVector256();
            Vector256<double> v2 = vector2.AsVector256();

            Vector256<double> temp = Vector256.Shuffle(v1, Vector256.Create(1, 2, 0, 3)) * Vector256.Shuffle(v2, Vector256.Create(2, 0, 1, 3));

            return Vector256.MultiplyAddEstimate(
                -Vector256.Shuffle(v1, Vector256.Create(2, 0, 1, 3)),
                 Vector256.Shuffle(v2, Vector256.Create(1, 2, 0, 3)),
                 temp
            ).AsVector3();
        }

        /// <inheritdoc cref="Vector4.DegreesToRadians(Vector4)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 DegreesToRadians(Vector3 degrees) => Vector256.DegreesToRadians(degrees.AsVector256Unsafe()).AsVector3();

        /// <summary>Computes the Euclidean distance between the two given points.</summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance.</returns>
    
        public static double Distance(Vector3 value1, Vector3 value2) => double.Sqrt(DistanceSquared(value1, value2));

        /// <summary>Returns the Euclidean distance squared between two specified points.</summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance squared.</returns>
    
        public static double DistanceSquared(Vector3 value1, Vector3 value2) => (value1 - value2).LengthSquared();

        /// <summary>Divides the first vector by the second.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The vector resulting from the division.</returns>
    
        public static Vector3 Divide(Vector3 left, Vector3 right) => left / right;

        /// <summary>Divides the specified vector by a specified scalar value.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="divisor">The scalar value.</param>
        /// <returns>The vector that results from the division.</returns>
    
        public static Vector3 Divide(Vector3 left, double divisor) => left / divisor;

        /// <summary>Returns the dot product of two vectors.</summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The dot product.</returns>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(Vector3 vector1, Vector3 vector2) => Vector256.Dot(vector1.AsVector256(), vector2.AsVector256());

        /// <inheritdoc cref="Vector4.Exp(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Exp(Vector3 vector) => Vector256.Exp(vector.AsVector256()).AsVector3();

        /// <inheritdoc cref="Vector256.MultiplyAddEstimate(Vector256{double}, Vector256{double}, Vector256{double})" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 FusedMultiplyAdd(Vector3 left, Vector3 right, Vector3 addend) => Vector256.FusedMultiplyAdd(left.AsVector256Unsafe(), right.AsVector256Unsafe(), addend.AsVector256Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.Hypot(Vector4, Vector4)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Hypot(Vector3 x, Vector3 y) => Vector256.Hypot(x.AsVector256Unsafe(), y.AsVector256Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.Lerp(Vector4, Vector4, double)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Lerp(Vector3 value1, Vector3 value2, double amount) => Lerp(value1, value2, Create(amount));

        /// <inheritdoc cref="Vector4.Lerp(Vector4, Vector4, Vector4)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Lerp(Vector3 value1, Vector3 value2, Vector3 amount) => Vector256.Lerp(value1.AsVector256Unsafe(), value2.AsVector256Unsafe(), amount.AsVector256Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.Log2(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Log(Vector3 vector) => Vector256.Log(Vector4.Create(vector, 1.0f).AsVector256()).AsVector3();

        /// <inheritdoc cref="Vector4.Log(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Log2(Vector3 vector) => Vector256.Log2(Vector4.Create(vector, 1.0f).AsVector256()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Max(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Max(Vector3 value1, Vector3 value2) => Vector256.Max(value1.AsVector256Unsafe(), value2.AsVector256Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxMagnitude(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MaxMagnitude(Vector3 value1, Vector3 value2) => Vector256.MaxMagnitude(value1.AsVector256Unsafe(), value2.AsVector256Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxMagnitudeNumber(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MaxMagnitudeNumber(Vector3 value1, Vector3 value2) => Vector256.MaxMagnitudeNumber(value1.AsVector256Unsafe(), value2.AsVector256Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxNative(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MaxNative(Vector3 value1, Vector3 value2) => Vector256.MaxNative(value1.AsVector256Unsafe(), value2.AsVector256Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxNumber(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MaxNumber(Vector3 value1, Vector3 value2) => Vector256.MaxNumber(value1.AsVector256Unsafe(), value2.AsVector256Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Min(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Min(Vector3 value1, Vector3 value2) => Vector256.Min(value1.AsVector256Unsafe(), value2.AsVector256Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinMagnitude(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MinMagnitude(Vector3 value1, Vector3 value2) => Vector256.MinMagnitude(value1.AsVector256Unsafe(), value2.AsVector256Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinMagnitudeNumber(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MinMagnitudeNumber(Vector3 value1, Vector3 value2) => Vector256.MinMagnitudeNumber(value1.AsVector256Unsafe(), value2.AsVector256Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinNative(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MinNative(Vector3 value1, Vector3 value2) => Vector256.MinNative(value1.AsVector256Unsafe(), value2.AsVector256Unsafe()).AsVector3();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinNumber(TSelf, TSelf)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MinNumber(Vector3 value1, Vector3 value2) => Vector256.MinNumber(value1.AsVector256Unsafe(), value2.AsVector256Unsafe()).AsVector3();

        /// <summary>Returns a new vector whose values are the product of each pair of elements in two specified vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The element-wise product vector.</returns>
    
        public static Vector3 Multiply(Vector3 left, Vector3 right) => left * right;

        /// <summary>Multiplies a vector by a specified scalar.</summary>
        /// <param name="left">The vector to multiply.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
    
        public static Vector3 Multiply(Vector3 left, double right) => left * right;

        /// <summary>Multiplies a scalar value by a specified vector.</summary>
        /// <param name="left">The scaled value.</param>
        /// <param name="right">The vector.</param>
        /// <returns>The scaled vector.</returns>
    
        public static Vector3 Multiply(double left, Vector3 right) => left * right;

        /// <inheritdoc cref="Vector256.MultiplyAddEstimate(Vector256{double}, Vector256{double}, Vector256{double})" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 MultiplyAddEstimate(Vector3 left, Vector3 right, Vector3 addend) => Vector256.MultiplyAddEstimate(left.AsVector256Unsafe(), right.AsVector256Unsafe(), addend.AsVector256Unsafe()).AsVector3();

        /// <summary>Negates a specified vector.</summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>The negated vector.</returns>
    
        public static Vector3 Negate(Vector3 value) => -value;

        /// <summary>Returns a vector with the same direction as the specified vector, but with a length of one.</summary>
        /// <param name="value">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
    
        public static Vector3 Normalize(Vector3 value) => value / value.Length();

        /// <inheritdoc cref="Vector4.RadiansToDegrees(Vector4)" />
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 RadiansToDegrees(Vector3 radians) => Vector256.RadiansToDegrees(radians.AsVector256Unsafe()).AsVector3();

        /// <summary>Returns the reflection of a vector off a surface that has the specified normal.</summary>
        /// <param name="vector">The source vector.</param>
        /// <param name="normal">The normal of the surface being reflected off.</param>
        /// <returns>The reflected vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Reflect(Vector3 vector, Vector3 normal)
        {
            // This implementation is based on the DirectX Math Library XMVector3Reflect method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl

            Vector3 tmp = Create(Dot(vector, normal));
            tmp += tmp;
            return MultiplyAddEstimate(-tmp, normal, vector);
        }

        /// <inheritdoc cref="Vector4.Round(Vector4)" />
    
        public static Vector3 Round(Vector3 vector) => Vector256.Round(vector.AsVector256Unsafe()).AsVector3();

        /// <inheritdoc cref="Vector4.Round(Vector4, MidpointRounding)" />
    
        public static Vector3 Round(Vector3 vector, MidpointRounding mode) => Vector256.Round(vector.AsVector256Unsafe(), mode).AsVector3();

        /// <inheritdoc cref="Vector4.Sin(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Sin(Vector3 vector) => Vector256.Sin(vector.AsVector256()).AsVector3();

        /// <inheritdoc cref="Vector4.SinCos(Vector4)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (Vector3 Sin, Vector3 Cos) SinCos(Vector3 vector)
        {
            (Vector256<double> sin, Vector256<double> cos) = Vector256.SinCos(vector.AsVector256());
            return (sin.AsVector3(), cos.AsVector3());
        }

        /// <summary>Returns a vector whose elements are the square root of each of a specified vector's elements.</summary>
        /// <param name="value">A vector.</param>
        /// <returns>The square root vector.</returns>
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 SquareRoot(Vector3 value) => Vector256.Sqrt(value.AsVector256Unsafe()).AsVector3();

        /// <summary>Subtracts the second vector from the first.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The difference vector.</returns>
    
        public static Vector3 Subtract(Vector3 left, Vector3 right) => left - right;

        /// <summary>Transforms a vector by a specified 4x4 matrix.</summary>
        /// <param name="position">The vector to transform.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Transform(Vector3 position, Matrix4x4 matrix) => Vector4.Transform(position, in matrix.AsImpl()).AsVector256().AsVector3();

        /// <summary>Transforms a vector by the specified Quaternion rotation value.</summary>
        /// <param name="value">The vector to rotate.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Transform(Vector3 value, Quaternion rotation) => Vector4.Transform(value, rotation).AsVector3();

        /// <summary>Transforms a vector normal by the given 4x4 matrix.</summary>
        /// <param name="normal">The source vector.</param>
        /// <param name="matrix">The matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 TransformNormal(Vector3 normal, Matrix4x4 matrix) => TransformNormal(normal, in matrix.AsImpl());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector3 TransformNormal(Vector3 normal, in Matrix4x4.Impl matrix)
        {
            Vector4 result = matrix.X * normal.X;

            result = Vector4.MultiplyAddEstimate(matrix.Y, Vector4.Create(normal.Y), result);
            result = Vector4.MultiplyAddEstimate(matrix.Z, Vector4.Create(normal.Z), result);

            return result.AsVector3();
        }

        /// <inheritdoc cref="Vector4.Truncate(Vector4)" />
    
        public static Vector3 Truncate(Vector3 vector) => Vector256.Truncate(vector.AsVector256Unsafe()).AsVector3();

        /// <summary>Copies the elements of the vector to a specified array.</summary>
        /// <param name="array">The destination array.</param>
        /// <remarks><paramref name="array" /> must have at least three elements. The method copies the vector's elements starting at index 0.</remarks>
        /// <exception cref="NullReferenceException"><paramref name="array" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">The number of elements in the current instance is greater than in the array.</exception>
        /// <exception cref="RankException"><paramref name="array" /> is multidimensional.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(double[] array)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if (array.Length < Count)
            {
                throw new ArgumentException("The destination is to short.");
                //ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref array[0]), this);
        }

        /// <summary>Copies the elements of the vector to a specified array starting at a specified index position.</summary>
        /// <param name="array">The destination array.</param>
        /// <param name="index">The index at which to copy the first element of the vector.</param>
        /// <remarks><paramref name="array" /> must have a sufficient number of elements to accommodate the three vector elements. In other words, elements <paramref name="index" />, <paramref name="index" /> + 1, and <paramref name="index" /> + 2 must already exist in <paramref name="array" />.</remarks>
        /// <exception cref="NullReferenceException"><paramref name="array" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentException">The number of elements in the current instance is greater than in the array.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> is less than zero.
        /// -or-
        /// <paramref name="index" /> is greater than or equal to the array length.</exception>
        /// <exception cref="RankException"><paramref name="array" /> is multidimensional.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(double[] array, int index)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if ((uint)index >= (uint)array.Length)
            {
                throw new ArgumentOutOfRangeException("Start Index is Out Of Range. It must be less than "+ (uint)array.Length);
                //ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
            }

            if ((array.Length - index) < Count)
            {
                throw new ArgumentOutOfRangeException("Destination is too short.");
                //ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref array[index]), this);
        }

        /// <summary>Copies the vector to the given <see cref="Span{T}" />. The length of the destination span must be at least 3.</summary>
        /// <param name="destination">The destination span which the values are copied into.</param>
        /// <exception cref="ArgumentException">If number of elements in source vector is greater than those available in destination span.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyTo(Span<double> destination)
        {
            if (destination.Length < Count)
            {
                throw new ArgumentOutOfRangeException("Destination is too short.");
                //ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref MemoryMarshal.GetReference(destination)), this);
        }

        /// <summary>Attempts to copy the vector to the given <see cref="Span{Double}" />. The length of the destination span must be at least 3.</summary>
        /// <param name="destination">The destination span which the values are copied into.</param>
        /// <returns><see langword="true" /> if the source vector was successfully copied to <paramref name="destination" />. <see langword="false" /> if <paramref name="destination" /> is not large enough to hold the source vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryCopyTo(Span<double> destination)
        {
            if (destination.Length < Count)
            {
                return false;
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<double, byte>(ref MemoryMarshal.GetReference(destination)), this);
            return true;
        }

        /// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true" /> if the current instance and <paramref name="obj" /> are equal; otherwise, <see langword="false" />. If <paramref name="obj" /> is <see langword="null" />, the method returns <see langword="false" />.</returns>
        /// <remarks>The current instance and <paramref name="obj" /> are equal if <paramref name="obj" /> is a <see cref="Vector3" /> object and their corresponding elements are equal.</remarks>
        public override readonly bool Equals([NotNullWhen(true)] object? obj) => (obj is Vector3 other) && Equals(other);

        /// <summary>Returns a value that indicates whether this instance and another vector are equal.</summary>
        /// <param name="other">The other vector.</param>
        /// <returns><see langword="true" /> if the two vectors are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two vectors are equal if their <see cref="X" />, <see cref="Y" />, and <see cref="Z" /> elements are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector3 other) => this.AsVector256().Equals(other.AsVector256());

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>The hash code.</returns>
        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Z);

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

            return $"<{X.ToString(format, formatProvider)}{separator} {Y.ToString(format, formatProvider)}{separator} {Z.ToString(format, formatProvider)}>";
        }
    }
}
