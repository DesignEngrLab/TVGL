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
    /// Dxf sections.
    /// </summary>
    internal static class StringCode
    {
        /// <summary>
        /// not defined.
        /// </summary>
        internal const string Unknown = "";

        /// <summary>
        /// header.
        /// </summary>
        internal const string HeaderSection = "HEADER";

        /// <summary>
        /// clases.
        /// </summary>
        internal const string ClassesSection = "CLASSES";

        /// <summary>
        /// tables.
        /// </summary>
        internal const string TablesSection = "TABLES";

        /// <summary>
        /// blocks.
        /// </summary>
        internal const string BlocksSection = "BLOCKS";

        /// <summary>
        /// entities.
        /// </summary>
        internal const string EntitiesSection = "ENTITIES";

        /// <summary>
        /// objects.
        /// </summary>
        internal const string ObjectsSection = "OBJECTS";

        /// <summary>
        /// dxf name string.
        /// </summary>
        internal const string BeginSection = "SECTION";

        /// <summary>
        /// end secction code.
        /// </summary>
        internal const string EndSection = "ENDSEC";

        /// <summary>
        /// layers.
        /// </summary>
        internal const string LayerTable = "LAYER";

        /// <summary>
        /// view ports.
        /// </summary>
        internal const string ViewPortTable = "VPORT";

        /// <summary>
        /// views.
        /// </summary>
        internal const string ViewTable = "VIEW";

        /// <summary>
        /// ucs.
        /// </summary>
        internal const string UcsTable = "UCS";
        
        /// <summary>
        /// block records.
        /// </summary>
        internal const string BlockRecordTable = "BLOCK_RECORD";

        /// <summary>
        /// line types.
        /// </summary>
        internal const string LineTypeTable = "LTYPE";

        /// <summary>
        /// text styles.
        /// </summary>
        internal const string TextStyleTable = "STYLE";

        /// <summary>
        /// dim styles.
        /// </summary>
        internal const string DimensionStyleTable = "DIMSTYLE";

        /// <summary>
        /// extended data application registry.
        /// </summary>
        internal const string ApplicationIDTable = "APPID";

        /// <summary>
        /// end table code.
        /// </summary>
        internal const string EndTable = "ENDTAB";

        /// <summary>
        /// dxf name string.
        /// </summary>
        internal const string Table = "TABLE";

        /// <summary>
        /// dxf name string.
        /// </summary>
        internal const string BeginBlock = "BLOCK";

        /// <summary>
        /// end table code.
        /// </summary>
        internal const string EndBlock = "ENDBLK";

        /// <summary>
        /// end of an element sequence
        /// </summary>
        internal const string EndSequence = "SEQEND";

        /// <summary>
        /// dictionary
        /// </summary>
        internal const string Dictionary = "DICTIONARY";

        /// <summary>
        /// end of file
        /// </summary>
        internal const string EndOfFile = "EOF";
    }
}