using System;
using System.Collections.Generic;
using UnityEngine;

namespace Houscaper
{
    /// <summary>Which palette slot a vertex takes its colour from at bake time.</summary>
    public enum Role : byte
    {
        Wall,
        WallShade,
        Trim,
        Roof,
        RoofShade,
        Glass,
        Door,
        Stone,
    }

    /// <summary>
    /// A CPU-side mesh under construction. Tiles are authored into one of these once at
    /// startup, then stamped into the world buffer with a rotation, offset, swatch and AO.
    /// </summary>
    public class MeshData
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Role> Roles = new List<Role>();
        public readonly List<float> Shades = new List<float>();
        public readonly List<byte> SwatchIds = new List<byte>();
        public readonly List<int> Triangles = new List<int>();

        /// <summary>
        /// When set, replaces swatch resolution with an explicit per-vertex colour. Scenery uses
        /// this since the island and sky have no building palette.
        /// </summary>
        public Func<int, Vector3, Color> ColorOverride;

        public int VertexCount => Vertices.Count;

        public void Clear()
        {
            Vertices.Clear();
            Normals.Clear();
            Roles.Clear();
            Shades.Clear();
            SwatchIds.Clear();
            Triangles.Clear();
        }

        /// <summary>Adds a quad wound a-b-c-d, counter-clockwise seen from the front.</summary>
        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Role role, float shade = 1f)
        {
            AddQuad(a, b, c, d, Vector3.Cross(b - a, c - a).normalized, role, shade);
        }

        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Role role, float shade = 1f)
        {
            int baseIndex = Vertices.Count;

            Vertices.Add(a); Vertices.Add(b); Vertices.Add(c); Vertices.Add(d);
            for (int i = 0; i < 4; i++)
            {
                Normals.Add(normal);
                Roles.Add(role);
                Shades.Add(shade);
                SwatchIds.Add(0);
            }

            Triangles.Add(baseIndex); Triangles.Add(baseIndex + 1); Triangles.Add(baseIndex + 2);
            Triangles.Add(baseIndex); Triangles.Add(baseIndex + 2); Triangles.Add(baseIndex + 3);
        }

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Role role, float shade = 1f)
        {
            AddTriangle(a, b, c, Vector3.Cross(b - a, c - a).normalized, role, shade);
        }

        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, Role role, float shade = 1f)
        {
            int baseIndex = Vertices.Count;

            Vertices.Add(a); Vertices.Add(b); Vertices.Add(c);
            for (int i = 0; i < 3; i++)
            {
                Normals.Add(normal);
                Roles.Add(role);
                Shades.Add(shade);
                SwatchIds.Add(0);
            }

            Triangles.Add(baseIndex); Triangles.Add(baseIndex + 1); Triangles.Add(baseIndex + 2);
        }


        /// <summary>
        /// Adds a quad whose front face is guaranteed to point along <paramref name="wantNormal"/>,
        /// reversing the winding when the corner order says otherwise. Keeps tile authoring
        /// free of winding bookkeeping.
        /// </summary>
        public void AddQuadOriented(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 wantNormal, Role role, float shade = 1f)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), wantNormal) < 0f)
            {
                AddQuad(a, d, c, b, wantNormal.normalized, role, shade);
            }
            else
            {
                AddQuad(a, b, c, d, wantNormal.normalized, role, shade);
            }
        }

        /// <summary>Vertical face on the z = <paramref name="z"/> plane looking down -Z.</summary>
        public void AddFaceNegZ(float x0, float x1, float y0, float y1, float z, Role role, float shade = 1f)
        {
            AddQuadOriented(
                new Vector3(x0, y0, z),
                new Vector3(x0, y1, z),
                new Vector3(x1, y1, z),
                new Vector3(x1, y0, z),
                Vector3.back, role, shade);
        }

        public void AddTriangleOriented(Vector3 a, Vector3 b, Vector3 c, Vector3 wantNormal, Role role, float shade = 1f)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), wantNormal) < 0f)
            {
                AddTriangle(a, c, b, wantNormal.normalized, role, shade);
            }
            else
            {
                AddTriangle(a, b, c, wantNormal.normalized, role, shade);
            }
        }

        /// <summary>Axis-aligned box between two corners, faces emitted outward.</summary>
        public void AddBox(Vector3 min, Vector3 max, Role role, float shade = 1f)
        {
            var a = new Vector3(min.x, min.y, min.z);
            var b = new Vector3(max.x, min.y, min.z);
            var c = new Vector3(max.x, min.y, max.z);
            var d = new Vector3(min.x, min.y, max.z);
            var e = new Vector3(min.x, max.y, min.z);
            var f = new Vector3(max.x, max.y, min.z);
            var g = new Vector3(max.x, max.y, max.z);
            var h = new Vector3(min.x, max.y, max.z);

            AddQuad(e, h, g, f, Vector3.up, role, shade);
            AddQuad(a, b, c, d, Vector3.down, role, shade * 0.86f);
            AddQuad(a, e, f, b, Vector3.back, role, shade);
            AddQuad(c, g, h, d, Vector3.forward, role, shade);
            AddQuad(d, h, e, a, Vector3.left, role, shade);
            AddQuad(b, f, g, c, Vector3.right, role, shade);
        }

        /// <summary>
        /// Stamps a tile into this buffer. <paramref name="ambientOcclusion"/> is sampled per
        /// vertex in world space so junctions between cells pick up contact shading.
        /// </summary>
        public void Append(
            MeshData tile,
            Quaternion rotation,
            Vector3 offset,
            byte swatchId,
            Func<Vector3, Vector3, float> ambientOcclusion = null)
        {
            int baseIndex = Vertices.Count;

            for (int i = 0; i < tile.Vertices.Count; i++)
            {
                var world = rotation * tile.Vertices[i] + offset;
                var normal = rotation * tile.Normals[i];
                Vertices.Add(world);
                Normals.Add(normal);
                Roles.Add(tile.Roles[i]);
                Shades.Add(tile.Shades[i] * (ambientOcclusion?.Invoke(world, normal) ?? 1f));
                SwatchIds.Add(swatchId);
            }

            for (int i = 0; i < tile.Triangles.Count; i++)
            {
                Triangles.Add(baseIndex + tile.Triangles[i]);
            }
        }

        /// <summary>
        /// Stamps a tile reflected across the x = z plane — the transform that swings the +X
        /// facade onto +Z without leaving the octant's own quadrant. A reflection reverses
        /// handedness, so the winding is flipped to keep faces pointing outward.
        /// </summary>
        public void AppendMirroredXZ(
            MeshData tile,
            Vector3 offset,
            byte swatchId,
            Func<Vector3, Vector3, float> ambientOcclusion = null)
        {
            int baseIndex = Vertices.Count;

            for (int i = 0; i < tile.Vertices.Count; i++)
            {
                var v = tile.Vertices[i];
                var n = tile.Normals[i];

                var world = new Vector3(v.z, v.y, v.x) + offset;
                var normal = new Vector3(n.z, n.y, n.x);

                Vertices.Add(world);
                Normals.Add(normal);
                Roles.Add(tile.Roles[i]);
                Shades.Add(tile.Shades[i] * (ambientOcclusion?.Invoke(world, normal) ?? 1f));
                SwatchIds.Add(swatchId);
            }

            for (int i = 0; i < tile.Triangles.Count; i += 3)
            {
                Triangles.Add(baseIndex + tile.Triangles[i]);
                Triangles.Add(baseIndex + tile.Triangles[i + 2]);
                Triangles.Add(baseIndex + tile.Triangles[i + 1]);
            }
        }

        /// <summary>Resolves roles and swatch ids to colours, then uploads to a Unity mesh.</summary>
        public void Upload(Mesh mesh)
        {
            var colors = new Color[Vertices.Count];
            for (int i = 0; i < colors.Length; i++)
            {
                var color = ColorOverride != null ? ColorOverride(i, Vertices[i]) : Resolve(Roles[i], SwatchIds[i]);
                float shade = Shades[i];
                colors[i] = new Color(color.r * shade, color.g * shade, color.b * shade, color.a);
            }

            mesh.Clear();
            mesh.indexFormat = Vertices.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(Triangles, 0);
            mesh.RecalculateBounds();
        }

        public static Color Resolve(Role role, byte swatchId)
        {
            var swatch = Palette.Get(swatchId);
            switch (role)
            {
                case Role.Wall:      return swatch.Wall;
                case Role.WallShade: return swatch.Wall * 0.88f;
                case Role.Trim:      return swatch.Trim;
                case Role.Roof:      return swatch.Roof;
                case Role.RoofShade: return swatch.Roof * 0.82f;
                case Role.Glass:     return swatch.Glass;
                case Role.Door:      return swatch.Roof * 0.72f;
                case Role.Stone:     return Palette.Cliff;
                default:             return swatch.Wall;
            }
        }
    }
}
