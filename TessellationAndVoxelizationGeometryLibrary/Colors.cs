// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 05-26-2016
// ***********************************************************************
// <copyright file="Colors.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Reflection;

namespace TVGL
{
    /// <summary>
    ///     Enum KnownColors
    /// </summary>
    public enum KnownColors : uint
    {
        /// <summary>
        ///     The unknown color
        /// </summary>
        UnknownColor = 1U,

        /// <summary>
        ///     The transparent
        /// </summary>
        Transparent = 16777215U,

        /// <summary>
        ///     The black
        /// </summary>
        Black = 4278190080U,

        /// <summary>
        ///     The navy
        /// </summary>
        Navy = 4278190208U,

        /// <summary>
        ///     The dark blue
        /// </summary>
        DarkBlue = 4278190219U,

        /// <summary>
        ///     The medium blue
        /// </summary>
        MediumBlue = 4278190285U,

        /// <summary>
        ///     The blue
        /// </summary>
        Blue = 4278190335U,

        /// <summary>
        ///     The dark green
        /// </summary>
        DarkGreen = 4278215680U,

        /// <summary>
        ///     The green
        /// </summary>
        Green = 4278222848U,

        /// <summary>
        ///     The teal
        /// </summary>
        Teal = 4278222976U,

        /// <summary>
        ///     The dark cyan
        /// </summary>
        DarkCyan = 4278225803U,

        /// <summary>
        ///     The deep sky blue
        /// </summary>
        DeepSkyBlue = 4278239231U,

        /// <summary>
        ///     The dark turquoise
        /// </summary>
        DarkTurquoise = 4278243025U,

        /// <summary>
        ///     The medium spring green
        /// </summary>
        MediumSpringGreen = 4278254234U,

        /// <summary>
        ///     The lime
        /// </summary>
        Lime = 4278255360U,

        /// <summary>
        ///     The spring green
        /// </summary>
        SpringGreen = 4278255487U,

        /// <summary>
        ///     The aqua
        /// </summary>
        Aqua = 4278255615U,

        /// <summary>
        ///     The cyan
        /// </summary>
        Cyan = 4278255615U,

        /// <summary>
        ///     The midnight blue
        /// </summary>
        MidnightBlue = 4279834992U,

        /// <summary>
        ///     The dodger blue
        /// </summary>
        DodgerBlue = 4280193279U,

        /// <summary>
        ///     The light sea green
        /// </summary>
        LightSeaGreen = 4280332970U,

        /// <summary>
        ///     The forest green
        /// </summary>
        ForestGreen = 4280453922U,

        /// <summary>
        ///     The sea green
        /// </summary>
        SeaGreen = 4281240407U,

        /// <summary>
        ///     The dark slate gray
        /// </summary>
        DarkSlateGray = 4281290575U,

        /// <summary>
        ///     The lime green
        /// </summary>
        LimeGreen = 4281519410U,

        /// <summary>
        ///     The medium sea green
        /// </summary>
        MediumSeaGreen = 4282168177U,

        /// <summary>
        ///     The turquoise
        /// </summary>
        Turquoise = 4282441936U,

        /// <summary>
        ///     The royal blue
        /// </summary>
        RoyalBlue = 4282477025U,

        /// <summary>
        ///     The steel blue
        /// </summary>
        SteelBlue = 4282811060U,

        /// <summary>
        ///     The dark slate blue
        /// </summary>
        DarkSlateBlue = 4282924427U,

        /// <summary>
        ///     The medium turquoise
        /// </summary>
        MediumTurquoise = 4282962380U,

        /// <summary>
        ///     The indigo
        /// </summary>
        Indigo = 4283105410U,

        /// <summary>
        ///     The dark olive green
        /// </summary>
        DarkOliveGreen = 4283788079U,

        /// <summary>
        ///     The cadet blue
        /// </summary>
        CadetBlue = 4284456608U,

        /// <summary>
        ///     The cornflower blue
        /// </summary>
        CornflowerBlue = 4284782061U,

        /// <summary>
        ///     The medium aquamarine
        /// </summary>
        MediumAquamarine = 4284927402U,

        /// <summary>
        ///     The dim gray
        /// </summary>
        DimGray = 4285098345U,

        /// <summary>
        ///     The slate blue
        /// </summary>
        SlateBlue = 4285160141U,

        /// <summary>
        ///     The olive drab
        /// </summary>
        OliveDrab = 4285238819U,

