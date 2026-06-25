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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using SharpDxf.Blocks;
using SharpDxf.Entities;
using SharpDxf.Header;
using SharpDxf.Objects;
using SharpDxf.Tables;
using TVGL;
using Circle = SharpDxf.Entities.Circle;

namespace SharpDxf
{
    /// <summary>
    /// Low level dxf writer.
    /// </summary>
    internal sealed class DxfWriter
    {
        #region private fields

        private int reservedHandles = 10;
        private readonly string file;
        private bool isFileOpen;
        private string activeSection = StringCode.Unknown;
        private string activeTable = StringCode.Unknown;
        private bool isHeader;
        private bool isClassesSection;
        private bool isTableSection;
        private bool isBlockDefinition;
        private bool isBlockEntities;
        private bool isEntitiesSection;
        private bool isObjectsSection;
        private Stream output;
        private StreamWriter writer;
        private readonly DxfVersion version;

        #endregion

        #region constructors

        internal DxfWriter(string file, DxfVersion version)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            this.file = file;
            this.version = version;
        }

        #endregion

        #region internal properties

        /// <summary>
        /// Gets the active section.
        /// </summary>
        internal String ActiveSection
        {
            get { return this.activeSection; }
        }

        /// <summary>
        /// Gets if the file is opent.
        /// </summary>
        internal bool IsFileOpen
        {
            get { return this.isFileOpen; }
        }

        #endregion

        #region internal methods

        internal void Open()
        {
            if (this.isFileOpen)
            {
                throw new DxfException(this.file, "The file is already open");
            }
            try
            {
                this.output = File.Create(this.file);
                this.writer = new StreamWriter(this.output, Encoding.ASCII);
                this.isFileOpen = true;
            }
            catch (Exception ex)
            {
                throw (new DxfException(this.file, "Error when trying to create the dxf file", ex));
            }
        }

        /// <summary>
        /// Closes the dxf file.
        /// </summary>
        internal void Close()
        {
            if (this.activeSection != StringCode.Unknown)
            {
                throw new OpenDxfSectionException(this.activeSection, this.file);
            }
            if (this.activeTable != StringCode.Unknown)
            {
                throw new OpenDxfTableException(this.activeTable, this.file);
            }
            this.WriteCodePair(0, StringCode.EndOfFile);

            if (this.isFileOpen)
            {
                this.writer.Close();
                this.output.Close();
            }

            this.isFileOpen = false;
        }

        /// <summary>
        /// Opens a new section.
        /// </summary>
        /// <param name="section">Section type to open.</param>
        /// <remarks>There can be only one type section.</remarks>
        internal void BeginSection(string section)
        {
            if (! this.isFileOpen)
            {
                throw new DxfException(this.file, "The file is not open");
            }
            if (this.activeSection != StringCode.Unknown)
            {
                throw new OpenDxfSectionException(this.activeSection, this.file);
            }

            this.WriteCodePair(0, StringCode.BeginSection);

            if (section == StringCode.HeaderSection)
            {
                if (this.isHeader)
                {
                    throw (new ClosedDxfSectionException(StringCode.HeaderSection, this.file));
                }
                this.WriteCodePair(2, StringCode.HeaderSection);
                this.isHeader = true;
            }
            if (section == StringCode.ClassesSection)
            {
                if (this.isClassesSection)
                {
                    throw (new ClosedDxfSectionException(StringCode.ClassesSection, this.file));
                }
                this.WriteCodePair(2, StringCode.ClassesSection);
                this.isClassesSection = true;
            }
            if (section == StringCode.TablesSection)
            {
                if (this.isTableSection)
                {
                    throw (new ClosedDxfSectionException(StringCode.TablesSection, this.file));
                }
                this.WriteCodePair(2, StringCode.TablesSection);
                this.isTableSection = true;
            }
            if (section == StringCode.BlocksSection)
            {
                if (this.isBlockDefinition)
                {
                    throw (new ClosedDxfSectionException(StringCode.BlocksSection, this.file));
                }
                this.WriteCodePair(2, StringCode.BlocksSection);
                this.isBlockDefinition = true;
            }
            if (section == StringCode.EntitiesSection)
            {
                if (this.isEntitiesSection)
                {
                    throw (new ClosedDxfSectionException(StringCode.EntitiesSection, this.file));
                }
                this.WriteCodePair(2, StringCode.EntitiesSection);
                this.isEntitiesSection = true;
            }
            if (section == StringCode.ObjectsSection)
            {
                if (this.isObjectsSection)
                {
                    throw (new ClosedDxfSectionException(StringCode.ObjectsSection, this.file));
                }
                this.WriteCodePair(2, StringCode.ObjectsSection);
                this.isObjectsSection = true;
            }
            this.activeSection = section;
        }

