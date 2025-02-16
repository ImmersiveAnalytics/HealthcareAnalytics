﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ChartAndGraph
{
    /// <summary>
    /// pie mesh creation class
    /// </summary>
    class PieMesh
    {
        public static void Generate3dMesh(WorldSpaceChartMesh mesh, float startAngle, float angleSpan, float radius, float innerRadius, int segments,float depth)
        {
            float halfDepth = depth * 0.5f;
            float segmentAngle = angleSpan / segments;
            float currentAngle = startAngle;
            float segmenUv = 1f / segments;
            float currentUv = 0f;
            float cos = Mathf.Cos(currentAngle);
            float sin = Mathf.Sin(currentAngle);

            UIVertex innerV = ChartCommon.CreateVertex(new Vector3(cos * innerRadius, sin * innerRadius, -halfDepth), new Vector2(currentUv, 0f));
            UIVertex outerV = ChartCommon.CreateVertex(new Vector3(cos * radius, sin * radius, -halfDepth), new Vector2(currentUv, 1f));

            int currentInner = mesh.AddVertex(innerV);
            int currentOuter = mesh.AddVertex(outerV);
            int prevInnerVertex = mesh.AddVertex(innerV);
            int prevOuterVertex = mesh.AddVertex(outerV);
            int prevOpeningVertex = mesh.AddVertex(innerV);
            int prevClosingVertex = mesh.AddVertex(outerV);
            innerV.position.z = halfDepth;
            outerV.position.z = halfDepth;

            int currentInnerDeep = mesh.AddVertex(innerV);
            int currentOuterDeep = mesh.AddVertex(outerV);
            int prevInnerVertexDeep = mesh.AddVertex(innerV);
            int prevOuterVertexDeep = mesh.AddVertex(outerV);

            mesh.AddTringle(currentInner, currentOuter,currentOuterDeep);
            mesh.AddTringle(currentOuterDeep, currentInnerDeep, currentInner);

            int prevOpeningVertexDeep = mesh.AddVertex(innerV);
            int prevClosingVertexDeep = mesh.AddVertex(outerV);

            for (int i = 1; i <= segments; i++)
            {
                currentUv += segmenUv;
                currentAngle += segmentAngle;
                cos = Mathf.Cos(currentAngle);
                sin = Mathf.Sin(currentAngle);

                UIVertex innerVertex = ChartCommon.CreateVertex(new Vector3(cos * innerRadius, sin * innerRadius, -halfDepth), new Vector2(currentUv, 0f));
                UIVertex outerVertex = ChartCommon.CreateVertex(new Vector3(cos * radius, sin * radius, -halfDepth), new Vector2(currentUv, 1f));

                int leftBottom = -1;
                int rightBottomAdded = -1;
                if (innerRadius > 0f)
                {
                    rightBottomAdded = mesh.AddVertex(innerVertex);
                    leftBottom = prevInnerVertex;
                }

                int leftTop = prevOuterVertex;
                int rightTop = mesh.AddVertex(outerVertex);
                int rightBottom = mesh.AddVertex(innerVertex);
                int rightTopAdded = mesh.AddVertex(outerVertex);
                
                innerVertex.position.z = halfDepth;
                outerVertex.position.z = halfDepth;

                int leftBottomDeep = -1;
                if (innerRadius > 0f)
                    leftBottomDeep = prevInnerVertexDeep;

                int leftTopDeep = prevOuterVertexDeep;
                int rightTopDeep = mesh.AddVertex(outerVertex);
                int rightBottomDeep = mesh.AddVertex(innerVertex);
                int rightTopAddedDeep = mesh.AddVertex(outerVertex);
                
                mesh.AddTringle(rightBottom, rightTop, leftTop);
                mesh.AddTringle(leftTopDeep, rightTopDeep, rightBottomDeep);

                mesh.AddTringle(prevClosingVertexDeep, prevClosingVertex, rightTopAdded);
                mesh.AddTringle(rightTopAdded, rightTopAddedDeep, prevClosingVertexDeep);

                prevClosingVertex = rightTopAdded;
                prevClosingVertexDeep = rightTopAddedDeep;

                if (innerRadius > 0f)
                {
                    int rightBottomAddedDeep = mesh.AddVertex(innerVertex);
                    mesh.AddTringle(leftTop, leftBottom, rightBottom);
                    mesh.AddTringle(rightBottomDeep, leftBottomDeep, leftTopDeep);

                    mesh.AddTringle(rightBottomAdded, prevOpeningVertex, prevOpeningVertexDeep);
                    mesh.AddTringle(prevOpeningVertexDeep, rightBottomAddedDeep, rightBottomAdded);
                    prevOpeningVertexDeep = rightBottomAddedDeep;
                    prevOpeningVertex = rightBottomAdded;
                }
                prevInnerVertex = rightBottom;
                prevOuterVertex = rightTop;
                prevInnerVertexDeep = rightBottomDeep;
                prevOuterVertexDeep = rightTopDeep;

                if(i==segments)
                {
                    rightTopDeep = mesh.AddVertex(outerVertex);
                    rightBottomDeep = mesh.AddVertex(innerVertex);
                    innerVertex.position.z = -halfDepth;
                    outerVertex.position.z = -halfDepth;
                    rightTop = mesh.AddVertex(outerVertex);
                    rightBottom = mesh.AddVertex(innerVertex);
                    mesh.AddTringle(rightTopDeep, rightTop, rightBottom);
                    mesh.AddTringle(rightBottom, rightBottomDeep, rightTopDeep);
                }
            }

        }

        public static void Generate2dMesh(IChartMesh mesh, float startAngle,float angleSpan,float radius,float innerRadius,int segments)
        {
            float segmentAngle = angleSpan / segments;
            float currentAngle = startAngle;
            float segmenUv = 1f / segments;
            float currentUv = 0f; 
            float cos = Mathf.Cos(currentAngle);
            float sin = Mathf.Sin(currentAngle);

            UIVertex prevInnerVertex = ChartCommon.CreateVertex(new Vector3(cos * innerRadius, sin * innerRadius, 0f), new Vector2(currentUv, 0f));
            UIVertex prevOuterVertex = ChartCommon.CreateVertex(new Vector3(cos * radius, sin * radius, 0f), new Vector2(currentUv, 1f));
            for (int i=1; i<segments+1; i++)
            {
                currentUv += segmenUv;
                currentAngle += segmentAngle;
                cos = Mathf.Cos(currentAngle);
                sin = Mathf.Sin(currentAngle);

                UIVertex innerVertex = ChartCommon.CreateVertex(new Vector3(cos * innerRadius, sin * innerRadius, 0f), new Vector2(currentUv, 0f));
                UIVertex outerVertex = ChartCommon.CreateVertex(new Vector3(cos * radius, sin * radius, 0f), new Vector2(currentUv, 1f));
                mesh.AddQuad(prevInnerVertex, innerVertex, prevOuterVertex, outerVertex);
                prevInnerVertex = innerVertex;
                prevOuterVertex = outerVertex;
            }
        }
    }
}
