using System.Collections;
using System.Collections.Generic;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    internal static class OutpostComfort
    {
        private const float ComfortRadius = 10f;

        public const int ComfortLevel1 = 4;   // roof + fire + bed + chair
        public const int ComfortLevel2 = 7;   // + table, banner, torches, carpet
        public const int ComfortLevel3 = 10;  // well-furnished house
        public const int ComfortLevel4 = 13;  // richly decorated

        public static int GetComfortAtPoint(Vector3 point, bool verbose = false)
        {
            if (!RoofCheck.HasRoofAbove(point))
            {
                if (verbose) Log.Info("Comfort: no roof");
                return 0;
            }

            if (Reflect.Piece_s_allComfortPieces == null)
            {
                if (verbose) Log.Info("Comfort: Piece_s_allComfortPieces reflection = null");
                return 0;
            }

            // s_allComfortPieces — HashSet<Piece>, not List<Piece>
            var rawValue = Reflect.Piece_s_allComfortPieces.GetValue(null);
            var allComfortPieces = rawValue as IEnumerable;
            if (allComfortPieces == null)
            {
                if (verbose) Log.Info($"Comfort: s_allComfortPieces cast failed, type={rawValue?.GetType()}");
                return 0;
            }

            if (verbose) Log.Info($"Comfort: s_allComfortPieces type={rawValue.GetType().Name}");

            var bestByGroup = new Dictionary<int, int>();
            int piecesChecked = 0;

            foreach (object obj in allComfortPieces)
            {
                Piece piece = obj as Piece;
                if (piece == null)
                    continue;

                piecesChecked++;

                if (piece.m_comfort <= 0)
                    continue;

                float dist = Vector3.Distance(point, piece.transform.position);
                if (dist > ComfortRadius)
                    continue;

                // m_comfortObject — if set, must be active (e.g., burning fire)
                if (piece.m_comfortObject != null && !piece.m_comfortObject.activeInHierarchy)
                    continue;

                int group = (int)piece.m_comfortGroup;
                int value = piece.m_comfort;

                if (!bestByGroup.TryGetValue(group, out int current) || value > current)
                    bestByGroup[group] = value;
            }

            int total = 1; // base comfort
            foreach (var kvp in bestByGroup)
                total += kvp.Value;

            if (verbose) Log.Info($"Comfort: checked={piecesChecked}, groups={bestByGroup.Count}, total={total}");
            return total;
        }

        public static int GetRequiredComfort(int targetLevel)
        {
            switch (targetLevel)
            {
                case 1: return ComfortLevel1;
                case 2: return ComfortLevel2;
                case 3: return ComfortLevel3;
                case 4: return ComfortLevel4;
                default: return 0;
            }
        }
    }
}
