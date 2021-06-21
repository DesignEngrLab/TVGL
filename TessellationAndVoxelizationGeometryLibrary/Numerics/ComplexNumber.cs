using System;
using System.Globalization;

namespace TVGL.Numerics
{
    public readonly struct ComplexNumber
    {
        private const double SlerpEpsilon = 1e-6;

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
        public ComplexNumber(double real, double imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }

        /// <summary>
        /// Calculates the length of the ComplexNumber.
        /// </summary>
        /// <returns>The computed length of the ComplexNumber.</returns>
        public double Length()
        {
            return Math.Sqrt(LengthSquared());
        }

        /// <summary>
        /// Calculates the length squared of the ComplexNumber. This operation is cheaper than Length().
        /// </summary>
        /// <returns>The length squared of the ComplexNumber.</returns>
        public double LengthSquared()
        {
            return Real * Real + Imaginary * Imaginary;
        }

        /// <summary>
        /// Divides each component of the ComplexNumber by the length of the ComplexNumber.
        /// </summary>
        /// <param name="value">The source ComplexNumber.</param>
        /// <returns>The normalized ComplexNumber.</returns>
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
        /// <param name="value1">The source ComplexNumber.</param>
        /// <param name="value2">The scalar value.</param>
        /// <returns>The result of the multiplication.</returns>
        public static ComplexNumber Multiply(ComplexNumber value1, double value2)
        {
            return new ComplexNumber(value2 * value1.Real, value2 * value1.Imaginary);
        }

        /// <summary>
        /// Multiplies a ComplexNumber by a scalar value.
        /// </summary>
        /// <param name="value1">The source ComplexNumber.</param>
        /// <param name="value2">The scalar value.</param>
        /// <returns>The result of the multiplication.</returns>
        public static ComplexNumber operator *(ComplexNumber value1, double value2) => Multiply(value1, value2);

        /// <summary>
        /// Multiplies a ComplexNumber by a scalar value.
        /// </summary>
        /// <param name="value1">The scalar value.</param>
        /// <param name="value2">The source ComplexNumber.</param>
        /// <returns>The result of the multiplication.</returns>
        public static ComplexNumber operator *(double value1, ComplexNumber value2) => Multiply(value2, value1);

        /// <summary>
        /// Divides a ComplexNumber by another ComplexNumber.
        /// </summary>
        /// <param name="value1">The source ComplexNumber.</param>
        /// <param name="value2">The divisor.</param>
        /// <returns>The result of the division.</returns>
        public static ComplexNumber Divide(ComplexNumber value1, double value2)
        {
            var oneOverDenom = 1 / value2;
            return new ComplexNumber(oneOverDenom * value1.Real, oneOverDenom * value1.Imaginary);
        }


        /// <summary>
        /// Divides a ComplexNumber by another ComplexNumber.
        /// </summary>
        /// <param name="value1">The source ComplexNumber.</param>
        /// <param name="value2">The divisor.</param>
        /// <returns>The result of the division.</returns>
        public static ComplexNumber operator /(ComplexNumber value1, double value2) => Divide(value1, value2);

        /// <summary>
        /// Divides a ComplexNumber by another ComplexNumber.
        /// </summary>
        /// <param name="value1">The source ComplexNumber.</param>
        /// <param name="value2">The divisor.</param>
        /// <returns>The result of the division.</returns>
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
        /// <param name="value1">The source ComplexNumber.</param>
        /// <param name="value2">The divisor.</param>
        /// <returns>The result of the division.</returns>
        public static ComplexNumber Divide(double value1, ComplexNumber value2)
        {
            var oneOverDenom = 1 / (value2.Real * value2.Real + value2.Imaginary * value2.Imaginary);
            return new ComplexNumber(oneOverDenom * value1 * value2.Real, -oneOverDenom * value2.Imaginary);
        }

        /// <summary>
        /// Divides a ComplexNumber by another ComplexNumber.
        /// </summary>
        /// <param name="value1">The source ComplexNumber.</param>
        /// <param name="value2">The divisor.</param>
        /// <returns>The result of the division.</returns>
        public static ComplexNumber operator /(double value1, ComplexNumber value2) => Divide(value1, value2);


        public static ComplexNumber Sqrt(ComplexNumber value1)
        {
            if (value1.JustRealNumber) return new ComplexNumber(Math.Sqrt(value1.Real), 0);
            var angle = Math.Atan2(value1.Imaginary, value1.Real);
            angle *= 0.5;
            var radius = Math.Sqrt(Math.Sqrt(value1.Real * value1.Real + value1.Imaginary * value1.Imaginary));
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
        public bool JustRealNumber => Imaginary.IsNegligible();


        /// <summary>
        /// Performs an explicit conversion from <see cref="ComplexNumber"/> to <see cref="System.Double"/>.
        /// </summary>
        /// <param name="value1">The value1.</param>
        /// <returns>The result of the conversion.</returns>
        public static double ToDouble(ComplexNumber value1)
        {
            return value1.Real;
        }

        /// <summary>
        /// Performs an explicit conversion from <see cref="ComplexNumber"/> to <see cref="System.Double"/>.
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
