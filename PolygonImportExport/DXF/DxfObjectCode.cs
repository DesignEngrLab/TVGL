#region SharpDxf, Copyright(C) 2012 Lomatus, Licensed under LGPL.

//                        SharpDxf library( Base on netDxf by Daniel Carvajal )
// Copyright (C) 2012 Lomatus (tourszhou@gmail.com)
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 

#endregion

namespace SharpDxf
{
    /// <summary>
    /// Dxf entities codes.
    /// </summary>
    internal sealed class DxfObjectCode
    {
        /// <summary>
        /// application registry.
        /// </summary>
        internal const string AppId = "APPID";

        /// <summary>
        /// dimension style.
        /// </summary>
        internal const string DimStyle = "DIMSTYLE";

        /// <summary>
        /// block record.
        /// </summary>
        internal const string BlockRecord = "BLOCK_RECORD";

        /// <summary>
        /// line type.
        /// </summary>
        internal const string LineType = "LTYPE";

        /// <summary>
        /// layer.
        /// </summary>
        internal const string Layer = "LAYER";

        /// <summary>
        /// viewport.
        /// </summary>
        internal const string ViewPort = "VPORT";

        /// <summary>
        /// text style.
        /// </summary>
        internal const string TextStyle = "STYLE";

        /// <summary>
        /// view.
        /// </summary>
        internal const string View = "VIEW";

        /// <summary>
        /// ucs.
        /// </summary>
        internal const string Ucs = "UCS";

        /// <summary>
        /// block.
        /// </summary>
        internal const string Block = "BLOCK";

        /// <summary>
        /// block.
        /// </summary>
        internal const string BlockEnd = "ENDBLK";

        /// <summary>
        /// line.
        /// </summary>
        internal const string Line = "LINE";

        /// <summary>
        /// ellipse.
        /// </summary>
        internal const string Ellipse = "ELLIPSE";

        /// <summary>
        /// polyline.
        /// </summary>
        internal const string Polyline = "POLYLINE";

        /// <summary>
        /// light weight polyline.
        /// </summary>
        internal const string LightWeightPolyline = "LWPOLYLINE";

        /// <summary>
        /// circle.
        /// </summary>
        internal const string Circle = "CIRCLE";

        /// <summary>
        /// point.
        /// </summary>
        internal const string Point = "POINT";

        /// <summary>
        /// arc.
        /// </summary>
        internal const string Arc = "ARC";

        /// <summary>
        /// solid.
        /// </summary>
        internal const string Solid = "SOLID";

        /// <summary>
        /// text string.
        /// </summary>
        internal const string Text = "TEXT";

        /// <summary>
        /// 3d face.
        /// </summary>
        internal const string Face3D = "3DFACE";

        /// <summary>
        /// block insertion.
        /// </summary>
        internal const string Insert = "INSERT";

        /// <summary>
        /// hatch.
        /// </summary>
        internal const string Hatch = "HATCH";

        /// <summary>
        /// attribute definition.
        /// </summary>
        internal const string AttributeDefinition = "ATTDEF";

        /// <summary>
        /// attribute.
        /// </summary>
        internal const string Attribute = "ATTRIB";

        /// <summary>
        /// vertex.
        /// </summary>
        internal const string Vertex = "VERTEX";

        /// <summary>
        /// end sequence.
        /// </summary>
        internal const string EndSequence = "SEQEND";

        /// <summary>
        /// dim.
        /// </summary>
        internal const string Dimension = "DIMENSION";

        /// <summary>
        /// dictionary.
        /// </summary>
        internal const string Dictionary = "DICTIONARY";
    }
}