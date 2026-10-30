// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 10-21-2026
//
// GENERATED FILE - do not edit by hand. Generated from the entity schema (src/prc.json, PRC
// version 8137) of prc-rs, https://github.com/ralovich/prc-rs, commit
// 023fc07c823a85f1ef2ebc077c3b327a97b09701, which is MIT licensed:
// Copyright (c) Kristof Ralovich 2025-2026.
//
// One reader per PRC (ISO 14739-1) entity of the globals, tree, tessellation and model-file
// sections, in file order. PRC entities carry no length, so every entity before the meshes has to
// be read field by field. Each reader returns a PrcNode holding the fields by their schema name.
// One erratum is applied over the schema: pdf-issues #833, face_has_texture of
// PRC_TYPE_TESS_3D_Compressed holds one flag when has_faces is FALSE.
// ***********************************************************************
using System;
using System.Linq;

namespace TVGL
{
    /// <summary> PRC entity type identifiers used by the entity reader. </summary>
    internal static class PrcType
    {
        internal const uint ROOT_PRCBaseWithGraphics = 2;
        internal const uint SURF_Plane = 86;
        internal const uint TESS = 170;
        internal const uint TESS_3D = 172;
        internal const uint TESS_3D_Compressed = 173;
        internal const uint TESS_Face = 174;
        internal const uint TESS_3D_Wire = 175;
        internal const uint TESS_Markup = 176;
        internal const uint MISC_Attribute = 201;
        internal const uint MISC_CartesianTransformation = 202;
        internal const uint MISC_EntityReference = 203;
        internal const uint MISC_MarkupLinkedItem = 204;
        internal const uint MISC_ReferenceOnPRCBase = 205;
        internal const uint MISC_ReferenceOnTopology = 206;
        internal const uint MISC_GeneralTransformation = 207;
        internal const uint RI_RepresentationItem = 231;
        internal const uint RI_BrepModel = 232;
        internal const uint RI_Curve = 233;
        internal const uint RI_Direction = 234;
        internal const uint RI_Plane = 235;
        internal const uint RI_PointSet = 236;
        internal const uint RI_PolyBrepModel = 237;
        internal const uint RI_PolyWire = 238;
        internal const uint RI_Set = 239;
        internal const uint RI_CoordinateSystem = 240;
        internal const uint ASM_ModelFile = 301;
        internal const uint ASM_FileStructure = 302;
        internal const uint ASM_FileStructureGlobals = 303;
        internal const uint ASM_FileStructureTree = 304;
        internal const uint ASM_FileStructureTessellation = 305;
        internal const uint ASM_ProductOccurrence = 310;
        internal const uint ASM_PartDefinition = 311;
        internal const uint ASM_Filter = 320;
        internal const uint MKP_View = 501;
        internal const uint MKP_Markup = 502;
        internal const uint MKP_Leader = 503;
        internal const uint MKP_AnnotationItem = 504;
        internal const uint MKP_AnnotationSet = 505;
        internal const uint MKP_AnnotationReference = 506;
        internal const uint GRAPH_Style = 701;
        internal const uint GRAPH_Material = 702;
        internal const uint GRAPH_Picture = 703;
        internal const uint GRAPH_TextureApplication = 711;
        internal const uint GRAPH_TextureDefinition = 712;
        internal const uint GRAPH_TextureTransformation = 713;
        internal const uint GRAPH_LinePattern = 721;
        internal const uint GRAPH_FillPattern = 722;
        internal const uint GRAPH_DottingPattern = 723;
        internal const uint GRAPH_HatchingPattern = 724;
        internal const uint GRAPH_SolidPattern = 725;
        internal const uint GRAPH_VPicturePattern = 726;
        internal const uint GRAPH_AmbientLight = 731;
        internal const uint GRAPH_PointLight = 732;
        internal const uint GRAPH_DirectionalLight = 733;
        internal const uint GRAPH_SpotLight = 734;
        internal const uint GRAPH_SceneDisplayParameters = 741;
        internal const uint GRAPH_Camera = 742;
    }