        /// <summary>
        /// Closes the active section.
        /// </summary>
        internal void EndSection()
        {
            if (this.activeSection == StringCode.Unknown)
            {
                throw new ClosedDxfSectionException(StringCode.Unknown, this.file);
            }
            this.WriteCodePair(0, StringCode.EndSection);
            switch (this.activeSection)
            {
                case StringCode.HeaderSection:
                    this.isEntitiesSection = false;
                    break;
                case StringCode.ClassesSection:
                    this.isEntitiesSection = false;
                    break;
                case StringCode.TablesSection:
                    this.isTableSection = false;
                    break;
                case StringCode.BlocksSection:
                    this.isBlockDefinition = true;
                    break;
                case StringCode.EntitiesSection:
                    this.isEntitiesSection = false;
                    break;
                case StringCode.ObjectsSection:
                    this.isEntitiesSection = false;
                    break;
            }
            this.activeSection = StringCode.Unknown;
        }

        /// <summary>
        /// Opens a new table.
        /// </summary>
        /// <param name="table">Table type to open.</param>
        internal void BeginTable(string table)
        {
            if (! this.isFileOpen)
            {
                throw new DxfException(this.file, "The file is not open");
            }
            if (this.activeTable != StringCode.Unknown)
            {
                throw new OpenDxfTableException(table, this.file);
            }
            this.WriteCodePair(0, StringCode.Table);
            this.WriteCodePair(2, table);
            this.WriteCodePair(5, this.reservedHandles++);
            this.WriteCodePair(100, SubclassMarker.Table);

            if (table == StringCode.DimensionStyleTable)
                this.WriteCodePair(100, SubclassMarker.DimensionStyleTable);
            this.activeTable = table;
        }

        /// <summary>
        /// Closes the active table.
        /// </summary>
        internal void EndTable()
        {
            if (this.activeTable == StringCode.Unknown)
            {
                throw new ClosedDxfTableException(StringCode.Unknown, this.file);
            }

            this.WriteCodePair(0, StringCode.EndTable);
            this.activeTable = StringCode.Unknown;
        }

        #endregion

        #region methods for Header section

        internal void WriteComment(string comment)
        {
            if (!string.IsNullOrEmpty(comment))
                this.WriteCodePair(999, comment);
        }

        internal void WriteSystemVariable(HeaderVariable variable)
        {
            if (this.activeSection != StringCode.HeaderSection)
            {
                throw new InvalidDxfSectionException(this.activeSection, this.file);
            }
            this.WriteCodePair(HeaderVariable.NAME_CODE_GROUP, variable.Name);
            this.WriteCodePair(variable.CodeGroup, variable.Value);
        }

        #endregion

        #region methods for Table section

