// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="3mf.classes.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************


using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Xml.Serialization;

namespace TVGL.threemfclasses
{

    #region Build and Item

    /// <summary>
    /// Class Build is a major categoy usually following resources.
    /// </summary>
#if help
    internal class Build
#else
    public class Build
#endif
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Build" /> class.
        /// </summary>
        public Build()
        {
            Items = new List<Item>();
        }

        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        [XmlElement("item")]
        public List<Item> Items { get; set; }
    }

    /// <summary>
    /// Class Item - is used in the build section.
    /// </summary>
#if help
    internal class Item
#else
    public class Item
#endif
    {
        /// <summary>
        /// Gets or sets the objectid.
        /// </summary>
        /// <value>The objectid.</value>
        [XmlAttribute]
        public int objectid { get; set; }

        /// <summary>
        /// Gets or sets the transform.
        /// </summary>
        /// <value>The transform.</value>
        [XmlAttribute]
        public string transform { get; set; }
        /// <summary>
        /// Gets the transform array.
        /// </summary>
        /// <value>The transform array.</value>
        internal double[] transformArray => MakeTransformArray(transform);
        /// <summary>
        /// Gets the transform matrix.
        /// </summary>
        /// <value>The transform matrix.</value>
        internal Matrix4x4 transformMatrix => MakeTransformMatrix(transformArray);

        /// <summary>
        /// Gets or sets the itemref.
        /// </summary>
        /// <value>The itemref.</value>
        [XmlAttribute]
        public string itemref { get; set; }


        /// <summary>
        /// Makes the transform array.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <returns>System.Double[].</returns>
        internal static double[] MakeTransformArray(string transform)
        {

            if (string.IsNullOrWhiteSpace(transform)) return null;
            var stringTerms = transform.Trim().Split(' ', ',');
            var num = stringTerms.Length;
            var result = new double[num];
            for (var i = 0; i < num; i++)
            {
                double term;
                if (double.TryParse(stringTerms[i], out term))
                    result[i] = term;
                else result[i] = double.NaN;
            }
            return result;
        }


        /// <summary>
        /// Makes the transform matrix.
        /// </summary>
        /// <param name="transformArray">The transform array.</param>
        /// <returns>Matrix4x4.</returns>
        internal static Matrix4x4 MakeTransformMatrix(double[] transformArray)
        {
            if (transformArray == null || (transformArray.Length != 3 && transformArray.Length != 12)) return new Matrix4x4();
            if (transformArray.Length == 3)
                return Matrix4x4.CreateTranslation(transformArray[0], transformArray[1], transformArray[2]);
            return new Matrix4x4(transformArray[0], transformArray[1], transformArray[2], 
                transformArray[3], transformArray[4], transformArray[5], 
                transformArray[6], transformArray[7], transformArray[8],
                transformArray[9], transformArray[10], transformArray[11]
                );
        }
    }
    #endregion

    #region just MetaData

    /// <summary>
    /// Class Metadata is used in the header and potentially other places.
    /// </summary>
#if help
    internal class Metadata
#else
    public class Metadata
#endif
    {
        /// <summary>
        /// The type
        /// </summary>
        [XmlAttribute("name")]
        public string type;

        /// <summary>
        /// The value
        /// </summary>
        [XmlText]
        public string Value;
    }

    #endregion

    #region Objects: Component, Mesh, etc.

    /// <summary>
    /// Class Component.
    /// </summary>
#if help
    internal class Component
#else
    public class Component
#endif
    {
        /// <summary>
        /// Gets or sets the objectid.
        /// </summary>
        /// <value>The objectid.</value>
        [XmlAttribute]
        public int objectid
        { get; set; }

        /// <summary>
        /// Gets or sets the transform.
        /// </summary>
        /// <value>The transform.</value>
        [XmlAttribute]
        public string transform { get; set; }
        /// <summary>
        /// Gets the transform array.
        /// </summary>
        /// <value>The transform array.</value>
        internal double[] transformArray => Item.MakeTransformArray(transform);
        /// <summary>
        /// Gets the transform matrix.
        /// </summary>
        /// <value>The transform matrix.</value>
        internal Matrix4x4 transformMatrix => Item.MakeTransformMatrix(transformArray);
    }

    /// <summary>
    /// Class CT_Triangle.
    /// </summary>
#if help
    internal class Triangle
#else
    public class Triangle
