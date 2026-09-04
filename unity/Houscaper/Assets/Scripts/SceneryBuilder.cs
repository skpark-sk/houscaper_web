using UnityEngine;

namespace Houscaper
{
    /// <summary>Static backdrop geometry: the island, the sea, the sky dome and the build grid.</summary>
    public static class SceneryBuilder
    {
        const int Segments = 96;
        const float CliffDepth = 2.6f;
        const float IslandTop = 0f;

        static float RimRadius(float angle)
        {
            // A softly irregular outline keeps the island from reading as a plain disc.
            float wobble = Mathf.Sin(angle * 3f) * 0.55f
                         + Mathf.Sin(angle * 5f + 1.7f) * 0.34f
                         + Mathf.Sin(angle * 8f + 4.1f) * 0.18f;
            return (VoxelWorld.Radius + 2.6f) + wobble;
        }

        public static Mesh BuildIsland()
        {
            var mesh = new Mesh { name = "Island" };
            var m = new MeshData();

            var rim = new Vector3[Segments];
            var foot = new Vector3[Segments];

            for (int i = 0; i < Segments; i++)
            {
                float angle = i / (float)Segments * Mathf.PI * 2f;
                float radius = RimRadius(angle);

                rim[i] = new Vector3(Mathf.Cos(angle) * radius, IslandTop, Mathf.Sin(angle) * radius);
                foot[i] = new Vector3(
                    Mathf.Cos(angle) * radius * 0.62f,
                    -CliffDepth,
                    Mathf.Sin(angle) * radius * 0.62f);
            }

            var centre = new Vector3(0f, IslandTop, 0f);
            var keel = new Vector3(0f, -CliffDepth - 0.5f, 0f);

            for (int i = 0; i < Segments; i++)
            {
                int j = (i + 1) % Segments;

                m.AddTriangleOriented(centre, rim[i], rim[j], Vector3.up, Role.Stone);

                var outward = new Vector3(rim[i].x, 0f, rim[i].z).normalized;
                m.AddQuadOriented(rim[i], rim[j], foot[j], foot[i], outward, Role.Stone);

                m.AddTriangleOriented(keel, foot[i], foot[j], Vector3.down, Role.Stone);
            }

            float outerRadius = VoxelWorld.Radius + 2.6f;
            m.ColorOverride = (_, v) =>
            {
                if (v.y > IslandTop - 0.01f)
                {
                    float t = Mathf.InverseLerp(outerRadius, VoxelWorld.Radius - 1f, new Vector2(v.x, v.z).magnitude);
                    return Color.Lerp(Palette.GrassDark, Palette.Grass, Mathf.Clamp01(t));
                }

                float depth = Mathf.InverseLerp(IslandTop, -CliffDepth, v.y);
                return Color.Lerp(Palette.Cliff, Palette.CliffDark, depth);
            };

            m.Upload(mesh);
            return mesh;
        }

        public static Mesh BuildWater()
        {
            var mesh = new Mesh { name = "Water" };
            var m = new MeshData();
            const float size = 300f;
            const float y = -0.6f;

            m.AddQuadOriented(
                new Vector3(-size, y, -size),
                new Vector3(-size, y, size),
                new Vector3(size, y, size),
                new Vector3(size, y, -size),
                Vector3.up, Role.Stone);

            m.ColorOverride = (_, __) => Palette.Water;
            m.Upload(mesh);
            return mesh;
        }

        /// <summary>Gradient dome drawn before the scene; the sky shader ignores culling.</summary>
        public static Mesh BuildSky()
        {
            var mesh = new Mesh { name = "Sky" };
            var m = new MeshData();

            const int rings = 12;
            const int sectors = 24;
            const float radius = 420f;

            for (int r = 0; r < rings; r++)
            {
                float p0 = Mathf.PI * 0.5f * (r / (float)rings);
                float p1 = Mathf.PI * 0.5f * ((r + 1) / (float)rings);

                for (int s = 0; s < sectors; s++)
                {
                    float a0 = Mathf.PI * 2f * (s / (float)sectors);
                    float a1 = Mathf.PI * 2f * ((s + 1) / (float)sectors);

                    m.AddQuad(
                        Dome(radius, p0, a0), Dome(radius, p0, a1),
                        Dome(radius, p1, a1), Dome(radius, p1, a0),
                        Vector3.up, Role.Stone);
                }
            }

            m.ColorOverride = (_, v) =>
            {
                float t = Mathf.Clamp01(v.y / (radius * 0.7f));
                return Color.Lerp(Palette.SkyHorizon, Palette.SkyTop, Mathf.Pow(t, 0.7f));
            };

            m.Upload(mesh);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
            return mesh;
        }

        static Vector3 Dome(float radius, float pitch, float yaw)
        {
            // Pitch 0 sits under the horizon so the sea line never shows a gap.
            float y = Mathf.Sin(pitch) * radius - radius * 0.26f;
            float ring = Mathf.Cos(pitch) * radius;
            return new Vector3(Mathf.Cos(yaw) * ring, y, Mathf.Sin(yaw) * ring);
        }

        /// <summary>Faint lines over the buildable footprint: readable, but quiet.</summary>
        public static Mesh BuildGrid()
        {
            var mesh = new Mesh { name = "Grid" };
            var m = new MeshData();

            const float y = IslandTop + 0.02f;
            const float halfWidth = 0.013f;
            float extent = (VoxelWorld.Radius + 0.5f) * VoxelWorld.CellSize;

            for (int i = -VoxelWorld.Radius; i <= VoxelWorld.Radius + 1; i++)
            {
                float c = (i - 0.5f) * VoxelWorld.CellSize;

                m.AddQuadOriented(
                    new Vector3(c - halfWidth, y, -extent), new Vector3(c - halfWidth, y, extent),
                    new Vector3(c + halfWidth, y, extent), new Vector3(c + halfWidth, y, -extent),
                    Vector3.up, Role.Stone);

                m.AddQuadOriented(
                    new Vector3(-extent, y, c - halfWidth), new Vector3(extent, y, c - halfWidth),
                    new Vector3(extent, y, c + halfWidth), new Vector3(-extent, y, c + halfWidth),
                    Vector3.up, Role.Stone);
            }

            m.ColorOverride = (_, __) => Palette.GridLine;
            m.Upload(mesh);
            return mesh;
        }

        /// <summary>Translucent cube used to preview where the next block lands.</summary>
        public static Mesh BuildGhost()
        {
            var mesh = new Mesh { name = "Ghost" };
            var m = new MeshData();

            // The pick volume of a corner is the unit cube centred on it.
            const float inset = 0.03f;
            float hx = VoxelWorld.CellSize * 0.5f - inset;
            float hy = VoxelWorld.LevelHeight * 0.5f - inset;

            m.AddBox(new Vector3(-hx, -hy, -hx), new Vector3(hx, hy, hx), Role.Wall);

            m.ColorOverride = (_, __) => new Color(1f, 1f, 1f, 0.34f);
            m.Upload(mesh);
            return mesh;
        }
    }
}