        /// <summary>
        /// Writes a new extended data application registry to the table section.
        /// </summary>
        /// <param name="appReg">Nombre del registro de aplicación.</param>
        internal void RegisterApplication(ApplicationRegistry appReg)
        {
            if (this.activeTable != StringCode.ApplicationIDTable)
            {
                throw new InvalidDxfTableException(StringCode.ApplicationIDTable, this.file);
            }

            this.WriteCodePair(0, StringCode.ApplicationIDTable);
            this.WriteCodePair(5, appReg.Handle);
            this.WriteCodePair(100, SubclassMarker.TableRecord);
            this.WriteCodePair(100, SubclassMarker.ApplicationId);
            this.WriteCodePair(2, appReg);
            this.WriteCodePair(70, 0);
        }

        /// <summary>
        /// Writes a new view port to the table section.
        /// </summary>
        /// <param name="vp">Viewport.</param>
        internal void WriteViewPort(ViewPort vp)
        {
            if (this.activeTable != StringCode.ViewPortTable)
            {
                throw new InvalidDxfTableException(this.activeTable, this.file);
            }
            this.WriteCodePair(0, vp.CodeName);
            this.WriteCodePair(5, vp.Handle);
            this.WriteCodePair(100, SubclassMarker.TableRecord);

            this.WriteCodePair(100, SubclassMarker.ViewPort);
            this.WriteCodePair(2, vp);
            this.WriteCodePair(70, 0);

            this.WriteCodePair(10, vp.LowerLeftCorner.X);
            this.WriteCodePair(20, vp.LowerLeftCorner.Y);

            this.WriteCodePair(11, vp.UpperRightCorner.X);
            this.WriteCodePair(21, vp.UpperRightCorner.Y);

            this.WriteCodePair(12, vp.LowerLeftCorner.X - vp.UpperRightCorner.X);
            this.WriteCodePair(22, vp.UpperRightCorner.Y - vp.LowerLeftCorner.Y);

            this.WriteCodePair(13, vp.SnapBasePoint.X);
            this.WriteCodePair(23, vp.SnapBasePoint.Y);

            this.WriteCodePair(14, vp.SnapSpacing.X);
            this.WriteCodePair(24, vp.SnapSpacing.Y);

            this.WriteCodePair(15, vp.GridSpacing.X);
            this.WriteCodePair(25, vp.GridSpacing.Y);

            Vector3 dir = vp.Camera - vp.Target;
            this.WriteCodePair(16, dir.X);
            this.WriteCodePair(26, dir.Y);
            this.WriteCodePair(36, dir.Z);

            this.WriteCodePair(17, vp.Target.X);
            this.WriteCodePair(27, vp.Target.Y);
            this.WriteCodePair(37, vp.Target.Z);
        }

        /// <summary>
        /// Writes a new dimension style to the table section.
        /// </summary>
        /// <param name="dimStyle">DimensionStyle.</param>
        internal void WriteDimensionStyle(DimensionStyle dimStyle)
        {
            if (this.activeTable != StringCode.DimensionStyleTable)
            {
                throw new InvalidDxfTableException(this.activeTable, this.file);
            }
            this.WriteCodePair(0, dimStyle.CodeName);
            this.WriteCodePair(105, dimStyle.Handle);

            this.WriteCodePair(100, SubclassMarker.TableRecord);

            this.WriteCodePair(100, SubclassMarker.DimensionStyle);

            this.WriteCodePair(2, dimStyle);

            // flags
            this.WriteCodePair(70, 0);
        }

        /// <summary>
        /// Writes a new block record to the table section.
        /// </summary>
        /// <param name="blockRecord">Block.</param>
        internal void WriteBlockRecord(BlockRecord blockRecord)
        {
            if (this.activeTable != StringCode.BlockRecordTable)
            {
                throw new InvalidDxfTableException(this.activeTable, this.file);
            }
            this.WriteCodePair(0, blockRecord.CodeName);
            this.WriteCodePair(5, blockRecord.Handle);
            this.WriteCodePair(100, SubclassMarker.TableRecord);

            this.WriteCodePair(100, SubclassMarker.BlockRecord);

            this.WriteCodePair(2, blockRecord);
        }

