using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Bygd.Framework;
using UnityEngine;

namespace Bygd
{
    internal static class BlueprintParser
    {
        public static BlueprintData Parse(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Log.Error($"Blueprint not found: {filePath}");
                return null;
            }

            var data = new BlueprintData
            {
                Pieces = new List<BlueprintPiece>()
            };

            bool inPieces = false;

            foreach (string line in File.ReadAllLines(filePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("#"))
                {
                    ParseHeader(line, data, ref inPieces);
                    continue;
                }

                if (inPieces)
                {
                    var piece = ParsePieceLine(line);
                    if (piece.HasValue)
                        data.Pieces.Add(piece.Value);
                }
            }

            Log.Info($"Blueprint '{data.Name}' loaded: {data.Pieces.Count} pieces");
            return data;
        }

        private static void ParseHeader(string line, BlueprintData data, ref bool inPieces)
        {
            if (line.StartsWith("#Name:"))
                data.Name = line.Substring(6).Trim();
            else if (line.StartsWith("#Creator:"))
                data.Creator = line.Substring(9).Trim();
            else if (line.StartsWith("#Description:"))
                data.Description = line.Substring(13).Trim().Trim('"');
            else if (line.StartsWith("#Category:"))
                data.BlueprintCategory = line.Substring(10).Trim();
            else if (line == "#Pieces")
                inPieces = true;
            else if (line == "#SnapPoints" || line == "#Terrain")
                inPieces = false;
        }

        private static BlueprintPiece? ParsePieceLine(string line)
        {
            string[] f = line.Split(';');
            if (f.Length < 13)
            {
                Log.Error($"Invalid line format (expected 13 fields, got {f.Length}): {line}");
                return null;
            }

            return new BlueprintPiece
            {
                PrefabName = f[0],
                Category = f[1],
                Position = new Vector3(
                    Float(f[2]), Float(f[3]), Float(f[4])),
                Rotation = new Quaternion(
                    Float(f[5]), Float(f[6]), Float(f[7]), Float(f[8])),
                AdditionalInfo = f[9].Trim('"'),
                Scale = new Vector3(
                    Float(f[10]), Float(f[11]), Float(f[12]))
            };
        }

        private static float Float(string s)
        {
            float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float result);
            return result;
        }
    }
}
