#ifndef TVGL_STEP_BRIDGE_H
#define TVGL_STEP_BRIDGE_H

#ifdef _WIN32
    #ifdef TVGL_STEP_BRIDGE_EXPORTS
        #define TVGL_STEP_API __declspec(dllexport)
    #else
        #define TVGL_STEP_API __declspec(dllimport)
    #endif
#else
    #define TVGL_STEP_API __attribute__((visibility("default")))
#endif

#include <cstdint>

#ifdef __cplusplus
extern "C" {
#endif

// Opaque pointer representing a loaded STEP model and its extracted mesh/surface data
typedef void* StepModelHandle;

// Surface primitive classification
enum StepPrimitiveType : int32_t {
    StepPrim_Plane = 0,
    StepPrim_Cylinder = 1,
    StepPrim_Cone = 2,
    StepPrim_Sphere = 3,
    StepPrim_Torus = 4,
    StepPrim_Unknown = 5
};

#pragma pack(push, 8)
// Surface metadata for a single CAD face / primitive
struct FacePrimitiveData {
    int32_t Type;                // StepPrimitiveType
    int32_t TriangleStartIndex;  // 0-based triangle index in the body's triangle array
    int32_t TriangleCount;       // Number of triangles belonging to this face
    int32_t IsPositive;          // 1 if outward/positive normal orientation, 0 if inward/negative

    // Analytic surface parameters
    double OriginX;              // Point on plane / Anchor of cylinder / Apex of cone / Center of sphere & torus
    double OriginY;
    double OriginZ;

    double AxisX;                // Normal of plane / Axis direction of cylinder, cone, torus
    double AxisY;
    double AxisZ;

    double Radius;               // Radius (cylinder, sphere, torus major radius)
    double MinorRadius;          // Torus minor radius
    double Aperture;             // Cone aperture: tan(semiAngle)

    // Face color (RGBA 0..255)
    uint8_t ColorR;
    uint8_t ColorG;
    uint8_t ColorB;
    uint8_t ColorA;

    // Descriptive name of the surface type (e.g. "Plane", "Cylinder", "BSplineSurface", "BezierSurface")
    char SurfaceTypeName[32];
};

// Summary metadata for an individual body/solid within the STEP model
struct BodyMetadata {
    int32_t VertexCount;         // Number of unique 3D vertices
    int32_t TriangleCount;       // Number of triangles (face index triplets)
    int32_t FaceCount;           // Number of CAD faces / primitive surfaces
    int32_t IsSheetBody;         // 1 if open shell / sheet body, 0 if closed solid
    char Name[64];               // Body / solid name if available
};
#pragma pack(pop)

/**
 * Loads a STEP file, tessellates its boundary representation using Open CASCADE,
 * extracts analytic surface definitions, and deduplicates vertices.
 *
 * @param filePath UTF-8 encoded path to the .step / .stp file.
 * @param linearDeflection Maximum chordal deflection tolerance for tessellation (e.g. 0.1 mm).
 * @param angularDeflection Maximum angular deflection in radians (e.g. 0.5 rad ~ 28.6 deg).
 * @return Handle to the extracted model, or NULL if loading/meshing failed.
 */
TVGL_STEP_API StepModelHandle StepModel_Load(
    const char* filePath,
    double linearDeflection,
    double angularDeflection
);

/**
 * Returns the number of distinct bodies/solids contained in the loaded model.
 */
TVGL_STEP_API int32_t StepModel_GetBodyCount(StepModelHandle handle);

/**
 * Retrieves summary metadata (counts and properties) for the specified body.
 *
 * @param handle The StepModelHandle.
 * @param bodyIndex 0-based index of the body.
 * @param outMetadata Pointer to BodyMetadata struct to populate.
 * @return true on success, false if bodyIndex is out of range or handle is invalid.
 */
TVGL_STEP_API bool StepModel_GetBodyMetadata(
    StepModelHandle handle,
    int32_t bodyIndex,
    BodyMetadata* outMetadata
);

/**
 * Copies the vertex, triangle index, and face primitive data for a given body
 * into caller-allocated arrays.
 *
 * @param handle The StepModelHandle.
 * @param bodyIndex 0-based index of the body.
 * @param outVertices Array of size [VertexCount * 3] doubles (X, Y, Z coordinates).
 * @param outTriangles Array of size [TriangleCount * 3] int32_t vertex indices (0-based).
 * @param outFaces Array of size [FaceCount] FacePrimitiveData structs.
 * @return true on success, false otherwise.
 */
TVGL_STEP_API bool StepModel_CopyBodyData(
    StepModelHandle handle,
    int32_t bodyIndex,
    double* outVertices,
    int32_t* outTriangles,
    FacePrimitiveData* outFaces
);

/**
 * Frees all memory and Open CASCADE resources associated with the loaded model.
 */
TVGL_STEP_API void StepModel_Free(StepModelHandle handle);

/**
 * Creates a valid test STEP file containing a box with a cylindrical hole.
 * Used for automated verification and unit tests.
 */
TVGL_STEP_API bool StepModel_CreateTestStepFile(const char* filePath);

#ifdef __cplusplus
}
#endif

#endif // TVGL_STEP_BRIDGE_H
