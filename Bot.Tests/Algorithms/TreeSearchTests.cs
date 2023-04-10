using Bot.Algorithms;

namespace Bot.Tests.Algorithms;

// ← ↑ → ↓
public class TreeSearchTests {
    [Fact]
    public void GivenAStartingIsolatedNode_WhenBFS_ThenReturnsOnlyThatNode() {
        // Arrange
        const int startingNode = 1;

        // 6←---   1   2
        // ↑   |       |
        // |   |       |
        // |   |       ↓
        // 5←------3--→4
        var directedGraph = new Dictionary<int, (bool isTerminal, List<int> neighbors)>
        {
            { 1, (false, new List<int> { }) },
            { 2, (false, new List<int> { 4 }) },
            { 3, (false, new List<int> { 4, 5, 6 }) },
            { 4, (false, new List<int> { }) },
            { 5, (false, new List<int> { 6 }) },
            { 6, (false, new List<int> { }) },
        };

        // Act
        var results = TreeSearch.BreadthFirstSearch(
            startingNode,
            node => directedGraph[node].neighbors,
            node => directedGraph[node].isTerminal
        ).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(startingNode, results[0]);
    }

    [Fact]
    public void GivenAStartingTerminalNode_WhenBFS_ThenReturnsOnlyThatNode() {
        // Arrange
        const int startingNode = 1;

        // 6←---   1--→2
        // ↑   |   |   |
        // |   |   |   |
        // |   |   ↓   ↓
        // 5←------3--→4
        var directedGraph = new Dictionary<int, (bool isTerminal, List<int> neighbors)>
        {
            { 1, (true , new List<int> { 2, 3 }) },
            { 2, (false, new List<int> { 4 }) },
            { 3, (false, new List<int> { 4, 5, 6 }) },
            { 4, (false, new List<int> { }) },
            { 5, (false, new List<int> { 6 }) },
            { 6, (false, new List<int> { }) },
        };

        // Act
        var results = TreeSearch.BreadthFirstSearch(
            startingNode,
            node => directedGraph[node].neighbors,
            node => directedGraph[node].isTerminal
        ).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(startingNode, results[0]);
    }

    [Fact]
    public void GivenDirectedGraph_WhenBFS_ThenReturnsInBreadthFirstOrder() {
        // Arrange
        const int startingNode = 1;

        // 6←---   1--→2
        // ↑   |   |   |
        // |   |   |   |
        // |   |   ↓   ↓
        // 5←------3--→4
        var directedGraph = new Dictionary<int, (bool isTerminal, List<int> neighbors)>
        {
            { 1, (false, new List<int> { 2, 3 }) },
            { 2, (false, new List<int> { 4 }) },
            { 3, (false, new List<int> { 4, 5, 6 }) },
            { 4, (false, new List<int> { }) },
            { 5, (false, new List<int> { 6 }) },
            { 6, (false, new List<int> { }) },
        };

        // Act
        var results = TreeSearch.BreadthFirstSearch(
            startingNode,
            node => directedGraph[node].neighbors,
            node => directedGraph[node].isTerminal
        ).ToList();

        // Assert
        var expected = new List<int> { 1, 2, 3, 4, 5, 6 };
        Assert.Equal(expected, results);
    }

    [Fact]
    public void GivenGraph_WhenBFS_ThenReturnsInBreadthFirstOrder() {
        // Arrange
        const int startingNode = 1;

        // 6←---   1←-→2
        // ↑   |   ↑   ↑
        // |   |   |   |
        // ↓   |   ↓   ↓
        // 5←-----→3←-→4
        var graph = new Dictionary<int, (bool isTerminal, List<int> neighbors)>
        {
            { 1, (false, new List<int> { 2, 3 }) },
            { 2, (false, new List<int> { 1, 4 }) },
            { 3, (false, new List<int> { 1, 4, 5, 6 }) },
            { 4, (false, new List<int> { 2, 3 }) },
            { 5, (false, new List<int> { 3, 6 }) },
            { 6, (false, new List<int> { 3, 5 }) },
        };

        // Act
        var results = TreeSearch.BreadthFirstSearch(
            startingNode,
            node => graph[node].neighbors,
            node => graph[node].isTerminal
        ).ToList();

        // Assert
        var expected = new List<int> { 1, 2, 3, 4, 5, 6 };
        Assert.Equal(expected, results);
    }