        /// <summary>
        /// Writes a new line type to the table section.
        /// </summary>
        /// <param name="tl">Line type.</param>
        internal void WriteLineType(LineType tl)
        {
            if (this.version == DxfVersion.AutoCad12)
                if (tl.Name == "ByLayer" || tl.Name == "ByBlock")
                    return;

            if (this.activeTable != StringCode.LineTypeTable)
            {
                throw new InvalidDxfTableException(this.activeTable, this.file);
            }

            this.WriteCodePair(0, tl.CodeName);
            this.WriteCodePair(5, tl.Handle);
            this.WriteCodePair(100, SubclassMarker.TableRecord);

            this.WriteCodePair(100, SubclassMarker.LineType);

            this.WriteCodePair(70, 0);
            this.WriteCodePair(2, tl);
            this.WriteCodePair(3, tl.Description);
            this.WriteCodePair(72, 65);
            this.WriteCodePair(73, tl.Segments.Count);
            this.WriteCodePair(40, tl.Legth());
            foreach (double s in tl.Segments)
            {
                this.WriteCodePair(49, s);
                if (this.version != DxfVersion.AutoCad12)
                    this.WriteCodePair(74, 0);
            }
        }

        /// <summary>
        /// Writes a new layer to the table section.
        /// </summary>
        /// <param name="layer">Layer.</param>
        internal void WriteLayer(Layer layer)
        {
            if (this.activeTable != StringCode.LayerTable)
            {
                throw new InvalidDxfTableException(this.activeTable, this.file);
            }

            this.WriteCodePair(0, layer.CodeName);
            this.WriteCodePair(5, layer.Handle);
            this.WriteCodePair(100, SubclassMarker.TableRecord);

            this.WriteCodePair(100, SubclassMarker.Layer);
            this.WriteCodePair(70, 0);
            this.WriteCodePair(2, layer);

            //a negative color represents a hidden layer.
            if (layer.IsVisible)
            {
                this.WriteCodePair(62, layer.Color.Index);
            }
            else
            {
                this.WriteCodePair(62, -layer.Color.Index);
            }

            this.WriteCodePair(6, layer.LineType.Name);
            if (this.version != DxfVersion.AutoCad12)
                this.WriteCodePair(390, Layer.PlotStyleHandle);
        }

        /// <summary>
        /// Writes a new text style to the table section.
        /// </summary>
        /// <param name="style">TextStyle.</param>
        internal void WriteTextStyle(TextStyle style)
        {
            if (this.activeTable != StringCode.TextStyleTable)
            {
                throw new InvalidDxfTableException(this.activeTable, this.file);
            }

            this.WriteCodePair(0, style.CodeName);
            this.WriteCodePair(5, style.Handle);
            this.WriteCodePair(100, SubclassMarker.TableRecord);

            this.WriteCodePair(100, SubclassMarker.TextStyle);

            this.WriteCodePair(2, style);
            this.WriteCodePair(3, style.Font);

            if (style.IsVertical)
            {
                this.WriteCodePair(70, 4);
            }
            else
            {
                this.WriteCodePair(70, 0);
            }

            if (style.IsBackward && style.IsUpsideDown)
            {
                this.WriteCodePair(71, 6);
            }
            else if (style.IsBackward)
            {
                this.WriteCodePair(71, 2);
            }
            else if (style.IsUpsideDown)
            {
                this.WriteCodePair(71, 4);
            }
            else
            {
                this.WriteCodePair(71, 0);
            }

            this.WriteCodePair(40, style.Height);
            this.WriteCodePair(41, style.WidthFactor);
            this.WriteCodePair(42, style.Height);
            this.WriteCodePair(50, style.ObliqueAngle);
        }

        #endregion

        #region methods for Block section

