using System.Collections.Generic;
using UnityEngine;

namespace Bygd
{
    internal struct BlueprintPiece
    {
        public string PrefabName;
        public string Category;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public string AdditionalInfo;
    }

    internal class TransformedPiece
    {
        public Vector3 WorldPos;
        public Quaternion WorldRot;
        public BlueprintPiece Piece;
    }

    internal class BlueprintData
    {
        public string Name;
        public string Creator;
        public string Description;
        public string BlueprintCategory;
        public List<BlueprintPiece> Pieces;

        public List<BlueprintPiece> GetBuildOrder()
        {
            var sorted = new List<BlueprintPiece>(Pieces);
            sorted.Sort((a, b) =>
            {
                int cmp = a.Position.y.CompareTo(b.Position.y);
                if (cmp != 0) return cmp;
                cmp = a.Position.x.CompareTo(b.Position.x);
                if (cmp != 0) return cmp;
                return a.Position.z.CompareTo(b.Position.z);
            });
            return sorted;
        }

        public List<TransformedPiece> Transform(Vector3 anchor, float yRotation)
        {
            var result = new List<TransformedPiece>();
            var anchorRot = Quaternion.Euler(0f, yRotation, 0f);
            Vector3 center = GetFootprintCenter();

            // The minimum blueprint Y is treated as ground level.
            float minY = GetMinY();

            foreach (var piece in Pieces)
            {
                // Offset from the footprint center while measuring Y from ground level.
                Vector3 offset = piece.Position - center;
                offset.y = piece.Position.y - minY;

                Vector3 worldPos = anchor + anchorRot * new Vector3(offset.x, 0f, offset.z);
                worldPos.y = anchor.y + offset.y;

                result.Add(new TransformedPiece
                {
                    WorldPos = worldPos,
                    WorldRot = anchorRot * piece.Rotation,
                    Piece = piece
                });
            }

            return result;
        }

        private float GetMinY()
        {
            float minY = float.MaxValue;
            foreach (var p in Pieces)
            {
                if (p.Position.y < minY)
                    minY = p.Position.y;
            }
            return minY;
        }

        private Vector3 GetFootprintCenter()
        {
            if (Pieces.Count == 0)
                return Vector3.zero;

            float minX = float.MaxValue, maxX = float.MinValue;
            float minZ = float.MaxValue, maxZ = float.MinValue;

            foreach (var p in Pieces)
            {
                if (p.Position.x < minX) minX = p.Position.x;
                if (p.Position.x > maxX) maxX = p.Position.x;
                if (p.Position.z < minZ) minZ = p.Position.z;
                if (p.Position.z > maxZ) maxZ = p.Position.z;
            }

            return new Vector3((minX + maxX) / 2f, 0f, (minZ + maxZ) / 2f);
        }
    }
}
