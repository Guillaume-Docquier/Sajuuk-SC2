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
    /// <param name="isTerminalNode">A predicate that determines if we should stop exploring the given node.</param>
    /// <typeparam name="TNode">The type of the node</typeparam>
    /// <returns>The list of all the target nodes.</returns>
    public static IEnumerable<TNode> BreadthFirstSearch<TNode>(
        TNode startingNode,
        Func<TNode, IEnumerable<TNode>> getNodeNeighbors,
        Predicate<TNode> isTerminalNode
    ) {
        var explorationQueue = new Queue<TNode>();
        explorationQueue.Enqueue(startingNode);

        var explored = new HashSet<TNode> { startingNode };

        while (explorationQueue.Any()) {
            var node = explorationQueue.Dequeue();

            yield return node;

            if (isTerminalNode(node)) {
                continue;
            }

            var nodeNeighbors = getNodeNeighbors(node)
                .Where(neighbor => !explored.Contains(neighbor));

            foreach (var nodeNeighbor in nodeNeighbors) {
                explored.Add(nodeNeighbor);
                explorationQueue.Enqueue(nodeNeighbor);
            }
        }
    }

    /// <summary>
    /// Performs a depth first search starting from the given node and returns the list of nodes that were searched for.
    /// </summary>
    /// <param name="startingNode">The node to start the search from.</param>
    /// <param name="getNodeNeighbors">A functor that should return all the neighbors to explore of a given node.</param>
    /// <param name="isTerminalNode">A predicate that determines if we should stop exploring the given node.</param>
    /// <typeparam name="TNode">The type of the node</typeparam>
    /// <returns>The list of all the target nodes.</returns>
    public static IEnumerable<TNode> DepthFirstSearch<TNode>(
        TNode startingNode,
        Func<TNode, IEnumerable<TNode>> getNodeNeighbors,
        Predicate<TNode> isTerminalNode
    ) {
        var explorationStack = new Stack<TNode>();
        explorationStack.Push(startingNode);

        var explored = new HashSet<TNode> { startingNode };

        while (explorationStack.Any()) {
            var node = explorationStack.Pop();

            yield return node;

            if (isTerminalNode(node)) {
                continue;
            }

            var nodeNeighbors = getNodeNeighbors(node)
                .Where(neighbor => !explored.Contains(neighbor));

            foreach (var nodeNeighbor in nodeNeighbors) {
                explored.Add(nodeNeighbor);
                explorationStack.Push(nodeNeighbor);
            }
        }
    }
}