        internal void WriteBlock(Block block, List<IEntityObject> entityObjects)
        {
            if (this.version == DxfVersion.AutoCad12)
                if (block.Name == "*Model_Space" || block.Name == "*Paper_Space")
                    return;

            if (this.activeSection != StringCode.BlocksSection)
            {
                throw new InvalidDxfSectionException(this.activeSection, this.file);
            }

            this.WriteCodePair(0, block.CodeName);
            this.WriteCodePair(5, block.Handle);
            this.WriteCodePair(100, SubclassMarker.Entity);
            this.WriteCodePair(8, block.Layer);

            this.WriteCodePair(100, SubclassMarker.BlockBegin);

            this.WriteCodePair(2, block);

            //flags
            this.WriteCodePair(70, 0);

            this.WriteCodePair(10, block.BasePoint.X);
            this.WriteCodePair(20, block.BasePoint.Y);
            this.WriteCodePair(30, block.BasePoint.Z);

            this.WriteCodePair(3, block);

            //block entities, if version is AutoCad12 we will write the converted entities
            this.isBlockEntities = true;
            foreach (IEntityObject entity in entityObjects)
            {
                this.WriteEntity(entity);
            }
            this.isBlockEntities = false;

            this.WriteBlockEnd(block.End);
        }

        internal void WriteBlockEnd(BlockEnd blockEnd)
        {
            this.WriteCodePair(0, blockEnd.CodeName);
            this.WriteCodePair(5, blockEnd.Handle);
            this.WriteCodePair(100, SubclassMarker.Entity);
            this.WriteCodePair(8, blockEnd.Layer);

            this.WriteCodePair(100, SubclassMarker.BlockEnd);
        }

        #endregion

        #region methods for Entity section

        internal void WriteEntity(IEntityObject entity)
        {
            switch (entity.Type)
            {
                case EntityType.Arc:
                    this.WriteArc((Arc) entity);
                    break;
                case EntityType.Circle:
                    this.WriteCircle((Circle) entity);
                    break;
                case EntityType.Ellipse:
                    this.WriteEllipse((Ellipse) entity);
                    break;
                case EntityType.NurbsCurve:
                    this.WriteNurbsCurve((NurbsCurve) entity);
                    break;
                case EntityType.LightWeightPolyline:
                    this.WriteLightWeightPolyline((LightWeightPolyline) entity);
                    break;
                case EntityType.Polyline:
                    this.WritePolyline2d((Polyline) entity);
                    break;
                default:
                    throw new NotImplementedException(entity.Type.ToString());
            }
        }

        private void WriteArc(Arc arc)
        {
            if (this.activeSection != StringCode.EntitiesSection && !this.isBlockEntities)
            {
                throw new InvalidDxfSectionException(this.activeSection, this.file);
            }

            this.WriteCodePair(0, arc.CodeName);
            this.WriteCodePair(5, arc.Handle);
            this.WriteCodePair(100, SubclassMarker.Entity);
            this.WriteEntityCommonCodes(arc);
            this.WriteCodePair(100, SubclassMarker.Circle);

            this.WriteCodePair(39, arc.Thickness);

            this.WriteCodePair(10, arc.Center.X);
            this.WriteCodePair(20, arc.Center.Y);
            this.WriteCodePair(30, arc.Center.Z);

            this.WriteCodePair(40, arc.Radius);

            this.WriteCodePair(210, arc.Normal.X);
            this.WriteCodePair(220, arc.Normal.Y);
            this.WriteCodePair(230, arc.Normal.Z);

            this.WriteCodePair(100, SubclassMarker.Arc);
            this.WriteCodePair(50, arc.StartAngle);
            this.WriteCodePair(51, arc.EndAngle);

            this.WriteXData(arc.XData);
        }

