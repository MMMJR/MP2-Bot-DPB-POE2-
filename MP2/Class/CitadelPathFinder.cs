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
    public class CitadelPathFinder
    {
        private static readonly Vector2i RefugePosition = new Vector2i(0, 0); // Posição do centro (Refuge)

        public static List<AtlasPanel2.AtlasNode> FindPathToClosestCitadel(AtlasPanel2.AtlasNode fromNode)
        {
            // Filtrar nodes com "Citadel" no nome
            var citadelNodes = LokiPoe.InGameState.AtlasUi.AtlasNodes
                .Where(node => node.Area.Name.Contains("Citadel") && !node.IsCompleted && !node.IsVisited && !OpenMapTask.cachedCitadels.Contains(node.Coordinate))
                .ToList();

            if (citadelNodes.Count == 0)
            {
                GlobalLog.Error("[CitadelPathFinder] Nenhuma Citadel encontrada no AtlasNodes.");
                citadelNodes = LokiPoe.InGameState.AtlasUi.AtlasNodes
                .Where(node => node.Area.Name.Contains("Vaal") && !node.IsCompleted && !node.IsVisited && !OpenMapTask.cachedCitadels.Contains(node.Coordinate))
                .ToList();
                if (citadelNodes.Count == 0)
                {
                    GlobalLog.Error("[CitadelPathFinder] Nenhuma Vaal encontrada no AtlasNodes.");
                    return null;
                }
            }

            // Encontrar a Citadel mais próxima
            var closestCitadel = citadelNodes
                .OrderBy(node => GetDistance(node.Coordinate, fromNode.Coordinate))
                .FirstOrDefault();

            if (closestCitadel == null)
            {
                GlobalLog.Error("[CitadelPathFinder] Nenhuma Citadel válida encontrada.");
                return null;
            }

            GlobalLog.Debug($"[CitadelPathFinder] Citadel mais próxima encontrada: {closestCitadel.Area.Name} em {closestCitadel.Coordinate}");

            // Traçar o caminho até a Citadel mais próxima
            return TracePathToNode(closestCitadel, fromNode);
        }

        private static List<AtlasPanel2.AtlasNode> TracePathToNode(AtlasPanel2.AtlasNode targetNode, AtlasPanel2.AtlasNode fromNode)
        {
            var queue = new Queue<AtlasPanel2.AtlasNode>();
            var visited = new HashSet<AtlasPanel2.AtlasNode>();
            var parentMap = new Dictionary<AtlasPanel2.AtlasNode, AtlasPanel2.AtlasNode>();

            // Começar do centro (Refuge)
            var startNode = fromNode;

            if (startNode == null)
            {
                GlobalLog.Error("[CitadelPathFinder] Node 'Refuge' não encontrado.");
                return null;
            }

            queue.Enqueue(startNode);
            visited.Add(startNode);

            // BFS para encontrar o caminho
            while (queue.Count > 0)
            {
                var currentNode = queue.Dequeue();

                // Verificar se chegamos ao node desejado
                if (currentNode == targetNode)
                {
                    return ReconstructPath(parentMap, currentNode);
                }

                // Explorar conexões
                foreach (var connectedNode in currentNode.ConnectedNodes)
                {
                    if (!visited.Contains(connectedNode))
                    {
                        queue.Enqueue(connectedNode);
                        visited.Add(connectedNode);
                        parentMap[connectedNode] = currentNode;
                    }
                }
            }

            GlobalLog.Error("[CitadelPathFinder] Não foi possível traçar um caminho até a Citadel.");
            return null;
        }

        private static List<AtlasPanel2.AtlasNode> ReconstructPath(Dictionary<AtlasPanel2.AtlasNode, AtlasPanel2.AtlasNode> parentMap, AtlasPanel2.AtlasNode currentNode)
        {
            var path = new List<AtlasPanel2.AtlasNode>();

            while (currentNode != null)
            {
                path.Add(currentNode);
                parentMap.TryGetValue(currentNode, out currentNode);
            }

            path.Reverse();
            return path;
        }

        private static double GetDistance(Vector2i position1, Vector2i position2)
        {
            // Distância Euclidiana
            return Math.Sqrt(Math.Pow(position1.X - position2.X, 2) + Math.Pow(position1.Y - position2.Y, 2));
        }
    }

}