    [Fact]
    public void GivenDirectedGraphWithTerminalNodes_WhenBFS_ThenDoesntExplorePastTerminalNode() {
        // Arrange
        const int startingNode = 1;

        // 6←---   1--→2
        // ↑   |   |   |
        // |   |   |   |
        // |   |   ↓   ↓
        // 5←------3--→4
        var directedGraph = new Dictionary<int, (bool isTerminal, List<int> neighbors)>
        {
            { 1, (false, new List<int> { 2, 3 }) },
            { 2, (false, new List<int> { 4 }) },
            { 3, (true, new List<int> { 4, 5, 6 }) },
            { 4, (false, new List<int> { }) },
            { 5, (false, new List<int> { 6 }) },
            { 6, (false, new List<int> { }) },
        };

        // Act
        var results = TreeSearch.BreadthFirstSearch(
            startingNode,
            node => directedGraph[node].neighbors,
            node => directedGraph[node].isTerminal
        ).ToList();

        // Assert
        var expected = new List<int> { 1, 2, 3, 4 };
        Assert.Equal(expected, results);
    }

    [Fact]
    public void GivenGraphWithTerminalNodes_WhenBFS_ThenDoesntExplorePastTerminalNode() {
        // Arrange
        const int startingNode = 1;

        // 6←---   1←-→2
        // ↑   |   ↑   ↑
        // |   |   |   |
        // ↓   |   ↓   ↓
        // 5←-----→3←-→4
        var graph = new Dictionary<int, (bool isTerminal, List<int> neighbors)>
        {
            { 1, (false, new List<int> { 2, 3 }) },
            { 2, (false, new List<int> { 1, 4 }) },
            { 3, (true , new List<int> { 1, 4, 5, 6 }) },
            { 4, (false, new List<int> { 2, 3 }) },
            { 5, (false, new List<int> { 3, 6 }) },
            { 6, (false, new List<int> { 3, 5 }) },
        };

        // Act
        var results = TreeSearch.BreadthFirstSearch(
            startingNode,
            node => graph[node].neighbors,
            node => graph[node].isTerminal
        ).ToList();

        // Assert
        var expected = new List<int> { 1, 2, 3, 4 };
        Assert.Equal(expected, results);
    }

    [Fact]
    public void GivenAStartingIsolatedNode_WhenDFS_ThenReturnsOnlyThatNode() {
        // Arrange
        const int startingNode = 1;

        // 6←---   1   2
        // ↑   |       |
        // |   |       |
        // |   |       ↓
        // 5←------3--→4
        var directedGraph = new Dictionary<int, (bool isTerminal, List<int> neighbors)>
        {
            { 1, (false, new List<int> { }) },
            { 2, (false, new List<int> { 4 }) },
            { 3, (false, new List<int> { 4, 5, 6 }) },
            { 4, (false, new List<int> { }) },
            { 5, (false, new List<int> { 6 }) },
            { 6, (false, new List<int> { }) },
        };

        // Act
        var results = TreeSearch.DepthFirstSearch(
            startingNode,
            node => directedGraph[node].neighbors,
            node => directedGraph[node].isTerminal
        ).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(startingNode, results[0]);
    }

    [Fact]
    public void GivenAStartingTerminalNode_WhenDFS_ThenReturnsOnlyThatNode() {
        // Arrange
        const int startingNode = 1;

        // 6←---   1--→2
        // ↑   |   |   |
        // |   |   |   |
        // |   |   ↓   ↓
        // 5←------3--→4
        var directedGraph = new Dictionary<int, (bool isTerminal, List<int> neighbors)>
        {
            { 1, (true , new List<int> { 2, 3 }) },
            { 2, (false, new List<int> { 4 }) },
            { 3, (false, new List<int> { 4, 5, 6 }) },
            { 4, (false, new List<int> { }) },
            { 5, (false, new List<int> { 6 }) },
            { 6, (false, new List<int> { }) },
        };

        // Act
        var results = TreeSearch.DepthFirstSearch(
            startingNode,
            node => directedGraph[node].neighbors,
            node => directedGraph[node].isTerminal
        ).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(startingNode, results[0]);
    }