        private void WriteCircle(Circle circle)
        {
            if (this.activeSection != StringCode.EntitiesSection && !this.isBlockEntities)
            {
                throw new InvalidDxfSectionException(this.activeSection, this.file);
            }

            this.WriteCodePair(0, circle.CodeName);
            this.WriteCodePair(5, circle.Handle);
            this.WriteCodePair(100, SubclassMarker.Entity);
            this.WriteEntityCommonCodes(circle);
            this.WriteCodePair(100, SubclassMarker.Circle);


            this.WriteCodePair(10, circle.Center.X);
            this.WriteCodePair(20, circle.Center.Y);
            this.WriteCodePair(30, circle.Center.Z);

            this.WriteCodePair(40, circle.Radius);

            this.WriteCodePair(39, circle.Thickness);

            this.WriteCodePair(210, circle.Normal.X);
            this.WriteCodePair(220, circle.Normal.Y);
            this.WriteCodePair(230, circle.Normal.Z);

            this.WriteXData(circle.XData);
        }

        private void WriteEllipse(Ellipse ellipse)
        {
            if (this.activeSection != StringCode.EntitiesSection && !this.isBlockEntities)
            {
                throw new InvalidDxfSectionException(this.activeSection, this.file);
            }

            if (this.version == DxfVersion.AutoCad12)
            {
                this.WriteEllipseAsPolyline(ellipse);
                return;
            }

            this.WriteCodePair(0, ellipse.CodeName);
            this.WriteCodePair(5, ellipse.Handle);
            this.WriteCodePair(100, SubclassMarker.Entity);
            this.WriteEntityCommonCodes(ellipse);
            this.WriteCodePair(100, SubclassMarker.Ellipse);


            this.WriteCodePair(10, ellipse.Center.X);
            this.WriteCodePair(20, ellipse.Center.Y);
            this.WriteCodePair(30, ellipse.Center.Z);


            double sine = (double) (0.5*ellipse.MajorAxis*Math.Sin(ellipse.Rotation*MathHelper.DegToRad));
            double cosine = (double) (0.5*ellipse.MajorAxis*Math.Cos(ellipse.Rotation*MathHelper.DegToRad));
            Vector3 axisPoint = MathHelper.Transform((Vector3) new Vector3(cosine, sine, 0),
                                                      (Vector3) ellipse.Normal,
                                                      MathHelper.CoordinateSystem.Object,
                                                      MathHelper.CoordinateSystem.World);

            this.WriteCodePair(11, axisPoint.X);
            this.WriteCodePair(21, axisPoint.Y);
            this.WriteCodePair(31, axisPoint.Z);

            this.WriteCodePair(210, ellipse.Normal.X);
            this.WriteCodePair(220, ellipse.Normal.Y);
            this.WriteCodePair(230, ellipse.Normal.Z);

            this.WriteCodePair(40, ellipse.MinorAxis/ellipse.MajorAxis);
            this.WriteCodePair(41, ellipse.StartAngle*MathHelper.DegToRad);
            this.WriteCodePair(42, ellipse.EndAngle*MathHelper.DegToRad);

            this.WriteXData(ellipse.XData);
        }

        private void WriteEllipseAsPolyline(Ellipse ellipse)
        {
            //we will draw the ellipse as a polyline, it is not supported in AutoCad12 dxf files
            this.WriteCodePair(0, DxfObjectCode.Polyline);

            this.WriteEntityCommonCodes(ellipse);

            //closed polyline
            this.WriteCodePair(70, 1);

            //dummy point
            this.WriteCodePair(10, 0.0f);
            this.WriteCodePair(20, 0.0f);
            this.WriteCodePair(30, ellipse.Center.Z);

            this.WriteCodePair(39, ellipse.Thickness);

            this.WriteCodePair(210, ellipse.Normal.X);
            this.WriteCodePair(220, ellipse.Normal.Y);
            this.WriteCodePair(230, ellipse.Normal.Z);

            //Obsolete; formerly an “entities follow flag” (optional; ignore if present)
            //but its needed to load the dxf file in AutoCAD
            this.WriteCodePair(66, "1");

            this.WriteXData(ellipse.XData);

            List<Vector2> points = ellipse.PolygonalVertexes(ellipse.CurvePoints);
            foreach (Vector2 v in points)
            {
                this.WriteCodePair(0, DxfObjectCode.Vertex);
                this.WriteCodePair(8, ellipse.Layer);
                this.WriteCodePair(70, 0);
                this.WriteCodePair(10, v.X);
                this.WriteCodePair(20, v.Y);
            }
            this.WriteCodePair(0, StringCode.EndSequence);
        }

