﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ChartAndGraph.Axis
{
    /// <summary>
    /// generates an axis mesh from an AxisBase settings instance. 
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [ExecuteInEditMode]
    class AxisGenerator : MonoBehaviour, IAxisGenerator
    {
        MeshRenderer mRenderer;
        MeshFilter mFilter;
        Mesh mCleanMesh;
        List<BillboardText> mTexts;
        AxisBase mAxis;
        Material mDispose;
        Material mMaterial;
        float mTiling=1f;

        void Start()
        {

        }

        void OnDestroy()
        {
            ChartCommon.CleanMesh(null, ref mCleanMesh);
            ChartCommon.SafeDestroy(mDispose);
        }

        /// <summary>
        /// fix the labels after the axis data is updated
        /// </summary>
        /// <param name="parent"></param>
        public void FixLabels(AnyChart parent)
        {
            if (mAxis == null)
                return;
            for(int i=0; i<mTexts.Count; i++)
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

        /// <summary>
        /// used internally to get the tiling for a chart axis division
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="orientation"></param>
        /// <param name="inf"></param>
        /// <returns></returns>
        float GetTiling(AnyChart parent, ChartOrientation orientation, ChartDivisionInfo inf)
        {
            MaterialTiling tiling = inf.MaterialTiling;
            if (tiling.EnableTiling == false || tiling.TileFactor <= 0f)
                return 1f;
            float length = Math.Abs(ChartCommon.GetAutoLength(parent, orientation, inf));
            float backLength = ChartCommon.GetAutoLength(parent, orientation);
            float depth = ChartCommon.GetAutoDepth(parent, orientation, inf);
            if (inf.MarkBackLength.Automatic == false)
                backLength = inf.MarkBackLength.Value;
            if (backLength != 0 && depth > 0)
                length += Math.Abs(backLength) + Math.Abs(depth);
            return length / tiling.TileFactor;
        }

        /// <summary>
        /// sets the axis settings. Calling this method will cause the axisgenerator to create the axis mesh
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="axis"></param>
        /// <param name="axisOrientation"></param>
        /// <param name="isSubDivisions"></param>
        public void SetAxis(AnyChart parent,AxisBase axis,ChartOrientation axisOrientation, bool isSubDivisions)
        {
            WorldSpaceChartMesh mesh = new WorldSpaceChartMesh(2);
            mesh.Orientation = axisOrientation;
            mAxis = axis;
            if (isSubDivisions)
                axis.AddSubdivisionToChartMesh(parent, transform, mesh, axisOrientation);
            else
                axis.AddMainDivisionToChartMesh(parent, transform, mesh, axisOrientation);
            if (mesh.TextObjects != null)
            {
                foreach (BillboardText text in mesh.TextObjects)
                {
                    ((IInternalUse)parent).InternalTextController.AddText(text);
                }
            }
            mTexts = mesh.TextObjects;

            Mesh newMesh = mesh.Generate();
            newMesh.hideFlags = HideFlags.DontSave;
            if (mFilter == null)
                mFilter = GetComponent<MeshFilter>();
            mFilter.sharedMesh = newMesh;
            MeshCollider collider = GetComponent<MeshCollider>();
            if (collider != null)
                collider.sharedMesh = newMesh;
            ChartCommon.CleanMesh(newMesh, ref mCleanMesh);

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if(renderer != null)
            {
                Material m = mAxis.MainDivisions.Material;
                float tiling = GetTiling(parent, axisOrientation, mAxis.MainDivisions);
                if (isSubDivisions)
                {
                    m = mAxis.SubDivisions.Material;
                    tiling = GetTiling(parent, axisOrientation, mAxis.SubDivisions);
                }
                mMaterial = m;
                if (m != null)
                {
                    ChartCommon.SafeDestroy(mDispose);
                    mDispose = new Material(m);
                    mDispose.hideFlags = HideFlags.DontSave;
                    renderer.sharedMaterial = mDispose;
                    mTiling = tiling;
                    if (mDispose.HasProperty("_ChartTiling"))
                        mDispose.SetFloat("_ChartTiling", mTiling);
                }
            }
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

        public UnityEngine.Object This()
        {
            return this;
        }

        public GameObject GetGameObject()
        {
            return gameObject;
        }
    }
}
