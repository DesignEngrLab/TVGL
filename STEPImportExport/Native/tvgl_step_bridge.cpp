#include "tvgl_step_bridge.h"

#include <STEPControl_Reader.hxx>
#include <STEPControl_Writer.hxx>
#include <STEPControl_StepModelType.hxx>
#include <BRepPrimAPI_MakeBox.hxx>
#include <BRepPrimAPI_MakeCylinder.hxx>
#include <BRepAlgoAPI_Cut.hxx>
#include <IFSelect_ReturnStatus.hxx>
#include <TopoDS_Shape.hxx>
#include <TopoDS.hxx>
#include <TopoDS_Solid.hxx>
#include <TopoDS_Shell.hxx>
#include <TopoDS_Face.hxx>
#include <TopExp_Explorer.hxx>
#include <TopAbs_ShapeEnum.hxx>
#include <BRepMesh_IncrementalMesh.hxx>
#include <BRepAdaptor_Surface.hxx>
#include <BRep_Tool.hxx>
#include <Poly_Triangulation.hxx>
#include <Poly_Triangle.hxx>
#include <TopLoc_Location.hxx>
#include <gp_Trsf.hxx>
#include <gp_Pnt.hxx>
#include <gp_Dir.hxx>
#include <gp_Pln.hxx>
#include <gp_Cylinder.hxx>
#include <gp_Cone.hxx>
#include <gp_Sphere.hxx>
#include <gp_Torus.hxx>
#include <GeomAbs_SurfaceType.hxx>

#include <vector>
#include <string>
#include <cstring>
#include <cmath>
#include <unordered_map>
#include <memory>
#include <algorithm>

namespace {

struct Vec3d {
    double x, y, z;
};

struct CellKey {
    int64_t cx, cy, cz;
    bool operator==(const CellKey& o) const {
        return cx == o.cx && cy == o.cy && cz == o.cz;
    }
};

struct CellKeyHash {
    size_t operator()(const CellKey& k) const {
        size_t h = 14695981039346656037ull;
        h = (h ^ static_cast<size_t>(k.cx)) * 1099511628211ull;
        h = (h ^ static_cast<size_t>(k.cy)) * 1099511628211ull;
        h = (h ^ static_cast<size_t>(k.cz)) * 1099511628211ull;
        return h;
    }
};

class SpatialWelder {
private:
    double m_invTol;
    double m_tolSq;
    std::unordered_multimap<CellKey, int32_t, CellKeyHash> m_cells;
    std::vector<Vec3d>& m_vertices;

public:
    SpatialWelder(double tol, std::vector<Vec3d>& vertices)
        : m_invTol(1.0 / tol), m_tolSq(tol * tol), m_vertices(vertices) {}

