using System;
using System.Collections.Generic;
using System.Linq;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Elements;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using MP2.EXtensions;
using MP2.EXtensions.Tasks;

namespace MP2.Class
{
    public class TowerRunner
    {
        public static List<AtlasPanel2.AtlasNode> FindTowers()
        {
            var towerNodes = LokiPoe.InGameState.AtlasUi.AtlasNodes
                .Where(node => node.IsTower && node.IsCompleted && node.CanUseTablet)
                .ToList();

            List<AtlasPanel2.AtlasNode> result = new List<AtlasPanel2.AtlasNode>();

            if (towerNodes.Count == 0)
            {
                GlobalLog.Error("[CitadelPathFinder] Cant find completed towers.");
                return null;
            }

            foreach (var t in towerNodes)
            {
                var secondTowerNode = LokiPoe.InGameState.AtlasUi.AtlasNodes
                .Find(node => node.IsTower && node.IsCompleted && node.CanUseTablet && node.Coordinate.Distance(t.Coordinate) <= 6);

                if (secondTowerNode == null) continue;
                int MapFoundCount = 0;
                foreach (var node in LokiPoe.InGameState.AtlasUi.AtlasNodes)
                {
                    if (node.IsCompleted) continue;
                    if (node.IsTower) continue;
                    if (node.Coordinate.Distance(t.Coordinate) > 11) continue;
                    MapFoundCount++;
                }
                if(MapFoundCount >= 3)
                {
                    result.Add(t);
                    result.Add(secondTowerNode);
                    return result;
                }
            }

            int MapFoundCount2 = 0;
            foreach (var t in towerNodes)
            {
                foreach (var node in LokiPoe.InGameState.AtlasUi.AtlasNodes)
                {
                    if (node.IsCompleted) continue;
                    if (node.IsTower) continue;
                    if (node.Coordinate.Distance(t.Coordinate) > 11) continue;
                    MapFoundCount2++;
                }
                if (MapFoundCount2 >= 3)
                {
                    result.Add(t);
                    return result;
                }
                MapFoundCount2 = 0;
            }
            GlobalLog.Debug("Cant find Towers to run.");
            return null;
        }

        private static double GetDistance(Vector2i position1, Vector2i position2)
        {
            // Distância Euclidiana
            return Math.Sqrt(Math.Pow(position1.X - position2.X, 2) + Math.Pow(position1.Y - position2.Y, 2));
        }
    }

}