        private void WriteNurbsCurve(NurbsCurve nurbsCurve)
        {
            if (this.activeSection != StringCode.EntitiesSection && !this.isBlockEntities)
            {
                throw new InvalidDxfSectionException(this.activeSection, this.file);
            }


            //we will draw the nurbsCurve as a polyline, it is not supported in AutoCad12 dxf files
            this.WriteCodePair(0, DxfObjectCode.Polyline);

            this.WriteEntityCommonCodes(nurbsCurve);

            //open polyline
            this.WriteCodePair(70, 0);

            //dummy point
            this.WriteCodePair(10, 0.0f);
            this.WriteCodePair(20, 0.0f);
            this.WriteCodePair(30, nurbsCurve.Elevation);

            this.WriteCodePair(39, nurbsCurve.Thickness);

            this.WriteCodePair(210, nurbsCurve.Normal.X);
            this.WriteCodePair(220, nurbsCurve.Normal.Y);
            this.WriteCodePair(230, nurbsCurve.Normal.Z);

            //Obsolete; formerly an “entities follow flag” (optional; ignore if present)
            //but its needed to load the dxf file in AutoCAD
            this.WriteCodePair(66, "1");

            this.WriteXData(nurbsCurve.XData);

            List<Vector2> points = nurbsCurve.PolygonalVertexes(nurbsCurve.CurvePoints);
            foreach (Vector2 v in points)
            {
                this.WriteCodePair(0, DxfObjectCode.Vertex);
                this.WriteCodePair(8, nurbsCurve.Layer);
                this.WriteCodePair(70, 0);
                this.WriteCodePair(10, v.X);
                this.WriteCodePair(20, v.Y);
            }
            this.WriteCodePair(0, StringCode.EndSequence);
        }

        private void WritePolyline2d(Polyline polyline)
        {
            if (this.activeSection != StringCode.EntitiesSection && !this.isBlockEntities)
            {
                throw new InvalidDxfSectionException(this.activeSection, this.file);
            }

            this.WriteCodePair(0, polyline.CodeName);
            this.WriteCodePair(100, SubclassMarker.Entity);
            this.WriteEntityCommonCodes(polyline);
            this.WriteCodePair(5, polyline.Handle);
            this.WriteCodePair(100, SubclassMarker.Polyline);

            this.WriteCodePair(70, (int) polyline.Flags);

            //dummy point
            this.WriteCodePair(10, 0.0);
            this.WriteCodePair(20, 0.0);

            this.WriteCodePair(30, polyline.Elevation);
            this.WriteCodePair(39, polyline.Thickness);

            this.WriteCodePair(210, polyline.Normal.X);
            this.WriteCodePair(220, polyline.Normal.Y);
            this.WriteCodePair(230, polyline.Normal.Z);

            //Obsolete; formerly an “entities follow flag” (optional; ignore if present)
            //but its needed to load the dxf file in AutoCAD
            this.WriteCodePair(66, "1");

            this.WriteXData(polyline.XData);

            foreach (PolylineVertex v in polyline.Vertexes)
            {
               
                this.WriteCodePair(0, v.CodeName);
                this.WriteCodePair(5, v.Handle);
                this.WriteCodePair(100, SubclassMarker.Entity);
                this.WriteCodePair(8, v.Layer);
                this.WriteCodePair(100, SubclassMarker.Vertex);
                this.WriteCodePair(100, SubclassMarker.PolylineVertex);
                this.WriteCodePair(70, (int) v.Flags);
                this.WriteCodePair(10, v.Location.X);
                this.WriteCodePair(20, v.Location.Y);
                this.WriteCodePair(40, v.BeginThickness);
                this.WriteCodePair(41, v.EndThickness);
                this.WriteCodePair(42, v.Bulge);

                this.WriteXData(v.XData);
            }

            this.WriteCodePair(0, polyline.EndSequence.CodeName);
            this.WriteCodePair(5, polyline.EndSequence.Handle);
            this.WriteCodePair(100, SubclassMarker.Entity);
            this.WriteCodePair(8, polyline.EndSequence.Layer);
        }