        /// <summary>
        ///     The slate gray
        /// </summary>
        SlateGray = 4285563024U,

        /// <summary>
        ///     The light slate gray
        /// </summary>
        LightSlateGray = 4286023833U,

        /// <summary>
        ///     The medium slate blue
        /// </summary>
        MediumSlateBlue = 4286277870U,

        /// <summary>
        ///     The lawn green
        /// </summary>
        LawnGreen = 4286381056U,

        /// <summary>
        ///     The chartreuse
        /// </summary>
        Chartreuse = 4286578432U,

        /// <summary>
        ///     The aquamarine
        /// </summary>
        Aquamarine = 4286578644U,

        /// <summary>
        ///     The maroon
        /// </summary>
        Maroon = 4286578688U,

        /// <summary>
        ///     The purple
        /// </summary>
        Purple = 4286578816U,

        /// <summary>
        ///     The olive
        /// </summary>
        Olive = 4286611456U,

        /// <summary>
        ///     The gray
        /// </summary>
        Gray = 4286611584U,

        /// <summary>
        ///     The sky blue
        /// </summary>
        SkyBlue = 4287090411U,

        /// <summary>
        ///     The light sky blue
        /// </summary>
        LightSkyBlue = 4287090426U,

        /// <summary>
        ///     The blue violet
        /// </summary>
        BlueViolet = 4287245282U,

        /// <summary>
        ///     The dark red
        /// </summary>
        DarkRed = 4287299584U,

        /// <summary>
        ///     The dark magenta
        /// </summary>
        DarkMagenta = 4287299723U,

        /// <summary>
        ///     The saddle brown
        /// </summary>
        SaddleBrown = 4287317267U,

        /// <summary>
        ///     The dark sea green
        /// </summary>
        DarkSeaGreen = 4287609999U,

        /// <summary>
        ///     The light green
        /// </summary>
        LightGreen = 4287688336U,

        /// <summary>
        ///     The medium purple
        /// </summary>
        MediumPurple = 4287852763U,

        /// <summary>
        ///     The dark violet
        /// </summary>
        DarkViolet = 4287889619U,

        /// <summary>
        ///     The pale green
        /// </summary>
        PaleGreen = 4288215960U,

        /// <summary>
        ///     The dark orchid
        /// </summary>
        DarkOrchid = 4288230092U,

        /// <summary>
        ///     The yellow green
        /// </summary>
        YellowGreen = 4288335154U,

        /// <summary>
        ///     The sienna
        /// </summary>
        Sienna = 4288696877U,

        /// <summary>
        ///     The brown
        /// </summary>
        Brown = 4289014314U,

        /// <summary>
        ///     The dark gray
        /// </summary>
        DarkGray = 4289309097U,

        /// <summary>
        ///     The light blue
        /// </summary>
        LightBlue = 4289583334U,

        /// <summary>
        ///     The green yellow
        /// </summary>
        GreenYellow = 4289593135U,

        /// <summary>
        ///     The pale turquoise
        /// </summary>
        PaleTurquoise = 4289720046U,

        /// <summary>
        ///     The light steel blue
        /// </summary>
        LightSteelBlue = 4289774814U,

        /// <summary>
        ///     The powder blue
        /// </summary>
        PowderBlue = 4289781990U,

        /// <summary>
        ///     The firebrick
        /// </summary>
        Firebrick = 4289864226U,

        /// <summary>
        ///     The dark goldenrod
        /// </summary>
        DarkGoldenrod = 4290283019U,

        /// <summary>
        ///     The medium orchid
        /// </summary>
        MediumOrchid = 4290401747U,

        /// <summary>
        ///     The rosy brown
        /// </summary>
        RosyBrown = 4290547599U,

        /// <summary>
        ///     The dark khaki
        /// </summary>
        DarkKhaki = 4290623339U,

        /// <summary>
        ///     The silver
        /// </summary>
        Silver = 4290822336U,

        /// <summary>
        ///     The medium violet red
        /// </summary>
        MediumVioletRed = 4291237253U,

        /// <summary>
        ///     The indian red
        /// </summary>
        IndianRed = 4291648604U,

        /// <summary>
        ///     The peru
        /// </summary>
        Peru = 4291659071U,

        /// <summary>
        ///     The chocolate
        /// </summary>
        Chocolate = 4291979550U,

