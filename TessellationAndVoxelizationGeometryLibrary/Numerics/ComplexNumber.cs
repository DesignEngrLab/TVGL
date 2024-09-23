// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="ComplexNumber.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace TVGL
{
    /// <summary>
    /// Struct ComplexNumber
    /// </summary>
    public readonly struct ComplexNumber
    {
        /// <summary>
        /// The real imag tolerance
        /// </summary>
        private const double RealImagTolerance = 1e-6;
        /// <summary>
        /// Specifies the real-value of the vector component of the ComplexNumber.
        /// </summary>
        public readonly double Real;
        /// <summary>
        /// Specifies the Imaginary-value of the vector component of the ComplexNumber.
        /// </summary>
        public readonly double Imaginary;
        /// <summary>
        /// Constructs a ComplexNumber from the given components.
        /// </summary>
        /// <param name="real">The real.</param>
        /// <param name="imaginary">The imaginary.</param>
        public ComplexNumber(double real, double imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }

        /// <summary>
        /// Constructs a ComplexNumber from the given components.
        /// </summary>
        /// <param name="real">The real.</param>
        public ComplexNumber(double real)
        {
            Real = real;
            Imaginary = 0.0;
        }


        /// <summary>
        /// Gets the na n.
        /// </summary>
        /// <value>The na n.</value>
        public static ComplexNumber NaN => new ComplexNumber(double.NaN, double.NaN);
        public static ComplexNumber Zero => new ComplexNumber(0.0, 0.0);

        /// <summary>
        /// Calculates the length of the ComplexNumber.
        /// </summary>
        /// <returns>The computed length of the ComplexNumber.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Length()
        {
            return Math.Sqrt(LengthSquared());
        }

        /// <summary>
        /// Calculates the length squared of the ComplexNumber. This operation is cheaper than Length().
        /// </summary>
        /// <returns>The length squared of the ComplexNumber.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double LengthSquared()
        {
            return Real * Real + Imaginary * Imaginary;
        }

        /// <summary>
        /// Divides each component of the ComplexNumber by the length of the ComplexNumber.
        /// </summary>
        /// <param name="value">The source ComplexNumber.</param>
        /// <returns>The normalized ComplexNumber.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComplexNumber Normalize(ComplexNumber value)
        {
            double invNorm = 1.0 / value.Length();
            return new ComplexNumber(invNorm * value.Real, invNorm * value.Imaginary);
        }

        /// <summary>
        /// Creates the conjugate of a specified ComplexNumber.
        /// </summary>
        /// <param name="value">The ComplexNumber of which to return the conjugate.</param>
        /// <returns>A new ComplexNumber that is the conjugate of the specified one.</returns>
        public static ComplexNumber Conjugate(ComplexNumber value)
        {
            return new ComplexNumber(value.Real, -value.Imaginary);
        }


        /// <summary>
        /// Flips the sign of each component of the ComplexNumber.
        /// </summary>
        /// <param name="value">The source ComplexNumber.</param>
        /// <returns>The negated ComplexNumber.</returns>
        public static ComplexNumber Negate(ComplexNumber value)
        {
            return new ComplexNumber(-value.Real, -value.Imaginary);
        }

        /// <summary>
        /// Flips the sign of each component of the ComplexNumber.
        /// </summary>
        /// <param name="value">The source ComplexNumber.</param>
        /// <returns>The negated ComplexNumber.</returns>
        public static ComplexNumber operator -(ComplexNumber value) => Negate(value);


        /// <summary>
        /// Adds two ComplexNumbers element-by-element.
        /// </summary>
        /// <param name="value1">The first source ComplexNumber.</param>
        /// <param name="value2">The second source ComplexNumber.</param>
        /// <returns>The result of adding the ComplexNumbers.</returns>
        public static ComplexNumber Add(ComplexNumber value1, ComplexNumber value2)
        {
            return new ComplexNumber(value1.Real + value2.Real,
                value1.Imaginary + value2.Imaginary);
        }

        /// <summary>
        /// Adds two ComplexNumbers element-by-element.
        /// </summary>
        /// <param name="value1">The first source ComplexNumber.</param>
        /// <param name="value2">The second source ComplexNumber.</param>
        /// <returns>The result of adding the ComplexNumbers.</returns>
        public static ComplexNumber operator +(ComplexNumber value1, ComplexNumber value2) => Add(value1, value2);

        /// <summary>
        /// Subtracts one ComplexNumber from another.
        /// </summary>
        /// <param name="value1">The first source ComplexNumber.</param>
        /// <param name="value2">The second ComplexNumber, to be subtracted from the first.</param>
        /// <returns>The result of the subtraction.</returns>
        public static ComplexNumber Subtract(ComplexNumber value1, ComplexNumber value2)
        {
            return new ComplexNumber(value1.Real - value2.Real,
                value1.Imaginary - value2.Imaginary);
        }
        /// <summary>
        /// Subtracts one ComplexNumber from another.
        /// </summary>
        /// <param name="value1">The first source ComplexNumber.</param>
        /// <param name="value2">The second ComplexNumber, to be subtracted from the first.</param>
        /// <returns>The result of the subtraction.</returns>
        public static ComplexNumber operator -(ComplexNumber value1, ComplexNumber value2) => Subtract(value1, value2);

        /// <summary>
        /// Subtracts one ComplexNumber from another.
        /// </summary>
        /// <param name="real">The first source ComplexNumber.</param>
        /// <param name="complex">The second ComplexNumber, to be subtracted from the first.</param>
        /// <returns>The result of the subtraction.</returns>
        public static ComplexNumber Subtract(double real, ComplexNumber complex)
        {
            return new ComplexNumber(real - complex.Real, -complex.Imaginary);
        }
        /// <summary>
        /// Subtracts one ComplexNumber from another.
        /// </summary>
        /// <param name="real">The first source ComplexNumber.</param>
        /// <param name="complex">The second ComplexNumber, to be subtracted from the first.</param>
        /// <returns>The result of the subtraction.</returns>
        public static ComplexNumber operator -(double real, ComplexNumber complex) => Subtract(real, complex);

        /// <summary>
        /// Subtracts one ComplexNumber from another.
        /// </summary>
        /// <param name="complex">The first source ComplexNumber.</param>
        /// <param name="real">The second ComplexNumber, to be subtracted from the first.</param>
        /// <returns>The result of the subtraction.</returns>
        public static ComplexNumber Subtract(ComplexNumber complex, double real)
        {
            return new ComplexNumber(complex.Real - real, complex.Imaginary);
        }
        /// <summary>
        /// Subtracts one ComplexNumber from another.
        /// </summary>
        /// <param name="complex">The first source ComplexNumber.</param>
        /// <param name="real">The second ComplexNumber, to be subtracted from the first.</param>
        /// <returns>The result of the subtraction.</returns>
        public static ComplexNumber operator -(ComplexNumber complex, double real) => Subtract(complex, real);


        /// <summary>
        /// Multiplies two ComplexNumbers together.
        /// </summary>
        /// <param name="value1">The ComplexNumber on the left side of the multiplication.</param>
        /// <param name="value2">The ComplexNumber on the right side of the multiplication.</param>
        /// <returns>The result of the multiplication.</returns>
        public static ComplexNumber Multiply(ComplexNumber value1, ComplexNumber value2)
        {
            return new ComplexNumber(value2.Real * value1.Real - value2.Imaginary * value1.Imaginary,
                value2.Imaginary * value1.Real + value2.Real * value1.Imaginary);
        }


        /// <summary>
        /// Multiplies two ComplexNumbers together.
        /// </summary>
        /// <param name="value1">The ComplexNumber on the left side of the multiplication.</param>
        /// <param name="value2">The ComplexNumber on the right side of the multiplication.</param>
        /// <returns>The result of the multiplication.</returns>
        public static ComplexNumber operator *(ComplexNumber value1, ComplexNumber value2) => Multiply(value1, value2);

        /// <summary>
        /// Multiplies a ComplexNumber by a scalar value.
        /// </summary>
        /// <param name="complex">The source ComplexNumber.</param>
        /// <param name="real">The scalar value.</param>
        /// <returns>The result of the multiplication.</returns>
        public static ComplexNumber Multiply(ComplexNumber complex, double real)
        {
            return new ComplexNumber(real * complex.Real, real * complex.Imaginary);
        }

        /// <summary>
        /// Multiplies a ComplexNumber by a scalar value.
        /// </summary>
        /// <param name="complex">The source ComplexNumber.</param>
        /// <param name="real">The scalar value.</param>
        /// <returns>The result of the multiplication.</returns>
        public static ComplexNumber operator *(ComplexNumber complex, double real) => Multiply(complex, real);

        /// <summary>
        /// Multiplies a ComplexNumber by a scalar value.
        /// </summary>
        /// <param name="real">The scalar value.</param>
        /// <param name="complex">The source ComplexNumber.</param>
        /// <returns>The result of the multiplication.</returns>
        public static ComplexNumber operator *(double real, ComplexNumber complex) => Multiply(complex, real);

        /// <summary>
        /// Divides a ComplexNumber by another ComplexNumber.
        /// </summary>
        /// <param name="complex">The source ComplexNumber.</param>
        /// <param name="real">The divisor.</param>
        /// <returns>The result of the division.</returns>
        public static ComplexNumber Divide(ComplexNumber complex, double real)
        {
            var oneOverDenom = 1 / real;
            return new ComplexNumber(oneOverDenom * complex.Real, oneOverDenom * complex.Imaginary);
        }


        /// <summary>
        /// Divides a ComplexNumber by another ComplexNumber.
        /// </summary>
        /// <param name="complex">The source ComplexNumber.</param>
        /// <param name="real">The divisor.</param>
        /// <returns>The result of the division.</returns>
        public static ComplexNumber operator /(ComplexNumber complex, double real) => Divide(complex, real);

        /// <summary>
        /// Divides a ComplexNumber by another ComplexNumber.
        /// </summary>
        /// <param name="value1">The source ComplexNumber.</param>
        /// <param name="value2">The divisor.</param>
        /// <returns>The result of the division.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComplexNumber Divide(ComplexNumber value1, ComplexNumber value2)
        {
            var oneOverDenom = 1 / (value2.Real * value2.Real + value2.Imaginary * value2.Imaginary);
            return new ComplexNumber(oneOverDenom * (value1.Real * value2.Real + value1.Imaginary * value2.Imaginary),
                oneOverDenom * (value1.Imaginary * value2.Real - value1.Real * value2.Imaginary));
        }

        /// <summary>
        /// Divides a ComplexNumber by another ComplexNumber.
        /// </summary>
        /// <param name="value1">The source ComplexNumber.</param>
        /// <param name="value2">The divisor.</param>
        /// <returns>The result of the division.</returns>
        public static ComplexNumber operator /(ComplexNumber value1, ComplexNumber value2) => Divide(value1, value2);


        /// <summary>
        /// Divides a ComplexNumber by another ComplexNumber.
        /// </summary>
        /// <param name="real">The source ComplexNumber.</param>
        /// <param name="complex">The divisor.</param>
        /// <returns>The result of the division.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComplexNumber Reciprocal(ComplexNumber complex)
        {
            var oneOverDenom = 1 / (complex.Real * complex.Real + complex.Imaginary * complex.Imaginary);
            return new ComplexNumber(oneOverDenom * complex.Real, -oneOverDenom * complex.Imaginary);
        }
        /// <summary>
        /// Divides a ComplexNumber by another ComplexNumber.
        /// </summary>
        /// <param name="real">The source ComplexNumber.</param>
        /// <param name="complex">The divisor.</param>
        /// <returns>The result of the division.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComplexNumber Divide(double real, ComplexNumber complex) => real * Reciprocal(complex);

        /// <summary>
        /// Divides a ComplexNumber by another ComplexNumber.
        /// </summary>
        /// <param name="value1">The source ComplexNumber.</param>
        /// <param name="value2">The divisor.</param>
        /// <returns>The result of the division.</returns>
        public static ComplexNumber operator /(double value1, ComplexNumber value2) => Divide(value1, value2);


        /// <summary>
        /// SQRTs the specified value1.
        /// </summary>
        /// <param name="value1">The value1.</param>
        /// <returns>ComplexNumber.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComplexNumber Sqrt(ComplexNumber value1)
        {
            if (value1.IsRealNumber)
            {
                if (value1.Real > 0)
                    return new ComplexNumber(Math.Sqrt(value1.Real));
                return new ComplexNumber(0, Math.Sqrt(-value1.Real));
            }
            var angle = Math.Atan2(value1.Imaginary, value1.Real);
            angle /= 2;
            var radius = Math.Sqrt(value1.Length());
            return new ComplexNumber(radius * Math.Cos(angle), radius * Math.Sin(angle));
        }

        /// <summary>
        /// CBRTs the specified value1.
        /// </summary>
        /// <param name="value1">The value1.</param>
        /// <returns>ComplexNumber.</returns>
        public static ComplexNumber Cbrt(ComplexNumber value1)
        {
            if (value1.IsRealNumber) return new ComplexNumber(Math.Cbrt(value1.Real));
            var angle = Math.Atan2(value1.Imaginary, value1.Real);
            angle /= 3;
            var radius = Math.Cbrt(value1.Length());
            return new ComplexNumber(radius * Math.Cos(angle), radius * Math.Sin(angle));
        }

        /// <summary>
        /// Returns a boolean indicating whether the two given ComplexNumbers are equal.
        /// </summary>
        /// <param name="value1">The first ComplexNumber to compare.</param>
        /// <param name="value2">The second ComplexNumber to compare.</param>
        /// <returns>True if the ComplexNumbers are equal; False otherwise.</returns>
        public static bool operator ==(ComplexNumber value1, ComplexNumber value2)
        {
            return value1.Real == value2.Real &&
                    value1.Imaginary == value2.Imaginary;
        }

        /// <summary>
        /// Returns a boolean indicating whether the two given ComplexNumbers are not equal.
        /// </summary>
        /// <param name="value1">The first ComplexNumber to compare.</param>
        /// <param name="value2">The second ComplexNumber to compare.</param>
        /// <returns>True if the ComplexNumbers are not equal; False if they are equal.</returns>
        public static bool operator !=(ComplexNumber value1, ComplexNumber value2)
        {
            return value1.Real != value2.Real ||
                    value1.Imaginary != value2.Imaginary;
        }

        /// <summary>
        /// Returns a boolean indicating whether the given ComplexNumber is equal to this ComplexNumber instance.
        /// </summary>
        /// <param name="other">The ComplexNumber to compare this instance to.</param>
        /// <returns>True if the other ComplexNumber is equal to this instance; False otherwise.</returns>
        public bool Equals(ComplexNumber other)
        {
            return Real == other.Real &&
                    Imaginary == other.Imaginary;
        }


        /// <summary>
        /// Gets a value indicating whether [just a real number] i.e. the imaginary part is zero.
        /// </summary>
        /// <value><c>true</c> if [just real]; otherwise, <c>false</c>.</value>
        public bool IsRealNumber => (Imaginary / Real).IsNegligible();


        /// <summary>
        /// Performs an explicit conversion from <see cref="ComplexNumber" /> to <see cref="System.Double" />.
        /// </summary>
        /// <param name="value1">The value1.</param>
        /// <returns>The result of the conversion.</returns>
        public static double ToDouble(ComplexNumber value1)
        {
            return value1.Real;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="ComplexNumber" /> to <see cref="System.Double" />.
        /// </summary>
        /// <param name="value1">The value1.</param>
        /// <returns>The result of the conversion.</returns>
        public static explicit operator double(ComplexNumber value1)
        {
            return value1.Real;
        }

        /// <summary>
        /// Returns a boolean indicating whether the given Object is equal to this ComplexNumber instance.
        /// </summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this ComplexNumber; False otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj is ComplexNumber ComplexNumber)
            {
                return Equals(ComplexNumber);
            }

            return false;
        }

        /// <summary>
        /// Returns a String representing this ComplexNumber instance.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            if (IsRealNumber) string.Format(CultureInfo.CurrentCulture, "{0}", Real);
            if (Imaginary < 0)
                return string.Format(CultureInfo.CurrentCulture, "{0} - {1}i", Real, -Imaginary);
            return string.Format(CultureInfo.CurrentCulture, "{0} + {1}i", Real, Imaginary);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return unchecked(Real.GetHashCode() + 1234567890 * Imaginary.GetHashCode());
        }

    }
}
