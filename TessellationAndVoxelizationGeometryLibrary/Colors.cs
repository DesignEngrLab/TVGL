// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="Colors.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// Enum KnownColors
    /// </summary>
    public enum KnownColors : uint
    {
        /// <summary>
        /// The unknown color
        /// </summary>
        Black = 4278190080U,

        /// <summary>
        /// The navy
        /// </summary>
        Navy = 4278190208U,

        /// <summary>
        /// The dark blue
        /// </summary>
        DarkBlue = 4278190219U,

        /// <summary>
        /// The medium blue
        /// </summary>
        MediumBlue = 4278190285U,

        /// <summary>
        /// The blue
        /// </summary>
        Blue = 4278190335U,

        /// <summary>
        /// The dark green
        /// </summary>
        DarkGreen = 4278215680U,

        /// <summary>
        /// The green
        /// </summary>
        Green = 4278222848U,

        /// <summary>
        /// The teal
        /// </summary>
        Teal = 4278222976U,

        /// <summary>
        /// The dark cyan
        /// </summary>
        DarkCyan = 4278225803U,

        /// <summary>
        /// The deep sky blue
        /// </summary>
        DeepSkyBlue = 4278239231U,

        /// <summary>
        /// The dark turquoise
        /// </summary>
        DarkTurquoise = 4278243025U,

        /// <summary>
        /// The medium spring green
        /// </summary>
        MediumSpringGreen = 4278254234U,

        /// <summary>
        /// The lime
        /// </summary>
        Lime = 4278255360U,

        /// <summary>
        /// The spring green
        /// </summary>
        SpringGreen = 4278255487U,

        /// <summary>
        /// The aqua
        /// </summary>
        Aqua = 4278255615U,

        /// <summary>
        /// The cyan
        /// </summary>
        Cyan = 4278255615U,

        /// <summary>
        /// The midnight blue
        /// </summary>
        MidnightBlue = 4279834992U,

        /// <summary>
        /// The dodger blue
        /// </summary>
        DodgerBlue = 4280193279U,

        /// <summary>
        /// The light sea green
        /// </summary>
        LightSeaGreen = 4280332970U,

        /// <summary>
        /// The forest green
        /// </summary>
        ForestGreen = 4280453922U,

        /// <summary>
        /// The sea green
        /// </summary>
        SeaGreen = 4281240407U,

        /// <summary>
        /// The dark slate gray
        /// </summary>
        DarkSlateGray = 4281290575U,

        /// <summary>
        /// The lime green
        /// </summary>
        LimeGreen = 4281519410U,

        /// <summary>
        /// The medium sea green
        /// </summary>
        MediumSeaGreen = 4282168177U,

        /// <summary>
        /// The turquoise
        /// </summary>
        Turquoise = 4282441936U,

        /// <summary>
        /// The royal blue
        /// </summary>
        RoyalBlue = 4282477025U,

        /// <summary>
        /// The steel blue
        /// </summary>
        SteelBlue = 4282811060U,

        /// <summary>
        /// The dark slate blue
        /// </summary>
        DarkSlateBlue = 4282924427U,

        /// <summary>
        /// The medium turquoise
        /// </summary>
        MediumTurquoise = 4282962380U,

        /// <summary>
        /// The indigo
        /// </summary>
        Indigo = 4283105410U,

        /// <summary>
        /// The dark olive green
        /// </summary>
        DarkOliveGreen = 4283788079U,

        /// <summary>
        /// The cadet blue
        /// </summary>
        CadetBlue = 4284456608U,

        /// <summary>
        /// The cornflower blue
        /// </summary>
        CornflowerBlue = 4284782061U,

        /// <summary>
        /// The medium aquamarine
        /// </summary>
        MediumAquamarine = 4284927402U,

        /// <summary>
        /// The dim gray
        /// </summary>
        DimGray = 4285098345U,

        /// <summary>
        /// The slate blue
        /// </summary>
        SlateBlue = 4285160141U,

        /// <summary>
        /// The olive drab
        /// </summary>
        OliveDrab = 4285238819U,

        /// <summary>
        /// The slate gray
        /// </summary>
        SlateGray = 4285563024U,

        /// <summary>
        /// The light slate gray
        /// </summary>
        LightSlateGray = 4286023833U,

        /// <summary>
        /// The medium slate blue
        /// </summary>
        MediumSlateBlue = 4286277870U,

        /// <summary>
        /// The lawn green
        /// </summary>
        LawnGreen = 4286381056U,

        /// <summary>
        /// The chartreuse
        /// </summary>
        Chartreuse = 4286578432U,

        /// <summary>
        /// The aquamarine
        /// </summary>
        Aquamarine = 4286578644U,

        /// <summary>
        /// The maroon
        /// </summary>
        Maroon = 4286578688U,

        /// <summary>
        /// The purple
        /// </summary>
        Purple = 4286578816U,

        /// <summary>
        /// The olive
        /// </summary>
        Olive = 4286611456U,

        /// <summary>
        /// The gray
        /// </summary>
        Gray = 4286611584U,

        /// <summary>
        /// The sky blue
        /// </summary>
        SkyBlue = 4287090411U,

        /// <summary>
        /// The light sky blue
        /// </summary>
        LightSkyBlue = 4287090426U,

        /// <summary>
        /// The blue violet
        /// </summary>
        BlueViolet = 4287245282U,

        /// <summary>
        /// The dark red
        /// </summary>
        DarkRed = 4287299584U,

        /// <summary>
        /// The dark magenta
        /// </summary>
        DarkMagenta = 4287299723U,

        /// <summary>
        /// The saddle brown
        /// </summary>
        SaddleBrown = 4287317267U,

        /// <summary>
        /// The dark sea green
        /// </summary>
        DarkSeaGreen = 4287609999U,

        /// <summary>
        /// The light green
        /// </summary>
        LightGreen = 4287688336U,

        /// <summary>
        /// The medium purple
        /// </summary>
        MediumPurple = 4287852763U,

        /// <summary>
        /// The dark violet
        /// </summary>
        DarkViolet = 4287889619U,

        /// <summary>
        /// The pale green
        /// </summary>
        PaleGreen = 4288215960U,

        /// <summary>
        /// The dark orchid
        /// </summary>
        DarkOrchid = 4288230092U,

        /// <summary>
        /// The yellow green
        /// </summary>
        YellowGreen = 4288335154U,

        /// <summary>
        /// The sienna
        /// </summary>
        Sienna = 4288696877U,

        /// <summary>
        /// The brown
        /// </summary>
        Brown = 4289014314U,

        /// <summary>
        /// The dark gray
        /// </summary>
        DarkGray = 4289309097U,

        /// <summary>
        /// The light blue
        /// </summary>
        LightBlue = 4289583334U,

        /// <summary>
        /// The green yellow
        /// </summary>
        GreenYellow = 4289593135U,

        /// <summary>
        /// The pale turquoise
        /// </summary>
        PaleTurquoise = 4289720046U,

        /// <summary>
        /// The light steel blue
        /// </summary>
        LightSteelBlue = 4289774814U,

        /// <summary>
        /// The powder blue
        /// </summary>
        PowderBlue = 4289781990U,

        /// <summary>
        /// The firebrick
        /// </summary>
        Firebrick = 4289864226U,

        /// <summary>
        /// The dark goldenrod
        /// </summary>
        DarkGoldenrod = 4290283019U,

        /// <summary>
        /// The medium orchid
        /// </summary>
        MediumOrchid = 4290401747U,

        /// <summary>
        /// The rosy brown
        /// </summary>
        RosyBrown = 4290547599U,

        /// <summary>
        /// The dark khaki
        /// </summary>
        DarkKhaki = 4290623339U,

        /// <summary>
        /// The silver
        /// </summary>
        Silver = 4290822336U,

        /// <summary>
        /// The medium violet red
        /// </summary>
        MediumVioletRed = 4291237253U,

        /// <summary>
        /// The indian red
        /// </summary>
        IndianRed = 4291648604U,

        /// <summary>
        /// The peru
        /// </summary>
        Peru = 4291659071U,

        /// <summary>
        /// The chocolate
        /// </summary>
        Chocolate = 4291979550U,

        /// <summary>
        /// The tan
        /// </summary>
        Tan = 4291998860U,

        /// <summary>
        /// The light gray
        /// </summary>
        LightGray = 4292072403U,

        /// <summary>
        /// The thistle
        /// </summary>
        Thistle = 4292394968U,

        /// <summary>
        /// The orchid
        /// </summary>
        Orchid = 4292505814U,

        /// <summary>
        /// The goldenrod
        /// </summary>
        Goldenrod = 4292519200U,

        /// <summary>
        /// The pale violet red
        /// </summary>
        PaleVioletRed = 4292571283U,

        /// <summary>
        /// The crimson
        /// </summary>
        Crimson = 4292613180U,

        /// <summary>
        /// The gainsboro
        /// </summary>
        Gainsboro = 4292664540U,

        /// <summary>
        /// The plum
        /// </summary>
        Plum = 4292714717U,

        /// <summary>
        /// The burly wood
        /// </summary>
        BurlyWood = 4292786311U,

        /// <summary>
        /// The light cyan
        /// </summary>
        LightCyan = 4292935679U,

        /// <summary>
        /// The lavender
        /// </summary>
        Lavender = 4293322490U,

        /// <summary>
        /// The dark salmon
        /// </summary>
        DarkSalmon = 4293498490U,

        /// <summary>
        /// The violet
        /// </summary>
        Violet = 4293821166U,

        /// <summary>
        /// The pale goldenrod
        /// </summary>
        PaleGoldenrod = 4293847210U,

        /// <summary>
        /// The light coral
        /// </summary>
        LightCoral = 4293951616U,

        /// <summary>
        /// The khaki
        /// </summary>
        Khaki = 4293977740U,

        /// <summary>
        /// The alice blue
        /// </summary>
        AliceBlue = 4293982463U,

        /// <summary>
        /// The honeydew
        /// </summary>
        Honeydew = 4293984240U,

        /// <summary>
        /// The azure
        /// </summary>
        Azure = 4293984255U,

        /// <summary>
        /// The sandy brown
        /// </summary>
        SandyBrown = 4294222944U,

        /// <summary>
        /// The wheat
        /// </summary>
        Wheat = 4294303411U,

        /// <summary>
        /// The beige
        /// </summary>
        Beige = 4294309340U,

        /// <summary>
        /// The white smoke
        /// </summary>
        WhiteSmoke = 4294309365U,

        /// <summary>
        /// The mint cream
        /// </summary>
        MintCream = 4294311930U,

        /// <summary>
        /// The ghost white
        /// </summary>
        GhostWhite = 4294506751U,

        /// <summary>
        /// The salmon
        /// </summary>
        Salmon = 4294606962U,

        /// <summary>
        /// The antique white
        /// </summary>
        AntiqueWhite = 4294634455U,

        /// <summary>
        /// The linen
        /// </summary>
        Linen = 4294635750U,

        /// <summary>
        /// The light goldenrod yellow
        /// </summary>
        LightGoldenrodYellow = 4294638290U,

        /// <summary>
        /// The old lace
        /// </summary>
        OldLace = 4294833638U,

        /// <summary>
        /// The red
        /// </summary>
        Red = 4294901760U,

        /// <summary>
        /// The fuchsia
        /// </summary>
        Fuchsia = 4294902015U,

        /// <summary>
        /// The magenta
        /// </summary>
        Magenta = 4294902015U,

        /// <summary>
        /// The deep pink
        /// </summary>
        DeepPink = 4294907027U,

        /// <summary>
        /// The orange red
        /// </summary>
        OrangeRed = 4294919424U,

        /// <summary>
        /// The tomato
        /// </summary>
        Tomato = 4294927175U,

        /// <summary>
        /// The hot pink
        /// </summary>
        HotPink = 4294928820U,

        /// <summary>
        /// The coral
        /// </summary>
        Coral = 4294934352U,

        /// <summary>
        /// The dark orange
        /// </summary>
        DarkOrange = 4294937600U,

        /// <summary>
        /// The light salmon
        /// </summary>
        LightSalmon = 4294942842U,

        /// <summary>
        /// The orange
        /// </summary>
        Orange = 4294944000U,

        /// <summary>
        /// The light pink
        /// </summary>
        LightPink = 4294948545U,

        /// <summary>
        /// The pink
        /// </summary>
        Pink = 4294951115U,

        /// <summary>
        /// The gold
        /// </summary>
        Gold = 4294956800U,

        /// <summary>
        /// The peach puff
        /// </summary>
        PeachPuff = 4294957753U,

        /// <summary>
        /// The navajo white
        /// </summary>
        NavajoWhite = 4294958765U,

        /// <summary>
        /// The moccasin
        /// </summary>
        Moccasin = 4294960309U,

        /// <summary>
        /// The bisque
        /// </summary>
        Bisque = 4294960324U,

        /// <summary>
        /// The misty rose
        /// </summary>
        MistyRose = 4294960353U,

        /// <summary>
        /// The blanched almond
        /// </summary>
        BlanchedAlmond = 4294962125U,

        /// <summary>
        /// The papaya whip
        /// </summary>
        PapayaWhip = 4294963157U,

        /// <summary>
        /// The lavender blush
        /// </summary>
        LavenderBlush = 4294963445U,

        /// <summary>
        /// The sea shell
        /// </summary>
        SeaShell = 4294964718U,

        /// <summary>
        /// The cornsilk
        /// </summary>
        Cornsilk = 4294965468U,

        /// <summary>
        /// The lemon chiffon
        /// </summary>
        LemonChiffon = 4294965965U,

        /// <summary>
        /// The floral white
        /// </summary>
        FloralWhite = 4294966000U,

        /// <summary>
        /// The snow
        /// </summary>
        Snow = 4294966010U,

        /// <summary>
        /// The yellow
        /// </summary>
        Yellow = 4294967040U,

        /// <summary>
        /// The light yellow
        /// </summary>
        LightYellow = 4294967264U,

        /// <summary>
        /// The ivory
        /// </summary>
        Ivory = 4294967280U,

        /// <summary>
        /// The white
        /// </summary>
        White = 4294967295U
    }

    /// <summary>
    /// Struct Color
    /// </summary>
    public class Color
    {
        /// <summary>
        /// Converts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Single.</returns>
        private static float Convert(byte value)
        {
            return value / 255f;
        }

        /// <summary>
        /// Converts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Byte.</returns>
        private static byte Convert(float value)
        {
            if (value < 0.0f)
                return 0;
            if (value > 1.0f)
                return 255;
            return (byte)(value * 255f);
        }

        /// <summary>
        /// Checks if color is equal to another color
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (!(obj is Color)) return false;
            var otherColor = (Color)obj;
            return A == otherColor.A && B == otherColor.B
                   && G == otherColor.G && R == otherColor.R;
        }

        /// <summary>
        /// Gets the Hash code for the object.
        /// </summary>
        /// <returns>System.Int.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Color" /> struct.
        /// </summary>
        /// <param name="knownColor">Color of the known.</param>
        public Color(KnownColors knownColor)
            : this((uint)knownColor)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color" /> class.
        /// </summary>
        /// <param name="amfColor">Color of the amf.</param>
        internal Color(amfclasses.AMF_Color amfColor)
            : this(amfColor.a, amfColor.r, amfColor.g, amfColor.b)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color" /> struct.
        /// </summary>
        /// <param name="argb">The ARGB.</param>
        public Color(uint argb)
        {
            A = (byte)((argb & 0xff000000) >> 24);
            R = (byte)((argb & 0x00ff0000) >> 16);
            G = (byte)((argb & 0x0000ff00) >> 8);
            B = (byte)(argb & 0x000000ff);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color" /> struct.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="r">The r.</param>
        /// <param name="g">The g.</param>
        /// <param name="b">The b.</param>
        public Color(float a, float r, float g, float b)
        {
            A = Convert(a);
            R = Convert(r);
            G = Convert(g);
            B = Convert(b);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color" /> struct.
        /// </summary>
        /// <param name="r">The r.</param>
        /// <param name="g">The g.</param>
        /// <param name="b">The b.</param>
        public Color(float r, float g, float b)
        {
            A = 1;
            R = Convert(r);
            G = Convert(g);
            B = Convert(b);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color" /> struct.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="r">The r.</param>
        /// <param name="g">The g.</param>
        /// <param name="b">The b.</param>
        public Color(byte a, byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color" /> struct.
        /// </summary>
        /// <param name="r">The r.</param>
        /// <param name="g">The g.</param>
        /// <param name="b">The b.</param>
        public Color(byte r, byte g, byte b)
            : this(0xff, r, g, b)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color"/> class.
        /// </summary>
        public Color()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Color" /> class.
        /// </summary>
        /// <param name="colorString">The color string.</param>
        public Color(string colorString)
        {
            if (!string.IsNullOrWhiteSpace(colorString) && (colorString.Length == 7 || colorString.Length == 9))
            {
                R = System.Convert.ToByte(colorString.Substring(1, 2), 16);
                G = System.Convert.ToByte(colorString.Substring(3, 2), 16);
                B = System.Convert.ToByte(colorString.Substring(5, 2), 16);
                if (colorString.Length == 9)
                {
                    A = System.Convert.ToByte(colorString.Substring(7, 2), 16);
                }
                else A = 255;
            }
        }

        /// <summary>
        /// The hue offset
        /// </summary>
        const double HueOffset = -0.3;
        /// <summary>
        /// The hue contraction
        /// </summary>
        const double HueContraction = 0.7;
        /// <summary>
        /// Converts the HSV color values to RGB.
        /// This is based on the formula at: https://en.wikipedia.org/wiki/HSL_and_HSV#HSV_to_RGB
        /// </summary>
        /// <param name="hue">The hue.</param>
        /// <param name="saturation">The saturation.</param>
        /// <param name="value">The value.</param>
        /// <param name="alpha">The alpha.</param>
        /// <returns>Color.</returns>
        public static Color HSVtoRGB(double hue, double saturation = 1, double value = 1, double alpha = 1)
        {
            hue += HueOffset;
            if (hue < 0) hue = Math.Abs(1 + hue);
            if (hue > 1) hue = hue % 1;
            hue *= HueContraction;
            if (saturation < 0) saturation = Math.Abs(1 + saturation);
            if (saturation > 1) saturation = saturation % 1;
            if (value < 0) value = Math.Abs(1 + value);
            if (value > 1) value = value % 1;

            var chroma = value * saturation;
            var huePrime = 6 * hue;
            var x = chroma * (1 - Math.Abs(huePrime % 2 - 1));
            double red, green, blue;
            if (huePrime > 5)
            {
                red = chroma;
                green = 0;
                blue = x;
            }
            else if (huePrime > 4)
            {
                red = x;
                green = 0;
                blue = chroma;
            }
            else if (huePrime > 3)
            {
                red = 0;
                green = x;
                blue = chroma;
            }
            else if (huePrime > 2)
            {
                red = 0;
                green = chroma;
                blue = x;
            }
            else if (huePrime > 1)
            {
                red = x;
                green = chroma;
                blue = 0;
            }
            else
            {
                red = chroma;
                green = x;
                blue = 0;
            }
            var m = value - chroma;
            red += m;
            green += m;
            blue += m;

            return new Color((float)alpha, (float)red, (float)green, (float)blue);
        }

        #endregion Constructors

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            string hex = "#" + BitConverter.ToString(new[] { R, G, B, A });
            return hex.Replace("-", "");
        }

        #region Public Properties

        /// <summary>
        /// A
        /// </summary>
        [JsonIgnore]
        public byte A;

        /// <summary>
        /// B
        /// </summary>
        [JsonIgnore]
        public byte B;

        /// <summary>
        /// R
        /// </summary>
        [JsonIgnore]
        public byte R;
        /// <summary>
        /// G
        /// </summary>
        [JsonIgnore]
        public byte G;

        /// <summary>
        /// Gets or sets the opacity property of the color
        /// </summary>
        /// <value>The Alpha channel as a float whose range is [0..1].
        /// the value is allowed to be out of range</value>
        [JsonIgnore]
        public float Af
        {
            get => Convert(A);
            set => A = Convert(value);
        }

        /// <summary>
        /// Gets or sets the red property of the color
        /// </summary>
        /// <value>The rf.</value>
        [JsonIgnore]
        public float Rf
        {
            get => Convert(R);
            set => R = Convert(value);
        }

        /// <summary>
        /// Gets or sets the green property of the color.
        /// </summary>
        /// <value>The gf.</value>
        [JsonIgnore]
        public float Gf
        {
            get => Convert(G);
            set => G = Convert(value);
        }

        /// <summary>
        /// Gets or sets the blue property of the color.
        /// </summary>
        /// <value>The bf.</value>
        [JsonIgnore]
        public float Bf
        {
            get => Convert(B);
            set => B = Convert(value);
        }

        /// <summary>
        /// Gets the hue as a float.
        /// </summary>
        /// <returns>System.Single.</returns>
        public float GetHue()
        {
            float h;
            var min = Math.Min(Rf, Math.Min(Gf, Bf));
            var max = Math.Max(Rf, Math.Max(Gf, Bf));
            var delta = max - min;
            if (max <= 0)
                return 0f;
            if (Rf == max)
                h = (Gf - Bf) / delta; // between yellow & magenta
            else if (Gf == max)
                h = 2 + (Bf - Rf) / delta; // between cyan & yellow
            else
                h = 4 + (Rf - Gf) / delta; // between magenta & cyan
            h /= 6f; // degrees
            if (h < 0)
                h += 1f;
            return h;
        }

        /// <summary>
        /// Gets the saturation as a float.
        /// </summary>
        /// <returns>System.Single.</returns>
        public float GetSaturation()
        {
            var min = Math.Min(Rf, Math.Min(Gf, Bf));
            var max = Math.Max(Rf, Math.Max(Gf, Bf));
            var delta = max - min;
            if (max != 0)
                return delta / max; // s
            else return 0f;
        }

        /// <summary>
        /// Gets the value (as in the hue-saturation-value) of the color as a float.
        /// </summary>
        /// <returns>System.Single.</returns>
        public float GetValue()
        {
            return Math.Max(Rf, Math.Max(Gf, Bf));
        }

        #endregion Public Properties

        #region new known colors

        /// <summary>
        /// Gets the random color names.
        /// </summary>
        /// <returns>IEnumerable&lt;System.String&gt;.</returns>
        public static IEnumerable<string> GetRandomColorNames(string seed)
        {
            int hash1 = 5381;
            int hash2 = hash1;

            for (int i = 0; i < seed.Length && seed[i] != '\0'; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ seed[i];
                if (i == seed.Length - 1 || seed[i + 1] == '\0')
                    break;
                hash2 = ((hash2 << 5) + hash2) ^ seed[i + 1];
            }

            var intSeed = hash1 + (hash2 * 1566083941);
            return GetRandomColorNames(intSeed);
        }
        /// <summary>
        /// Gets the random color names.
        /// </summary>
        /// <returns>IEnumerable&lt;System.String&gt;.</returns>
        public static IEnumerable<string> GetRandomColorNames(int seed = int.MinValue)
        {
            var random = seed == int.MinValue ? new Random() : new Random(seed);
            var families = ColorDictionary.Values.OrderBy(dummy => random.NextDouble())
                .Select(dict => dict.Keys.OrderBy(dummy2 => random.NextDouble()).ToList()).ToList();
            var innerIndex = 0;
            for (int i = 0; i < families.Count; i++)
            {
                yield return families[i][innerIndex % families[i].Count];
                if (i == families.Count - 1)
                {
                    innerIndex++;
                    i = 0; //this will make it cycle forever
                }
            }
        }
        /// <summary>
        /// Gets the random colors.
        /// </summary>
        /// <param name="seed">The seed.</param>
        /// <returns>IEnumerable&lt;Color&gt;.</returns>
        public static IEnumerable<Color> GetRandomColors(int seed = int.MinValue)
        {
            var random = seed == int.MinValue ? new Random() : new Random(seed);
            var families = ColorDictionary.Values.OrderBy(dummy => random.NextDouble())
                .Select(dict => dict.Values.OrderBy(dummy2 => random.NextDouble()).ToList()).ToList();
            var innerIndex = 0;
            for (int i = 0; i < families.Count; i++)
            {
                yield return families[i][innerIndex % families[i].Count];
                if (i == families.Count - 1)
                {
                    innerIndex++;
                    i = 0; //this will make it cycle forever
                }
            }
        }
        /// <summary>
        /// Gets the name of the color from.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Color.</returns>
        public static Color GetColorFromName(string name)
        {
            foreach (var dict in ColorDictionary.Values)
                if (dict.TryGetValue(name, out var color))
                    return color;
            return null;
        }

        /// <summary>
        /// The new known colors are taken from http://www.workwithcolor.com/
        /// </summary>
        public static Dictionary<ColorFamily, Dictionary<string, Color>> ColorDictionary
            = new Dictionary<ColorFamily, Dictionary<string, Color>>()
            {
                { ColorFamily.Red, new Dictionary<string, Color>()
                    {
                        {"Snow", new Color(255, 250, 250)},
                        {"Baby Pink", new Color(244, 194, 194)},
                        {"Pastel Red", new Color(255, 105, 97)},
                        {"Indian Red", new Color(255, 92, 92) },
                        { "Ferrari Red", new Color(255, 28, 0)},
                        { "Candy Apple Red", new Color(255, 8, 0)},
                        { "Red", new Color(255, 0, 0)},
                        { "Chestnut", new Color(205, 92, 92)},
                        { "Cinnabar", new Color(227, 66, 52)},
                        { "Jasper", new Color(215, 59, 62)},
                        { "Fire Engine Red", new Color(206, 22, 32)},
                        { "Boston University Red", new Color(204, 0, 0)},
                        { "Firebrick", new Color(178, 34, 34)},
                        { "Cornell Red", new Color(179, 27, 27)},
                        { "Dark Candy Apple Red", new Color(164, 0, 0)},
                        { "Maroon", new Color(128, 0, 0)},
                        { "Prune", new Color(112, 28, 28)},
                        { "Dark Sienna", new Color(60, 20, 20)},
                        { "Seal Brown", new Color(50, 20, 20) } } },
                { ColorFamily.Pink, new Dictionary<string, Color>()
                    {
                        { "Bubble Gum", new Color(255, 193, 204)},
                        {"Amaranth", new Color(229, 43, 80)},
                        {"Dark Terra Cotta", new Color(204, 78, 92)},
                        {"Bazaar", new Color(152, 119, 123)},
                        {"Alizarin", new Color(227, 38, 54)},
                        {"Crimson", new Color(220, 20, 60)},
                        {"Cadmium Red", new Color(227, 0, 34)},
                        {"Cardinal", new Color(196, 30, 58)},
                        {"Cordovan", new Color(137, 63, 69)},
                        {"Carmine", new Color(150, 0, 24)},
                        {"Rosewood", new Color(101, 0, 11)},
                        {"Pastel Pink", new Color(255, 209, 220)},
                        {"Pink", new Color(255, 192, 203)},
                        {"Cherry Blossom Pink", new Color(255, 183, 197)},
                        {"Flamingo Pink", new Color(252, 142, 172)},
                        {"Dark Pink", new Color(231, 84, 128)},
                        {"Blush", new Color(222, 93, 131)},
                        {"Cherry", new Color(222, 49, 99)},
                        {"Raspberry", new Color(227, 11, 93)},
                        {"Ruby", new Color(224, 17, 95)},
                        {"Bright Maroon", new Color(195, 33, 72)},
                        {"Burgundy", new Color(128, 0, 32)},
                        {"Dark Scarlet", new Color(86, 3, 25) }
                    } },
                { ColorFamily.Magenta, new Dictionary<string, Color>()
                    {
                        { "Lavender Blush", new Color(255, 240, 245)},
                        {"Classic Rose", new Color(251, 204, 231)},
                        { "Pastel Magenta", new Color(244, 154, 194)},
                        { "Sky Magenta", new Color(207, 113, 175)},
                        { "Bright Pink", new Color(255, 0, 127)},
                        { "Dark Raspberry", new Color(135, 38, 87)},
                        { "Eggplant", new Color(97, 64, 81)},
                        { "Bright Ube", new Color(209, 159, 232)},
                        { "Magenta", new Color(255, 0, 255)},
                        { "Pastel Violet", new Color(203, 153, 201)},
                        { "Pastel Purple", new Color(179, 158, 181)},
                        { "Byzantine", new Color(189, 51, 164)},
                        { "Dark Violet", new Color(148, 0, 211)},
                        { "Plum", new Color(142, 69, 133)},
                        { "Dark Magenta", new Color(139, 0, 139)},
                        { "Byzantium", new Color(112, 41, 99)},
                        { "Dark Byzantium", new Color(93, 57, 84)},
                        { "Bright Lavender", new Color(191, 148, 228)},
                        { "Lavender", new Color(181, 126, 220)},
                        { "Dark Pastel Purple", new Color(150, 111, 214)},
                        { "Violet", new Color(143, 0, 255)},
                        { "Dark Lavender", new Color(115, 79, 150)},
                        { "Purple Heart", new Color(105, 53, 156) }
                    } },
                { ColorFamily.Blue, new Dictionary<string, Color>()
                    {
                        { "Ghost White", new Color(248, 248, 255)},
                        { "Lavender Blue", new Color(204, 204, 255)},
                        { "Lavender Gray", new Color(196, 195, 208)},
                        { "Ceil", new Color(146, 161, 207)},
                        { "Cool Grey", new Color(140, 146, 172)},
                        { "Blue", new Color(0, 0, 255)},
                        { "Cerulean Blue", new Color(42, 82, 190)},
                        { "International Klein Blue", new Color(0, 47, 167)},
                        { "Dark Powder Blue", new Color(0, 51, 153)},
                        { "Duke Blue", new Color(0, 0, 156)},
                        { "Ultramarine", new Color(18, 10, 143)},
                        { "Dark Blue", new Color(0, 0, 139)},
                        { "Navy Blue", new Color(0, 0, 128)},
                        { "Midnight Blue", new Color(25, 25, 112)},
                        { "Sapphire", new Color(8, 37, 103)},
                        { "Royal Blue", new Color(0, 35, 102) }
                    } },
                { ColorFamily.CyanBlue, new Dictionary<string, Color>()
                    {
                        {"Ghost White", new Color(248, 248, 255)},
                        { "Lavender Blue", new Color(204, 204, 255)},
                        { "Lavender Gray", new Color(196, 195, 208)},
                        { "Ceil", new Color(146, 161, 207)},
                        { "Cool Grey", new Color(140, 146, 172)},
                        { "Blue", new Color(0, 0, 255)},
                        { "Cerulean Blue", new Color(42, 82, 190)},
                        { "International Klein Blue", new Color(0, 47, 167)},
                        { "Dark Powder Blue", new Color(0, 51, 153)},
                        { "Duke Blue", new Color(0, 0, 156)},
                        { "Ultramarine", new Color(18, 10, 143)},
                        { "Dark Blue", new Color(0, 0, 139)},
                        { "Navy Blue", new Color(0, 0, 128)},
                        { "Midnight Blue", new Color(25, 25, 112)},
                        { "Sapphire", new Color(8, 37, 103)},
                        { "Royal Blue", new Color(0, 35, 102) }
                    } },
                { ColorFamily.Cyan, new Dictionary<string, Color>()
                    {
                        { "Bubbles", new Color(231, 254, 255)},
                        { "Cyan", new Color(0, 255, 255)},
                        { "Columbia Blue", new Color(155, 221, 255)},
                        { "Bright Turquoise", new Color(8, 232, 222)},
                        { "Baby Blue", new Color(137, 207, 240)},
                        { "Sky Blue", new Color(135, 206, 235)},
                        { "Pastel Blue", new Color(174, 198, 207)},
                        { "Turquoise", new Color(48, 213, 200)},
                        { "Dark Cyan", new Color(0, 139, 139)},
                        { "Cerulean", new Color(0, 123, 167)},
                        { "Teal", new Color(0, 128, 128)},
                        { "Pine Green", new Color(1, 121, 111)},
                        { "Dark Slate Gray", new Color(47, 79, 79) }
                    } },
                { ColorFamily.GreenCyan, new Dictionary<string, Color>()
                    {
                        {"Aquamarine", new Color(127, 255, 212)},
                        {"Clover", new Color(0, 255, 111)},
                        {"Ash Grey", new Color(178, 190, 181)},
                        {"Cambridge Blue", new Color(163, 193, 173)},
                        {"Caribbean Green", new Color(0, 204, 153)},
                        {"Emerald", new Color(80, 200, 120)},
                        {"Mint", new Color(62, 180, 137)},
                        {"Dark Pastel Green", new Color(3, 192, 60)},
                        {"Jade", new Color(0, 168, 107)},
                        {"Xanadu", new Color(115, 134, 120)},
                        {"Tropical Rain Forest", new Color(0, 117, 94)},
                        {"Dark Spring Green", new Color(23, 114, 69)},
                        {"Cadmium Green", new Color(0, 107, 60)},
                        {"Forest Green", new Color(1, 68, 33)},
                        {"British Racing Green", new Color(0, 66, 37)},
                        {"Dark Green", new Color(1, 50, 32)},
                        {"Dark Jungle Green", new Color(26, 36, 33) }
                    } },
                { ColorFamily.Green, new Dictionary<string, Color>()
                    {
                        { "Inchworm", new Color(178, 236, 93)},
                        {"Lawn Green", new Color(124, 252, 0)},
                        {"Bright Green", new Color(102, 255, 0)},
                        {"Celadon", new Color(172, 225, 175)},
                        {"Pastel Green", new Color(119, 221, 119)},
                        {"Pistachio", new Color(147, 197, 114)},
                        {"Dollar Bill", new Color(133, 187, 101)},
                        {"Asparagus", new Color(135, 169, 107)},
                        {"Dark Pastel Green", new Color(3, 192, 60)},
                        {"Camouflage Green", new Color(120, 134, 107)},
                        {"India Green", new Color(19, 136, 8)},
                        {"Green", new Color(0, 128, 0)},
                        {"Dark Olive Green", new Color(85, 107, 47)},
                        {"Rifle Green", new Color(65, 72, 51)},
                        {"Chartreuse", new Color(223, 255, 0)},
                        {"Lime", new Color(191, 255, 0)},
                        {"Spring Bud", new Color(167, 252, 0)},
                        {"Pear", new Color(209, 226, 49)},
                        {"Android Green", new Color(164, 198, 57)},
                        {"Apple Green", new Color(141, 182, 0)},
                        {"Battleship Grey", new Color(132, 132, 130)},
                        {"Olive", new Color(128, 128, 0)},
                        {"Army Green", new Color(75, 83, 32) }
                    } },
                { ColorFamily.Yellow, new Dictionary<string, Color>()
                    {
                        { "Ivory", new Color(255, 255, 240)},
                        {"Cream", new Color(255, 253, 208)},
                        {"Pastel Yellow", new Color(253, 253, 150)},
                        {"Beige", new Color(245, 245, 220)},
                        {"Daffodil", new Color(255, 255, 49)},
                        {"Yellow", new Color(255, 255, 0)},
                        {"Icterine", new Color(252, 247, 94)},
                        {"Lemon", new Color(255, 247, 0)},
                        {"Canary Yellow", new Color(255, 239, 0)},
                        {"Flavescent", new Color(247, 233, 142)},
                        {"Corn", new Color(251, 236, 93)},
                        {"Golden Yellow", new Color(255, 223, 0)},
                        {"Titanium Yellow", new Color(238, 230, 0)},
                        {"Dandelion", new Color(240, 225, 48)},
                        {"Peridot", new Color(230, 226, 0)},
                        {"Straw", new Color(228, 217, 111)},
                        {"Sandstorm", new Color(236, 213, 64)},
                        {"Pastel Gray", new Color(207, 207, 196)},
                        {"Citrine", new Color(228, 208, 10)},
                        {"Dark Khaki", new Color(189, 183, 107)},
                        {"Brass", new Color(181, 166, 66) }
                    } },
                { ColorFamily.OrangeYellow, new Dictionary<string, Color>()
                    {
                        { "Cornsilk", new Color(255, 248, 220)},
                        { "Blond", new Color(250, 240, 190)},
                        { "Pearl", new Color(240, 234, 214)},
                        { "Platinum", new Color(229, 228, 226)},
                        { "Vanilla", new Color(243, 229, 171)},
                        { "Mustard", new Color(255, 219, 88)},
                        { "Buff", new Color(240, 220, 130)},
                        { "Banana Yellow", new Color(255, 209, 42)},
                        { "Arylide Yellow", new Color(233, 214, 107)},
                        { "Saffron", new Color(244, 196, 48)},
                        { "Amber", new Color(255, 191, 0)},
                        { "Sand", new Color(194, 178, 128)},
                        { "Goldenrod", new Color(218, 165, 32)},
                        { "Dark Goldenrod", new Color(184, 134, 11)},
                        { "Dark Tan", new Color(145, 129, 81)},
                        { "Sand Dune", new Color(150, 113, 23) }
                    } },
                { ColorFamily.OrangeBrown, new Dictionary<string, Color>()
                    {
                        { "Seashell", new Color(255, 245, 238)},
                        { "Champagne", new Color(247, 231, 206)},
                        { "Peach", new Color(255, 229, 180)},
                        { "Wheat", new Color(245, 222, 179)},
                        { "Sunset", new Color(250, 214, 165)},
                        { "Apricot", new Color(251, 206, 177)},
                        { "Desert Sand", new Color(237, 201, 175)},
                        { "Pastel Orange", new Color(255, 179, 71)},
                        { "Burlywood", new Color(222, 184, 135)},
                        { "Dark Tangerine", new Color(255, 168, 18)},
                        { "Chrome Yellow", new Color(255, 167, 0)},
                        { "Fawn", new Color(229, 170, 112)},
                        { "Earth Yellow", new Color(225, 169, 95)},
                        { "Indian Yellow", new Color(227, 168, 87)},
                        { "Khaki", new Color(195, 176, 145)},
                        { "Dark Orange", new Color(255, 140, 0)},
                        { "Gamboge", new Color(228, 155, 15)},
                        { "Carrot Orange", new Color(237, 145, 33)},
                        { "Orange", new Color(255, 127, 0)},
                        { "Tangerine", new Color(242, 133, 0)},
                        { "Cadmium Orange", new Color(237, 135, 45)},
                        { "Pumpkin", new Color(255, 117, 24)},
                        { "Desert", new Color(193, 154, 107)},
                        { "Safety Orange", new Color(255, 103, 0)},
                        { "Bronze", new Color(205, 127, 50)},
                        { "Ochre", new Color(204, 119, 34)},
                        { "Cinnamon", new Color(210, 105, 30)},
                        { "Copper", new Color(184, 115, 51)},
                        { "Chamoisee", new Color(160, 120, 90)},
                        { "Burnt Orange", new Color(204, 85, 0)},
                        { "Shadow", new Color(138, 121, 93)},
                        { "Golden Brown", new Color(153, 101, 21)},
                        { "Pastel Brown", new Color(131, 105, 83)},
                        { "Raw Umber", new Color(130, 102, 68)},
                        { "Brown", new Color(150, 75, 0)},
                        { "Chocolate", new Color(123, 63, 0)},
                        { "Sepia", new Color(112, 66, 20)},
                        { "Dark Brown", new Color(101, 67, 33)},
                        { "Dark Lava", new Color(72, 60, 50)},
                        { "Bistre", new Color(61, 43, 31) }
                    } },
                { ColorFamily.RedOrange, new Dictionary<string, Color>()
                    {
                        {"Atomic Tangerine", new Color(255, 153, 102)},
                        { "Salmon", new Color(255, 140, 105)},
                        { "Dark Salmon", new Color(233, 150, 122)},
                        { "Coral", new Color(255, 127, 80)},
                        { "Burnt Sienna", new Color(233, 116, 81)},
                        { "Portland Orange", new Color(255, 90, 54)},
                        { "Terra Cotta", new Color(226, 114, 91)},
                        { "Coquelicot", new Color(255, 56, 0)},
                        { "Flame", new Color(226, 88, 34)},
                        { "Cinereous", new Color(152, 129, 123)},
                        { "Dark Coral", new Color(205, 91, 69)},
                        { "Dark Chestnut", new Color(152, 105, 96)},
                        { "Sinopia", new Color(203, 65, 11)},
                        { "Dark Pastel Red", new Color(194, 59, 34)},
                        { "Mahogany", new Color(192, 64, 0)},
                        { "Rust", new Color(183, 65, 14)},
                        { "Bole", new Color(121, 68, 59)},
                        { "Burnt Umber", new Color(138, 51, 36)},
                        { "Auburn", new Color(109, 53, 26) }
                    } }
            };

        #endregion new known colors
    }

    /// <summary>
    /// Enum ColorFamily
    /// </summary>
    public enum ColorFamily
    {
        /// <summary>
        /// The red
        /// </summary>
        Red, Pink, Magenta, Blue, CyanBlue, Cyan, GreenCyan, Green, Yellow, OrangeYellow, OrangeBrown, RedOrange
    };
}