#endif
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Triangle" /> class.
        /// </summary>
        public Triangle()
        {
            p1 = p2 = p3 = pid = -1;
        }
        /// <summary>
        /// Gets or sets the v1.
        /// </summary>
        /// <value>The v1.</value>
        [XmlAttribute]
        public int v1 { get; set; }

        /// <summary>
        /// Gets or sets the v2.
        /// </summary>
        /// <value>The v2.</value>
        [XmlAttribute]
        public int v2 { get; set; }

        /// <summary>
        /// Gets or sets the v3.
        /// </summary>
        /// <value>The v3.</value>
        [XmlAttribute]
        public int v3 { get; set; }

        /// <summary>
        /// Gets or sets the p1.
        /// </summary>
        /// <value>The p1.</value>
        [XmlAttribute]
        [DefaultValue(-1)]
        public int p1 { get; set; }

        /// <summary>
        /// Gets or sets the p2.
        /// </summary>
        /// <value>The p2.</value>
        [XmlAttribute]
        [DefaultValue(-1)]
        public int p2 { get; set; }

        /// <summary>
        /// Gets or sets the p3.
        /// </summary>
        /// <value>The p3.</value>
        [XmlAttribute]
        [DefaultValue(-1)]
        public int p3 { get; set; }

        /// <summary>
        /// Gets or sets the pid.
        /// </summary>
        /// <value>The pid.</value>
        [XmlAttribute]
        [DefaultValue(-1)]
        public int pid { get; set; }
    }

    /// <summary>
    /// Class CT_Vertex.
    /// </summary>
#if help
    internal class Vertex
#else
    public class Vertex
#endif
    {
        /// <summary>
        /// Gets or sets the x.
        /// </summary>
        /// <value>The x.</value>
        [XmlAttribute]
        public double x { get; set; }

        /// <summary>
        /// Gets or sets the y.
        /// </summary>
        /// <value>The y.</value>
        [XmlAttribute]
        public double y { get; set; }

        /// <summary>
        /// Gets or sets the z.
        /// </summary>
        /// <value>The z.</value>
        [XmlAttribute]
        public double z { get; set; }
    }

    /// <summary>
    /// Class CT_Mesh.
    /// </summary>
#if help
    internal class Mesh
#else
    public class Mesh
#endif
    {
        /// <summary>
        /// Gets or sets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        [XmlArrayItem("vertex", IsNullable = false)]
        public List<Vertex> vertices { get; set; }

        /// <summary>
        /// Gets or sets the triangles.
        /// </summary>
        /// <value>The triangles.</value>
        [XmlArrayItem("triangle", IsNullable = false)]
        public List<Triangle> triangles { get; set; }
    }

    /// <summary>
    /// Class Object.
    /// </summary>
#if help
    internal class Object
#else
    public class Object
#endif
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Object" /> class.
        /// </summary>
        public Object()
        {
            MaterialID = -1;
        }
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [XmlAttribute]
        public int id { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        [XmlAttribute]
        //[DefaultValue(ObjectType.model)]
        public ObjectType type { get; set; }

        /// <summary>
        /// Gets or sets the material identifier.
        /// </summary>
        /// <value>The material identifier.</value>
        [XmlAttribute("materialid")]
        [DefaultValue(-1)]
        public int MaterialID { get; set; }

        /// <summary>
        /// Gets or sets the thumbnail.
        /// </summary>
        /// <value>The thumbnail.</value>
        [XmlAttribute]
        public string thumbnail { get; set; }

        /// <summary>
        /// Gets or sets the partnumber.
        /// </summary>
        /// <value>The partnumber.</value>
        [XmlAttribute]
        [DefaultValue(0)]
        public int partnumber { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [XmlAttribute]
        public string name { get; set; }

        /// <summary>
        /// Gets or sets the mesh.
        /// </summary>
        /// <value>The mesh.</value>
        [XmlElement]
        public Mesh mesh { get; set; }

        /// <summary>
        /// Gets or sets the components.
        /// </summary>
        /// <value>The components.</value>
        [XmlArrayItem("component", IsNullable = false)]
        public List<Component> components { get; set; }
    }

    /// <summary>
    /// Enum ST_ObjectType
    /// </summary>
#if help
    internal enum ObjectType
#else
    public enum ObjectType
#endif
    {
        /// <summary>
        /// The model
        /// </summary>
        model,

        /// <summary>
        /// The support
        /// </summary>
        support,

        /// <summary>
        /// The other
        /// </summary>
        other
    }

    #endregion

    #region Resources

    /// <summary>
    /// Class CT_Resources.
    /// </summary>
#if help
    internal class Resources
#else
    public class Resources
#endif
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Resources" /> class.
        /// </summary>
        public Resources()
        {
            objects = new List<Object>();
            materials = new List<Material>();
            basematerials = new List<BaseMaterials>();
        }

        /// <summary>
        /// Gets or sets the basematerials.
        /// </summary>
        /// <value>The basematerials.</value>
        [XmlElement("basematerials")]
        public List<BaseMaterials> basematerials { get; set; }

        /// <summary>
        /// Gets or sets the materials.
        /// </summary>
        /// <value>The materials.</value>
        [XmlElement("material")]
        public List<Material> materials { get; set; }

        /// <summary>
        /// Gets or sets the colors.
        /// </summary>
        /// <value>The colors.</value>
        [XmlElement("color")]
        public List<Color3MF> colors { get; set; }

        /// <summary>
        /// Gets or sets the object.
        /// </summary>
        /// <value>The object.</value>
        [XmlElement("object")]
        public List<Object> objects { get; set; }
    }

    #endregion

    #region Materials and Colors
    #region the 2013/01 approach
    /// <summary>
    /// Class Material.
    /// </summary>