        /// <summary>
        ///     The tan
        /// </summary>
        Tan = 4291998860U,

        /// <summary>
        ///     The light gray
        /// </summary>
        LightGray = 4292072403U,

        /// <summary>
        ///     The thistle
        /// </summary>
        Thistle = 4292394968U,

        /// <summary>
        ///     The orchid
        /// </summary>
        Orchid = 4292505814U,

        /// <summary>
        ///     The goldenrod
        /// </summary>
        Goldenrod = 4292519200U,

        /// <summary>
        ///     The pale violet red
        /// </summary>
        PaleVioletRed = 4292571283U,

        /// <summary>
        ///     The crimson
        /// </summary>
        Crimson = 4292613180U,

        /// <summary>
        ///     The gainsboro
        /// </summary>
        Gainsboro = 4292664540U,

        /// <summary>
        ///     The plum
        /// </summary>
        Plum = 4292714717U,

        /// <summary>
        ///     The burly wood
        /// </summary>
        BurlyWood = 4292786311U,

        /// <summary>
        ///     The light cyan
        /// </summary>
        LightCyan = 4292935679U,

        /// <summary>
        ///     The lavender
        /// </summary>
        Lavender = 4293322490U,

        /// <summary>
        ///     The dark salmon
        /// </summary>
        DarkSalmon = 4293498490U,

        /// <summary>
        ///     The violet
        /// </summary>
        Violet = 4293821166U,

        /// <summary>
        ///     The pale goldenrod
        /// </summary>
        PaleGoldenrod = 4293847210U,

        /// <summary>
        ///     The light coral
        /// </summary>
        LightCoral = 4293951616U,

        /// <summary>
        ///     The khaki
        /// </summary>
        Khaki = 4293977740U,

        /// <summary>
        ///     The alice blue
        /// </summary>
        AliceBlue = 4293982463U,

        /// <summary>
        ///     The honeydew
        /// </summary>
        Honeydew = 4293984240U,

        /// <summary>
        ///     The azure
        /// </summary>
        Azure = 4293984255U,

        /// <summary>
        ///     The sandy brown
        /// </summary>
        SandyBrown = 4294222944U,

        /// <summary>
        ///     The wheat
        /// </summary>
        Wheat = 4294303411U,

        /// <summary>
        ///     The beige
        /// </summary>
        Beige = 4294309340U,

        /// <summary>
        ///     The white smoke
        /// </summary>
        WhiteSmoke = 4294309365U,

        /// <summary>
        ///     The mint cream
        /// </summary>
        MintCream = 4294311930U,

        /// <summary>
        ///     The ghost white
        /// </summary>
        GhostWhite = 4294506751U,

        /// <summary>
        ///     The salmon
        /// </summary>
        Salmon = 4294606962U,

        /// <summary>
        ///     The antique white
        /// </summary>
        AntiqueWhite = 4294634455U,

        /// <summary>
        ///     The linen
        /// </summary>
        Linen = 4294635750U,

        /// <summary>
        ///     The light goldenrod yellow
        /// </summary>
        LightGoldenrodYellow = 4294638290U,

        /// <summary>
        ///     The old lace
        /// </summary>
        OldLace = 4294833638U,

        /// <summary>
        ///     The red
        /// </summary>
        Red = 4294901760U,

        /// <summary>
        ///     The fuchsia
        /// </summary>
        Fuchsia = 4294902015U,

        /// <summary>
        ///     The magenta
        /// </summary>
        Magenta = 4294902015U,

        /// <summary>
        ///     The deep pink
        /// </summary>
        DeepPink = 4294907027U,

        /// <summary>
        ///     The orange red
        /// </summary>
        OrangeRed = 4294919424U,

        /// <summary>
        ///     The tomato
        /// </summary>
        Tomato = 4294927175U,

        /// <summary>
        ///     The hot pink
        /// </summary>
        HotPink = 4294928820U,

        /// <summary>
        ///     The coral
        /// </summary>
        Coral = 4294934352U,

        /// <summary>
        ///     The dark orange
        /// </summary>
        DarkOrange = 4294937600U,

        /// <summary>
        ///     The light salmon
        /// </summary>
        LightSalmon = 4294942842U,

        /// <summary>
        ///     The orange
        /// </summary>
        Orange = 4294944000U,

        /// <summary>
        ///     The light pink
        /// </summary>
        LightPink = 4294948545U,

