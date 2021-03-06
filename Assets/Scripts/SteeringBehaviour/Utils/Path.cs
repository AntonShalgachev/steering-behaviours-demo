using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityPrototype
{
    public class Path : MonoBehaviour
    {
        private PathPoint[] m_cachedPoints = null;
        private PathPoint[] m_points
        {
            get
            {
                if (m_cachedPoints == null)
                    m_cachedPoints = FindPathPoints();
                return m_cachedPoints;
            }
        }

        public int pointsCount => m_points.Length;
        public PathPoint first => pointsCount > 0 ? m_points[0] : null;
        public PathPoint last => pointsCount > 0 ? m_points[pointsCount - 1] : null;

        private PathPoint[] FindPathPoints()
        {
            if (!gameObject.activeInHierarchy)
                return new PathPoint[] { };
            return GetComponentsInChildren<PathPoint>();
        }

        public Vector2 GetPoint(int index)
        {
            return m_points[index].transform.position;
        }

        public void DrawCurveGizmos(float pointRadius, float lastPointArrivalRadius, bool loop, int highlightedIndex)
        {
            var points = FindPathPoints();
            if (points.Length <= 0)
                return;

            var pointPositions = FindPathPoints().Select((PathPoint point) => (Vector2)point.transform.position);

            Gizmos.color = Color.red;
            GizmosHelper.DrawCurve(pointPositions, loop: loop);

            var pointIndex = 0;
            foreach (var point in pointPositions)
            {
                var isCurrent = pointIndex == highlightedIndex;
                Gizmos.color = isCurrent ? Color.blue : Color.red;
                GizmosHelper.DrawCircle(point, pointRadius);
                pointIndex++;
            }

            if (lastPointArrivalRadius > 0.0f)
            {
                Gizmos.color = Color.green;
                GizmosHelper.DrawCircle(points.Last().transform.position, lastPointArrivalRadius);
            }
        }
    }
}
