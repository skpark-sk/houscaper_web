using System.Collections.Generic;
using UnityEngine;

namespace Houscaper
{
    /// <summary>How an exposed octant facade is dressed.</summary>
    public enum FaceStyle { Plain, Window, Door }

    /// <summary>
    /// The Houscaper octant tileset.
    ///
    /// Geometry is never emitted per voxel. Every set lattice corner owns the eight octants of
    /// the cubes around it, and each octant is shaped by three neighbouring corners — the two
    /// horizontal ones it faces (A and B) and the vertical one (Y) — plus the diagonal (D) that
    /// decides whether a roof plateau continues across the corner.
    ///
    /// Modules are authored in the canonical +X/+Z quadrant and placed with one of four yaws,
    /// which is what makes four octants tile a full corner without mirrored geometry.
    /// </summary>
    public class TileLibrary
    {
        public static readonly float HX = VoxelWorld.CellSize * 0.5f;
        public static readonly float HY = VoxelWorld.LevelHeight * 0.5f;
        public static readonly float HZ = VoxelWorld.CellSize * 0.5f;

        /// <summary>Ridge rise above the octant top, in world units.</summary>
        public const float RidgeRise = 0.44f;
        public const float Overhang = 0.15f;

        const float Proud = 0.05f;
        const float Reveal = 0.075f;
        const float BandHeight = 0.1f;

        /// <summary>Quadrants in yaw order: (+X,+Z), (+X,-Z), (-X,-Z), (-X,+Z).</summary>
        public static readonly Vector2Int[] Quadrants =
        {
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 1),
        };

        public static readonly Quaternion[] Yaw =
        {
            Quaternion.Euler(0f, 0f, 0f),
            Quaternion.Euler(0f, 90f, 0f),
            Quaternion.Euler(0f, 180f, 0f),
            Quaternion.Euler(0f, 270f, 0f),
        };

