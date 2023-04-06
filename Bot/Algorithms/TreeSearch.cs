using System;
using System.Collections.Generic;
using System.Linq;

namespace Bot.Algorithms;

public static class TreeSearch {
    /// <summary>
    /// Performs a breadth first search starting from the given node and returns the list of nodes that were searched for.
    /// </summary>
    /// <param name="startingNode">The node to start the search from.</param>
    /// <param name="getNodeNeighbors">A functor that should return all the neighbors to explore of a given node.</param>
    /// <param name="isTerminalNode">A functor that determines if we should stop exploring the given node.</param>
    /// <typeparam name="TNode">The type of the node</typeparam>
    /// <returns>The list of all the target nodes.</returns>
    public static IEnumerable<TNode> BreathFirstSearch<TNode>(
        TNode startingNode,
        Func<TNode, IEnumerable<TNode>> getNodeNeighbors,
        Func<TNode, bool> isTerminalNode
    ) {
        var explored = new HashSet<TNode>();
        var explorationQueue = new Queue<TNode>();
        explorationQueue.Enqueue(startingNode);

        while (explorationQueue.Any()) {
            var node = explorationQueue.Dequeue();
            explored.Add(node);

            yield return node;

            if (isTerminalNode(node)) {
                continue;
            }

            var nodeNeighbors = getNodeNeighbors(node)
                .Where(neighbor => !explored.Contains(neighbor));

            foreach (var nodeNeighbor in nodeNeighbors) {
                explorationQueue.Enqueue(nodeNeighbor);
            }
        }
    }

    /// <summary>
    /// Performs a depth first search starting from the given node and returns the list of nodes that were searched for.
    /// </summary>
    /// <param name="startingNode">The node to start the search from.</param>
    /// <param name="getNodeNeighbors">A functor that should return all the neighbors to explore of a given node.</param>
    /// <param name="isTerminalNode">A functor that determines if we should stop exploring the given node.</param>
    /// <typeparam name="TNode">The type of the node</typeparam>
    /// <returns>The list of all the target nodes.</returns>
    public static IEnumerable<TNode> DepthFirstSearch<TNode>(
        TNode startingNode,
        Func<TNode, IEnumerable<TNode>> getNodeNeighbors,
        Func<TNode, bool> isTerminalNode
    ) {
        var explored = new HashSet<TNode>();
        var explorationStack = new Stack<TNode>();
        explorationStack.Push(startingNode);

        while (explorationStack.Any()) {
            var node = explorationStack.Pop();
            explored.Add(node);

            yield return node;

            if (isTerminalNode(node)) {
                continue;
            }

            var nodeNeighbors = getNodeNeighbors(node)
                .Where(neighbor => !explored.Contains(neighbor));

            foreach (var nodeNeighbor in nodeNeighbors) {
                explorationStack.Push(nodeNeighbor);
            }
        }
    }
}
