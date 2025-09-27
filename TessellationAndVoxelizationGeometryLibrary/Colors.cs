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
using System.Numerics;

namespace TVGL
{
    /// <summary>
    /// Enum KnownColors
    /// </summary>
    public enum KnownColors : uint
    {
        AliceBlue = 4293982463U,
        Alizarin = 4293076534U,
        Amaranth = 4293208912U,
        Amber = 4294950656U,
        AndroidGreen = 4288988729U,
        AntiqueWhite = 4294634455U,
        AppleGreen = 4287477248U,
        Apricot = 4294692529U,
        Aqua = 4278255615U,
        Aquamarine = 4286578644U,
        ArmyGreen = 4283126560U,
        ArylideYellow = 4293514859U,
        AshGrey = 4289904309U,
        Asparagus = 4287080811U,
        AtomicTangerine = 4294941030U,
        Auburn = 4285347098U,
        Azure = 4293984255U,
        BabyBlue = 4287221744U,
        BabyPink = 4294230722U,
        BananaYellow = 4294955306U,
        BattleshipGrey = 4286874754U,
        Bazaar = 4288182139U,
        Beige = 4294309340U,
        Bisque = 4294960324U,
        Bistre = 4282198815U,
        Black = 4278190080U,
        BlanchedAlmond = 4294962125U,
        Blond = 4294635710U,
        Blue = 4278190335U,
        BlueViolet = 4287245282U,
        Blush = 4292763011U,
        Bole = 4286137403U,
        BostonUniversityRed = 4291559424U,
        Brass = 4290094658U,
        BrightGreen = 4284940032U,
        BrightLavender = 4290745572U,
        BrightMaroon = 4290978120U,
        BrightPink = 4294901887U,
        BrightTurquoise = 4278773982U,
        BrightUbe = 4291928040U,
        BritishRacingGreen = 4278207013U,
        Bronze = 4291657522U,
        Brown = 4288039680U,
        BrownKhaki = 4291014801U,
        BubbleGum = 4294951372U,
        Bubbles = 4293394175U,
        Buff = 4293975170U,
        Burgundy = 4286578720U,
        Burlywood = 4292786311U,
        BurlyWood = 4292786311U,
        BurntOrange = 4291581184U,
        BurntSienna = 4293489745U,
        BurntUmber = 4287247140U,
        Byzantine = 4290589604U,
        Byzantium = 4285540707U,
        CadetBlue = 4284456608U,
        CadmiumGreen = 4278217532U,
        CadmiumOrange = 4293756717U,
        CadmiumRed = 4293066786U,
        CambridgeBlue = 4288922029U,
        CamouflageGreen = 4286088811U,
        CanaryYellow = 4294962944U,
        CandyAppleRed = 4294903808U,
        Cardinal = 4291042874U,
        CaribbeanGreen = 4278242457U,
        Carmine = 4288020504U,
        CarrotOrange = 4293759265U,
        Ceil = 4287799759U,
        Celadon = 4289520047U,
        Cerulean = 4278221735U,
        CeruleanBlue = 4280963774U,
        Chamoisee = 4288706650U,
        Champagne = 4294436814U,
        Chartreuse = 4289248611U,
        Cherry = 4292751715U,
        CherryBlossomPink = 4294948805U,
        Chestnut = 4291648604U,
        Chocolate = 4286267136U,
        ChromeYellow = 4294944512U,
        Cinereous = 4288184699U,
        Cinnabar = 4293083700U,
        Cinnamon = 4291979550U,
        Citrine = 4293185546U,
        ClassicRose = 4294692071U,
        Clover = 4278255471U,
        ColumbiaBlue = 4288404991U,
        CoolGrey = 4287402668U,
        Copper = 4290278195U,
        Coquelicot = 4294916096U,
        Coral = 4294934352U,
        Cordovan = 4287184709U,
        Corn = 4294700125U,
        CornellRed = 4289927963U,
        CornflowerBlue = 4284782061U,
        Cornsilk = 4294965468U,
        Cream = 4294966736U,
        Crimson = 4292613180U,
        Cyan = 4278255615U,
        Daffodil = 4294967089U,
        Dandelion = 4293976368U,
        DarkBlue = 4278190219U,
        DarkBrown = 4284826401U,
        DarkByzantium = 4284299604U,
        DarkCandyAppleRed = 4288937984U,
        DarkChestnut = 4288178528U,
        DarkCoral = 4291648325U,
        DarkCyan = 4278225803U,
        DarkGoldenrod = 4290283019U,
        DarkGray = 4289309097U,
        DarkGreen = 4278268448U,
        DarkJungleGreen = 4279903265U,
        DarkKhaki = 4290623339U,
        DarkLava = 4282924082U,
        DarkLavender = 4285747094U,
        DarkMagenta = 4287299723U,
        DarkOliveGreen = 4283788079U,
        DarkOrange = 4294937600U,
        DarkOrchid = 4288230092U,
        DarkPastelGreen = 4278435900U,
        DarkPastelPurple = 4288049110U,
        DarkPastelRed = 4290919202U,
        DarkPink = 4293350528U,
        DarkPowderBlue = 4278203289U,
        DarkRaspberry = 4287047255U,
        DarkRed = 4287299584U,
        DarkSalmon = 4293498490U,
        DarkScarlet = 4283826969U,
        DarkSeaGreen = 4287609999U,
        DarkSienna = 4282127380U,
        DarkSlateBlue = 4282924427U,
        DarkSlateGray = 4281290575U,
        DarkSpringGreen = 4279726661U,
        DarkTan = 4287725905U,
        DarkTangerine = 4294944786U,
        DarkTerraCotta = 4291579484U,
        DarkTurquoise = 4278243025U,
        DarkViolet = 4287889619U,
        DeepPink = 4294907027U,
        DeepSkyBlue = 4278239231U,
        Desert = 4290878059U,
        DesertSand = 4293773743U,
        DimGray = 4285098345U,
        DodgerBlue = 4280193279U,
        DollarBill = 4286954341U,
        DukeBlue = 4278190236U,
        EarthYellow = 4292979039U,
        Eggplant = 4284563537U,
        Emerald = 4283484280U,
        Fawn = 4293241456U,
        FerrariRed = 4294908928U,
        Firebrick = 4289864226U,
        FireEngineRed = 4291696160U,
        Flame = 4293023778U,
        FlamingoPink = 4294741676U,
        Flavescent = 4294437262U,
        FloralWhite = 4294966000U,
        ForestGreen = 4278273057U,
        Gainsboro = 4292664540U,
        Gamboge = 4293171983U,
        GhostWhite = 4294506751U,
        Gold = 4294956800U,
        GoldenBrown = 4288242965U,
        Goldenrod = 4292519200U,
        GoldenYellow = 4294958848U,
        Gray = 4286611584U,
        Green = 4278222848U,
        GreenYellow = 4289593135U,
        Honeydew = 4293984240U,
        HotPink = 4294928820U,
        Icterine = 4294768478U,
        Inchworm = 4289915997U,
        IndiaGreen = 4279470088U,
        IndianRed = 4294925404U,
        IndianYellow = 4293109847U,
        Indigo = 4283105410U,
        InternationalKleinBlue = 4278202279U,
        Ivory = 4294967280U,
        Jade = 4278233195U,
        Jasper = 4292295486U,
        Khaki = 4293977740U,
        Lavender = 4290084572U,
        LavenderBlue = 4291611903U,
        LavenderBlush = 4294963445U,
        LavenderGray = 4291085264U,
        LawnGreen = 4286381056U,
        Lemon = 4294964992U,
        LemonChiffon = 4294965965U,
        LightBlue = 4289583334U,
        LightCoral = 4293951616U,
        LightCyan = 4292935679U,
        LightGoldenrodYellow = 4294638290U,
        LightGray = 4292072403U,
        LightGreen = 4287688336U,
        LightPink = 4294948545U,
        LightSalmon = 4294942842U,
        LightSeaGreen = 4280332970U,
        LightSkyBlue = 4287090426U,
        LightSlateGray = 4286023833U,
        LightSteelBlue = 4289774814U,
        LightYellow = 4294967264U,
        Lime = 4287495936U,
        LimeGreen = 4281519410U,
        Linen = 4294635750U,
        Magenta = 4294902015U,
        Mahogany = 4290789376U,
        Maroon = 4286578688U,
        MediumAquamarine = 4284927402U,
        MediumBlue = 4278190285U,
        MediumOrchid = 4290401747U,
        MediumPurple = 4287852763U,
        MediumSeaGreen = 4282168177U,
        MediumSlateBlue = 4286277870U,
        MediumSpringGreen = 4278254234U,
        MediumTurquoise = 4282962380U,
        MediumVioletRed = 4291237253U,
        MidnightBlue = 4279834992U,
        Mint = 4282299529U,
        MintCream = 4294311930U,
        MistyRose = 4294960353U,
        Moccasin = 4294960309U,
        Mustard = 4294957912U,
        NavajoWhite = 4294958765U,
        Navy = 4278190208U,
        NavyBlue = 4278190208U,
        Ochre = 4291589922U,
        OldLace = 4294833638U,
        Olive = 4286611456U,
        OliveDrab = 4285238819U,
        Orange = 4294934272U,
        OrangeRed = 4294919424U,
        Orchid = 4292505814U,
        PaleGoldenrod = 4293847210U,
        PaleGreen = 4288215960U,
        PaleTurquoise = 4289720046U,
        PaleVioletRed = 4292571283U,
        PapayaWhip = 4294963157U,
        PastelBlue = 4289644239U,
        PastelBrown = 4286802259U,
        PastelGray = 4291809220U,
        PastelGreen = 4286045559U,
        PastelMagenta = 4294220482U,
        PastelOrange = 4294947655U,
        PastelPink = 4294955484U,
        PastelPurple = 4289961653U,
        PastelRed = 4294928737U,
        PastelViolet = 4291533257U,
        PastelYellow = 4294835606U,
        Peach = 4294960564U,
        PeachPuff = 4294957753U,
        Pear = 4291945009U,
        Pearl = 4293978838U,
        Peridot = 4293321216U,
        Peru = 4291659071U,
        PineGreen = 4278286703U,
        Pink = 4294951115U,
        Pistachio = 4287874418U,
        Platinum = 4293256418U,
        Plum = 4287513989U,
        PortlandOrange = 4294924854U,
        PowderBlue = 4289781990U,
        Prune = 4285537308U,
        Pumpkin = 4294931736U,
        Purple = 4286578816U,
        PurpleHeart = 4285085084U,
        Raspberry = 4293069661U,
        RawUmber = 4286735940U,
        Red = 4294901760U,
        RifleGreen = 4282468403U,
        Rosewood = 4284809227U,
        RosyBrown = 4290547599U,
        RoyalBlue = 4278199142U,
        Ruby = 4292874591U,
        Rust = 4290199822U,
        SaddleBrown = 4287317267U,
        SafetyOrange = 4294928128U,
        Saffron = 4294231088U,
        Salmon = 4294937705U,
        Sand = 4290949760U,
        SandDune = 4288049431U,
        Sandstorm = 4293711168U,
        SandyBrown = 4294222944U,
        Sapphire = 4278723943U,
        SeaGreen = 4281240407U,
        SealBrown = 4281472020U,
        Seashell = 4294964718U,
        SeaShell = 4294964718U,
        Sepia = 4285547028U,
        Shadow = 4287265117U,
        Sienna = 4288696877U,
        Silver = 4290822336U,
        Sinopia = 4291510539U,
        SkyBlue = 4287090411U,
        SkyMagenta = 4291785135U,
        SlateBlue = 4285160141U,
        SlateGray = 4285563024U,
        Snow = 4294966010U,
        SpringBud = 4289199104U,
        SpringGreen = 4278255487U,
        SteelBlue = 4282811060U,
        Straw = 4293187951U,
        Sunset = 4294629029U,
        Tan = 4291998860U,
        Tangerine = 4294083840U,
        Teal = 4278222976U,
        TerraCotta = 4293030491U,
        Thistle = 4292394968U,
        TitaniumYellow = 4293846528U,
        Tomato = 4294927175U,
        TropicalRainForest = 4278220126U,
        Turquoise = 4281390536U,
        Ultramarine = 4279372431U,
        Vanilla = 4294174123U,
        Violet = 4287561983U,
        Wheat = 4294303411U,
        White = 4294967295U,
        WhiteSmoke = 4294309365U,
        Xanadu = 4285761144U,
        Yellow = 4294967040U,
        YellowGreen = 4288335154U,
    }