    int32_t AddOrFind(double x, double y, double z) {
        int64_t cx = static_cast<int64_t>(std::floor(x * m_invTol));
        int64_t cy = static_cast<int64_t>(std::floor(y * m_invTol));
        int64_t cz = static_cast<int64_t>(std::floor(z * m_invTol));

        for (int64_t dx = -1; dx <= 1; ++dx) {
            for (int64_t dy = -1; dy <= 1; ++dy) {
                for (int64_t dz = -1; dz <= 1; ++dz) {
                    CellKey key{ cx + dx, cy + dy, cz + dz };
                    auto range = m_cells.equal_range(key);
                    for (auto it = range.first; it != range.second; ++it) {
                        int32_t vIdx = it->second;
                        const Vec3d& pt = m_vertices[vIdx];
                        double ex = pt.x - x;
                        double ey = pt.y - y;
                        double ez = pt.z - z;
                        if ((ex * ex + ey * ey + ez * ez) <= m_tolSq) {
                            return vIdx;
                        }
                    }
                }
            }
        }

        int32_t newIndex = static_cast<int32_t>(m_vertices.size());
        m_vertices.push_back({ x, y, z });
        m_cells.emplace(CellKey{ cx, cy, cz }, newIndex);
        return newIndex;
    }
};

struct ExtractedBody {
    BodyMetadata Metadata;
    std::vector<double> Vertices;            // flat X, Y, Z
    std::vector<int32_t> Triangles;          // flat triplet indices
    std::vector<FacePrimitiveData> Faces;    // per-face analytic and slice metadata
};

struct StepModelInternal {
    std::vector<ExtractedBody> Bodies;
};

void ExtractFaceGeometry(const TopoDS_Face& face, FacePrimitiveData& data) {
    std::memset(&data, 0, sizeof(FacePrimitiveData));
    data.ColorR = 210;
    data.ColorG = 210;
    data.ColorB = 210;
    data.ColorA = 255;

    BRepAdaptor_Surface surf(face, Standard_True);
    GeomAbs_SurfaceType stype = surf.GetType();
    bool isReversed = (face.Orientation() == TopAbs_REVERSED);

    switch (stype) {
        case GeomAbs_Plane: {
            gp_Pln pln = surf.Plane();
            gp_Ax1 ax = pln.Axis();
            gp_Pnt pos = ax.Location();
            gp_Dir normal = ax.Direction();
            if (isReversed) {
                normal.Reverse();
            }
            data.Type = StepPrim_Plane;
            data.OriginX = pos.X();
            data.OriginY = pos.Y();
            data.OriginZ = pos.Z();
            data.AxisX = normal.X();
            data.AxisY = normal.Y();
            data.AxisZ = normal.Z();
            data.IsPositive = 1;
            std::strncpy(data.SurfaceTypeName, "Plane", sizeof(data.SurfaceTypeName) - 1);
            break;
        }
        case GeomAbs_Cylinder: {
            gp_Cylinder cyl = surf.Cylinder();
            gp_Ax1 ax = cyl.Axis();
            gp_Pnt loc = ax.Location();
            gp_Dir dir = ax.Direction();
            data.Type = StepPrim_Cylinder;
            data.OriginX = loc.X();
            data.OriginY = loc.Y();
            data.OriginZ = loc.Z();
            data.AxisX = dir.X();
            data.AxisY = dir.Y();
            data.AxisZ = dir.Z();
            data.Radius = cyl.Radius();
            data.IsPositive = isReversed ? 0 : 1;
            std::strncpy(data.SurfaceTypeName, "Cylinder", sizeof(data.SurfaceTypeName) - 1);
            break;
        }
        case GeomAbs_Cone: {
            gp_Cone cone = surf.Cone();
            gp_Ax1 ax = cone.Axis();
            gp_Pnt apex = cone.Apex();
            gp_Dir dir = ax.Direction();
            double semiAngle = cone.SemiAngle();
            data.Type = StepPrim_Cone;
            data.OriginX = apex.X();
            data.OriginY = apex.Y();
            data.OriginZ = apex.Z();
            data.AxisX = dir.X();
            data.AxisY = dir.Y();
            data.AxisZ = dir.Z();
            data.Radius = cone.RefRadius();
            data.Aperture = std::tan(semiAngle);
            data.IsPositive = isReversed ? 0 : 1;
            std::strncpy(data.SurfaceTypeName, "Cone", sizeof(data.SurfaceTypeName) - 1);
            break;
        }
        case GeomAbs_Sphere: {
            gp_Sphere sph = surf.Sphere();
            gp_Pnt center = sph.Location();
            data.Type = StepPrim_Sphere;
            data.OriginX = center.X();
            data.OriginY = center.Y();
            data.OriginZ = center.Z();
            data.AxisX = 0.0;
            data.AxisY = 0.0;
            data.AxisZ = 1.0;
            data.Radius = sph.Radius();
            data.IsPositive = isReversed ? 0 : 1;
            std::strncpy(data.SurfaceTypeName, "Sphere", sizeof(data.SurfaceTypeName) - 1);
            break;
        }
        case GeomAbs_Torus: {
            gp_Torus tor = surf.Torus();
            gp_Ax1 ax = tor.Axis();
            gp_Pnt center = ax.Location();
            gp_Dir dir = ax.Direction();
            data.Type = StepPrim_Torus;
            data.OriginX = center.X();
            data.OriginY = center.Y();
            data.OriginZ = center.Z();
            data.AxisX = dir.X();
            data.AxisY = dir.Y();
            data.AxisZ = dir.Z();
            data.Radius = tor.MajorRadius();
            data.MinorRadius = tor.MinorRadius();
            data.IsPositive = isReversed ? 0 : 1;
            std::strncpy(data.SurfaceTypeName, "Torus", sizeof(data.SurfaceTypeName) - 1);
            break;
        }
        default: {
            data.Type = StepPrim_Unknown;
            data.IsPositive = 1;
            const char* typeName = "Unknown";
            if (stype == GeomAbs_BSplineSurface) typeName = "BSplineSurface";
            else if (stype == GeomAbs_BezierSurface) typeName = "BezierSurface";
            else if (stype == GeomAbs_SurfaceOfRevolution) typeName = "SurfaceOfRevolution";
            else if (stype == GeomAbs_SurfaceOfExtrusion) typeName = "SurfaceOfExtrusion";
            else if (stype == GeomAbs_OffsetSurface) typeName = "OffsetSurface";
            else typeName = "OtherSurface";
            std::strncpy(data.SurfaceTypeName, typeName, sizeof(data.SurfaceTypeName) - 1);
            break;
        }
    }
}

} // anonymous namespace

