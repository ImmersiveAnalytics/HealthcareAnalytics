﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ChartAndGraph
{

    /// <summary>
    /// this class is used internally in order to draw lines, line fill and line points into a mesh
    /// </summary>
    public class CanvasLines : MaskableGraphic
    {
        public float Thickness = 2f;
        public float Tiling = 1;
        bool mFillRender = false;
        bool mPointRender = false;
        float mPointSize = 5f;
        Rect mFillRect;
        bool mStretchY;
        Material mCachedMaterial;


        /// <summary>
        /// Sets point render mode
        /// </summary>
        /// <param name="pointSize"></param>
        public void MakePointRender(float pointSize)
        {
            mPointSize = pointSize;
            mPointRender = true;
        }

        /// <summary>
        /// sets inner fill render mode
        /// </summary>
        /// <param name="fillRect"></param>
        /// <param name="stretchY"></param>
        public void MakeFillRender(Rect fillRect,bool stretchY)
        {
            mFillRect = fillRect;
            mFillRender = true;
            mStretchY = stretchY;
        }

        UIVertex[] mTmpVerts = new UIVertex[4];
        /// <summary>
        /// holds line data and pre cacultates normal and speration
        /// </summary>
        internal struct Line
        {
            public Line(Vector3 from,Vector3 to,float halfThickness,bool hasNext,bool hasPrev) : this()
            {

                Vector3 diff = (to - from);
                float magDec = 0;
                if (hasNext)
                    magDec += halfThickness;
                if (hasPrev)
                    magDec += halfThickness;
                Mag = diff.magnitude - magDec*2;
                Degenerated = false;
                if (Mag <= 0)
                    Degenerated = true;
                Dir = diff.normalized;
                Vector3 add = halfThickness*2 * Dir;
                if(hasPrev)
                    from += add;
                if(hasNext)
                    to -= add;
                From = from;
                To = to;
                Normal = new Vector3(Dir.y, -Dir.x, Dir.z);
                P1 = From + Normal * halfThickness;
                P2 = from - Normal * halfThickness;
                P3 = to + Normal * halfThickness;
                P4 = to  - Normal * halfThickness;
            }

            public bool Degenerated { get; private set; }
            public Vector3 P1 { get; private set; }
            public Vector3 P2 { get; private set; }
            public Vector3 P3 { get; private set; }
            public Vector3 P4 { get; private set; }

            public Vector3 From { get; private set; }
            public Vector3 To { get; private set; }
            public Vector3 Dir { get; private set; }
            public float Mag { get; private set; }
            public Vector3 Normal { get; private set; }
        }

        /// <summary>
        /// represents one line segemenet.
        /// </summary>
        internal class LineSegement
        {
            Vector3[] mLines;
            public LineSegement(Vector3[] lines)
            {
                mLines = lines;
            }
            public int PointCount
            {
                get
                {
                    if (mLines == null)
                        return 0;
                    return mLines.Length;
                }
            }
            public int LineCount { get
                {
                    if (mLines == null)
                        return 0;
                    if (mLines.Length < 2)
                        return 0;
                    return mLines.Length - 1;
                }
            }
            public Vector3 getPoint(int index)
            {
                return mLines[index];
            }
            public void GetLine(int index, out Vector3 from,out Vector3 to)
            {
                from = mLines[index];
                to = mLines[index + 1];
            }
            public Line GetLine(int index,float halfThickness,bool hasPrev,bool hasNext)
            {
                Vector3 from = mLines[index];
                Vector3 to = mLines[index + 1];
                return new Line(from, to, halfThickness,false,false);
            }
        }

        List<LineSegement> mLines;

        public CanvasLines()
        {

        }

        /// <summary>
        /// sets the lines for this renderer
        /// </summary>
        /// <param name="lines"></param>
        internal void SetLines(List<LineSegement> lines)
        {
            mLines = lines;
            SetAllDirty();
            Rebuild(CanvasUpdate.PreRender);
        }

        protected override void UpdateMaterial()
        {
            base.UpdateMaterial();
            canvasRenderer.SetTexture(material.mainTexture);
        }

        void GetSide(Vector3 point, Vector3 dir,Vector3 normal,float dist,float size,float z,out Vector3 p1,out Vector3 p2)
        {
            point.z = z;
            point += dir * dist;
            normal *= size;
            p1 = point + normal;
            p2 = point - normal;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ChartCommon.SafeDestroy(mCachedMaterial);
        }

        public override Material material
        {
            get
            {  
                return base.material;
            }
             
            set
            {
                ChartCommon.SafeDestroy(mCachedMaterial);
                if (value == null)
                { 
                    mCachedMaterial = null;
                    base.material = null;
                    return;
                }
                mCachedMaterial = new Material(value);
                mCachedMaterial.hideFlags = HideFlags.DontSave;
                if (mCachedMaterial.HasProperty("_ChartTiling"))
                    mCachedMaterial.SetFloat("_ChartTiling", Tiling);
                base.material = mCachedMaterial;
            }
        }

        protected void Update()
        {
            Material mat = material;
            if (mCachedMaterial != null && mat!=null && mCachedMaterial.HasProperty("_ChartTiling"))
            {
                if (mCachedMaterial != mat)
                    mCachedMaterial.CopyPropertiesFromMaterial(mat);
                mCachedMaterial.SetFloat("_ChartTiling", Tiling);
            }
        }

        IEnumerable<UIVertex> getDotVeritces()
        {
            if (mLines == null)
                yield break;
            float z = 0f;
            float halfSize = mPointSize * 0.5f;
            for (int i = 0; i < mLines.Count; ++i)
            {
                LineSegement seg = mLines[i];
                int total = seg.PointCount;
                for (int j = 0; j < total; ++j)
                {
                    Vector3 point = seg.getPoint(j);
                    Vector3 p1 = point + new Vector3(-halfSize, -halfSize, 0f);
                    Vector3 p2 = point + new Vector3(halfSize, -halfSize, 0f);
                    Vector3 p3 = point + new Vector3(-halfSize, halfSize, 0f);
                    Vector3 p4 = point + new Vector3(halfSize, halfSize, 0f);
                    Vector2 uv1 = new Vector2(0f, 0f);
                    Vector2 uv2 = new Vector2(1f, 0f);
                    Vector2 uv3 = new Vector2(0f, 1f);
                    Vector2 uv4 = new Vector2(1f, 1f);

                    UIVertex v1 = ChartCommon.CreateVertex(p1, uv1, z);
                    UIVertex v2 = ChartCommon.CreateVertex(p2, uv2, z);
                    UIVertex v3 = ChartCommon.CreateVertex(p3, uv3, z);
                    UIVertex v4 = ChartCommon.CreateVertex(p4, uv4, z);

                    yield return v1;
                    yield return v2;
                    yield return v3;
                    yield return v4;
                }
            }
        }
        IEnumerable<UIVertex> getFillVeritces()
        {
            if (mLines == null)
                yield break;
            float z = 0f;
            for (int i = 0; i < mLines.Count; ++i)
            {
                LineSegement seg = mLines[i];
                int totalLines = seg.LineCount;
                for (int j = 0; j < totalLines; ++j)
                {
                    Vector3 from;
                    Vector3 to;
                    seg.GetLine(j,out from, out to);
                    Vector3 fromBottom = from;
                    Vector3 toBottom = to;
                    fromBottom.y = mFillRect.yMin;
                    toBottom.y = mFillRect.yMin;

                    float fromV = 1f;
                    float toV = 1f;
                    if (mStretchY == false)
                    {
                        fromV = Mathf.Abs((from.y - mFillRect.yMin) / mFillRect.height);
                        toV = Mathf.Abs((to.y - mFillRect.yMin) / mFillRect.height);
                    }
                    float fromU = ((from.x - mFillRect.xMin) / mFillRect.width);
                    float toU = ((to.x - mFillRect.xMin) / mFillRect.width);
                    Vector2 uv1 = new Vector2(fromU, fromV);
                    Vector2 uv2 = new Vector2(toU, toV);
                    Vector2 uv3 = new Vector2(fromU, 0f);
                    Vector2 uv4 = new Vector2(toU, 0f);

                    UIVertex v1 = ChartCommon.CreateVertex(from, uv1, z);
                    UIVertex v2 = ChartCommon.CreateVertex(to, uv2, z);
                    UIVertex v3 = ChartCommon.CreateVertex(fromBottom, uv3, z);
                    UIVertex v4 = ChartCommon.CreateVertex(toBottom, uv4, z);

                    yield return v1;
                    yield return v2;
                    yield return v3;
                    yield return v4;
                }
            }
        }

        IEnumerable<UIVertex> getLineVertices()
        {
            if (mLines == null)
                yield break;
            float halfThickness = Thickness * 0.5f;
            float z = 0f;

            for (int i = 0; i < mLines.Count; ++i)
            {
                LineSegement seg = mLines[i];
                int totalLines = seg.LineCount;
                Line? peek = null;
                Line? prev = null;
                float tileUv = 0f;
                float totalUv = 0f;
                for (int j = 0; j < totalLines; ++j)
                {
                    Line line = seg.GetLine(j, halfThickness, false, false);
                    totalUv += line.Mag;
                }
                for (int j = 0; j < totalLines; ++j)
                {
                    Line line;
                    bool hasNext = j + 1 < totalLines;
                    if (peek.HasValue)
                        line = peek.Value;
                    else
                        line = seg.GetLine(j, halfThickness, prev.HasValue, hasNext);
                    peek = null;
                    if (j + 1 < totalLines)
                        peek = seg.GetLine(j + 1, halfThickness, true, j + 2 < totalLines);

                    Vector3 p1 = line.P1;
                    Vector3 p2 = line.P2;
                    Vector3 p3 = line.P3;
                    Vector3 p4 = line.P4;

                    Vector2 uv1 = new Vector2(tileUv * Tiling, 0f);
                    Vector2 uv2 = new Vector2(tileUv * Tiling, 1f);
                    tileUv += line.Mag / totalUv;

                    Vector2 uv3 = new Vector2(tileUv * Tiling, 0f);
                    Vector2 uv4 = new Vector2(tileUv * Tiling, 1f);

                    UIVertex v1 = ChartCommon.CreateVertex(p1, uv1, z);
                    UIVertex v2 = ChartCommon.CreateVertex(p2, uv2, z);
                    UIVertex v3 = ChartCommon.CreateVertex(p3, uv3, z);
                    UIVertex v4 = ChartCommon.CreateVertex(p4, uv4, z);

                    yield return v1;
                    yield return v2;
                    yield return v3;
                    yield return v4;

                    if (peek.HasValue)
                    {
                        float myZ = z + 0.2f;
                        Vector3 a1, a2;
                        GetSide(line.To, line.Dir, line.Normal, halfThickness * 0.5f, halfThickness * 0.6f, v3.position.z, out a1, out a2);
                        yield return v3;
                        yield return v4;
                        yield return ChartCommon.CreateVertex(a1, v3.uv0, myZ);
                        yield return ChartCommon.CreateVertex(a2, v4.uv0, myZ);
                    }
                    if (prev.HasValue)
                    {
                        float myZ = z + 0.2f;
                        Vector3 a1, a2;
                        GetSide(line.From, -line.Dir, line.Normal, halfThickness * 0.5f, halfThickness * 0.6f, v1.position.z, out a1, out a2);
                        yield return ChartCommon.CreateVertex(a1, v1.uv0, myZ);
                        yield return ChartCommon.CreateVertex(a2, v2.uv0, myZ);
                        yield return v1;
                        yield return v2;
                    }
                    z -= 0.05f;
                    prev = line;
                }
            }
        }

        IEnumerable<UIVertex> getVerices()
        {
            if (mPointRender)
                return getDotVeritces();
            if(mFillRender)
                return getFillVeritces();
            return getLineVertices();
        }

        #if (!UNITY_5_2_0) && (!UNITY_5_2_1)
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);
            vh.Clear();
            int vPos = 0;
            foreach (UIVertex v in getVerices())
            {
                mTmpVerts[vPos++] = v;
                if (vPos == 4)
                {
                    UIVertex tmp = mTmpVerts[2];
                    mTmpVerts[2] = mTmpVerts[3];
                    mTmpVerts[3] = tmp;
                    vPos = 0;
                    vh.AddUIVertexQuad(mTmpVerts);
                }
            }
        }
        #endif
#pragma warning disable 0672

        protected override void OnPopulateMesh(Mesh m)
        {
            WorldSpaceChartMesh mesh = new WorldSpaceChartMesh(1);
            int vPos = 0;
            foreach (UIVertex v in getVerices())
            {
                mTmpVerts[vPos++] = v;
                if(vPos == 4)
                {
                    vPos = 0;
                    
                    mesh.AddQuad(mTmpVerts[0], mTmpVerts[1], mTmpVerts[2], mTmpVerts[3]);
                }
            }
            mesh.ApplyToMesh(m);
        }
#pragma warning restore 0672

    }
}