    [Fact]
    public void GivenDirectedGraph_WhenDFS_ThenReturnsInDepthFirstOrder() {
        // Arrange
        const int startingNode = 1;

        // 6←---   1--→2
        // ↑   |   |   |
        // |   |   |   |
        // |   |   ↓   ↓
        // 5←------3--→4
        var directedGraph = new Dictionary<int, (bool isTerminal, List<int> neighbors)>
        {
            { 1, (false, new List<int> { 2, 3 }) },
            { 2, (false, new List<int> { 4 }) },
            { 3, (false, new List<int> { 4, 5, 6 }) },
            { 4, (false, new List<int> { }) },
            { 5, (false, new List<int> { 6 }) },
            { 6, (false, new List<int> { }) },
        };

        // Act
        var results = TreeSearch.DepthFirstSearch(
            startingNode,
            node => directedGraph[node].neighbors,
            node => directedGraph[node].isTerminal
        ).ToList();

        // Assert
        var expected = new List<int> { 1, 3, 6, 5, 4, 2 };
        Assert.Equal(expected, results);
    }

    [Fact]
    public void GivenGraph_WhenDFS_ThenReturnsInDepthFirstOrder() {
        // Arrange
        const int startingNode = 1;

        // 6←---   1←-→2
        // ↑   |   ↑   ↑
        // |   |   |   |
        // ↓   |   ↓   ↓
        // 5←-----→3←-→4
        var graph = new Dictionary<int, (bool isTerminal, List<int> neighbors)>
        {
            { 1, (false, new List<int> { 2, 3 }) },
            { 2, (false, new List<int> { 1, 4 }) },
            { 3, (false, new List<int> { 1, 4, 5, 6 }) },
            { 4, (false, new List<int> { 2, 3 }) },
            { 5, (false, new List<int> { 3, 6 }) },
            { 6, (false, new List<int> { 3, 5 }) },
        };

        // Act
        var results = TreeSearch.DepthFirstSearch(
            startingNode,
            node => graph[node].neighbors,
            node => graph[node].isTerminal
        ).ToList();

        // Assert
        var expected = new List<int> { 1, 3, 6, 5, 4, 2 };
        Assert.Equal(expected, results);
    }

    [Fact]
    public void GivenDirectedGraphWithTerminalNodes_WhenDFS_ThenDoesntExplorePastTerminalNode() {
        // Arrange
        const int startingNode = 1;

        // 6←---   1--→2
        // ↑   |   |   |
        // |   |   |   |
        // |   |   ↓   ↓
        // 5←------3--→4
        var directedGraph = new Dictionary<int, (bool isTerminal, List<int> neighbors)>
        {
            { 1, (false, new List<int> { 2, 3 }) },
            { 2, (false, new List<int> { 4 }) },
            { 3, (true , new List<int> { 4, 5, 6 }) },
            { 4, (false, new List<int> { }) },
            { 5, (false, new List<int> { 6 }) },
            { 6, (false, new List<int> { }) },
        };

        // Act
        var results = TreeSearch.DepthFirstSearch(
            startingNode,
            node => directedGraph[node].neighbors,
            node => directedGraph[node].isTerminal
        ).ToList();

        // Assert
        var expected = new List<int> { 1, 3, 2, 4 };
        Assert.Equal(expected, results);
    }

    [Fact]
    public void GivenGraphWithTerminalNodes_WhenDFS_ThenDoesntExplorePastTerminalNode() {
        // Arrange
        const int startingNode = 1;

        // 6←---   1←-→2
        // ↑   |   ↑   ↑
        // |   |   |   |
        // ↓   |   ↓   ↓
        // 5←-----→3←-→4
        var graph = new Dictionary<int, (bool isTerminal, List<int> neighbors)>
        {
            { 1, (false, new List<int> { 2, 3 }) },
            { 2, (false, new List<int> { 1, 4 }) },
            { 3, (true , new List<int> { 1, 4, 5, 6 }) },
            { 4, (false, new List<int> { 2, 3 }) },
            { 5, (false, new List<int> { 3, 6 }) },
            { 6, (false, new List<int> { 3, 5 }) },
        };

        // Act
        var results = TreeSearch.DepthFirstSearch(
            startingNode,
            node => graph[node].neighbors,
            node => graph[node].isTerminal
        ).ToList();

        // Assert
        var expected = new List<int> { 1, 3, 2, 4 };
        Assert.Equal(expected, results);
    }
}