extern "C" {

TVGL_STEP_API StepModelHandle StepModel_Load(
    const char* filePath,
    double linearDeflection,
    double angularDeflection
) {
    if (!filePath || linearDeflection <= 0.0) {
        return nullptr;
    }
    if (angularDeflection <= 0.0) {
        angularDeflection = 0.5; // default ~28.6 degrees
    }

    STEPControl_Reader reader;
    IFSelect_ReturnStatus status = reader.ReadFile(filePath);
    if (status != IFSelect_RetDone) {
        return nullptr;
    }

    reader.TransferRoots();
    TopoDS_Shape shape = reader.OneShape();
    if (shape.IsNull()) {
        return nullptr;
    }

    // Tessellate the entire shape
    BRepMesh_IncrementalMesh mesher(shape, linearDeflection, Standard_False, angularDeflection, Standard_True);
    mesher.Perform();

    auto model = std::make_unique<StepModelInternal>();

    // Discover individual solids / bodies
    std::vector<std::pair<TopoDS_Shape, bool>> bodyShapes; // shape, isSheetBody
    TopExp_Explorer solidExp(shape, TopAbs_SOLID);
    for (; solidExp.More(); solidExp.Next()) {
        bodyShapes.emplace_back(solidExp.Current(), false);
    }

    if (bodyShapes.empty()) {
        TopExp_Explorer shellExp(shape, TopAbs_SHELL);
        for (; shellExp.More(); shellExp.Next()) {
            bodyShapes.emplace_back(shellExp.Current(), true);
        }
    }

    if (bodyShapes.empty()) {
        bodyShapes.emplace_back(shape, true);
    }

    const double weldTolerance = 1e-6;

    for (size_t bIdx = 0; bIdx < bodyShapes.size(); ++bIdx) {
        const TopoDS_Shape& bodyShape = bodyShapes[bIdx].first;
        bool isSheet = bodyShapes[bIdx].second;

        ExtractedBody body;
        std::memset(&body.Metadata, 0, sizeof(BodyMetadata));
        body.Metadata.IsSheetBody = isSheet ? 1 : 0;
        std::snprintf(body.Metadata.Name, sizeof(body.Metadata.Name), "Body_%d", static_cast<int>(bIdx + 1));

        std::vector<Vec3d> uniquePoints;
        SpatialWelder welder(weldTolerance, uniquePoints);

        TopExp_Explorer faceExp(bodyShape, TopAbs_FACE);
        for (; faceExp.More(); faceExp.Next()) {
            TopoDS_Face face = TopoDS::Face(faceExp.Current());
            TopLoc_Location loc;
            Handle(Poly_Triangulation) tri = BRep_Tool::Triangulation(face, loc);
            if (tri.IsNull() || tri->NbNodes() == 0 || tri->NbTriangles() == 0) {
                continue;
            }

            gp_Trsf trsf = loc.Transformation();
            int nbNodes = tri->NbNodes();

            // Map each local node of the face to the welded global vertex index
            std::vector<int32_t> localToGlobal(nbNodes + 1, -1);
            for (int i = 1; i <= nbNodes; ++i) {
                gp_Pnt p = tri->Node(i).Transformed(trsf);
                localToGlobal[i] = welder.AddOrFind(p.X(), p.Y(), p.Z());
            }

            FacePrimitiveData faceData;
            ExtractFaceGeometry(face, faceData);

            int32_t triangleStart = static_cast<int32_t>(body.Triangles.size() / 3);
            int32_t validTrianglesCount = 0;
            bool isReversed = (face.Orientation() == TopAbs_REVERSED);

            int nbTriangles = tri->NbTriangles();
            for (int t = 1; t <= nbTriangles; ++t) {
                int n1, n2, n3;
                tri->Triangle(t).Get(n1, n2, n3);
                int32_t v1 = localToGlobal[n1];
                int32_t v2 = localToGlobal[n2];
                int32_t v3 = localToGlobal[n3];

                // Skip degenerate triangles collapsed during welding
                if (v1 == v2 || v2 == v3 || v1 == v3) {
                    continue;
                }

                if (isReversed) {
                    std::swap(v2, v3);
                }

                body.Triangles.push_back(v1);
                body.Triangles.push_back(v2);
                body.Triangles.push_back(v3);
                validTrianglesCount++;
            }

            if (validTrianglesCount > 0) {
                faceData.TriangleStartIndex = triangleStart;
                faceData.TriangleCount = validTrianglesCount;
                body.Faces.push_back(faceData);
            }
        }

        if (!uniquePoints.empty() && !body.Triangles.empty()) {
            body.Vertices.reserve(uniquePoints.size() * 3);
            for (const auto& pt : uniquePoints) {
                body.Vertices.push_back(pt.x);
                body.Vertices.push_back(pt.y);
                body.Vertices.push_back(pt.z);
            }

            body.Metadata.VertexCount = static_cast<int32_t>(uniquePoints.size());
            body.Metadata.TriangleCount = static_cast<int32_t>(body.Triangles.size() / 3);
            body.Metadata.FaceCount = static_cast<int32_t>(body.Faces.size());

            model->Bodies.push_back(std::move(body));
        }
    }

    if (model->Bodies.empty()) {
        return nullptr;
    }

    return static_cast<StepModelHandle>(model.release());
}

TVGL_STEP_API int32_t StepModel_GetBodyCount(StepModelHandle handle) {
    if (!handle) return 0;
    auto model = static_cast<StepModelInternal*>(handle);
    return static_cast<int32_t>(model->Bodies.size());
}

TVGL_STEP_API bool StepModel_GetBodyMetadata(
    StepModelHandle handle,
    int32_t bodyIndex,
    BodyMetadata* outMetadata
) {
    if (!handle || !outMetadata) return false;
    auto model = static_cast<StepModelInternal*>(handle);
    if (bodyIndex < 0 || bodyIndex >= static_cast<int32_t>(model->Bodies.size())) {
        return false;
    }

    *outMetadata = model->Bodies[bodyIndex].Metadata;
    return true;
}

TVGL_STEP_API bool StepModel_CopyBodyData(
    StepModelHandle handle,
    int32_t bodyIndex,
    double* outVertices,
    int32_t* outTriangles,
    FacePrimitiveData* outFaces
) {
    if (!handle || !outVertices || !outTriangles || !outFaces) return false;
    auto model = static_cast<StepModelInternal*>(handle);
    if (bodyIndex < 0 || bodyIndex >= static_cast<int32_t>(model->Bodies.size())) {
        return false;
    }

    const auto& body = model->Bodies[bodyIndex];
    std::memcpy(outVertices, body.Vertices.data(), body.Vertices.size() * sizeof(double));
    std::memcpy(outTriangles, body.Triangles.data(), body.Triangles.size() * sizeof(int32_t));
    std::memcpy(outFaces, body.Faces.data(), body.Faces.size() * sizeof(FacePrimitiveData));

    return true;
}

TVGL_STEP_API void StepModel_Free(StepModelHandle handle) {
    if (handle) {
        delete static_cast<StepModelInternal*>(handle);
    }
}

TVGL_STEP_API bool StepModel_CreateTestStepFile(const char* filePath) {
    if (!filePath) return false;
    try {
        TopoDS_Shape box = BRepPrimAPI_MakeBox(50.0, 40.0, 30.0).Shape();
        gp_Ax2 cylAx(gp_Pnt(25.0, 20.0, 0.0), gp_Dir(0.0, 0.0, 1.0));
        TopoDS_Shape cyl = BRepPrimAPI_MakeCylinder(cylAx, 10.0, 30.0).Shape();
        TopoDS_Shape cut = BRepAlgoAPI_Cut(box, cyl).Shape();

        STEPControl_Writer writer;
        IFSelect_ReturnStatus stat = writer.Transfer(cut, STEPControl_AsIs);
        if (stat != IFSelect_RetDone) return false;
        stat = writer.Write(filePath);
        return (stat == IFSelect_RetDone);
    } catch (...) {
        return false;
    }
}

} // extern "C"