        private void WriteLightWeightPolyline(LightWeightPolyline polyline)
        {
            if (this.activeSection != StringCode.EntitiesSection && !this.isBlockEntities)
            {
                throw new InvalidDxfSectionException(this.activeSection, this.file);
            }

            this.WriteCodePair(0, DxfObjectCode.LightWeightPolyline);
            this.WriteCodePair(100, SubclassMarker.Entity);
            this.WriteEntityCommonCodes(polyline);
            this.WriteCodePair(5, polyline.Handle);
            this.WriteCodePair(100, SubclassMarker.LightWeightPolyline);
            this.WriteCodePair(90, polyline.Vertexes.Count);
            this.WriteCodePair(70, (int) polyline.Flags);

            this.WriteCodePair(38, polyline.Elevation);
            this.WriteCodePair(39, polyline.Thickness);


            foreach (LightWeightPolylineVertex v in polyline.Vertexes)
            {
                this.WriteCodePair(10, v.Location.X);
                this.WriteCodePair(20, v.Location.Y);
                this.WriteCodePair(40, v.BeginThickness);
                this.WriteCodePair(41, v.EndThickness);
                this.WriteCodePair(42, v.Bulge);
            }

            this.WriteCodePair(210, polyline.Normal.X);
            this.WriteCodePair(220, polyline.Normal.Y);
            this.WriteCodePair(230, polyline.Normal.Z);

            this.WriteXData(polyline.XData);
        }

        #endregion

        #region methods for Entity section

        internal void WriteDictionary(Dictionary dictionary)
        {
            //if (this.activeTable != StringCode.ObjectsSection)
            //{
            //    throw new InvalidDxfTableException(this.activeTable, this.file);
            //}

            this.WriteCodePair(0, StringCode.Dictionary);
            this.WriteCodePair(5, Convert.ToString(10, 16));
            this.WriteCodePair(100, SubclassMarker.Dictionary);
            this.WriteCodePair(281, 1);
            this.WriteCodePair(3, dictionary);
            this.WriteCodePair(350, Convert.ToString(11, 16));

            this.WriteCodePair(0, StringCode.Dictionary);
            this.WriteCodePair(5, Convert.ToString(11, 16));
            this.WriteCodePair(100, SubclassMarker.Dictionary);
            this.WriteCodePair(281, 1);
        }

        #endregion

        #region private methods

        private void WriteXData(Dictionary<ApplicationRegistry, XData> xData)
        {
            if (xData == null)
                return;

            foreach (ApplicationRegistry appReg in xData.Keys)
            {
                this.WriteCodePair(XDataCode.AppReg, appReg);
                foreach (XDataRecord x in xData[appReg].XDataRecord)
                {
                    this.WriteCodePair(x.Code, x.Value.ToString());
                }
            }
        }

        private void WriteEntityCommonCodes(IEntityObject entity)
        {
            this.WriteCodePair(8, entity.Layer);
            this.WriteCodePair(62, entity.Color.Index);
            this.WriteCodePair(6, entity.LineType);
        }

        private void WriteCodePair(int codigo, object valor)
        {
            // AutoCad12 does not allow strings with spaces
            string nameConversion;
            nameConversion = valor == null ? string.Empty : valor.ToString();

            if (this.version == DxfVersion.AutoCad12 && valor is DxfObject) nameConversion = nameConversion.Replace(' ', '_');
            this.writer.WriteLine(codigo);
            this.writer.WriteLine(nameConversion);
        }

        #endregion
    }
}