#if help
    internal class Material
#else
    public class Material
#endif
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [XmlAttribute]
        public string name { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [XmlAttribute]
        public int id { get; set; }

        /// <summary>
        /// Gets or sets the colorid.
        /// </summary>
        /// <value>The colorid.</value>
        [XmlAttribute]
        public int colorid { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        [XmlAttribute]
        public string type { get; set; }
    }

    /// <summary>
    /// Class Color3MF.
    /// </summary>
#if help
    internal class Color3MF
#else
    public class Color3MF
#endif
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [XmlAttribute]
        public string name { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [XmlAttribute]
        public int id { get; set; }

        /// <summary>
        /// Gets or sets the color string.
        /// </summary>
        /// <value>The color string.</value>
        [XmlAttribute("value")]
        public string colorString { get; set; }

        /// <summary>
        /// Gets the color.
        /// </summary>
        /// <value>The color.</value>
        internal Color color
        {
            get
            {
                if (_color == null)
                    _color = new Color(colorString);
                return _color;
            }
        }
        /// <summary>
        /// The color
        /// </summary>
        private Color _color;


    }
    #endregion
    #region the 2015/02 approach
    /// <summary>
    /// Class BaseMaterials.
    /// </summary>
#if help
    internal class BaseMaterials
#else
    public class BaseMaterials
#endif
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseMaterials" /> class.
        /// </summary>
        public BaseMaterials()
        {
            bases = new List<Base>();
        }

        /// <summary>
        /// Gets or sets the base.
        /// </summary>
        /// <value>The base.</value>
        [XmlElement("base")]
        public List<Base> bases { get; set; }
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [XmlAttribute]
        public int id { get; set; }
    }
    /// <summary>
    /// Class Base.
    /// </summary>
#if help
    internal class Base
#else
    public class Base
#endif
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [XmlAttribute]
        public string name { get; set; } = "noname";

        /// <summary>
        /// Gets or sets the color string.
        /// </summary>
        /// <value>The color string.</value>
        [XmlAttribute("displaycolor")]
        public string colorString { get; set; }

        /// <summary>
        /// Gets the color.
        /// </summary>
        /// <value>The color.</value>
        internal Color color
        {
            get
            {
                if (_color == null)
                    _color = new Color(colorString);
                return _color;
            }
        }
        /// <summary>
        /// The color
        /// </summary>
        private Color _color;

    }

    #endregion
    #endregion

    #region Content_Types
#if help
    internal class Types
#else
    /// <summary>
    /// Class Types.
    /// </summary>
    public class Types
#endif
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Types" /> class.
        /// </summary>
        public Types()
        {
            Defaults = new List<Default>();
        }

        /// <summary>
        /// Gets or sets the defaults.
        /// </summary>
        /// <value>The defaults.</value>
        [XmlElement("Default")]
        public List<Default> Defaults { get; set; }

        /// <summary>
        /// The rels
        /// </summary>
        [XmlElement("Relationship")]
        public Relationship[] rels;
    }
#if help
    internal class Default
#else
    /// <summary>
    /// Class Default.
    /// </summary>
    public class Default
#endif
    {
        /// <summary>
        /// Gets or sets the extension.
        /// </summary>
        /// <value>The extension.</value>
        [XmlAttribute]
        public string Extension { get; set; }

        /// <summary>
        /// Gets or sets the type of the content.
        /// </summary>
        /// <value>The type of the content.</value>
        [XmlAttribute]
        public string ContentType { get; set; }
    }
    #endregion

    #region Relationships


#if help
    internal class Relationship
#else
    /// <summary>
    /// Class Relationship.
    /// </summary>
    public class Relationship
#endif
    {
        /// <summary>
        /// Gets or sets the target.
        /// </summary>
        /// <value>The target.</value>
        [XmlAttribute]
        public string Target { get; set; }
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [XmlAttribute]
        public string Id { get; set; }
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        [XmlAttribute]
        public string Type { get; set; }
    }
    #endregion

}