        /// <summary>World step that local +X maps to under each yaw. This is neighbour A.</summary>
        public static readonly Vector3Int[] AxisA =
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, 0, -1),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 0, 1),
        };

        /// <summary>World step that local +Z maps to under each yaw. This is neighbour B.</summary>
        public static readonly Vector3Int[] AxisB =
        {
            new Vector3Int(0, 0, 1),
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, 0, -1),
            new Vector3Int(-1, 0, 0),
        };

        readonly Dictionary<int, MeshData> _cache = new Dictionary<int, MeshData>();

        public MeshData Chimney { get; private set; }

        public TileLibrary()
        {
            Chimney = BuildChimney();
        }

        /// <summary>
        /// Fetches (building on first use) the octant module for one corner of one cube.
        /// </summary>
        /// <param name="upper">True for the four octants above the corner.</param>
        /// <param name="a">Corner one step along local +X is set.</param>
        /// <param name="b">Corner one step along local +Z is set.</param>
        /// <param name="y">Corner one step along the octant's vertical direction is set.</param>
        /// <param name="d">Diagonal corner (A + B) is set; only affects roof plateaus.</param>
        public MeshData Octant(bool upper, bool a, bool b, bool y, bool d, FaceStyle styleA, FaceStyle styleB)
        {
            int key = (upper ? 1 : 0)
                    | (a ? 2 : 0)
                    | (b ? 4 : 0)
                    | (y ? 8 : 0)
                    | (d ? 16 : 0)
                    | ((int)styleA << 5)
                    | ((int)styleB << 7);

            if (_cache.TryGetValue(key, out var cached)) return cached;

            var built = BuildOctant(upper, a, b, y, d, styleA, styleB);
            _cache[key] = built;
            return built;
        }

        // ── Octant assembly ─────────────────────────────────────────────────────

        static MeshData BuildOctant(bool upper, bool a, bool b, bool y, bool d, FaceStyle styleA, FaceStyle styleB)
        {
            var m = new MeshData();

            // Local vertical span. Upper octants run 0..HY, lower ones -HY..0.
            float yLow = upper ? 0f : -HY;
            float yHigh = upper ? HY : 0f;

            // Facades. The -X and -Z sides are shared with sibling octants of the same corner,
            // so only +X and +Z can ever be exposed.
            if (!a) Facade(m, yLow, yHigh, HX, styleA, upper, y, Axis.X);
            if (!b) Facade(m, yLow, yHigh, HZ, styleB, upper, y, Axis.Z);

            // A pillar wherever two exposed facades meet.
            if (!a && !b) CornerPost(m, yLow, yHigh);

            if (upper)
            {
                if (!y) Roof(m, a, b, d);
            }
            else
            {
                if (!y) Underside(m);
            }

            return m;
        }

        enum Axis { X, Z }

        /// <summary>
        /// One exposed octant facade. Authored on the +X plane and swung onto +Z when asked,
        /// so window and door layouts stay identical on both faces.
        /// </summary>
        static void Facade(MeshData m, float yLow, float yHigh, float extent, FaceStyle style, bool upper, bool yNeighbour, Axis axis)
        {
            var scratch = new MeshData();
            FacadeOnPlusX(scratch, yLow, yHigh, extent, style, upper, yNeighbour);

            if (axis == Axis.X)
            {
                m.Append(scratch, Quaternion.identity, Vector3.zero, 0);
            }
            else
            {
                // Reflecting across x = z lands the facade on +Z and, unlike a yaw, leaves the
                // octant inside its own quadrant.
                m.AppendMirroredXZ(scratch, Vector3.zero, 0);
            }
        }

        static void FacadeOnPlusX(MeshData m, float yLow, float yHigh, float x, FaceStyle style, bool upper, bool yNeighbour)
        {
            float z0 = 0f;
            float z1 = HZ;

            float fieldLow = yLow;
            float fieldHigh = yHigh;

            // A base band at the foot of the building and a cornice under the roof — not at
            // every storey, which is why they key off the vertical neighbour.
            if (!upper && !yNeighbour)
            {
                m.AddBox(new Vector3(x, yLow, z0), new Vector3(x + Proud, yLow + BandHeight, z1), Role.Trim);
                fieldLow = yLow + BandHeight;
            }

            if (upper && !yNeighbour)
            {
                m.AddBox(new Vector3(x, yHigh - BandHeight, z0), new Vector3(x + Proud, yHigh, z1), Role.Trim);
                fieldHigh = yHigh - BandHeight;
            }

            if (style == FaceStyle.Plain || fieldHigh - fieldLow < 0.22f)
            {
                PlusXQuad(m, x, fieldLow, fieldHigh, z0, z1, Role.Wall);
                return;
            }

            float inset = style == FaceStyle.Door ? 0.15f : 0.13f;
            float oz0 = z0 + inset;
            float oz1 = z1 - inset;
            float oy0 = style == FaceStyle.Door ? fieldLow : fieldLow + 0.09f;
            float oy1 = fieldHigh - 0.09f;

            // Wall field as four bands around the opening.
            PlusXQuad(m, x, fieldLow, oy0, z0, z1, Role.Wall);
            PlusXQuad(m, x, oy1, fieldHigh, z0, z1, Role.Wall);
            PlusXQuad(m, x, oy0, oy1, z0, oz0, Role.Wall);
            PlusXQuad(m, x, oy0, oy1, oz1, z1, Role.Wall);

            // Reveal walls of the recess.
            float xBack = x - Reveal;
            m.AddQuadOriented(
                new Vector3(x, oy0, oz0), new Vector3(x, oy1, oz0),
                new Vector3(xBack, oy1, oz0), new Vector3(xBack, oy0, oz0),
                new Vector3(0f, 0f, 1f), Role.WallShade, 0.9f);
            m.AddQuadOriented(
                new Vector3(x, oy0, oz1), new Vector3(x, oy1, oz1),
                new Vector3(xBack, oy1, oz1), new Vector3(xBack, oy0, oz1),
                new Vector3(0f, 0f, -1f), Role.WallShade, 0.9f);
            m.AddQuadOriented(
                new Vector3(x, oy1, oz0), new Vector3(x, oy1, oz1),
                new Vector3(xBack, oy1, oz1), new Vector3(xBack, oy1, oz0),
                Vector3.down, Role.WallShade, 0.76f);
            m.AddQuadOriented(
                new Vector3(x, oy0, oz0), new Vector3(x, oy0, oz1),
                new Vector3(xBack, oy0, oz1), new Vector3(xBack, oy0, oz0),
                Vector3.up, Role.WallShade);

            if (style == FaceStyle.Window)
            {
                PlusXQuad(m, xBack, oy0, oy1, oz0, oz1, Role.Glass);

                float xBar = xBack + 0.012f;
                float midZ = (oz0 + oz1) * 0.5f;
                float midY = (oy0 + oy1) * 0.5f;
                m.AddBox(new Vector3(xBar, oy0, midZ - 0.016f), new Vector3(xBar + 0.012f, oy1, midZ + 0.016f), Role.Trim);
                m.AddBox(new Vector3(xBar, midY - 0.016f, oz0), new Vector3(xBar + 0.012f, midY + 0.016f, oz1), Role.Trim);

                // Sill.
                m.AddBox(new Vector3(x - 0.01f, oy0 - 0.05f, oz0 - 0.045f), new Vector3(x + 0.065f, oy0, oz1 + 0.045f), Role.Trim);
            }
            else
            {
                PlusXQuad(m, xBack, oy0, oy1, oz0, oz1, Role.Door);

                m.AddBox(new Vector3(xBack, (oy0 + oy1) * 0.5f - 0.02f, oz1 - 0.07f),
                         new Vector3(xBack + 0.03f, (oy0 + oy1) * 0.5f + 0.02f, oz1 - 0.04f), Role.Trim);
                m.AddBox(new Vector3(x - 0.01f, oy1, oz0 - 0.05f), new Vector3(x + 0.05f, oy1 + 0.05f, oz1 + 0.05f), Role.Trim);
                m.AddBox(new Vector3(x - 0.01f, yLow, oz0 - 0.05f), new Vector3(x + 0.1f, yLow + 0.045f, oz1 + 0.05f), Role.Stone);
            }
        }

        static void PlusXQuad(MeshData m, float x, float y0, float y1, float z0, float z1, Role role)
        {
            if (y1 - y0 <= 0.0005f || z1 - z0 <= 0.0005f) return;

            m.AddQuadOriented(
                new Vector3(x, y0, z0), new Vector3(x, y1, z0),
                new Vector3(x, y1, z1), new Vector3(x, y0, z1),
                Vector3.right, role);
        }

        static void CornerPost(MeshData m, float yLow, float yHigh)
        {
            m.AddBox(
                new Vector3(HX - 0.11f, yLow, HZ - 0.11f),
                new Vector3(HX + Proud, yHigh, HZ + Proud),
                Role.Trim);
        }

        // ── Roof ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Roof height above the octant top at each of its four plan corners. The corner over the
        /// building's own axis is always at the ridge; the outer ones only rise when the run of
        /// corners continues, which is what turns clusters into hips, ridges and plateaus.
        /// </summary>
        public static void RoofCorners(bool a, bool b, bool d, out float h00, out float h10, out float h01, out float h11)
        {
            h00 = RidgeRise;
            h10 = a ? RidgeRise : 0f;
            h01 = b ? RidgeRise : 0f;
            h11 = a && b && d ? RidgeRise : 0f;
        }

        static void Roof(MeshData m, bool a, bool b, bool d)
        {
            RoofCorners(a, b, d, out float h00, out float h10, out float h01, out float h11);

            var p00 = new Vector3(0f, HY + h00, 0f);
            var p10 = new Vector3(HX, HY + h10, 0f);
            var p11 = new Vector3(HX, HY + h11, HZ);
            var p01 = new Vector3(0f, HY + h01, HZ);

            // Fan from the middle so a warped quad still meets its neighbours exactly at the edges.
            var centre = new Vector3(HX * 0.5f, HY + (h00 + h10 + h01 + h11) * 0.25f, HZ * 0.5f);

            m.AddTriangleOriented(centre, p00, p10, Vector3.up, Role.Roof);
            m.AddTriangleOriented(centre, p10, p11, Vector3.up, Role.Roof, 0.96f);
            m.AddTriangleOriented(centre, p11, p01, Vector3.up, Role.Roof, 0.92f);
            m.AddTriangleOriented(centre, p01, p00, Vector3.up, Role.Roof, 0.96f);

            // Eave boards on the open edges.
            if (!a) Eave(m, p10, p11, Vector3.right);
            if (!b) Eave(m, p11, p01, new Vector3(0f, 0f, 1f));
        }

        static void Eave(MeshData m, Vector3 inner0, Vector3 inner1, Vector3 outward)
        {
            const float drop = 0.055f;
            const float thickness = 0.08f;

            var offset = outward * Overhang - Vector3.up * drop;
            var outer0 = inner0 + offset;
            var outer1 = inner1 + offset;

            m.AddQuadOriented(inner0, inner1, outer1, outer0, Vector3.up, Role.Roof);

            var fascia0 = outer0 + Vector3.down * thickness;
            var fascia1 = outer1 + Vector3.down * thickness;
            m.AddQuadOriented(outer0, outer1, fascia1, fascia0, outward, Role.RoofShade);
            m.AddQuadOriented(fascia0, fascia1, inner1, inner0, Vector3.down, Role.RoofShade, 0.8f);
        }

        static void Underside(MeshData m)
        {
            m.AddQuadOriented(
                new Vector3(0f, -HY, 0f), new Vector3(HX, -HY, 0f),
                new Vector3(HX, -HY, HZ), new Vector3(0f, -HY, HZ),
                Vector3.down, Role.Trim, 0.82f);
        }

        static MeshData BuildChimney()
        {
            var m = new MeshData();
            m.AddBox(new Vector3(-0.1f, 0f, -0.1f), new Vector3(0.1f, 0.42f, 0.1f), Role.RoofShade);
            m.AddBox(new Vector3(-0.14f, 0.42f, -0.14f), new Vector3(0.14f, 0.5f, 0.14f), Role.Trim);
            return m;
        }
    }
}