    /// <summary>
    /// Struct Color
    /// </summary>
    public class Color : IEquatable<Color>
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
            if (obj is null) return false;
            if (!(obj is Color)) return false;
            return Equals((Color)obj);
        }

        public bool Equals(Color other)
        {
            if (other is null) return false;
            return A == other.A && B == other.B
                   && G == other.G && R == other.R;
        }

        public static bool operator ==(Color a, Color b)
        {
            if (a is null && b is null) return true;
            if (a is null) return false;
            return a.Equals(b);
        }

        public static bool operator !=(Color a, Color b)
        {
            if (a is null && b is null) return false;
            if (a is null) return true;
            return !a.Equals(b);
        }

        /// <summary>
        /// Gets the Hash code for the object.
        /// </summary>
        /// <returns>System.Int.</returns>
        public override int GetHashCode()
        {
            // Pack A,R,G,B into a single Int32 in ARGB order
            // (same layout as KnownColors uint values).
            return (A << 24) | (R << 16) | (G << 8) | B;
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
        public static Color[] Distinct64Colors = [
            new Color(KnownColors.Black),
new Color(KnownColors.NavyBlue),
new Color(KnownColors.Peridot),
new Color(KnownColors.Raspberry),
new Color(KnownColors.DarkMagenta),
new Color(KnownColors.DarkPowderBlue),
new Color(KnownColors.GoldenYellow),
new Color(KnownColors.CadmiumGreen),
new Color(KnownColors.LimeGreen),
new Color(KnownColors.Burgundy),
new Color(KnownColors.LightSalmon),
new Color(KnownColors.CornellRed),
new Color(KnownColors.RoyalBlue),
new Color(KnownColors.PastelBlue),
new Color(KnownColors.Rosewood),
new Color(KnownColors.DimGray),
new Color(KnownColors.Blue),
new Color(KnownColors.Cerulean),
new Color(KnownColors.CamouflageGreen),
new Color(KnownColors.Jade),
new Color(KnownColors.RosyBrown),
new Color(KnownColors.Desert),
new Color(KnownColors.DarkCyan),
new Color(KnownColors.DollarBill),
new Color(KnownColors.Red),
new Color(KnownColors.Magenta),
new Color(KnownColors.DeepPink),
new Color(KnownColors.Bole),
new Color(KnownColors.HotPink),
new Color(KnownColors.MediumPurple),
new Color(KnownColors.Inchworm),
new Color(KnownColors.Sienna),
new Color(KnownColors.Cyan),
new Color(KnownColors.Linen),
new Color(KnownColors.DarkOrange),
new Color(KnownColors.LavenderBlue),
new Color(KnownColors.DeepSkyBlue),
new Color(KnownColors.DarkGoldenrod),
new Color(KnownColors.PurpleHeart),
new Color(KnownColors.PaleTurquoise),
new Color(KnownColors.ClassicRose),
new Color(KnownColors.Chocolate),
new Color(KnownColors.Plum),
new Color(KnownColors.DarkJungleGreen),
new Color(KnownColors.Sapphire),
new Color(KnownColors.DarkScarlet),
new Color(KnownColors.Violet),
new Color(KnownColors.SandyBrown),
new Color(KnownColors.Mustard),
new Color(KnownColors.PaleGreen),
new Color(KnownColors.BlueViolet),
new Color(KnownColors.BrownKhaki),
new Color(KnownColors.Orchid),
new Color(KnownColors.Icterine),
new Color(KnownColors.SpringGreen),
new Color(KnownColors.DodgerBlue),
new Color(KnownColors.Green),
new Color(KnownColors.DodgerBlue),
new Color(KnownColors.AppleGreen),
new Color(KnownColors.DarkPastelGreen),
new Color(KnownColors.OliveDrab),
new Color(KnownColors.BrightTurquoise),
new Color(KnownColors.Tomato),
new Color(KnownColors.HotPink)];

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
                    { "Snow", new Color(KnownColors.Snow) },
{ "BabyPink", new Color(KnownColors.BabyPink) },
{ "PastelRed", new Color(KnownColors.PastelRed) },
{ "IndianRed", new Color(KnownColors.IndianRed) },
{ "FerrariRed", new Color(KnownColors.FerrariRed) },
{ "CandyAppleRed", new Color(KnownColors.CandyAppleRed) },
{ "Red", new Color(KnownColors.Red) },
{ "Chestnut", new Color(KnownColors.Chestnut) },
{ "Cinnabar", new Color(KnownColors.Cinnabar) },
{ "Jasper", new Color(KnownColors.Jasper) },
{ "FireEngineRed", new Color(KnownColors.FireEngineRed) },
{ "BostonUniversityRed", new Color(KnownColors.BostonUniversityRed) },
{ "Firebrick", new Color(KnownColors.Firebrick) },
{ "CornellRed", new Color(KnownColors.CornellRed) },
{ "DarkCandyAppleRed", new Color(KnownColors.DarkCandyAppleRed) },
{ "Maroon", new Color(KnownColors.Maroon) },
{ "Prune", new Color(KnownColors.Prune) },
{ "DarkSienna", new Color(KnownColors.DarkSienna) },
{ "SealBrown", new Color(KnownColors.SealBrown) }
                } },
                { ColorFamily.Pink, new Dictionary<string, Color>()
                    {
{ "BubbleGum", new Color(KnownColors.BubbleGum) },
{ "Amaranth", new Color(KnownColors.Amaranth) },
{ "DarkTerraCotta", new Color(KnownColors.DarkTerraCotta) },
{ "Bazaar", new Color(KnownColors.Bazaar) },
{ "Alizarin", new Color(KnownColors.Alizarin) },
{ "Crimson", new Color(KnownColors.Crimson) },
{ "CadmiumRed", new Color(KnownColors.CadmiumRed) },
{ "Cardinal", new Color(KnownColors.Cardinal) },
{ "Cordovan", new Color(KnownColors.Cordovan) },
{ "Carmine", new Color(KnownColors.Carmine) },
{ "Rosewood", new Color(KnownColors.Rosewood) },
{ "PastelPink", new Color(KnownColors.PastelPink) },
{ "Pink", new Color(KnownColors.Pink) },
{ "CherryBlossomPink", new Color(KnownColors.CherryBlossomPink) },
{ "FlamingoPink", new Color(KnownColors.FlamingoPink) },
{ "DarkPink", new Color(KnownColors.DarkPink) },
{ "Blush", new Color(KnownColors.Blush) },
{ "Cherry", new Color(KnownColors.Cherry) },
{ "Raspberry", new Color(KnownColors.Raspberry) },
{ "Ruby", new Color(KnownColors.Ruby) },
{ "BrightMaroon", new Color(KnownColors.BrightMaroon) },
{ "Burgundy", new Color(KnownColors.Burgundy) },
{ "DarkScarlet", new Color(KnownColors.DarkScarlet) }
                    } },
                { ColorFamily.Magenta, new Dictionary<string, Color>()
                    {
{ "LavenderBlush", new Color(KnownColors.LavenderBlush) },
{ "ClassicRose", new Color(KnownColors.ClassicRose) },
{ "PastelMagenta", new Color(KnownColors.PastelMagenta) },
{ "SkyMagenta", new Color(KnownColors.SkyMagenta) },
{ "BrightPink", new Color(KnownColors.BrightPink) },
{ "DarkRaspberry", new Color(KnownColors.DarkRaspberry) },
{ "Eggplant", new Color(KnownColors.Eggplant) },
{ "BrightUbe", new Color(KnownColors.BrightUbe) },
{ "Magenta", new Color(KnownColors.Magenta) },
{ "PastelViolet", new Color(KnownColors.PastelViolet) },
{ "PastelPurple", new Color(KnownColors.PastelPurple) },
{ "Byzantine", new Color(KnownColors.Byzantine) },
{ "DarkViolet", new Color(KnownColors.DarkViolet) },
{ "Plum", new Color(KnownColors.Plum) },
{ "DarkMagenta", new Color(KnownColors.DarkMagenta) },
{ "Byzantium", new Color(KnownColors.Byzantium) },
{ "DarkByzantium", new Color(KnownColors.DarkByzantium) },
{ "BrightLavender", new Color(KnownColors.BrightLavender) },
{ "Lavender", new Color(KnownColors.Lavender) },
{ "DarkPastelPurple", new Color(KnownColors.DarkPastelPurple) },
{ "Violet", new Color(KnownColors.Violet) },
{ "DarkLavender", new Color(KnownColors.DarkLavender) },
{ "PurpleHeart", new Color(KnownColors.PurpleHeart) },
                    } },
                { ColorFamily.Blue, new Dictionary<string, Color>()
                    {
{ "GhostWhite", new Color(KnownColors.GhostWhite) },
{ "LavenderBlue", new Color(KnownColors.LavenderBlue) },
{ "LavenderGray", new Color(KnownColors.LavenderGray) },
{ "Ceil", new Color(KnownColors.Ceil) },
{ "CoolGrey", new Color(KnownColors.CoolGrey) },
{ "Blue", new Color(KnownColors.Blue) },
{ "CeruleanBlue", new Color(KnownColors.CeruleanBlue) },
{ "InternationalKleinBlue", new Color(KnownColors.InternationalKleinBlue) },
{ "DarkPowderBlue", new Color(KnownColors.DarkPowderBlue) },
{ "DukeBlue", new Color(KnownColors.DukeBlue) },
{ "Ultramarine", new Color(KnownColors.Ultramarine) },
{ "DarkBlue", new Color(KnownColors.DarkBlue) },
{ "NavyBlue", new Color(KnownColors.NavyBlue) },
{ "MidnightBlue", new Color(KnownColors.MidnightBlue) },
{ "Sapphire", new Color(KnownColors.Sapphire) },
{ "RoyalBlue", new Color(KnownColors.RoyalBlue) }
                    } },
                { ColorFamily.Cyan, new Dictionary<string, Color>()
                    {
{ "Bubbles", new Color(KnownColors.Bubbles) },
{ "Cyan", new Color(KnownColors.Cyan) },
{ "ColumbiaBlue", new Color(KnownColors.ColumbiaBlue) },
{ "BrightTurquoise", new Color(KnownColors.BrightTurquoise) },
{ "BabyBlue", new Color(KnownColors.BabyBlue) },
{ "SkyBlue", new Color(KnownColors.SkyBlue) },
{ "PastelBlue", new Color(KnownColors.PastelBlue) },
{ "Turquoise", new Color(KnownColors.Turquoise) },
{ "DarkCyan", new Color(KnownColors.DarkCyan) },
{ "Cerulean", new Color(KnownColors.Cerulean) },
{ "Teal", new Color(KnownColors.Teal) },
{ "PineGreen", new Color(KnownColors.PineGreen) },
{ "DarkSlateGray", new Color(KnownColors.DarkSlateGray) }
                    } },
                { ColorFamily.GreenCyan, new Dictionary<string, Color>()
                    {
{ "Aquamarine", new Color(KnownColors.Aquamarine) },
{ "Clover", new Color(KnownColors.Clover) },
{ "AshGrey", new Color(KnownColors.AshGrey) },
{ "CambridgeBlue", new Color(KnownColors.CambridgeBlue) },
{ "CaribbeanGreen", new Color(KnownColors.CaribbeanGreen) },
{ "Emerald", new Color(KnownColors.Emerald) },
{ "Mint", new Color(KnownColors.Mint) },
{ "DarkPastelGreen", new Color(KnownColors.DarkPastelGreen) },
{ "Jade", new Color(KnownColors.Jade) },
{ "Xanadu", new Color(KnownColors.Xanadu) },
{ "TropicalRainForest", new Color(KnownColors.TropicalRainForest) },
{ "DarkSpringGreen", new Color(KnownColors.DarkSpringGreen) },
{ "CadmiumGreen", new Color(KnownColors.CadmiumGreen) },
{ "ForestGreen", new Color(KnownColors.ForestGreen) },
{ "BritishRacingGreen", new Color(KnownColors.BritishRacingGreen) },
{ "DarkGreen", new Color(KnownColors.DarkGreen) },
{ "DarkJungleGreen", new Color(KnownColors.DarkJungleGreen) }
                    } },
                { ColorFamily.Green, new Dictionary<string, Color>()
                    {
{ "Inchworm", new Color(KnownColors.Inchworm) },
{ "LawnGreen", new Color(KnownColors.LawnGreen) },
{ "BrightGreen", new Color(KnownColors.BrightGreen) },
{ "Celadon", new Color(KnownColors.Celadon) },
{ "PastelGreen", new Color(KnownColors.PastelGreen) },
{ "Pistachio", new Color(KnownColors.Pistachio) },
{ "DollarBill", new Color(KnownColors.DollarBill) },
{ "Asparagus", new Color(KnownColors.Asparagus) },
{ "DarkPastelGreen", new Color(KnownColors.DarkPastelGreen) },
{ "CamouflageGreen", new Color(KnownColors.CamouflageGreen) },
{ "IndiaGreen", new Color(KnownColors.IndiaGreen) },
{ "Green", new Color(KnownColors.Green) },
{ "DarkOliveGreen", new Color(KnownColors.DarkOliveGreen) },
{ "RifleGreen", new Color(KnownColors.RifleGreen) },
{ "Chartreuse", new Color(KnownColors.Chartreuse) },
{ "Lime", new Color(KnownColors.Lime) },
{ "SpringBud", new Color(KnownColors.SpringBud) },
{ "Pear", new Color(KnownColors.Pear) },
{ "AndroidGreen", new Color(KnownColors.AndroidGreen) },
{ "AppleGreen", new Color(KnownColors.AppleGreen) },
{ "BattleshipGrey", new Color(KnownColors.BattleshipGrey) },
{ "Olive", new Color(KnownColors.Olive) },
{ "ArmyGreen", new Color(KnownColors.ArmyGreen) },
                    } },
                { ColorFamily.Yellow, new Dictionary<string, Color>()
                    {
{ "Ivory", new Color(KnownColors.Ivory) },
{ "Cream", new Color(KnownColors.Cream) },
{ "PastelYellow", new Color(KnownColors.PastelYellow) },
{ "Beige", new Color(KnownColors.Beige) },
{ "Daffodil", new Color(KnownColors.Daffodil) },
{ "Yellow", new Color(KnownColors.Yellow) },
{ "Icterine", new Color(KnownColors.Icterine) },
{ "Lemon", new Color(KnownColors.Lemon) },
{ "CanaryYellow", new Color(KnownColors.CanaryYellow) },
{ "Flavescent", new Color(KnownColors.Flavescent) },
{ "Corn", new Color(KnownColors.Corn) },
{ "GoldenYellow", new Color(KnownColors.GoldenYellow) },
{ "TitaniumYellow", new Color(KnownColors.TitaniumYellow) },
{ "Dandelion", new Color(KnownColors.Dandelion) },
{ "Peridot", new Color(KnownColors.Peridot) },
{ "Straw", new Color(KnownColors.Straw) },
{ "Sandstorm", new Color(KnownColors.Sandstorm) },
{ "PastelGray", new Color(KnownColors.PastelGray) },
{ "Citrine", new Color(KnownColors.Citrine) },
{ "DarkKhaki", new Color(KnownColors.DarkKhaki) },
{ "Brass", new Color(KnownColors.Brass) }
                    } },
                { ColorFamily.OrangeYellow, new Dictionary<string, Color>()
                    {
{ "Cornsilk", new Color(KnownColors.Cornsilk) },
{ "Blond", new Color(KnownColors.Blond) },
{ "Pearl", new Color(KnownColors.Pearl) },
{ "Platinum", new Color(KnownColors.Platinum) },
{ "Vanilla", new Color(KnownColors.Vanilla) },
{ "Mustard", new Color(KnownColors.Mustard) },
{ "Buff", new Color(KnownColors.Buff) },
{ "BananaYellow", new Color(KnownColors.BananaYellow) },
{ "ArylideYellow", new Color(KnownColors.ArylideYellow) },
{ "Saffron", new Color(KnownColors.Saffron) },
{ "Amber", new Color(KnownColors.Amber) },
{ "Sand", new Color(KnownColors.Sand) },
{ "Goldenrod", new Color(KnownColors.Goldenrod) },
{ "DarkGoldenrod", new Color(KnownColors.DarkGoldenrod) },
{ "DarkTan", new Color(KnownColors.DarkTan) },
{ "SandDune", new Color(KnownColors.SandDune) }
                    } },
                { ColorFamily.OrangeBrown, new Dictionary<string, Color>()
                    {
                    { "Seashell", new Color(KnownColors.Seashell) },
{ "Champagne", new Color(KnownColors.Champagne) },
{ "Peach", new Color(KnownColors.Peach) },
{ "Wheat", new Color(KnownColors.Wheat) },
{ "Sunset", new Color(KnownColors.Sunset) },
{ "Apricot", new Color(KnownColors.Apricot) },
{ "DesertSand", new Color(KnownColors.DesertSand) },
{ "PastelOrange", new Color(KnownColors.PastelOrange) },
{ "Burlywood", new Color(KnownColors.Burlywood) },
{ "DarkTangerine", new Color(KnownColors.DarkTangerine) },
{ "ChromeYellow", new Color(KnownColors.ChromeYellow) },
{ "Fawn", new Color(KnownColors.Fawn) },
{ "EarthYellow", new Color(KnownColors.EarthYellow) },
{ "IndianYellow", new Color(KnownColors.IndianYellow) },
{ "BrownKhaki", new Color(KnownColors.BrownKhaki) },
{ "DarkOrange", new Color(KnownColors.DarkOrange) },
{ "Gamboge", new Color(KnownColors.Gamboge) },
{ "CarrotOrange", new Color(KnownColors.CarrotOrange) },
{ "Orange", new Color(KnownColors.Orange) },
{ "Tangerine", new Color(KnownColors.Tangerine) },
{ "CadmiumOrange", new Color(KnownColors.CadmiumOrange) },
{ "Pumpkin", new Color(KnownColors.Pumpkin) },
{ "Desert", new Color(KnownColors.Desert) },
{ "SafetyOrange", new Color(KnownColors.SafetyOrange) },
{ "Bronze", new Color(KnownColors.Bronze) },
{ "Ochre", new Color(KnownColors.Ochre) },
{ "Cinnamon", new Color(KnownColors.Cinnamon) },
{ "Copper", new Color(KnownColors.Copper) },
{ "Chamoisee", new Color(KnownColors.Chamoisee) },
{ "BurntOrange", new Color(KnownColors.BurntOrange) },
{ "Shadow", new Color(KnownColors.Shadow) },
{ "GoldenBrown", new Color(KnownColors.GoldenBrown) },
{ "PastelBrown", new Color(KnownColors.PastelBrown) },
{ "RawUmber", new Color(KnownColors.RawUmber) },
{ "Brown", new Color(KnownColors.Brown) },
{ "Chocolate", new Color(KnownColors.Chocolate) },
{ "Sepia", new Color(KnownColors.Sepia) },
{ "DarkBrown", new Color(KnownColors.DarkBrown) },
{ "DarkLava", new Color(KnownColors.DarkLava) },
{ "Bistre", new Color(KnownColors.Bistre) }
                    } },
                { ColorFamily.RedOrange, new Dictionary<string, Color>()
                    {
{ "AtomicTangerine", new Color(KnownColors.AtomicTangerine) },
{ "Salmon", new Color(KnownColors.Salmon) },
{ "DarkSalmon", new Color(KnownColors.DarkSalmon) },
{ "Coral", new Color(KnownColors.Coral) },
{ "BurntSienna", new Color(KnownColors.BurntSienna) },
{ "PortlandOrange", new Color(KnownColors.PortlandOrange) },
{ "TerraCotta", new Color(KnownColors.TerraCotta) },
{ "Coquelicot", new Color(KnownColors.Coquelicot) },
{ "Flame", new Color(KnownColors.Flame) },
{ "Cinereous", new Color(KnownColors.Cinereous) },
{ "DarkCoral", new Color(KnownColors.DarkCoral) },
{ "DarkChestnut", new Color(KnownColors.DarkChestnut) },
{ "Sinopia", new Color(KnownColors.Sinopia) },
{ "DarkPastelRed", new Color(KnownColors.DarkPastelRed) },
{ "Mahogany", new Color(KnownColors.Mahogany) },
{ "Rust", new Color(KnownColors.Rust) },
{ "Bole", new Color(KnownColors.Bole) },
{ "BurntUmber", new Color(KnownColors.BurntUmber) },
{ "Auburn", new Color(KnownColors.Auburn) }
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