    /// <summary> Reads PRC entities from a decompressed section. </summary>
    internal static class PrcEntityReader
    {
        internal static PrcNode Read_Schema(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("Schema");
            var f_schema_count = n.Set("schema_count", r.ReadUnsignedInteger());
            var f_schemas = new PrcNode[c.Count(f_schema_count, r)];
            for (var i = 0; i < f_schemas.Length; i++) f_schemas[i] = Read_Entity_schema_definition(r, c);
            n.Set("schemas", f_schemas);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_ASM_FileStructureGlobals(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_ASM_FileStructureGlobals");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.ASM_FileStructureGlobals, "PRC_TYPE_ASM_FileStructureGlobals.id");
            var f_base = n.Set("base", Read_ContentPRCBase(r, c));
            var f_file_count = n.Set("file_count", r.ReadUnsignedInteger());
            var f_unique_ids = new PrcNode[c.Count(f_file_count, r)];
            for (var i = 0; i < f_unique_ids.Length; i++) f_unique_ids[i] = Read_UniqueId(r, c);
            n.Set("unique_ids", f_unique_ids);
            var f_global_data = n.Set("global_data", Read_FileStructureInternalGlobalData(r, c));
            c.EvaluateSchema(r, (uint)PrcType.ASM_FileStructureGlobals);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_ASM_FileStructureTree(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_ASM_FileStructureTree");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.ASM_FileStructureTree, "PRC_TYPE_ASM_FileStructureTree.id");
            var f_base = n.Set("base", Read_ContentPRCBase(r, c));
            var f_part_count = n.Set("part_count", r.ReadUnsignedInteger());
            var f_parts = new PrcNode[c.Count(f_part_count, r)];
            for (var i = 0; i < f_parts.Length; i++) f_parts[i] = Read_PRC_TYPE_ASM_PartDefinition(r, c);
            n.Set("parts", f_parts);
            var f_product_count = n.Set("product_count", r.ReadUnsignedInteger());
            var f_products = new PrcNode[c.Count(f_product_count, r)];
            for (var i = 0; i < f_products.Length; i++) f_products[i] = Read_PRC_TYPE_ASM_ProductOccurrence(r, c);
            n.Set("products", f_products);
            var f_internal_data = n.Set("internal_data", Read_PRC_TYPE_ASM_FileStructure(r, c));
            c.EvaluateSchema(r, (uint)PrcType.ASM_FileStructureTree);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_ASM_FileStructureTessellation(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_ASM_FileStructureTessellation");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.ASM_FileStructureTessellation, "PRC_TYPE_ASM_FileStructureTessellation.id");
            var f_base = n.Set("base", Read_ContentPRCBase(r, c));
            var f_tess_count = n.Set("tess_count", r.ReadUnsignedInteger());
            var f_tess = new PrcNode[c.Count(f_tess_count, r)];
            for (var i = 0; i < f_tess.Length; i++) f_tess[i] = Read_PRC_TYPE_TESS(r, c);
            n.Set("tess", f_tess);
            c.EvaluateSchema(r, (uint)PrcType.ASM_FileStructureTessellation);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_ASM_ModelFile(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_ASM_ModelFile");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.ASM_ModelFile, "PRC_TYPE_ASM_ModelFile.id");
            var f_base = n.Set("base", Read_ContentPRCBase(r, c));
            var f_units_from_cad_file = n.Set("units_from_cad_file", r.ReadBoolean());
            var f_units_in_mm = n.Set("units_in_mm", r.ReadDouble());
            var f_number_of_root_product_occurrences = n.Set("number_of_root_product_occurrences", r.ReadUnsignedInteger());
            var f_product_occurrences = new PrcNode[c.Count(f_number_of_root_product_occurrences, r)];
            for (var i = 0; i < f_product_occurrences.Length; i++) f_product_occurrences[i] = Read_ProductOccurrenceReference(r, c);
            n.Set("product_occurrences", f_product_occurrences);
            var f_file_structure_index_in_model_file = new uint[c.Count(c.NumberOfFileStructures, r)];
            for (var i = 0; i < f_file_structure_index_in_model_file.Length; i++) f_file_structure_index_in_model_file[i] = r.ReadUnsignedInteger();
            n.Set("file_structure_index_in_model_file", f_file_structure_index_in_model_file);
            c.EvaluateSchema(r, (uint)PrcType.ASM_ModelFile);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_Entity_schema_definition(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("Entity_schema_definition");
            var f_entity_type = n.Set("entity_type", r.ReadUnsignedInteger());
            var f_token_count = n.Set("token_count", r.ReadUnsignedInteger());
            var f_schema_tokens = new uint[c.Count(f_token_count, r)];
            for (var i = 0; i < f_schema_tokens.Length; i++) f_schema_tokens[i] = r.ReadUnsignedInteger();
            n.Set("schema_tokens", f_schema_tokens);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_ContentPRCBase(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("ContentPRCBase");
            var f_attribute_data = n.Set("attribute_data", Read_AttributeData(r, c));
            var f_entity_name = n.Set("entity_name", Read_Name(r, c));
            c.Leave();
            return n;
        }

        internal static PrcNode Read_UniqueId(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("UniqueId");
            var f_unique_id0 = n.Set("unique_id0", r.ReadUnsignedInteger());
            var f_unique_id1 = n.Set("unique_id1", r.ReadUnsignedInteger());
            var f_unique_id2 = n.Set("unique_id2", r.ReadUnsignedInteger());
            var f_unique_id3 = n.Set("unique_id3", r.ReadUnsignedInteger());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_FileStructureInternalGlobalData(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("FileStructureInternalGlobalData");
            var f_tess_chord = n.Set("tess_chord", r.ReadDouble());
            var f_tess_angle = n.Set("tess_angle", r.ReadDouble());
            var f_serialize_help = n.Set("serialize_help", Read_MarkupSerializationHelper(r, c));
            var f_color_count = n.Set("color_count", r.ReadUnsignedInteger());
            var f_colors = new PrcNode[c.Count(f_color_count, r)];
            for (var i = 0; i < f_colors.Length; i++) f_colors[i] = Read_RgbColor(r, c);
            n.Set("colors", f_colors);
            var f_picture_count = n.Set("picture_count", r.ReadUnsignedInteger());
            var f_pictures = new PrcNode[c.Count(f_picture_count, r)];
            for (var i = 0; i < f_pictures.Length; i++) f_pictures[i] = Read_PRC_TYPE_GRAPH_Picture(r, c);
            n.Set("pictures", f_pictures);
            var f_texture_count = n.Set("texture_count", r.ReadUnsignedInteger());
            var f_textures = new PrcNode[c.Count(f_texture_count, r)];
            for (var i = 0; i < f_textures.Length; i++) f_textures[i] = Read_PRC_TYPE_GRAPH_TextureDefinition(r, c);
            n.Set("textures", f_textures);
            var f_material_count = n.Set("material_count", r.ReadUnsignedInteger());
            var f_materials = new PrcNode[c.Count(f_material_count, r)];
            for (var i = 0; i < f_materials.Length; i++) f_materials[i] = Read_Material(r, c);
            n.Set("materials", f_materials);
            var f_line_pattern_count = n.Set("line_pattern_count", r.ReadUnsignedInteger());
            var f_line_patterns = new PrcNode[c.Count(f_line_pattern_count, r)];
            for (var i = 0; i < f_line_patterns.Length; i++) f_line_patterns[i] = Read_PRC_TYPE_GRAPH_LinePattern(r, c);
            n.Set("line_patterns", f_line_patterns);
            var f_style_count = n.Set("style_count", r.ReadUnsignedInteger());
            var f_styles = new PrcNode[c.Count(f_style_count, r)];
            for (var i = 0; i < f_styles.Length; i++) f_styles[i] = Read_PRC_TYPE_GRAPH_Style(r, c);
            n.Set("styles", f_styles);
            var f_fill_count = n.Set("fill_count", r.ReadUnsignedInteger());
            var f_fills = new PrcNode[c.Count(f_fill_count, r)];
            for (var i = 0; i < f_fills.Length; i++) f_fills[i] = Read_PRC_TYPE_GRAPH_FillPattern(r, c);
            n.Set("fills", f_fills);
            var f_ref_coord_count = n.Set("ref_coord_count", r.ReadUnsignedInteger());
            var f_ref_coords = new PrcNode[c.Count(f_ref_coord_count, r)];
            for (var i = 0; i < f_ref_coords.Length; i++) f_ref_coords[i] = Read_PRC_TYPE_RI_CoordinateSystem(r, c);
            n.Set("ref_coords", f_ref_coords);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_ASM_PartDefinition(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_ASM_PartDefinition");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.ASM_PartDefinition, "PRC_TYPE_ASM_PartDefinition.id");
            var f_base = n.Set("base", Read_PRC_TYPE_ROOT_PRCBaseWithGraphics(r, c));
            var f_bounding_box = n.Set("bounding_box", Read_BoundingBox(r, c));
            var f_number_of_representation_items = n.Set("number_of_representation_items", r.ReadUnsignedInteger());
            var f_representation_items = new PrcNode[c.Count(f_number_of_representation_items, r)];
            for (var i = 0; i < f_representation_items.Length; i++) f_representation_items[i] = Read_PRC_TYPE_RI_RepresentationItem(r, c);
            n.Set("representation_items", f_representation_items);
            var f_markups = n.Set("markups", Read_MarkupData(r, c));
            var f_number_of_views = n.Set("number_of_views", r.ReadUnsignedInteger());
            var f_views = new PrcNode[c.Count(f_number_of_views, r)];
            for (var i = 0; i < f_views.Length; i++) f_views[i] = Read_PRC_TYPE_MKP_View(r, c);
            n.Set("views", f_views);
            c.EvaluateSchema(r, (uint)PrcType.ASM_PartDefinition);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_ASM_ProductOccurrence(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_ASM_ProductOccurrence");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.ASM_ProductOccurrence, "PRC_TYPE_ASM_ProductOccurrence.id");
            var f_base = n.Set("base", Read_PRC_TYPE_ROOT_PRCBaseWithGraphics(r, c));
            var f_references_product_occurrence = n.Set("references_product_occurrence", Read_ReferencesOfProductOccurrence(r, c));
            var f_product_behavior = n.Set("product_behavior", r.ReadCharacter());
            var f_product_information = n.Set("product_information", Read_ProductInformation(r, c));
            var f_has_transformation = n.Set("has_transformation", r.ReadBoolean());
            PrcNode f_location = default;
            if (f_has_transformation)
            {
                var t_location = r.PeekUnsignedInteger();
                f_location = n.Set("location", t_location switch
                {
                    PrcType.MISC_CartesianTransformation => Read_PRC_TYPE_MISC_CartesianTransformation(r, c),
                    PrcType.MISC_GeneralTransformation => Read_PRC_TYPE_MISC_GeneralTransformation(r, c),
                    _ => throw c.UnknownType("PRC_TYPE_ASM_ProductOccurrence.location", t_location),
                });
            }
            var f_entity_ref_count = n.Set("entity_ref_count", r.ReadUnsignedInteger());
            var f_entity_reference = new PrcNode[c.Count(f_entity_ref_count, r)];
            for (var i = 0; i < f_entity_reference.Length; i++) f_entity_reference[i] = Read_PRC_TYPE_MISC_EntityReference(r, c);
            n.Set("entity_reference", f_entity_reference);
            var f_markups = n.Set("markups", Read_MarkupData(r, c));
            var f_number_of_views = n.Set("number_of_views", r.ReadUnsignedInteger());
            var f_views = new PrcNode[c.Count(f_number_of_views, r)];
            for (var i = 0; i < f_views.Length; i++) f_views[i] = Read_PRC_TYPE_MKP_View(r, c);
            n.Set("views", f_views);
            var f_has_filter = n.Set("has_filter", r.ReadBoolean());
            PrcNode f_entity_filter = default;
            if (f_has_filter)
            {
                f_entity_filter = n.Set("entity_filter", Read_PRC_TYPE_ASM_Filter(r, c));
            }
            var f_number_of_display_filters = n.Set("number_of_display_filters", r.ReadUnsignedInteger());
            var f_display_filters = new PrcNode[c.Count(f_number_of_display_filters, r)];
            for (var i = 0; i < f_display_filters.Length; i++) f_display_filters[i] = Read_PRC_TYPE_ASM_Filter(r, c);
            n.Set("display_filters", f_display_filters);
            var f_number_of_scene_parameters = n.Set("number_of_scene_parameters", r.ReadUnsignedInteger());
            var f_scene_display_parameters = new PrcNode[c.Count(f_number_of_scene_parameters, r)];
            for (var i = 0; i < f_scene_display_parameters.Length; i++) f_scene_display_parameters[i] = Read_PRC_TYPE_GRAPH_SceneDisplayParameters(r, c);
            n.Set("scene_display_parameters", f_scene_display_parameters);
            c.EvaluateSchema(r, (uint)PrcType.ASM_ProductOccurrence);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_ASM_FileStructure(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_ASM_FileStructure");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.ASM_FileStructure, "PRC_TYPE_ASM_FileStructure.id");
            var f_base = n.Set("base", Read_ContentPRCBase(r, c));
            var f_next_available_index = n.Set("next_available_index", r.ReadUnsignedInteger());
            var f_index_product_occurrence = n.Set("index_product_occurrence", r.ReadUnsignedInteger());
            c.EvaluateSchema(r, (uint)PrcType.ASM_FileStructure);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_TESS(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_TESS");
            PrcNode f_id;
            var t_id = r.PeekUnsignedInteger();
            f_id = n.Set("id", t_id switch
            {
                PrcType.TESS_3D => Read_PRC_TYPE_TESS_3D(r, c),
                PrcType.TESS_3D_Wire => Read_PRC_TYPE_TESS_3D_Wire(r, c),
                PrcType.TESS_Markup => Read_PRC_TYPE_TESS_Markup(r, c),
                PrcType.TESS_3D_Compressed => Read_PRC_TYPE_TESS_3D_Compressed(r, c),
                _ => throw c.UnknownType("PRC_TYPE_TESS.id", t_id),
            });
            c.EvaluateSchema(r, (uint)PrcType.TESS);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_ProductOccurrenceReference(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("ProductOccurrenceReference");
            var f_unique_id = n.Set("unique_id", Read_UniqueId(r, c));
            var f_root_index = n.Set("root_index", r.ReadUnsignedInteger());
            var f_product_occurrence_is_active = n.Set("product_occurrence_is_active", r.ReadBoolean());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_AttributeData(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("AttributeData");
            var f_attribute_count = n.Set("attribute_count", r.ReadUnsignedInteger());
            var f_attributes = new PrcNode[c.Count(f_attribute_count, r)];
            for (var i = 0; i < f_attributes.Length; i++) f_attributes[i] = Read_PRC_TYPE_MISC_Attribute(r, c);
            n.Set("attributes", f_attributes);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_Name(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("Name");
            var f_same_name = n.Set("same_name", r.ReadBoolean());
            string f_name = default;
            if (!f_same_name)
            {
                f_name = n.Set("name", r.ReadString());
            }
            c.Leave();
            return n;
        }

        internal static PrcNode Read_MarkupSerializationHelper(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("MarkupSerializationHelper");
            var f_default_font_family_name = n.Set("default_font_family_name", r.ReadString());
            var f_font_keys_count = n.Set("font_keys_count", r.ReadUnsignedInteger());
            var f_font_keys_of_font = new PrcNode[c.Count(f_font_keys_count, r)];
            for (var i = 0; i < f_font_keys_of_font.Length; i++) f_font_keys_of_font[i] = Read_FontKeysSameFont(r, c);
            n.Set("font_keys_of_font", f_font_keys_of_font);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_RgbColor(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("RgbColor");
            var f_red = n.Set("red", r.ReadDouble());
            var f_green = n.Set("green", r.ReadDouble());
            var f_blue = n.Set("blue", r.ReadDouble());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_GRAPH_Picture(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_GRAPH_Picture");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.GRAPH_Picture, "PRC_TYPE_GRAPH_Picture.id");
            var f_base = n.Set("base", Read_ContentPRCBase(r, c));
            var f_format = n.Set("format", r.ReadUnsignedInteger());
            var f_biased_uncompressed_file_index = n.Set("biased_uncompressed_file_index", r.ReadUnsignedInteger());
            var f_pixel_width = n.Set("pixel_width", r.ReadUnsignedInteger());
            var f_pixel_height = n.Set("pixel_height", r.ReadUnsignedInteger());
            c.EvaluateSchema(r, (uint)PrcType.GRAPH_Picture);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_GRAPH_TextureDefinition(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_GRAPH_TextureDefinition");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.GRAPH_TextureDefinition, "PRC_TYPE_GRAPH_TextureDefinition.id");
            var f_base = n.Set("base", Read_ContentPRCRefBase(r, c));
            var f_biased_picture_index = n.Set("biased_picture_index", r.ReadUnsignedInteger());
            var f_texture_dimension = n.Set("texture_dimension", r.ReadCharacter());
            if (f_texture_dimension != 2) throw c.Invalid("texture_dimension of 2 expected");
            var f_texture_mapping_type = n.Set("texture_mapping_type", r.ReadInteger());
            int f_texture_mapping_operator = default;
            if (f_texture_mapping_type == 3)
            {
                f_texture_mapping_operator = n.Set("texture_mapping_operator", r.ReadInteger());
            }
            bool f_has_transformation = default;
            if (f_texture_mapping_type == 3)
            {
                f_has_transformation = n.Set("has_transformation", r.ReadBoolean());
            }
            PrcNode f_transformation = default;
            if (f_texture_mapping_type == 3 && f_has_transformation)
            {
                f_transformation = n.Set("transformation", Read_PRC_TYPE_MISC_CartesianTransformation(r, c));
            }
            var f_texture_mapping_attributes = n.Set("texture_mapping_attributes", r.ReadUnsignedInteger());
            var f_number_of_texture_mapping_attributes_intensities = n.Set("number_of_texture_mapping_attributes_intensities", r.ReadUnsignedInteger());
            if (f_number_of_texture_mapping_attributes_intensities > 1) throw c.Invalid("unexpected number_of_texture_mapping_attributes_intensities");
            double[] f_texture_mapping_attributes_intensities = default;
            if (f_number_of_texture_mapping_attributes_intensities != 0)
            {
                f_texture_mapping_attributes_intensities = new double[c.Count(f_number_of_texture_mapping_attributes_intensities, r)];
                for (var i = 0; i < f_texture_mapping_attributes_intensities.Length; i++) f_texture_mapping_attributes_intensities[i] = r.ReadDouble();
                n.Set("texture_mapping_attributes_intensities", f_texture_mapping_attributes_intensities);
            }
            var f_number_of_texture_mapping_attributes_components = n.Set("number_of_texture_mapping_attributes_components", r.ReadUnsignedInteger());
            if (f_number_of_texture_mapping_attributes_components > 1) throw c.Invalid("unexpected number_of_texture_mapping_attributes_components");
            sbyte[] f_texture_mapping_attributes_components = default;
            if (f_number_of_texture_mapping_attributes_components != 0)
            {
                f_texture_mapping_attributes_components = new sbyte[c.Count(f_number_of_texture_mapping_attributes_components, r)];
                for (var i = 0; i < f_texture_mapping_attributes_components.Length; i++) f_texture_mapping_attributes_components[i] = r.ReadCharacter();
                n.Set("texture_mapping_attributes_components", f_texture_mapping_attributes_components);
            }
            var f_texture_function = n.Set("texture_function", r.ReadInteger());
            double[] f_blend_src = default;
            if (f_texture_function == 3)
            {
                f_blend_src = new double[4];
                for (var i = 0; i < 4; i++) f_blend_src[i] = r.ReadDouble();
                n.Set("blend_src", f_blend_src);
                if (f_blend_src != null && f_blend_src.Any(v => !(v >= 0.0 && v <= 1.0))) throw c.Invalid("blend_src out of range");
            }
            var f_blend_src_rgb = n.Set("blend_src_rgb", r.ReadInteger());
            int f_blend_dst_rgb = default;
            if (f_blend_src_rgb != 0)
            {
                f_blend_dst_rgb = n.Set("blend_dst_rgb", r.ReadInteger());
            }
            var f_blend_src_alpha = n.Set("blend_src_alpha", r.ReadInteger());
            int f_blend_dst_alpha = default;
            if (f_blend_src_alpha != 0)
            {
                f_blend_dst_alpha = n.Set("blend_dst_alpha", r.ReadInteger());
            }
            var f_texture_application_mode = n.Set("texture_application_mode", r.ReadCharacter());
            int f_alpha_test = default;
            if ((f_texture_application_mode & 2) != 0)
            {
                f_alpha_test = n.Set("alpha_test", r.ReadInteger());
            }
            double f_alpha_test_reference = default;
            if ((f_texture_application_mode & 2) != 0)
            {
                f_alpha_test_reference = n.Set("alpha_test_reference", r.ReadDouble());
            }
            var f_texture_wrapping_mode_s = n.Set("texture_wrapping_mode_s", r.ReadInteger());
            int f_texture_wrapping_mode_t = default;
            if (f_texture_dimension > 1)
            {
                f_texture_wrapping_mode_t = n.Set("texture_wrapping_mode_t", r.ReadInteger());
            }
            int f_texture_wrapping_mode_r = default;
            if (f_texture_dimension > 2)
            {
                f_texture_wrapping_mode_r = n.Set("texture_wrapping_mode_r", r.ReadInteger());
            }
            var f_has_texture_transformation = n.Set("has_texture_transformation", r.ReadBoolean());
            PrcNode f_texture_transformation = default;
            if (f_has_texture_transformation)
            {
                f_texture_transformation = n.Set("texture_transformation", Read_PRC_TYPE_GRAPH_TextureTransformation(r, c));
            }
            c.EvaluateSchema(r, (uint)PrcType.GRAPH_TextureDefinition);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_Material(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("Material");
            PrcNode f_id;
            var t_id = r.PeekUnsignedInteger();
            f_id = n.Set("id", t_id switch
            {
                PrcType.GRAPH_Material => Read_PRC_TYPE_GRAPH_Material(r, c),
                PrcType.GRAPH_TextureApplication => Read_PRC_TYPE_GRAPH_TextureApplication(r, c),
                _ => throw c.UnknownType("Material.id", t_id),
            });
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_GRAPH_LinePattern(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_GRAPH_LinePattern");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.GRAPH_LinePattern, "PRC_TYPE_GRAPH_LinePattern.id");
            var f_base = n.Set("base", Read_ContentPRCRefBase(r, c));
            var f_number_of_elements = n.Set("number_of_elements", r.ReadUnsignedInteger());
            var f_length = new double[c.Count(f_number_of_elements, r)];
            for (var i = 0; i < f_length.Length; i++) f_length[i] = r.ReadDouble();
            n.Set("length", f_length);
            var f_start_offset = n.Set("start_offset", r.ReadDouble());
            var f_scale = n.Set("scale", r.ReadBoolean());
            c.EvaluateSchema(r, (uint)PrcType.GRAPH_LinePattern);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_GRAPH_Style(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_GRAPH_Style");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.GRAPH_Style, "PRC_TYPE_GRAPH_Style.id");
            var f_base = n.Set("base", Read_ContentPRCRefBase(r, c));
            var f_line_width = n.Set("line_width", r.ReadDouble());
            var f_is_vpicture = n.Set("is_vpicture", r.ReadBoolean());
            var f_biased_pattern_index = n.Set("biased_pattern_index", r.ReadUnsignedInteger());
            var f_is_material = n.Set("is_material", r.ReadBoolean());
            var f_biased_color_index = n.Set("biased_color_index", r.ReadUnsignedInteger());
            var f_is_transparency = n.Set("is_transparency", r.ReadBoolean());
            sbyte f_transparency = default;
            if (f_is_transparency)
            {
                f_transparency = n.Set("transparency", r.ReadCharacter());
            }
            var f_is_rendering_parameters = n.Set("is_rendering_parameters", r.ReadBoolean());
            sbyte f_rendering_parameters = default;
            if (f_is_rendering_parameters)
            {
                f_rendering_parameters = n.Set("rendering_parameters", r.ReadCharacter());
            }
            var f_is_rendering_parameters2 = n.Set("is_rendering_parameters2", r.ReadBoolean());
            sbyte f_rendering_parameters2 = default;
            if (f_is_rendering_parameters2)
            {
                f_rendering_parameters2 = n.Set("rendering_parameters2", r.ReadCharacter());
            }
            var f_is_rendering_parameters3 = n.Set("is_rendering_parameters3", r.ReadBoolean());
            sbyte f_rendering_parameters3 = default;
            if (f_is_rendering_parameters3)
            {
                f_rendering_parameters3 = n.Set("rendering_parameters3", r.ReadCharacter());
            }
            c.EvaluateSchema(r, (uint)PrcType.GRAPH_Style);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_GRAPH_FillPattern(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_GRAPH_FillPattern");
            PrcNode f_data;
            var t_data = r.PeekUnsignedInteger();
            f_data = n.Set("data", t_data switch
            {
                PrcType.GRAPH_DottingPattern => Read_PRC_TYPE_GRAPH_DottingPattern(r, c),
                PrcType.GRAPH_HatchingPattern => Read_PRC_TYPE_GRAPH_HatchingPattern(r, c),
                PrcType.GRAPH_SolidPattern => Read_PRC_TYPE_GRAPH_SolidPattern(r, c),
                PrcType.GRAPH_VPicturePattern => Read_PRC_TYPE_GRAPH_VPicturePattern(r, c),
                _ => throw c.UnknownType("PRC_TYPE_GRAPH_FillPattern.data", t_data),
            });
            c.EvaluateSchema(r, (uint)PrcType.GRAPH_FillPattern);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_RI_CoordinateSystem(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_RI_CoordinateSystem");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.RI_CoordinateSystem, "PRC_TYPE_RI_CoordinateSystem.id");
            var f_item_content = n.Set("item_content", Read_RepresentationItemContent(r, c));
            PrcNode f_transform;
            var t_transform = r.PeekUnsignedInteger();
            f_transform = n.Set("transform", t_transform switch
            {
                PrcType.MISC_GeneralTransformation => Read_PRC_TYPE_MISC_GeneralTransformation(r, c),
                PrcType.MISC_CartesianTransformation => Read_PRC_TYPE_MISC_CartesianTransformation(r, c),
                _ => throw c.UnknownType("PRC_TYPE_RI_CoordinateSystem.transform", t_transform),
            });
            c.EvaluateSchema(r, (uint)PrcType.RI_CoordinateSystem);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_ROOT_PRCBaseWithGraphics(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_ROOT_PRCBaseWithGraphics");
            var f_base = n.Set("base", Read_ContentPRCRefBase(r, c));
            var f_same_graphics = n.Set("same_graphics", r.ReadBoolean());
            PrcNode f_graphics_content = default;
            if (!f_same_graphics)
            {
                f_graphics_content = n.Set("graphics_content", Read_GraphicsContent(r, c));
            }
            c.EvaluateSchema(r, (uint)PrcType.ROOT_PRCBaseWithGraphics);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_BoundingBox(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("BoundingBox");
            var f_minimum_corner = n.Set("minimum_corner", Read_Vector3D(r, c));
            var f_maximum_corner = n.Set("maximum_corner", Read_Vector3D(r, c));
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_RI_RepresentationItem(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_RI_RepresentationItem");
            PrcNode f_data;
            var t_data = r.PeekUnsignedInteger();
            f_data = n.Set("data", t_data switch
            {
                PrcType.RI_BrepModel => Read_PRC_TYPE_RI_BrepModel(r, c),
                PrcType.RI_Curve => Read_PRC_TYPE_RI_Curve(r, c),
                PrcType.RI_Direction => Read_PRC_TYPE_RI_Direction(r, c),
                PrcType.RI_Plane => Read_PRC_TYPE_RI_Plane(r, c),
                PrcType.RI_PointSet => Read_PRC_TYPE_RI_PointSet(r, c),
                PrcType.RI_PolyBrepModel => Read_PRC_TYPE_RI_PolyBrepModel(r, c),
                PrcType.RI_PolyWire => Read_PRC_TYPE_RI_PolyWire(r, c),
                PrcType.RI_Set => Read_PRC_TYPE_RI_Set(r, c),
                PrcType.RI_CoordinateSystem => Read_PRC_TYPE_RI_CoordinateSystem(r, c),
                _ => throw c.UnknownType("PRC_TYPE_RI_RepresentationItem.data", t_data),
            });
            c.EvaluateSchema(r, (uint)PrcType.RI_RepresentationItem);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_MarkupData(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("MarkupData");
            var f_number_of_linked_items = n.Set("number_of_linked_items", r.ReadUnsignedInteger());
            var f_linked_items = new PrcNode[c.Count(f_number_of_linked_items, r)];
            for (var i = 0; i < f_linked_items.Length; i++) f_linked_items[i] = Read_PRC_TYPE_MISC_MarkupLinkedItem(r, c);
            n.Set("linked_items", f_linked_items);
            var f_number_of_leaders = n.Set("number_of_leaders", r.ReadUnsignedInteger());
            var f_leaders = new PrcNode[c.Count(f_number_of_leaders, r)];
            for (var i = 0; i < f_leaders.Length; i++) f_leaders[i] = Read_PRC_TYPE_MKP_Leader(r, c);
            n.Set("leaders", f_leaders);
            var f_number_of_markups = n.Set("number_of_markups", r.ReadUnsignedInteger());
            var f_markups = new PrcNode[c.Count(f_number_of_markups, r)];
            for (var i = 0; i < f_markups.Length; i++) f_markups[i] = Read_PRC_TYPE_MKP_Markup(r, c);
            n.Set("markups", f_markups);
            var f_number_of_annotation_entities = n.Set("number_of_annotation_entities", r.ReadUnsignedInteger());
            var f_annotation_entities = new PrcNode[c.Count(f_number_of_annotation_entities, r)];
            for (var i = 0; i < f_annotation_entities.Length; i++) f_annotation_entities[i] = Read_AnnotationEntity(r, c);
            n.Set("annotation_entities", f_annotation_entities);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_MKP_View(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_MKP_View");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.MKP_View, "PRC_TYPE_MKP_View.id");
            var f_base = n.Set("base", Read_PRC_TYPE_ROOT_PRCBaseWithGraphics(r, c));
            var f_number_of_annotations = n.Set("number_of_annotations", r.ReadUnsignedInteger());
            var f_annotations = new PrcNode[c.Count(f_number_of_annotations, r)];
            for (var i = 0; i < f_annotations.Length; i++) f_annotations[i] = Read_PRC_TYPE_MISC_ReferenceOnPRCBase(r, c);
            n.Set("annotations", f_annotations);
            var f_annotation_plane = n.Set("annotation_plane", Read_PRC_TYPE_SURF_Plane(r, c));
            var f_has_parameters = n.Set("has_parameters", r.ReadBoolean());
            PrcNode f_scene_display_parameters = default;
            if (f_has_parameters)
            {
                f_scene_display_parameters = n.Set("scene_display_parameters", Read_PRC_TYPE_GRAPH_SceneDisplayParameters(r, c));
            }
            bool f_is_annotation_view = default;
            if (c.AuthoringVersion >= 8137)
            {
                f_is_annotation_view = n.Set("is_annotation_view", r.ReadBoolean());
            }
            bool f_is_default_view = default;
            if (c.AuthoringVersion >= 8137)
            {
                f_is_default_view = n.Set("is_default_view", r.ReadBoolean());
            }
            bool f_is_direction = default;
            if (c.AuthoringVersion >= 8137)
            {
                f_is_direction = n.Set("is_direction", r.ReadBoolean());
            }
            uint f_number_of_linked_items = default;
            if (c.AuthoringVersion >= 8137)
            {
                f_number_of_linked_items = n.Set("number_of_linked_items", r.ReadUnsignedInteger());
            }
            PrcNode[] f_linked_items = default;
            if (c.AuthoringVersion >= 8137)
            {
                f_linked_items = new PrcNode[c.Count(f_number_of_linked_items, r)];
                for (var i = 0; i < f_linked_items.Length; i++) f_linked_items[i] = Read_PRC_TYPE_MISC_ReferenceOnPRCBase(r, c);
                n.Set("linked_items", f_linked_items);
            }
            uint f_number_of_filters = default;
            if (c.AuthoringVersion >= 8137)
            {
                f_number_of_filters = n.Set("number_of_filters", r.ReadUnsignedInteger());
            }
            PrcNode[] f_filters = default;
            if (c.AuthoringVersion >= 8137)
            {
                f_filters = new PrcNode[c.Count(f_number_of_filters, r)];
                for (var i = 0; i < f_filters.Length; i++) f_filters[i] = Read_PRC_TYPE_ASM_Filter(r, c);
                n.Set("filters", f_filters);
            }
            c.EvaluateSchema(r, (uint)PrcType.MKP_View);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_ReferencesOfProductOccurrence(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("ReferencesOfProductOccurrence");
            var f_biased_index_part = n.Set("biased_index_part", r.ReadUnsignedInteger());
            var f_biased_index_prototype = n.Set("biased_index_prototype", r.ReadUnsignedInteger());
            PrcNode f_prototype_in_same_file_structure = default;
            if (f_biased_index_prototype != 0)
            {
                f_prototype_in_same_file_structure = n.Set("prototype_in_same_file_structure", Read_FileIdentifier(r, c));
            }
            var f_biased_index_external_data = n.Set("biased_index_external_data", r.ReadUnsignedInteger());
            PrcNode f_external_data_in_same_file_structure = default;
            if (f_biased_index_external_data != 0)
            {
                f_external_data_in_same_file_structure = n.Set("external_data_in_same_file_structure", Read_FileIdentifier(r, c));
            }
            var f_number_of_child_product_occurrences = n.Set("number_of_child_product_occurrences", r.ReadUnsignedInteger());
            var f_index_child_occurrence = new uint[c.Count(f_number_of_child_product_occurrences, r)];
            for (var i = 0; i < f_index_child_occurrence.Length; i++) f_index_child_occurrence[i] = r.ReadUnsignedInteger();
            n.Set("index_child_occurrence", f_index_child_occurrence);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_ProductInformation(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("ProductInformation");
            var f_unit_from_cad_file = n.Set("unit_from_cad_file", r.ReadBoolean());
            var f_unit = n.Set("unit", r.ReadDouble());
            var f_product_information_flags = n.Set("product_information_flags", r.ReadCharacter());
            var f_product_load_status = n.Set("product_load_status", r.ReadInteger());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_MISC_CartesianTransformation(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_MISC_CartesianTransformation");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.MISC_CartesianTransformation, "PRC_TYPE_MISC_CartesianTransformation.id");
            var f_transform = n.Set("transform", Read_Transformation3D(r, c));
            c.EvaluateSchema(r, (uint)PrcType.MISC_CartesianTransformation);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_MISC_GeneralTransformation(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_MISC_GeneralTransformation");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.MISC_GeneralTransformation, "PRC_TYPE_MISC_GeneralTransformation.id");
            var f_general_transform = new double[16];
            for (var i = 0; i < 16; i++) f_general_transform[i] = r.ReadDouble();
            n.Set("general_transform", f_general_transform);
            c.EvaluateSchema(r, (uint)PrcType.MISC_GeneralTransformation);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_MISC_EntityReference(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_MISC_EntityReference");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.MISC_EntityReference, "PRC_TYPE_MISC_EntityReference.id");
            var f_content_entity_reference = n.Set("content_entity_reference", Read_ContentEntityReference(r, c));
            c.EvaluateSchema(r, (uint)PrcType.MISC_EntityReference);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_ASM_Filter(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_ASM_Filter");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.ASM_Filter, "PRC_TYPE_ASM_Filter.id");
            var f_base = n.Set("base", Read_ContentPRCRefBase(r, c));
            var f_is_active = n.Set("is_active", r.ReadBoolean());
            var f_layer_filter = n.Set("layer_filter", Read_ContentLayerFilterItems(r, c));
            var f_entity_filter = n.Set("entity_filter", Read_ContentEntityFilterItems(r, c));
            c.EvaluateSchema(r, (uint)PrcType.ASM_Filter);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_GRAPH_SceneDisplayParameters(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_GRAPH_SceneDisplayParameters");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.GRAPH_SceneDisplayParameters, "PRC_TYPE_GRAPH_SceneDisplayParameters.id");
            var f_base = n.Set("base", Read_ContentPRCRefBase(r, c));
            var f_is_active = n.Set("is_active", r.ReadBoolean());
            var f_number_of_lights = n.Set("number_of_lights", r.ReadUnsignedInteger());
            var f_lights = new PrcNode[c.Count(f_number_of_lights, r)];
            for (var i = 0; i < f_lights.Length; i++) f_lights[i] = Read_Light(r, c);
            n.Set("lights", f_lights);
            var f_camera_defined = n.Set("camera_defined", r.ReadBoolean());
            PrcNode f_camera = default;
            if (f_camera_defined)
            {
                f_camera = n.Set("camera", Read_PRC_TYPE_GRAPH_Camera(r, c));
            }
            var f_rotation_center_defined = n.Set("rotation_center_defined", r.ReadBoolean());
            PrcNode f_rotation_center = default;
            if (f_rotation_center_defined)
            {
                f_rotation_center = n.Set("rotation_center", Read_Vector3D(r, c));
            }
            var f_number_of_clipping_planes = n.Set("number_of_clipping_planes", r.ReadUnsignedInteger());
            var f_clipping_planes = new PrcNode[c.Count(f_number_of_clipping_planes, r)];
            for (var i = 0; i < f_clipping_planes.Length; i++) f_clipping_planes[i] = Read_PRC_TYPE_SURF_Plane(r, c);
            n.Set("clipping_planes", f_clipping_planes);
            var f_biased_index_of_line_style_background = n.Set("biased_index_of_line_style_background", r.ReadUnsignedInteger());
            var f_biased_index_of_line_style_default = n.Set("biased_index_of_line_style_default", r.ReadUnsignedInteger());
            var f_number_default_styles = n.Set("number_default_styles", r.ReadUnsignedInteger());
            var f_styles = new uint[c.Count(2*f_number_default_styles, r)];
            for (var i = 0; i < f_styles.Length; i++) f_styles[i] = r.ReadUnsignedInteger();
            n.Set("styles", f_styles);
            bool f_is_absolute = default;
            if (c.AuthoringVersion >= 8137)
            {
                f_is_absolute = n.Set("is_absolute", r.ReadBoolean());
            }
            c.EvaluateSchema(r, (uint)PrcType.GRAPH_SceneDisplayParameters);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_TESS_3D(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_TESS_3D");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.TESS_3D, "PRC_TYPE_TESS_3D.id");
            var f_tessellation_coordinates = n.Set("tessellation_coordinates", Read_ContentBaseTessData(r, c));
            var f_has_faces = n.Set("has_faces", r.ReadBoolean());
            bool f_has_loops = default;
            if (c.AuthoringVersion >= 7039)
            {
                f_has_loops = n.Set("has_loops", r.ReadBoolean());
            }
            bool f_must_calculate_normals = default;
            if (c.AuthoringVersion >= 7047)
            {
                f_must_calculate_normals = n.Set("must_calculate_normals", r.ReadBoolean());
            }
            sbyte f_normal_recalculation_flags = default;
            if (f_must_calculate_normals && c.AuthoringVersion >= 7047)
            {
                f_normal_recalculation_flags = n.Set("normal_recalculation_flags", r.ReadCharacter());
            }
            double f_crease_angle = default;
            if (f_must_calculate_normals && c.AuthoringVersion >= 7047)
            {
                f_crease_angle = n.Set("crease_angle", r.ReadDouble());
            }
            var f_number_of_normal_coordinates = n.Set("number_of_normal_coordinates", r.ReadUnsignedInteger());
            var f_normal_coordinates = new double[c.Count(f_number_of_normal_coordinates, r)];
            for (var i = 0; i < f_normal_coordinates.Length; i++) f_normal_coordinates[i] = r.ReadDouble();
            n.Set("normal_coordinates", f_normal_coordinates);
            var f_number_of_wire_indices = n.Set("number_of_wire_indices", r.ReadUnsignedInteger());
            var f_wire_indices = new uint[c.Count(f_number_of_wire_indices, r)];
            for (var i = 0; i < f_wire_indices.Length; i++) f_wire_indices[i] = r.ReadUnsignedInteger();
            n.Set("wire_indices", f_wire_indices);
            var f_number_of_triangulated_indicies = n.Set("number_of_triangulated_indicies", r.ReadUnsignedInteger());
            var f_triangulated_index_array = new uint[c.Count(f_number_of_triangulated_indicies, r)];
            for (var i = 0; i < f_triangulated_index_array.Length; i++) f_triangulated_index_array[i] = r.ReadUnsignedInteger();
            n.Set("triangulated_index_array", f_triangulated_index_array);
            var f_number_of_face_tessellation = n.Set("number_of_face_tessellation", r.ReadUnsignedInteger());
            var f_face_tessellation_data = new PrcNode[c.Count(f_number_of_face_tessellation, r)];
            for (var i = 0; i < f_face_tessellation_data.Length; i++) f_face_tessellation_data[i] = Read_PRC_TYPE_TESS_Face(r, c);
            n.Set("face_tessellation_data", f_face_tessellation_data);
            var f_number_of_texture_coordinates = n.Set("number_of_texture_coordinates", r.ReadUnsignedInteger());
            var f_texture_coordinates = new double[c.Count(f_number_of_texture_coordinates, r)];
            for (var i = 0; i < f_texture_coordinates.Length; i++) f_texture_coordinates[i] = r.ReadDouble();
            n.Set("texture_coordinates", f_texture_coordinates);
            c.EvaluateSchema(r, (uint)PrcType.TESS_3D);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_TESS_3D_Wire(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_TESS_3D_Wire");
            c.EnterWire();
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.TESS_3D_Wire, "PRC_TYPE_TESS_3D_Wire.id");
            var f_tessellation_coordinates = n.Set("tessellation_coordinates", Read_ContentBaseTessData(r, c));
            var f_number_of_wire_indexes = n.Set("number_of_wire_indexes", r.ReadUnsignedInteger());
            var f_wire_indexes = new uint[c.Count(f_number_of_wire_indexes, r)];
            for (var i = 0; i < f_wire_indexes.Length; i++) f_wire_indexes[i] = r.ReadUnsignedInteger();
            n.Set("wire_indexes", f_wire_indexes);
            c.WireIndexes = f_wire_indexes;
            var f_has_vertex_colors = n.Set("has_vertex_colors", r.ReadBoolean());
            PrcNode f_vertex_color_data = default;
            if (f_has_vertex_colors)
            {
                f_vertex_color_data = n.Set("vertex_color_data", Read_VertexColors(r, c));
            }
            c.EvaluateSchema(r, (uint)PrcType.TESS_3D_Wire);
            c.LeaveWire();
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_TESS_Markup(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_TESS_Markup");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.TESS_Markup, "PRC_TYPE_TESS_Markup.id");
            var f_tessellation_coordinates = n.Set("tessellation_coordinates", Read_ContentBaseTessData(r, c));
            var f_number_of_codes = n.Set("number_of_codes", r.ReadUnsignedInteger());
            var f_code_numbers = new uint[c.Count(f_number_of_codes, r)];
            for (var i = 0; i < f_code_numbers.Length; i++) f_code_numbers[i] = r.ReadUnsignedInteger();
            n.Set("code_numbers", f_code_numbers);
            var f_number_of_text_strings = n.Set("number_of_text_strings", r.ReadUnsignedInteger());
            var f_text_strings = new string[c.Count(f_number_of_text_strings, r)];
            for (var i = 0; i < f_text_strings.Length; i++) f_text_strings[i] = r.ReadString();
            n.Set("text_strings", f_text_strings);
            var f_tessellation_label = n.Set("tessellation_label", r.ReadString());
            var f_behavior = n.Set("behavior", r.ReadCharacter());
            c.EvaluateSchema(r, (uint)PrcType.TESS_Markup);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_TESS_3D_Compressed(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_TESS_3D_Compressed");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.TESS_3D_Compressed, "PRC_TYPE_TESS_3D_Compressed.id");
            var f_is_calculated = n.Set("is_calculated", r.ReadBoolean());
            var f_has_faces = n.Set("has_faces", r.ReadBoolean());
            var f_tolerance = n.Set("tolerance", r.ReadDouble());
            float[] f_origin_array = default;
            if (c.AuthoringVersion >= 7031)
            {
                f_origin_array = new float[3];
                for (var i = 0; i < 3; i++) f_origin_array[i] = r.ReadFloatAsBytes();
                n.Set("origin_array", f_origin_array);
            }
            var f_point_array = n.Set("point_array", r.ReadCompressedIntegerArray());
            var f_edge_status_array = n.Set("edge_status_array", r.ReadCharacterArray(2));
            if (f_edge_status_array.Any(v => v < 0)) throw c.Invalid("negative edge status");
            var f_triangle_face_array = n.Set("triangle_face_array", r.ReadCompressedIndiceArray());
            if (f_edge_status_array.Length != f_triangle_face_array.Length && f_edge_status_array.Length != 3 * f_triangle_face_array.Length) throw c.Invalid("edge_status_array is neither one nor three entries per triangle");
            var f_reference_array_size = n.Set("reference_array_size", r.ReadUnsignedInteger());
            var f_point_is_reference_array = n.Set("point_is_reference_array", r.ReadUncompressedBoolArray(f_reference_array_size));
            var f_point_reference_array = n.Set("point_reference_array", r.ReadCompressedIndiceArray(false, PrcContext.CountTrue(f_point_is_reference_array) > 3));
            if (f_point_reference_array.Length != PrcContext.CountTrue(f_point_is_reference_array)) throw c.Invalid("point_reference_array does not match point_is_reference_array");
            var f_must_recalculate_normals = n.Set("must_recalculate_normals", r.ReadBoolean());
            if (f_point_array.Length % 3 != 0) throw c.Invalid("point_array is not a list of triples");
            bool[] f_normal_is_reversed = default;
            if (f_must_recalculate_normals)
            {
                f_normal_is_reversed = n.Set("normal_is_reversed", r.ReadUncompressedBoolArray(f_triangle_face_array.Length));
            }
            double f_crease_angle = default;
            if (f_must_recalculate_normals)
            {
                f_crease_angle = n.Set("crease_angle", r.ReadDouble());
            }
            sbyte f_normal_recalculation_flags = default;
            if (f_must_recalculate_normals)
            {
                f_normal_recalculation_flags = n.Set("normal_recalculation_flags", r.ReadCharacter());
            }
            sbyte f_normal_angle_number_of_bits = default;
            if (!f_must_recalculate_normals)
            {
                f_normal_angle_number_of_bits = n.Set("normal_angle_number_of_bits", r.ReadCharacter());
            }
            uint f_normal_binary_data_size = default;
            if (!f_must_recalculate_normals)
            {
                f_normal_binary_data_size = n.Set("normal_binary_data_size", r.ReadUnsignedInteger());
            }
            bool[] f_normal_binary_data = default;
            if (!f_must_recalculate_normals)
            {
                f_normal_binary_data = n.Set("normal_binary_data", r.ReadUncompressedBoolArray(f_normal_binary_data_size));
            }
            short[] f_normal_angle_array = default;
            if (!f_must_recalculate_normals)
            {
                f_normal_angle_array = n.Set("normal_angle_array", r.ReadShortArray(f_normal_angle_number_of_bits));
            }
            bool[] f_is_face_planar = default;
            if (!f_must_recalculate_normals)
            {
                f_is_face_planar = n.Set("is_face_planar", r.ReadUncompressedBoolArray(PrcContext.FaceCount(f_triangle_face_array)));
            }
            var f_is_point_color = n.Set("is_point_color", r.ReadBoolean());
            bool[] f_is_point_color_on_face = default;
            if (f_is_point_color)
            {
                f_is_point_color_on_face = n.Set("is_point_color_on_face", r.ReadUncompressedBoolArray((!f_has_faces ? 1 : PrcContext.FaceCount(f_triangle_face_array))));
            }
            sbyte[] f_point_color_array = default;
            if (f_is_point_color)
            {
                f_point_color_array = n.Set("point_color_array", r.ReadCharacterArray(8));
            }
            var f_is_multiple_line_attribute = n.Set("is_multiple_line_attribute", r.ReadBoolean());
            bool[] f_is_multiple_line_attribute_on_face = default;
            if (f_is_multiple_line_attribute)
            {
                f_is_multiple_line_attribute_on_face = n.Set("is_multiple_line_attribute_on_face", r.ReadUncompressedBoolArray((!f_has_faces ? 1 : PrcContext.FaceCount(f_triangle_face_array))));
            }
            var f_line_attribute_array = n.Set("line_attribute_array", r.ReadShortArray(16));
            var f_no_texture = n.Set("no_texture", r.ReadBoolean());
            PrcNode f_texture_data = default;
            if (!f_no_texture)
            {
                f_texture_data = n.Set("texture_data", Read_CompressedTextureParameter(r, c));
            }
            bool f_all_faces_have_texture = default;
            if (!f_no_texture)
            {
                f_all_faces_have_texture = n.Set("all_faces_have_texture", r.ReadBoolean());
            }
            bool[] f_face_has_texture = default;
            if (!f_no_texture && !f_all_faces_have_texture)
            {
                f_face_has_texture = n.Set("face_has_texture", r.ReadUncompressedBoolArray((!f_has_faces ? 1 : PrcContext.FaceCount(f_triangle_face_array))));
            }
            var f_has_behaviors = n.Set("has_behaviors", r.ReadBoolean());
            sbyte[] f_behaviors_array = default;
            if (f_has_behaviors)
            {
                f_behaviors_array = n.Set("behaviors_array", r.ReadCharacterArray(8));
            }
            c.EvaluateSchema(r, (uint)PrcType.TESS_3D_Compressed);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_MISC_Attribute(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_MISC_Attribute");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.MISC_Attribute, "PRC_TYPE_MISC_Attribute.id");
            var f_attribute_title = n.Set("attribute_title", Read_AttributeEntry(r, c));
            var f_number_of_attributes = n.Set("number_of_attributes", r.ReadUnsignedInteger());
            var f_attributes = new PrcNode[c.Count(f_number_of_attributes, r)];
            for (var i = 0; i < f_attributes.Length; i++) f_attributes[i] = Read_AttributeKeyValue(r, c);
            n.Set("attributes", f_attributes);
            c.EvaluateSchema(r, (uint)PrcType.MISC_Attribute);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_FontKeysSameFont(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("FontKeysSameFont");
            var f_font_name = n.Set("font_name", r.ReadString());
            var f_character_set = n.Set("character_set", r.ReadUnsignedInteger());
            var f_key_count = n.Set("key_count", r.ReadUnsignedInteger());
            var f_font_key_list = new PrcNode[c.Count(f_key_count, r)];
            for (var i = 0; i < f_font_key_list.Length; i++) f_font_key_list[i] = Read_FontKey(r, c);
            n.Set("font_key_list", f_font_key_list);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_ContentPRCRefBase(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("ContentPRCRefBase");
            var f_base = n.Set("base", Read_ContentPRCBase(r, c));
            var f_nonpersistent_id = n.Set("nonpersistent_id", r.ReadUnsignedInteger());
            var f_unique_id_cad = n.Set("unique_id_cad", r.ReadUnsignedInteger());
            var f_unique_id = n.Set("unique_id", r.ReadUnsignedInteger());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_GRAPH_TextureTransformation(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_GRAPH_TextureTransformation");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.GRAPH_TextureTransformation, "PRC_TYPE_GRAPH_TextureTransformation.id");
            var f_invert_s = n.Set("invert_s", r.ReadBoolean());
            var f_invert_t = n.Set("invert_t", r.ReadBoolean());
            var f_transform_2d = n.Set("transform_2d", r.ReadBoolean());
            if (!f_transform_2d) throw c.Invalid("2d texture transform expected");
            PrcNode f_transform = default;
            if (f_transform_2d)
            {
                f_transform = n.Set("transform", Read_Transformation2D(r, c));
            }
            c.EvaluateSchema(r, (uint)PrcType.GRAPH_TextureTransformation);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_GRAPH_Material(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_GRAPH_Material");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.GRAPH_Material, "PRC_TYPE_GRAPH_Material.id");
            var f_base = n.Set("base", Read_ContentPRCRefBase(r, c));
            var f_biased_ambient_index = n.Set("biased_ambient_index", r.ReadUnsignedInteger());
            var f_biased_diffuse_index = n.Set("biased_diffuse_index", r.ReadUnsignedInteger());
            var f_biased_emissive_index = n.Set("biased_emissive_index", r.ReadUnsignedInteger());
            var f_biased_specular_index = n.Set("biased_specular_index", r.ReadUnsignedInteger());
            var f_shininess = n.Set("shininess", r.ReadDouble());
            var f_ambient_alpha = n.Set("ambient_alpha", r.ReadDouble());
            var f_diffuse_alpha = n.Set("diffuse_alpha", r.ReadDouble());
            var f_emissive_alpha = n.Set("emissive_alpha", r.ReadDouble());
            var f_specular_alpha = n.Set("specular_alpha", r.ReadDouble());
            c.EvaluateSchema(r, (uint)PrcType.GRAPH_Material);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_GRAPH_TextureApplication(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_GRAPH_TextureApplication");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.GRAPH_TextureApplication, "PRC_TYPE_GRAPH_TextureApplication.id");
            var f_base = n.Set("base", Read_ContentPRCRefBase(r, c));
            var f_biased_material_generic_index = n.Set("biased_material_generic_index", r.ReadUnsignedInteger());
            var f_biased_texture_definition_index = n.Set("biased_texture_definition_index", r.ReadUnsignedInteger());
            var f_biased_next_texture_index = n.Set("biased_next_texture_index", r.ReadUnsignedInteger());
            var f_biased_uv_coordinates_index = n.Set("biased_uv_coordinates_index", r.ReadUnsignedInteger());
            c.EvaluateSchema(r, (uint)PrcType.GRAPH_TextureApplication);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_GRAPH_DottingPattern(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_GRAPH_DottingPattern");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.GRAPH_DottingPattern, "PRC_TYPE_GRAPH_DottingPattern.id");
            var f_base = n.Set("base", Read_ContentPRCRefBase(r, c));
            var f_biased_next_pattern_index = n.Set("biased_next_pattern_index", r.ReadUnsignedInteger());
            var f_pitch = n.Set("pitch", r.ReadDouble());
            var f_is_offset = n.Set("is_offset", r.ReadBoolean());
            var f_biased_color_index = n.Set("biased_color_index", r.ReadInteger());
            c.EvaluateSchema(r, (uint)PrcType.GRAPH_DottingPattern);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_GRAPH_HatchingPattern(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_GRAPH_HatchingPattern");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.GRAPH_HatchingPattern, "PRC_TYPE_GRAPH_HatchingPattern.id");
            var f_base = n.Set("base", Read_ContentPRCRefBase(r, c));
            var f_biased_next_pattern_index = n.Set("biased_next_pattern_index", r.ReadUnsignedInteger());
            var f_number_of_hatching_lines = n.Set("number_of_hatching_lines", r.ReadUnsignedInteger());
            var f_hatch = new PrcNode[c.Count(f_number_of_hatching_lines, r)];
            for (var i = 0; i < f_hatch.Length; i++) f_hatch[i] = Read_HatchingLine(r, c);
            n.Set("hatch", f_hatch);
            c.EvaluateSchema(r, (uint)PrcType.GRAPH_HatchingPattern);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_GRAPH_SolidPattern(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_GRAPH_SolidPattern");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.GRAPH_SolidPattern, "PRC_TYPE_GRAPH_SolidPattern.id");
            var f_base = n.Set("base", Read_ContentPRCRefBase(r, c));
            var f_biased_next_pattern_index = n.Set("biased_next_pattern_index", r.ReadUnsignedInteger());
            var f_is_material = n.Set("is_material", r.ReadBoolean());
            uint f_biased_material_index = default;
            if (f_is_material)
            {
                f_biased_material_index = n.Set("biased_material_index", r.ReadUnsignedInteger());
            }
            uint f_biased_color_index = default;
            if (!f_is_material)
            {
                f_biased_color_index = n.Set("biased_color_index", r.ReadUnsignedInteger());
            }
            c.EvaluateSchema(r, (uint)PrcType.GRAPH_SolidPattern);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_GRAPH_VPicturePattern(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_GRAPH_VPicturePattern");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.GRAPH_VPicturePattern, "PRC_TYPE_GRAPH_VPicturePattern.id");
            var f_base = n.Set("base", Read_ContentPRCRefBase(r, c));
            var f_biased_next_pattern_index = n.Set("biased_next_pattern_index", r.ReadUnsignedInteger());
            var f_pattern_dimensions = new double[2];
            for (var i = 0; i < 2; i++) f_pattern_dimensions[i] = r.ReadDouble();
            n.Set("pattern_dimensions", f_pattern_dimensions);
            var f_markup = n.Set("markup", Read_PRC_TYPE_TESS_Markup(r, c));
            c.EvaluateSchema(r, (uint)PrcType.GRAPH_VPicturePattern);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_RepresentationItemContent(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("RepresentationItemContent");
            var f_base = n.Set("base", Read_PRC_TYPE_ROOT_PRCBaseWithGraphics(r, c));
            var f_biased_index_local_coordinate_system = n.Set("biased_index_local_coordinate_system", r.ReadUnsignedInteger());
            var f_biased_index_tessellation = n.Set("biased_index_tessellation", r.ReadUnsignedInteger());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_GraphicsContent(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("GraphicsContent");
            var f_biased_layer_index = n.Set("biased_layer_index", r.ReadUnsignedInteger());
            var f_biased_index_of_line_style = n.Set("biased_index_of_line_style", r.ReadUnsignedInteger());
            var f_behavior_bit_field = n.Set("behavior_bit_field", r.ReadUnsignedShort());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_Vector3D(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("Vector3D");
            var f_x = n.Set("x", r.ReadDouble());
            var f_y = n.Set("y", r.ReadDouble());
            var f_z = n.Set("z", r.ReadDouble());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_RI_BrepModel(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_RI_BrepModel");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.RI_BrepModel, "PRC_TYPE_RI_BrepModel.id");
            var f_item_content = n.Set("item_content", Read_RepresentationItemContent(r, c));
            var f_exact_geometry = n.Set("exact_geometry", r.ReadBoolean());
            uint f_index_topological_context = default;
            if (f_exact_geometry)
            {
                f_index_topological_context = n.Set("index_topological_context", r.ReadUnsignedInteger());
            }
            uint f_index_body = default;
            if (f_exact_geometry)
            {
                f_index_body = n.Set("index_body", r.ReadUnsignedInteger());
            }
            var f_is_closed = n.Set("is_closed", r.ReadBoolean());
            c.EvaluateSchema(r, (uint)PrcType.RI_BrepModel);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_RI_Curve(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_RI_Curve");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.RI_Curve, "PRC_TYPE_RI_Curve.id");
            var f_item_content = n.Set("item_content", Read_RepresentationItemContent(r, c));
            var f_exact_geometry = n.Set("exact_geometry", r.ReadBoolean());
            uint f_index_topological_context = default;
            if (f_exact_geometry)
            {
                f_index_topological_context = n.Set("index_topological_context", r.ReadUnsignedInteger());
            }
            uint f_index_body = default;
            if (f_exact_geometry)
            {
                f_index_body = n.Set("index_body", r.ReadUnsignedInteger());
            }
            c.EvaluateSchema(r, (uint)PrcType.RI_Curve);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_RI_Direction(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_RI_Direction");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.RI_Direction, "PRC_TYPE_RI_Direction.id");
            var f_item_content = n.Set("item_content", Read_RepresentationItemContent(r, c));
            var f_has_orgin = n.Set("has_orgin", r.ReadBoolean());
            PrcNode f_origin = default;
            if (f_has_orgin)
            {
                f_origin = n.Set("origin", Read_Vector3D(r, c));
            }
            var f_direction = n.Set("direction", Read_Vector3D(r, c));
            c.EvaluateSchema(r, (uint)PrcType.RI_Direction);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_RI_Plane(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_RI_Plane");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.RI_Plane, "PRC_TYPE_RI_Plane.id");
            var f_item_content = n.Set("item_content", Read_RepresentationItemContent(r, c));
            var f_exact_geometry = n.Set("exact_geometry", r.ReadBoolean());
            uint f_index_topological_context = default;
            if (f_exact_geometry)
            {
                f_index_topological_context = n.Set("index_topological_context", r.ReadUnsignedInteger());
            }
            uint f_index_body = default;
            if (f_exact_geometry)
            {
                f_index_body = n.Set("index_body", r.ReadUnsignedInteger());
            }
            c.EvaluateSchema(r, (uint)PrcType.RI_Plane);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_RI_PointSet(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_RI_PointSet");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.RI_PointSet, "PRC_TYPE_RI_PointSet.id");
            var f_item_content = n.Set("item_content", Read_RepresentationItemContent(r, c));
            var f_number_of_points = n.Set("number_of_points", r.ReadUnsignedInteger());
            var f_points = new PrcNode[c.Count(f_number_of_points, r)];
            for (var i = 0; i < f_points.Length; i++) f_points[i] = Read_Vector3D(r, c);
            n.Set("points", f_points);
            c.EvaluateSchema(r, (uint)PrcType.RI_PointSet);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_RI_PolyBrepModel(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_RI_PolyBrepModel");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.RI_PolyBrepModel, "PRC_TYPE_RI_PolyBrepModel.id");
            var f_item_content = n.Set("item_content", Read_RepresentationItemContent(r, c));
            var f_is_closed = n.Set("is_closed", r.ReadBoolean());
            c.EvaluateSchema(r, (uint)PrcType.RI_PolyBrepModel);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_RI_PolyWire(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_RI_PolyWire");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.RI_PolyWire, "PRC_TYPE_RI_PolyWire.id");
            var f_item_content = n.Set("item_content", Read_RepresentationItemContent(r, c));
            c.EvaluateSchema(r, (uint)PrcType.RI_PolyWire);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_RI_Set(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_RI_Set");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.RI_Set, "PRC_TYPE_RI_Set.id");
            var f_item_content = n.Set("item_content", Read_RepresentationItemContent(r, c));
            var f_number_of_items = n.Set("number_of_items", r.ReadUnsignedInteger());
            var f_representation_items = new PrcNode[c.Count(f_number_of_items, r)];
            for (var i = 0; i < f_representation_items.Length; i++) f_representation_items[i] = Read_PRC_TYPE_RI_RepresentationItem(r, c);
            n.Set("representation_items", f_representation_items);
            c.EvaluateSchema(r, (uint)PrcType.RI_Set);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_MISC_MarkupLinkedItem(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_MISC_MarkupLinkedItem");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.MISC_MarkupLinkedItem, "PRC_TYPE_MISC_MarkupLinkedItem.id");
            var f_content_extended_entity_reference = n.Set("content_extended_entity_reference", Read_ContentExtendedEntityReference(r, c));
            var f_show_markup = n.Set("show_markup", r.ReadBoolean());
            var f_delete_markup = n.Set("delete_markup", r.ReadBoolean());
            var f_show_leader = n.Set("show_leader", r.ReadBoolean());
            var f_delete_leader = n.Set("delete_leader", r.ReadBoolean());
            c.EvaluateSchema(r, (uint)PrcType.MISC_MarkupLinkedItem);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_MKP_Leader(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_MKP_Leader");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.MKP_Leader, "PRC_TYPE_MKP_Leader.id");
            var f_base = n.Set("base", Read_PRC_TYPE_ROOT_PRCBaseWithGraphics(r, c));
            var f_is_first_linked_item = n.Set("is_first_linked_item", r.ReadBoolean());
            PrcNode f_first_linked_item = default;
            if (f_is_first_linked_item)
            {
                f_first_linked_item = n.Set("first_linked_item", Read_PRC_TYPE_MISC_ReferenceOnPRCBase(r, c));
            }
            var f_is_second_linked_item = n.Set("is_second_linked_item", r.ReadBoolean());
            PrcNode f_second_linked_item = default;
            if (f_is_second_linked_item)
            {
                f_second_linked_item = n.Set("second_linked_item", Read_PRC_TYPE_MISC_ReferenceOnPRCBase(r, c));
            }
            var f_biased_index_tessellation = n.Set("biased_index_tessellation", r.ReadUnsignedInteger());
            c.EvaluateSchema(r, (uint)PrcType.MKP_Leader);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_MKP_Markup(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_MKP_Markup");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.MKP_Markup, "PRC_TYPE_MKP_Markup.id");
            var f_base = n.Set("base", Read_PRC_TYPE_ROOT_PRCBaseWithGraphics(r, c));
            var f_markup_type = n.Set("markup_type", r.ReadUnsignedInteger());
            var f_markup_subtype = n.Set("markup_subtype", r.ReadUnsignedInteger());
            var f_number_of_linked_items = n.Set("number_of_linked_items", r.ReadUnsignedInteger());
            var f_linked_items = new PrcNode[c.Count(f_number_of_linked_items, r)];
            for (var i = 0; i < f_linked_items.Length; i++) f_linked_items[i] = Read_PRC_TYPE_MISC_ReferenceOnPRCBase(r, c);
            n.Set("linked_items", f_linked_items);
            var f_number_of_leaders = n.Set("number_of_leaders", r.ReadUnsignedInteger());
            var f_leaders = new PrcNode[c.Count(f_number_of_leaders, r)];
            for (var i = 0; i < f_leaders.Length; i++) f_leaders[i] = Read_PRC_TYPE_MISC_ReferenceOnPRCBase(r, c);
            n.Set("leaders", f_leaders);
            var f_biased_index_tessellation = n.Set("biased_index_tessellation", r.ReadUnsignedInteger());
            c.EvaluateSchema(r, (uint)PrcType.MKP_Markup);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_AnnotationEntity(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("AnnotationEntity");
            PrcNode f_data;
            var t_data = r.PeekUnsignedInteger();
            f_data = n.Set("data", t_data switch
            {
                PrcType.MKP_AnnotationItem => Read_PRC_TYPE_MKP_AnnotationItem(r, c),
                PrcType.MKP_AnnotationSet => Read_PRC_TYPE_MKP_AnnotationSet(r, c),
                PrcType.MKP_AnnotationReference => Read_PRC_TYPE_MKP_AnnotationReference(r, c),
                _ => throw c.UnknownType("AnnotationEntity.data", t_data),
            });
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_MISC_ReferenceOnPRCBase(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_MISC_ReferenceOnPRCBase");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.MISC_ReferenceOnPRCBase, "PRC_TYPE_MISC_ReferenceOnPRCBase.id");
            var f_type_of_entity = n.Set("type_of_entity", r.ReadUnsignedInteger());
            var f_flag = n.Set("flag", r.ReadBoolean());
            PrcNode f_different_unique_id = default;
            if (!f_flag)
            {
                f_different_unique_id = n.Set("different_unique_id", Read_UniqueId(r, c));
            }
            var f_unique_id = n.Set("unique_id", r.ReadUnsignedInteger());
            c.EvaluateSchema(r, (uint)PrcType.MISC_ReferenceOnPRCBase);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_SURF_Plane(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_SURF_Plane");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.SURF_Plane, "PRC_TYPE_SURF_Plane.id");
            var f_curve_data = n.Set("curve_data", Read_ContentSurface(r, c));
            var f_transform = n.Set("transform", Read_Transformation3D(r, c));
            var f_parameterization = n.Set("parameterization", Read_Domain(r, c));
            var f_u_parameter_coeff_a = n.Set("u_parameter_coeff_a", r.ReadDouble());
            var f_v_parameter_coeff_a = n.Set("v_parameter_coeff_a", r.ReadDouble());
            var f_u_parameter_coeff_b = n.Set("u_parameter_coeff_b", r.ReadDouble());
            var f_v_parameter_coeff_b = n.Set("v_parameter_coeff_b", r.ReadDouble());
            c.EvaluateSchema(r, (uint)PrcType.SURF_Plane);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_FileIdentifier(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("FileIdentifier");
            var f_flag = n.Set("flag", r.ReadBoolean());
            PrcNode f_unique_id = default;
            if (!f_flag)
            {
                f_unique_id = n.Set("unique_id", Read_UniqueId(r, c));
            }
            c.Leave();
            return n;
        }

        internal static PrcNode Read_Transformation3D(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("Transformation3D");
            var f_behavior = n.Set("behavior", r.ReadCharacter());
            PrcNode f_translation = default;
            if ((f_behavior & 1) != 0)
            {
                f_translation = n.Set("translation", Read_Vector3D(r, c));
            }
            PrcNode[] f_non_ortho_matrix = default;
            if ((f_behavior & 32) != 0)
            {
                f_non_ortho_matrix = new PrcNode[3];
                for (var i = 0; i < 3; i++) f_non_ortho_matrix[i] = Read_Vector3D(r, c);
                n.Set("non_ortho_matrix", f_non_ortho_matrix);
            }
            PrcNode[] f_rotation = default;
            if (((f_behavior & 2) != 0) && ((f_behavior & 32) == 0))
            {
                f_rotation = new PrcNode[2];
                for (var i = 0; i < 2; i++) f_rotation[i] = Read_Vector3D(r, c);
                n.Set("rotation", f_rotation);
            }
            PrcNode f_non_uniform_scale = default;
            if ((f_behavior & 16) != 0)
            {
                f_non_uniform_scale = n.Set("non_uniform_scale", Read_Vector3D(r, c));
            }
            double f_scale = default;
            if ((f_behavior & 8) != 0)
            {
                f_scale = n.Set("scale", r.ReadDouble());
            }
            double[] f_homogeneous = default;
            if ((f_behavior & 64) != 0)
            {
                f_homogeneous = new double[4];
                for (var i = 0; i < 4; i++) f_homogeneous[i] = r.ReadDouble();
                n.Set("homogeneous", f_homogeneous);
            }
            c.Leave();
            return n;
        }

        internal static PrcNode Read_ContentEntityReference(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("ContentEntityReference");
            var f_base = n.Set("base", Read_PRC_TYPE_ROOT_PRCBaseWithGraphics(r, c));
            var f_index_of_local_coordinate_system = n.Set("index_of_local_coordinate_system", r.ReadUnsignedInteger());
            var f_flag = n.Set("flag", r.ReadBoolean());
            PrcNode f_reference_data = default;
            if (f_flag)
            {
                f_reference_data = n.Set("reference_data", Read_ReferenceData(r, c));
            }
            c.Leave();
            return n;
        }

        internal static PrcNode Read_ContentLayerFilterItems(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("ContentLayerFilterItems");
            var f_b_is_inclusive = n.Set("b_is_inclusive", r.ReadBoolean());
            var f_number_of_layers = n.Set("number_of_layers", r.ReadUnsignedInteger());
            var f_layers = new uint[c.Count(f_number_of_layers, r)];
            for (var i = 0; i < f_layers.Length; i++) f_layers[i] = r.ReadUnsignedInteger();
            n.Set("layers", f_layers);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_ContentEntityFilterItems(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("ContentEntityFilterItems");
            var f_b_is_inclusive = n.Set("b_is_inclusive", r.ReadBoolean());
            var f_number_of_entities = n.Set("number_of_entities", r.ReadUnsignedInteger());
            var f_entities = new PrcNode[c.Count(f_number_of_entities, r)];
            for (var i = 0; i < f_entities.Length; i++) f_entities[i] = Read_PRC_TYPE_MISC_EntityReference(r, c);
            n.Set("entities", f_entities);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_Light(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("Light");
            PrcNode f_id;
            var t_id = r.PeekUnsignedInteger();
            f_id = n.Set("id", t_id switch
            {
                PrcType.GRAPH_AmbientLight => Read_PRC_TYPE_GRAPH_AmbientLight(r, c),
                PrcType.GRAPH_PointLight => Read_PRC_TYPE_GRAPH_PointLight(r, c),
                PrcType.GRAPH_DirectionalLight => Read_PRC_TYPE_GRAPH_DirectionalLight(r, c),
                PrcType.GRAPH_SpotLight => Read_PRC_TYPE_GRAPH_SpotLight(r, c),
                _ => throw c.UnknownType("Light.id", t_id),
            });
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_GRAPH_Camera(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_GRAPH_Camera");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.GRAPH_Camera, "PRC_TYPE_GRAPH_Camera.id");
            var f_base = n.Set("base", Read_ContentPRCRefBase(r, c));
            var f_is_orthographic = n.Set("is_orthographic", r.ReadBoolean());
            var f_position = n.Set("position", Read_Vector3D(r, c));
            var f_look = n.Set("look", Read_Vector3D(r, c));
            var f_up = n.Set("up", Read_Vector3D(r, c));
            var f_x = n.Set("x", r.ReadDouble());
            var f_y = n.Set("y", r.ReadDouble());
            var f_ratio = n.Set("ratio", r.ReadDouble());
            var f_clip_near = n.Set("clip_near", r.ReadDouble());
            var f_clip_far = n.Set("clip_far", r.ReadDouble());
            var f_zoom = n.Set("zoom", r.ReadDouble());
            c.EvaluateSchema(r, (uint)PrcType.GRAPH_Camera);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_ContentBaseTessData(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("ContentBaseTessData");
            var f_is_calculated = n.Set("is_calculated", r.ReadBoolean());
            var f_number_of_coordinates = n.Set("number_of_coordinates", r.ReadUnsignedInteger());
            var f_coordinates = new double[c.Count(f_number_of_coordinates, r)];
            for (var i = 0; i < f_coordinates.Length; i++) f_coordinates[i] = r.ReadDouble();
            n.Set("coordinates", f_coordinates);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_TESS_Face(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_TESS_Face");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.TESS_Face, "PRC_TYPE_TESS_Face.id");
            var f_size_of_line_attributes = n.Set("size_of_line_attributes", r.ReadUnsignedInteger());
            var f_line_attributes = new uint[c.Count(f_size_of_line_attributes, r)];
            for (var i = 0; i < f_line_attributes.Length; i++) f_line_attributes[i] = r.ReadUnsignedInteger();
            n.Set("line_attributes", f_line_attributes);
            var f_start_of_wire_data = n.Set("start_of_wire_data", r.ReadUnsignedInteger());
            var f_size_of_sizes_wire = n.Set("size_of_sizes_wire", r.ReadUnsignedInteger());
            var f_sizes_wire = new uint[c.Count(f_size_of_sizes_wire, r)];
            for (var i = 0; i < f_sizes_wire.Length; i++) f_sizes_wire[i] = r.ReadUnsignedInteger();
            n.Set("sizes_wire", f_sizes_wire);
            var f_used_entities_flag = n.Set("used_entities_flag", r.ReadUnsignedInteger());
            var f_start_triangulated = n.Set("start_triangulated", r.ReadUnsignedInteger());
            var f_size_of_triangulateddata = n.Set("size_of_triangulateddata", r.ReadUnsignedInteger());
            var f_triangulateddata = new uint[c.Count(f_size_of_triangulateddata, r)];
            for (var i = 0; i < f_triangulateddata.Length; i++) f_triangulateddata[i] = r.ReadUnsignedInteger();
            n.Set("triangulateddata", f_triangulateddata);
            c.SetVertexColorCountFromFace(f_used_entities_flag, f_triangulateddata);
            var f_number_of_textured_coordinate_indices = n.Set("number_of_textured_coordinate_indices", r.ReadUnsignedInteger());
            var f_has_vertex_colors = n.Set("has_vertex_colors", r.ReadBoolean());
            PrcNode f_vertex_color_data = default;
            if (f_has_vertex_colors)
            {
                f_vertex_color_data = n.Set("vertex_color_data", Read_VertexColors(r, c));
            }
            uint f_behavior = default;
            if (f_size_of_line_attributes > 0)
            {
                f_behavior = n.Set("behavior", r.ReadUnsignedInteger());
            }
            c.EvaluateSchema(r, (uint)PrcType.TESS_Face);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_VertexColors(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("VertexColors");
            var f_is_rgba = n.Set("is_rgba", r.ReadBoolean());
            bool f_is_segment_color = default;
            if (c.InsideWire)
            {
                f_is_segment_color = n.Set("is_segment_color", r.ReadBoolean());
                c.WireIsSegmentColor = f_is_segment_color;
            }
            var f_b_optimized = n.Set("b_optimized", r.ReadBoolean());
            PrcNode f_color_data_rgb_first_vertex = default;
            if (!f_b_optimized && !f_is_rgba)
            {
                f_color_data_rgb_first_vertex = n.Set("color_data_rgb_first_vertex", Read_ColorRGB(r, c));
            }
            PrcNode[] f_color_data_rgb_remaining_vertices = default;
            if (!f_b_optimized && !f_is_rgba)
            {
                f_color_data_rgb_remaining_vertices = new PrcNode[c.Count(c.VertexColorCount - 1, r)];
                for (var i = 0; i < f_color_data_rgb_remaining_vertices.Length; i++) f_color_data_rgb_remaining_vertices[i] = Read_ColorDataRemainderRGB(r, c);
                n.Set("color_data_rgb_remaining_vertices", f_color_data_rgb_remaining_vertices);
            }
            PrcNode f_color_data_rgba_first_vertex = default;
            if (!f_b_optimized && f_is_rgba)
            {
                f_color_data_rgba_first_vertex = n.Set("color_data_rgba_first_vertex", Read_ColorRGBA(r, c));
            }
            PrcNode[] f_color_data_rgba_remaining_vertices = default;
            if (!f_b_optimized && f_is_rgba)
            {
                f_color_data_rgba_remaining_vertices = new PrcNode[c.Count(c.VertexColorCount - 1, r)];
                for (var i = 0; i < f_color_data_rgba_remaining_vertices.Length; i++) f_color_data_rgba_remaining_vertices[i] = Read_ColorDataRemainderRGBA(r, c);
                n.Set("color_data_rgba_remaining_vertices", f_color_data_rgba_remaining_vertices);
            }
            c.Leave();
            return n;
        }

        internal static PrcNode Read_CompressedTextureParameter(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("CompressedTextureParameter");
            var f_binary_texture_data = n.Set("binary_texture_data", Read_BinaryTextureData(r, c));
            var f_reference_array_size = n.Set("reference_array_size", r.ReadUnsignedInteger());
            var f_reference_array = new uint[c.Count(f_reference_array_size, r)];
            for (var i = 0; i < f_reference_array.Length; i++) f_reference_array[i] = r.ReadNumberOfBitsThenUnsignedInteger();
            n.Set("reference_array", f_reference_array);
            var f_texture_parameters_tolerance = n.Set("texture_parameters_tolerance", r.ReadDouble());
            if (f_texture_parameters_tolerance < 0.0 || f_texture_parameters_tolerance > 1.0) throw c.Invalid("texture_parameters_tolerance out of range");
            var f_texture_parameters_size = n.Set("texture_parameters_size", r.ReadUnsignedInteger());
            if (f_texture_parameters_size == 0) throw c.Invalid("texture_parameters_size is 0");
            var f_texture_parameters = new float[c.Count(f_texture_parameters_size, r)];
            for (var i = 0; i < f_texture_parameters.Length; i++) f_texture_parameters[i] = r.ReadFloatAsBytes();
            n.Set("texture_parameters", f_texture_parameters);
            if (f_texture_parameters.Any(float.IsNaN)) throw c.Invalid("texture parameter is NaN");
            c.Leave();
            return n;
        }

        internal static PrcNode Read_AttributeEntry(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("AttributeEntry");
            var f_flag = n.Set("flag", r.ReadBoolean());
            uint f_integer_title = default;
            if (f_flag)
            {
                f_integer_title = n.Set("integer_title", r.ReadUnsignedInteger());
            }
            string f_string_title = default;
            if (!f_flag)
            {
                f_string_title = n.Set("string_title", r.ReadString());
            }
            c.Leave();
            return n;
        }

        internal static PrcNode Read_AttributeKeyValue(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("AttributeKeyValue");
            var f_title = n.Set("title", Read_AttributeEntry(r, c));
            var f_type_ = n.Set("type_", r.ReadUnsignedInteger());
            int f_valuei = default;
            if (f_type_ == 1)
            {
                f_valuei = n.Set("valuei", r.ReadInteger());
            }
            double f_valued = default;
            if (f_type_ == 2)
            {
                f_valued = n.Set("valued", r.ReadDouble());
            }
            int f_valuet = default;
            if (f_type_ == 3)
            {
                f_valuet = n.Set("valuet", r.ReadInteger());
            }
            string f_values = default;
            if (f_type_ == 4)
            {
                f_values = n.Set("values", r.ReadString());
            }
            int f_value_msp = default;
            if (f_type_ == 5)
            {
                f_value_msp = n.Set("value_msp", r.ReadInteger());
            }
            uint f_value_lsp = default;
            if (f_type_ == 5)
            {
                f_value_lsp = n.Set("value_lsp", r.ReadUnsignedInteger());
            }
            c.Leave();
            return n;
        }

        internal static PrcNode Read_FontKey(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("FontKey");
            var f_font_size = n.Set("font_size", r.ReadUnsignedInteger());
            var f_font_attributes = n.Set("font_attributes", r.ReadCharacter());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_Transformation2D(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("Transformation2D");
            var f_behavior = n.Set("behavior", r.ReadCharacter());
            PrcNode f_translation = default;
            if ((f_behavior & 1) != 0)
            {
                f_translation = n.Set("translation", Read_Vector2D(r, c));
            }
            PrcNode[] f_non_ortho_matrix = default;
            if ((f_behavior & 32) != 0)
            {
                f_non_ortho_matrix = new PrcNode[2];
                for (var i = 0; i < 2; i++) f_non_ortho_matrix[i] = Read_Vector2D(r, c);
                n.Set("non_ortho_matrix", f_non_ortho_matrix);
            }
            PrcNode f_rotation = default;
            if (((f_behavior & 2) != 0) && ((f_behavior & 32) == 0))
            {
                f_rotation = n.Set("rotation", Read_Vector2D(r, c));
            }
            PrcNode f_non_uniform_scale = default;
            if ((f_behavior & 16) != 0)
            {
                f_non_uniform_scale = n.Set("non_uniform_scale", Read_Vector2D(r, c));
            }
            double f_scale = default;
            if ((f_behavior & 8) != 0)
            {
                f_scale = n.Set("scale", r.ReadDouble());
            }
            double[] f_homogeneous = default;
            if ((f_behavior & 64) != 0)
            {
                f_homogeneous = new double[3];
                for (var i = 0; i < 3; i++) f_homogeneous[i] = r.ReadDouble();
                n.Set("homogeneous", f_homogeneous);
            }
            c.Leave();
            return n;
        }

        internal static PrcNode Read_HatchingLine(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("HatchingLine");
            var f_start_point = n.Set("start_point", Read_Vector2D(r, c));
            var f_end_point = n.Set("end_point", Read_Vector2D(r, c));
            var f_angle = n.Set("angle", r.ReadDouble());
            var f_biased_index_of_line_style = n.Set("biased_index_of_line_style", r.ReadInteger());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_ContentExtendedEntityReference(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("ContentExtendedEntityReference");
            var f_content_entity_reference = n.Set("content_entity_reference", Read_ContentEntityReference(r, c));
            var f_has_reference_data = n.Set("has_reference_data", r.ReadBoolean());
            PrcNode f_reference_data = default;
            if (f_has_reference_data)
            {
                f_reference_data = n.Set("reference_data", Read_ReferenceData(r, c));
            }
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_MKP_AnnotationItem(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_MKP_AnnotationItem");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.MKP_AnnotationItem, "PRC_TYPE_MKP_AnnotationItem.id");
            var f_base = n.Set("base", Read_PRC_TYPE_ROOT_PRCBaseWithGraphics(r, c));
            var f_unique_id = n.Set("unique_id", Read_PRC_TYPE_MISC_ReferenceOnPRCBase(r, c));
            c.EvaluateSchema(r, (uint)PrcType.MKP_AnnotationItem);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_MKP_AnnotationSet(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_MKP_AnnotationSet");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.MKP_AnnotationSet, "PRC_TYPE_MKP_AnnotationSet.id");
            var f_base = n.Set("base", Read_PRC_TYPE_ROOT_PRCBaseWithGraphics(r, c));
            var f_number_of_annotations = n.Set("number_of_annotations", r.ReadUnsignedInteger());
            var f_annotations = new PrcNode[c.Count(f_number_of_annotations, r)];
            for (var i = 0; i < f_annotations.Length; i++) f_annotations[i] = Read_AnnotationEntity(r, c);
            n.Set("annotations", f_annotations);
            c.EvaluateSchema(r, (uint)PrcType.MKP_AnnotationSet);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_MKP_AnnotationReference(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_MKP_AnnotationReference");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.MKP_AnnotationReference, "PRC_TYPE_MKP_AnnotationReference.id");
            var f_base = n.Set("base", Read_PRC_TYPE_ROOT_PRCBaseWithGraphics(r, c));
            var f_number_of_linked_items = n.Set("number_of_linked_items", r.ReadUnsignedInteger());
            var f_linked_items = new PrcNode[c.Count(f_number_of_linked_items, r)];
            for (var i = 0; i < f_linked_items.Length; i++) f_linked_items[i] = Read_PRC_TYPE_MISC_ReferenceOnPRCBase(r, c);
            n.Set("linked_items", f_linked_items);
            c.EvaluateSchema(r, (uint)PrcType.MKP_AnnotationReference);
            var f_user_data = n.Set("user_data", r.ReadUserData());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_ContentSurface(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("ContentSurface");
            var f_has_base_geometry = n.Set("has_base_geometry", r.ReadBoolean());
            PrcNode f_attribute_data = default;
            if (f_has_base_geometry)
            {
                f_attribute_data = n.Set("attribute_data", Read_AttributeData(r, c));
            }
            PrcNode f_name = default;
            if (f_has_base_geometry)
            {
                f_name = n.Set("name", Read_Name(r, c));
            }
            uint f_id = default;
            if (f_has_base_geometry)
            {
                f_id = n.Set("id", r.ReadUnsignedInteger());
            }
            var f_extend_type = n.Set("extend_type", r.ReadUnsignedInteger());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_Domain(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("Domain");
            var f_min_uv = n.Set("min_uv", Read_Vector2D(r, c));
            var f_max_uv = n.Set("max_uv", Read_Vector2D(r, c));
            c.Leave();
            return n;
        }

        internal static PrcNode Read_ReferenceData(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("ReferenceData");
            PrcNode f_data;
            var t_data = r.PeekUnsignedInteger();
            f_data = n.Set("data", t_data switch
            {
                PrcType.MISC_ReferenceOnTopology => Read_PRC_TYPE_MISC_ReferenceOnTopology(r, c),
                PrcType.MISC_ReferenceOnPRCBase => Read_PRC_TYPE_MISC_ReferenceOnPRCBase(r, c),
                _ => throw c.UnknownType("ReferenceData.data", t_data),
            });
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_GRAPH_AmbientLight(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_GRAPH_AmbientLight");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.GRAPH_AmbientLight, "PRC_TYPE_GRAPH_AmbientLight.id");
            var f_base = n.Set("base", Read_ContentPRCRefBase(r, c));
            var f_biased_ambient_index = n.Set("biased_ambient_index", r.ReadUnsignedInteger());
            var f_biased_diffuse_index = n.Set("biased_diffuse_index", r.ReadUnsignedInteger());
            var f_biased_specular_index = n.Set("biased_specular_index", r.ReadUnsignedInteger());
            c.EvaluateSchema(r, (uint)PrcType.GRAPH_AmbientLight);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_GRAPH_PointLight(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_GRAPH_PointLight");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.GRAPH_PointLight, "PRC_TYPE_GRAPH_PointLight.id");
            var f_base = n.Set("base", Read_ContentPRCRefBase(r, c));
            var f_biased_ambient_index = n.Set("biased_ambient_index", r.ReadUnsignedInteger());
            var f_biased_diffuse_index = n.Set("biased_diffuse_index", r.ReadUnsignedInteger());
            var f_biased_specular_index = n.Set("biased_specular_index", r.ReadUnsignedInteger());
            var f_location = n.Set("location", Read_Vector3D(r, c));
            var f_constant_attenuation_factor = n.Set("constant_attenuation_factor", r.ReadDouble());
            var f_linear_attenuation_factor = n.Set("linear_attenuation_factor", r.ReadDouble());
            var f_quadratic_attenuation_factor = n.Set("quadratic_attenuation_factor", r.ReadDouble());
            c.EvaluateSchema(r, (uint)PrcType.GRAPH_PointLight);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_GRAPH_DirectionalLight(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_GRAPH_DirectionalLight");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.GRAPH_DirectionalLight, "PRC_TYPE_GRAPH_DirectionalLight.id");
            var f_base = n.Set("base", Read_ContentPRCRefBase(r, c));
            var f_biased_ambient_index = n.Set("biased_ambient_index", r.ReadUnsignedInteger());
            var f_biased_diffuse_index = n.Set("biased_diffuse_index", r.ReadUnsignedInteger());
            var f_biased_specular_index = n.Set("biased_specular_index", r.ReadUnsignedInteger());
            var f_direction = n.Set("direction", Read_Vector3D(r, c));
            double f_intensity = default;
            if (c.AuthoringVersion >= 8030)
            {
                f_intensity = n.Set("intensity", r.ReadDouble());
            }
            c.EvaluateSchema(r, (uint)PrcType.GRAPH_DirectionalLight);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_GRAPH_SpotLight(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_GRAPH_SpotLight");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.GRAPH_SpotLight, "PRC_TYPE_GRAPH_SpotLight.id");
            var f_base = n.Set("base", Read_ContentPRCRefBase(r, c));
            var f_biased_ambient_index = n.Set("biased_ambient_index", r.ReadUnsignedInteger());
            var f_biased_diffuse_index = n.Set("biased_diffuse_index", r.ReadUnsignedInteger());
            var f_biased_specular_index = n.Set("biased_specular_index", r.ReadUnsignedInteger());
            var f_location = n.Set("location", Read_Vector3D(r, c));
            var f_constant_attenuation_factor = n.Set("constant_attenuation_factor", r.ReadDouble());
            var f_linear_attenuation_factor = n.Set("linear_attenuation_factor", r.ReadDouble());
            var f_quadratic_attenuation_factor = n.Set("quadratic_attenuation_factor", r.ReadDouble());
            var f_direction = n.Set("direction", Read_Vector3D(r, c));
            var f_fall_off_angle = n.Set("fall_off_angle", r.ReadDouble());
            var f_fall_off_exponent = n.Set("fall_off_exponent", r.ReadDouble());
            c.EvaluateSchema(r, (uint)PrcType.GRAPH_SpotLight);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_ColorRGB(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("ColorRGB");
            var f_color = new byte[3];
            for (var i = 0; i < 3; i++) f_color[i] = r.ReadUnsignedCharacter();
            n.Set("color", f_color);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_ColorDataRemainderRGB(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("ColorDataRemainderRGB");
            var f_is_same = n.Set("is_same", r.ReadBoolean());
            PrcNode f_color = default;
            if (!f_is_same)
            {
                f_color = n.Set("color", Read_ColorRGB(r, c));
            }
            c.Leave();
            return n;
        }

        internal static PrcNode Read_ColorRGBA(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("ColorRGBA");
            var f_color = new byte[4];
            for (var i = 0; i < 4; i++) f_color[i] = r.ReadUnsignedCharacter();
            n.Set("color", f_color);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_ColorDataRemainderRGBA(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("ColorDataRemainderRGBA");
            var f_is_same = n.Set("is_same", r.ReadBoolean());
            PrcNode f_color = default;
            if (!f_is_same)
            {
                f_color = n.Set("color", Read_ColorRGBA(r, c));
            }
            c.Leave();
            return n;
        }

        internal static PrcNode Read_BinaryTextureData(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("BinaryTextureData");
            var f_texture_binary_data_size = n.Set("texture_binary_data_size", r.ReadUnsignedInteger());
            var f_texture_binary_data = new byte[c.Count(f_texture_binary_data_size * 4, r)];
            for (var i = 0; i < f_texture_binary_data.Length; i++) f_texture_binary_data[i] = r.ReadUnsignedCharacter();
            n.Set("texture_binary_data", f_texture_binary_data);
            var f_last_integer_used_bit_number = n.Set("last_integer_used_bit_number", r.ReadUnsignedInteger());
            if (f_last_integer_used_bit_number > 32) throw c.Invalid("last_integer_used_bit_number > 32");
            c.Leave();
            return n;
        }

        internal static PrcNode Read_Vector2D(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("Vector2D");
            var f_x = n.Set("x", r.ReadDouble());
            var f_y = n.Set("y", r.ReadDouble());
            c.Leave();
            return n;
        }

        internal static PrcNode Read_PRC_TYPE_MISC_ReferenceOnTopology(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("PRC_TYPE_MISC_ReferenceOnTopology");
            var f_id = n.Set("id", r.ReadUnsignedInteger());
            c.Expect(f_id, PrcType.MISC_ReferenceOnTopology, "PRC_TYPE_MISC_ReferenceOnTopology.id");
            var f_type_of_entity = n.Set("type_of_entity", r.ReadUnsignedInteger());
            var f_flag = n.Set("flag", r.ReadBoolean());
            PrcNode f_data = default;
            if (f_flag)
            {
                f_data = n.Set("data", Read_AdditionalTargetData(r, c));
            }
            c.EvaluateSchema(r, (uint)PrcType.MISC_ReferenceOnTopology);
            c.Leave();
            return n;
        }

        internal static PrcNode Read_AdditionalTargetData(PrcBitReader r, PrcContext c)
        {
            c.Enter();
            var n = new PrcNode("AdditionalTargetData");
            var f_flag = n.Set("flag", r.ReadBoolean());
            PrcNode f_unique_id = default;
            if (!f_flag)
            {
                f_unique_id = n.Set("unique_id", Read_UniqueId(r, c));
            }
            var f_index_of_topological_index = n.Set("index_of_topological_index", r.ReadUnsignedInteger());
            var f_index_of_body = n.Set("index_of_body", r.ReadUnsignedInteger());
            var f_number_of_indices = n.Set("number_of_indices", r.ReadUnsignedInteger());
            var f_indices = new uint[c.Count(f_number_of_indices, r)];
            for (var i = 0; i < f_indices.Length; i++) f_indices[i] = r.ReadUnsignedInteger();
            n.Set("indices", f_indices);
            c.Leave();
            return n;
        }
    }
}
