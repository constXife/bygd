using UnityEngine;

namespace Bygd.Framework
{
    internal static class ObjectFinder
    {
        private static readonly Collider[] s_overlapBuffer = new Collider[128];

        /// <summary>
        /// Finds the nearest component T within radius from position.
        /// Uses FindObjectsOfType — for infrequent calls (not every frame).
        /// </summary>
        public static T FindNearest<T>(Vector3 position, float radius) where T : Component
        {
            T closest = null;
            float closestDist = radius;

            foreach (var comp in Object.FindObjectsOfType<T>())
            {
                if (comp == null) continue;
                float dist = Vector3.Distance(comp.transform.position, position);
                if (dist < closestDist)
                {
                    closest = comp;
                    closestDist = dist;
                }
            }

            return closest;
        }

        /// <summary>
        /// Finds the nearest component T via Physics.OverlapSphere (faster for frequent calls).
        /// </summary>
        public static T FindNearestByPhysics<T>(Vector3 position, float radius) where T : Component
        {
            int hits = Physics.OverlapSphereNonAlloc(position, radius, s_overlapBuffer);
            T closest = null;
            float closestDist = radius;

            for (int i = 0; i < hits; i++)
            {
                var col = s_overlapBuffer[i];
                if (col == null) continue;

                var comp = col.GetComponentInParent<T>();
                if (comp == null) continue;

                float dist = Vector3.Distance(comp.transform.position, position);
                if (dist < closestDist)
                {
                    closest = comp;
                    closestDist = dist;
                }
            }

            return closest;
        }

        /// <summary>
        /// Checks whether a component T exists within radius via Physics.OverlapSphere.
        /// </summary>
        public static bool HasNearby<T>(Vector3 position, float radius) where T : Component
        {
            int hits = Physics.OverlapSphereNonAlloc(position, radius, s_overlapBuffer);
            for (int i = 0; i < hits; i++)
            {
                if (s_overlapBuffer[i] != null && s_overlapBuffer[i].GetComponentInParent<T>() != null)
                    return true;
            }
            return false;
        }
    }
}
