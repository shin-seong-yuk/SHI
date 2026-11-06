using System.Collections.Generic;
using UnityEngine;

namespace SHI.Client
{
    public static class Iso
    {
        public static Vector3 P(float tx, float ty, float h = 0f) => new Vector3((tx - ty) * 0.5f, -(tx + ty) * 0.25f + h, 0f);
        public static Vector2 ToTile(Vector2 world) => new Vector2(world.x - 2f * world.y, -2f * world.y - world.x);
        public static int Order(float tx, float ty) => 1000 + Mathf.RoundToInt((tx + ty) * 20f);
    }

    public sealed class MeshBuilder
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<Color32> colors = new List<Color32>();
        readonly List<int> tris = new List<int>();

        public void Clear() { verts.Clear(); colors.Clear(); tris.Clear(); }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col)
        {
            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            Color32 c32 = col;
            colors.Add(c32); colors.Add(c32); colors.Add(c32); colors.Add(c32);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2); tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
        }

        public void Tile(float x, float y, float w, float d, float h, Color col)
        {
            Quad(Iso.P(x, y, h), Iso.P(x + w, y, h), Iso.P(x + w, y + d, h), Iso.P(x, y + d, h), col);
        }

        public void Box(float x, float y, float w, float d, float z, float h, Color col)
        {
            float t = z + h;
            Quad(Iso.P(x + w, y, z), Iso.P(x + w, y + d, z), Iso.P(x + w, y + d, t), Iso.P(x + w, y, t), Shade(col, 0.62f));
            Quad(Iso.P(x, y + d, z), Iso.P(x + w, y + d, z), Iso.P(x + w, y + d, t), Iso.P(x, y + d, t), Shade(col, 0.85f));
            Tile(x, y, w, d, t, Shade(col, 1.25f));
        }

        public void FrontPanel(float x0, float x1, float y, float z0, float z1, Color col)
        {
            Quad(Iso.P(x0, y, z0), Iso.P(x1, y, z0), Iso.P(x1, y, z1), Iso.P(x0, y, z1), col);
        }

        public void SidePanel(float x, float y0, float y1, float z0, float z1, Color col)
        {
            Quad(Iso.P(x, y0, z0), Iso.P(x, y1, z0), Iso.P(x, y1, z1), Iso.P(x, y0, z1), col);
        }

        public void Rect2D(Vector3 center, float w, float h, Color col)
        {
            Quad(center + new Vector3(-w / 2, -h / 2), center + new Vector3(w / 2, -h / 2), center + new Vector3(w / 2, h / 2), center + new Vector3(-w / 2, h / 2), col);
        }

        public void Fan(Vector3 center, float rx, float ry, float start, float end, int steps, Color col)
        {
            int c = verts.Count;
            verts.Add(center);
            Color32 c32 = col;
            colors.Add(c32);
            for (int s = 0; s <= steps; s++)
            {
                float a = Mathf.Lerp(start, end, s / (float)steps);
                verts.Add(center + new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry));
                colors.Add(c32);
                if (s > 0) { tris.Add(c); tris.Add(c + s); tris.Add(c + s + 1); }
            }
        }

        public void Ring(Vector3 center, float rx, float ry, float start, float end, int steps, float width, Color col)
        {
            for (int s = 0; s < steps; s++)
            {
                float a = Mathf.Lerp(start, end, s / (float)steps), b = Mathf.Lerp(start, end, (s + 1) / (float)steps);
                Vector3 p = new Vector3(Mathf.Cos(a), Mathf.Sin(a)), q = new Vector3(Mathf.Cos(b), Mathf.Sin(b));
                Quad(center + Scale(p, rx - width, ry - width), center + Scale(q, rx - width, ry - width), center + Scale(q, rx + width, ry + width), center + Scale(p, rx + width, ry + width), col);
            }
        }

        static Vector3 Scale(Vector3 v, float x, float y) => new Vector3(v.x * x, v.y * y);

        public static Color Shade(Color c, float f) => new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f), c.a);

        public Mesh ToMesh(Mesh mesh = null)
        {
            if (mesh == null) { mesh = new Mesh(); mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; mesh.MarkDynamic(); }
            mesh.Clear();
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    public static class Art
    {
        static Material lit, glow;
        public static Material Lit => lit != null ? lit : lit = Resources.Load<Material>("SHI/Lit");
        public static Material Glow => glow != null ? glow : glow = Resources.Load<Material>("SHI/Glow");

        public static readonly Color Amber = new Color(1f, 0.70f, 0.32f);
        public static readonly Color Cyan = new Color(0.22f, 0.85f, 1f);
        public static readonly Color Pink = new Color(1f, 0.26f, 0.68f);
        public static readonly Color Lime = new Color(0.45f, 1f, 0.45f);
        public static readonly Color Red = new Color(1f, 0.16f, 0.14f);

        static Font font;
        public static Font Font
        {
            get
            {
                if (font == null) font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "맑은 고딕", "Noto Sans CJK KR", "Arial" }, 32);
                return font;
            }
        }

        public static MeshRenderer MakeRenderer(string name, Transform parent, Mesh mesh, Material material, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }
    }
}
