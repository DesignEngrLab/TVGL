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
    /// Dxf object subclass string markers.
    /// </summary>
    internal static class SubclassMarker
    {
        internal const string ApplicationId = "AcDbRegAppTableRecord";
        internal const string Table = "AcDbSymbolTable";
        internal const string TableRecord = "AcDbSymbolTableRecord";
        internal const string Layer = "AcDbLayerTableRecord";
        internal const string ViewPort = "AcDbViewportTableRecord";
        internal const string View = "AcDbViewTableRecord";
        internal const string LineType = "AcDbLinetypeTableRecord";
        internal const string TextStyle = "AcDbTextStyleTableRecord";
        internal const string DimensionStyleTable = "AcDbDimStyleTable";
        internal const string DimensionStyle = "AcDbDimStyleTableRecord";
        internal const string BlockRecord = "AcDbBlockTableRecord";
        internal const string BlockBegin = "AcDbBlockBegin";
        internal const string BlockEnd = "AcDbBlockEnd";
        internal const string Entity = "AcDbEntity";
        internal const string Arc = "AcDbArc";
        internal const string Circle = "AcDbCircle";
        internal const string Ellipse = "AcDbEllipse";
        internal const string Face3d = "AcDbFace";
        internal const string Insert = "AcDbBlockReference";
        internal const string Line = "AcDbLine";
        internal const string Point = "AcDbPoint";
        internal const string Vertex = "AcDbVertex";
        internal const string Polyline = "AcDb2dPolyline";
        internal const string LightWeightPolyline = "AcDbPolyline";
        internal const string PolylineVertex = "AcDb2dVertex ";
        internal const string Polyline3d = "AcDb3dPolyline";
        internal const string Polyline3dVertex = "AcDb3dPolylineVertex";
        internal const string PolyfaceMesh = "AcDbPolyFaceMesh";
        internal const string PolyfaceMeshVertex = "AcDbPolyFaceMeshVertex";
        internal const string PolyfaceMeshFace = "AcDbFaceRecord";
        internal const string Solid = "AcDbTrace";
        internal const string Text = "AcDbText";
        internal const string Attribute = "AcDbAttribute";
        internal const string AttributeDefinition = "AcDbAttributeDefinition";
        internal const string Dictionary = "AcDbDictionary";
    }
}