﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ChartAndGraph.Axis
{
    /// <summary>
    /// Generates an axis mesh for a canvas chart
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    [ExecuteInEditMode]
    class CanvasAxisGenerator : Image, IAxisGenerator
    {
        MeshRenderer mRenderer;
        MeshFilter mFilter;
        Mesh mCleanMesh;
        List<BillboardText> mTexts;
        AxisBase mAxis;
        AnyChart mParent;
        ChartOrientation mOrientation;
        bool mIsSubDivisions;
        Material mDispose = null;
        Material mMaterial;
        float mTiling = 1f;

        #if (!UNITY_5_2_0) && (!UNITY_5_2_1)
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            base.OnPopulateMesh(vh);
            vh.Clear();
            if (mAxis == null || mParent == null)
                return;
            CanvasChartMesh mesh = new CanvasChartMesh(vh);
            AddToCanvasChartMesh(mesh);
        }
        #endif

        private void AddToCanvasChartMesh(CanvasChartMesh mesh)
        {
            mesh.Orientation = mOrientation;
            if (mIsSubDivisions)
                mAxis.AddSubdivisionToChartMesh(mParent, transform, mesh, mOrientation);
            else
                mAxis.AddMainDivisionToChartMesh(mParent, transform, mesh, mOrientation);
        }

#pragma warning disable 0672

        protected override void OnPopulateMesh(Mesh m)
        {
            m.Clear();
            if (mAxis == null || mParent == null)
                return;
            WorldSpaceChartMesh mesh = new WorldSpaceChartMesh(true);
            mesh.Orientation = mOrientation;
            if(mIsSubDivisions)
                mAxis.AddMainDivisionToChartMesh(mParent, transform, mesh, mOrientation);
            else
                mAxis.AddSubdivisionToChartMesh(mParent, transform, mesh, mOrientation);
            mesh.ApplyToMesh(m);
        }
#pragma warning restore 0672

        public void FixLabels(AnyChart parent)
        {
            if ((mAxis == null) || (mTexts == null))
                return;
            for (int i = 0; i < mTexts.Count; i++)
            {
                BillboardText text = mTexts[i];
                double min = ((IInternalUse)parent).InternalMinValue(mAxis);
                double max = ((IInternalUse)parent).InternalMaxValue(mAxis);
                if (text.UserData is AxisBase.TextData)
                {
                    AxisBase.TextData data = (AxisBase.TextData)text.UserData;
                    double newVal = min * (1.0 - (double)data.interp) + max * (double)data.interp;
                    string toSet = "";
                    if (mAxis.Format == AxisFormat.Number)
                        toSet = ChartAdancedSettings.Instance.FormatFractionDigits(data.fractionDigits, (float)newVal);
                    else
                    {
                        DateTime date = ChartDateUtility.ValueToDate(newVal);
                        if (mAxis.Format == AxisFormat.DateTime)
                            toSet = ChartDateUtility.DateToDateTimeString(date);
                        else
                        {
                            if (mAxis.Format == AxisFormat.Date)
                                toSet = ChartDateUtility.DateToDateString(date);
                            else
                                toSet = ChartDateUtility.DateToTimeString(date);
                        }

                    }
                    toSet = data.info.TextPrefix + toSet + data.info.TextSuffix;
                    text.UIText.text = toSet;
                }
            }
        }

        protected override void UpdateMaterial()
        {
            base.UpdateMaterial();
            if (material == null)
                return;
            if (material.mainTexture != null)
                canvasRenderer.SetTexture(material.mainTexture);
            canvasRenderer.SetColor(Color.white);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (mDispose != null)
                ChartCommon.SafeDestroy(mDispose);
        }

        float GetTiling(MaterialTiling tiling)
        {
            if (tiling.EnableTiling == false || tiling.TileFactor <= 0f)
                return 1f;
            ChartDivisionInfo inf = mAxis.MainDivisions;
            if (mIsSubDivisions)
                inf = mAxis.SubDivisions;
            float length = ChartCommon.GetAutoLength(mParent, mOrientation, inf);
            return length / tiling.TileFactor;
        }
        public void SetAxis(AnyChart parent, AxisBase axis, ChartOrientation axisOrientation,bool isSubDivisions)
        {
            raycastTarget = false;
            color = Color.white;
            mAxis = axis;
            mParent = parent;
            mIsSubDivisions = isSubDivisions;
            mOrientation = axisOrientation;

            CanvasChartMesh mesh = new CanvasChartMesh(true);

            if (mIsSubDivisions)
                mAxis.AddMainDivisionToChartMesh(mParent, transform, mesh, mOrientation);
            else
                mAxis.AddSubdivisionToChartMesh(mParent, transform, mesh, mOrientation);
            mTexts = mesh.TextObjects;
            if (mesh.TextObjects != null)
            {
                foreach (BillboardText text in mesh.TextObjects)
                {
                    ((IInternalUse)parent).InternalTextController.AddText(text);
                }
            }
            canvasRenderer.materialCount = 1;
            if (mDispose != null)
                ChartCommon.SafeDestroy(mDispose);
            float tiling = 1f;
            if (isSubDivisions)
            {
                if (axis.SubDivisions.Material != null)
                {
                    mDispose = new Material(mMaterial = axis.SubDivisions.Material);
                    mDispose.hideFlags = HideFlags.DontSave;
                    material = mDispose;
                    tiling = GetTiling(axis.SubDivisions.MaterialTiling);
                }
            }
            else
            {
                if (axis.MainDivisions.Material != null)
                {
                    mDispose = new Material(mMaterial = axis.MainDivisions.Material);
                    mDispose.hideFlags = HideFlags.DontSave;
                    material = mDispose;
                    tiling = GetTiling(axis.MainDivisions.MaterialTiling);
                }
            }
            mTiling = tiling;
            if(mDispose != null)
            {
                if (mDispose.HasProperty("_ChartTiling"))
                    mDispose.SetFloat("_ChartTiling", tiling);
            }
            SetAllDirty();
            Rebuild(CanvasUpdate.PreRender);
        }

        protected virtual void Update()
        {
            if (mMaterial != null && mDispose != null && mDispose.HasProperty("_ChartTiling"))
            {
                if (mDispose != mMaterial)
                    mDispose.CopyPropertiesFromMaterial(mMaterial);
                mDispose.SetFloat("_ChartTiling", mTiling);
            }
        }

        public GameObject GetGameObject()
        {
            return gameObject;
        }

        public UnityEngine.Object This()
        {
            return this;
        }
    }
}