        /// <summary>
        ///     The pink
        /// </summary>
        Pink = 4294951115U,

        /// <summary>
        ///     The gold
        /// </summary>
        Gold = 4294956800U,

        /// <summary>
        ///     The peach puff
        /// </summary>
        PeachPuff = 4294957753U,

        /// <summary>
        ///     The navajo white
        /// </summary>
        NavajoWhite = 4294958765U,

        /// <summary>
        ///     The moccasin
        /// </summary>
        Moccasin = 4294960309U,

        /// <summary>
        ///     The bisque
        /// </summary>
        Bisque = 4294960324U,

        /// <summary>
        ///     The misty rose
        /// </summary>
        MistyRose = 4294960353U,

        /// <summary>
        ///     The blanched almond
        /// </summary>
        BlanchedAlmond = 4294962125U,

        /// <summary>
        ///     The papaya whip
        /// </summary>
        PapayaWhip = 4294963157U,

        /// <summary>
        ///     The lavender blush
        /// </summary>
        LavenderBlush = 4294963445U,

        /// <summary>
        ///     The sea shell
        /// </summary>
        SeaShell = 4294964718U,

        /// <summary>
        ///     The cornsilk
        /// </summary>
        Cornsilk = 4294965468U,

        /// <summary>
        ///     The lemon chiffon
        /// </summary>
        LemonChiffon = 4294965965U,

        /// <summary>
        ///     The floral white
        /// </summary>
        FloralWhite = 4294966000U,

        /// <summary>
        ///     The snow
        /// </summary>
        Snow = 4294966010U,

        /// <summary>
        ///     The yellow
        /// </summary>
        Yellow = 4294967040U,

        /// <summary>
        ///     The light yellow
        /// </summary>
        LightYellow = 4294967264U,

        /// <summary>
        ///     The ivory
        /// </summary>
        Ivory = 4294967280U,

        /// <summary>
        ///     The white
        /// </summary>
        White = 4294967295U
    }

    /// <summary>
    ///     Struct Color
    /// </summary>
    public class Color
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Single.</returns>
        private static float Convert(byte value)
        {
            return value / 255f;
        }

        /// <summary>
        ///     Converts the specified value.
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
        ///     Checks if color is equal to another color
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

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="Color" /> struct.
        /// </summary>
        /// <param name="knownColor">Color of the known.</param>
        public Color(KnownColors knownColor)
            : this((uint)knownColor)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Color" /> class.
        /// </summary>
        /// <param name="amfColor">Color of the amf.</param>
        internal Color(IOFunctions.amfclasses.AMF_Color amfColor)
            : this(amfColor.a, amfColor.r, amfColor.g, amfColor.b)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Color" /> struct.
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
        ///     Initializes a new instance of the <see cref="Color" /> struct.
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
        ///     Initializes a new instance of the <see cref="Color" /> struct.
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
        /// Initializes a new instance of the <see cref="Color"/> struct.
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
        ///     Initializes a new instance of the <see cref="Color" /> struct.
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

        #endregion Constructors


        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            string hex ="#"+ BitConverter.ToString(new[] { R, G, B, A });
            return hex.Replace("-", "");
        }

        #region Public Properties

        /// <summary>
        ///     A
        /// </summary>
        public byte A;

        /// <summary>
        ///     B
        /// </summary>
        public byte B;

        /// <summary>
        ///     R
        /// </summary>
        public byte R;

        /// <summary>
        ///     G
        /// </summary>
        public byte G;

        /// <summary>
        ///     Gets or sets the af.
        /// </summary>
        /// <value>
        ///     The Alpha channel as a float whose range is [0..1].
        ///     the value is allowed to be out of range
        /// </value>
        public float Af
        {
            get { return Convert(A); }
            set { A = Convert(value); }
        }

        /// <summary>
        ///     Gets or sets the rf.
        /// </summary>
        /// <value>The rf.</value>
        public float Rf
        {
            get { return Convert(R); }
            set { R = Convert(value); }
        }

        /// <summary>
        ///     Gets or sets the gf.
        /// </summary>
        /// <value>The gf.</value>
        public float Gf
        {
            get { return Convert(G); }
            set { G = Convert(value); }
        }

        /// <summary>
        ///     Gets or sets the bf.
        /// </summary>
        /// <value>The bf.</value>
        public float Bf
        {
            get { return Convert(B); }
            set { B = Convert(value); }
        }

        #endregion Public Properties
    }
}