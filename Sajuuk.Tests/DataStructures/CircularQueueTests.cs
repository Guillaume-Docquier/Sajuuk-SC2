using System.Diagnostics.CodeAnalysis;
using Sajuuk.DataStructures;

namespace Sajuuk.Tests.DataStructures;

public class CircularQueueTests {
    [Fact]
    public void GivenEmptyQueue_WhenEnqueue_ThenAddsToEndOfQueue() {
        // Arrange
        const int numberOfItemsToQueue = 15;
        const int queueSize = 5;
        var circularQueue = new CircularQueue<int>(queueSize);

        // Act
        for (var i = 0; i < numberOfItemsToQueue; i++) {
            var valueToAdd = i + 1;
            circularQueue.Enqueue(valueToAdd);

            // Assert
            Assert.Equal(valueToAdd, circularQueue[^1]);
        }
    }

    [Fact]
    public void GivenEmptyQueue_WhenEnqueue_ThenReturnsQueueSize() {
        // Arrange
        const int numberOfItemsToQueue = 15;
        const int queueSize = 5;
        var circularQueue = new CircularQueue<int>(queueSize);

        // Act
        for (var i = 0; i < numberOfItemsToQueue; i++) {
            var queueLength = circularQueue.Enqueue(i);

            // Assert
            Assert.Equal(circularQueue.Length, queueLength);
        }
    }

    [Fact]
    public void GivenEmptyQueue_WhenDequeue_ThenThrows() {
        // Arrange
        var circularQueue = new CircularQueue<int>(5);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => circularQueue.Dequeue());
    }

    public static IEnumerable<object[]> AnyQueueData() {
        const int maxNumberOfItemsToQueue = 15;
        const int queueSize = 5;
        for (var numberOfItemsToQueue = 0; numberOfItemsToQueue < maxNumberOfItemsToQueue; numberOfItemsToQueue++) {
            var queue = new CircularQueue<int>(queueSize);
            var lastInsertedItems = new List<int>();
            for (var i = 0; i < numberOfItemsToQueue; i++) {
                queue.Enqueue(i);
                if (numberOfItemsToQueue - i <= queueSize) {
                    lastInsertedItems.Add(i);
                }
            }

            yield return new object[] { queue, lastInsertedItems };
        }
    }

    [Theory]
    [MemberData(nameof(AnyQueueData))]
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
    public void GivenAnyQueue_WhenDequeueTooManyTimes_ThenThrows(CircularQueue<int> queue, List<int> _) {
        // Act
        var numberOfElementsToRemove = queue.Length;
        for (var i = 0; i < numberOfElementsToRemove; i++) {
            queue.Dequeue();
        }

        // Assert
        Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
    }

    [Theory]
    [MemberData(nameof(AnyQueueData))]
    public void GivenAnyQueue_WhenDequeue_ThenDequeuesInInsertionOrder(CircularQueue<int> queue, List<int> lastInsertedItems) {
        // Act
        var numberOfElementsToRemove = queue.Length;
        var removedItems = new List<int>();
        for (var i = 0; i < numberOfElementsToRemove; i++) {
            removedItems.Add(queue.Dequeue());
        }

        // Assert
        Assert.Equal(lastInsertedItems, removedItems);
    }

    [Theory]
    [MemberData(nameof(AnyQueueData))]
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
    public void GivenAnyQueue_WhenClear_ThenLengthIsSetToZero(CircularQueue<int> queue, List<int> _) {
        // Act
        queue.Clear();

        // Assert
        Assert.Equal(0, queue.Length);
    }

    [Theory]
    [MemberData(nameof(AnyQueueData))]
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
    public void GivenAnyQueue_WhenClear_ThenCannotIterate(CircularQueue<int> queue, List<int> _) {
        // Act
        queue.Clear();

        // Assert
        Assert.Empty(queue);
    }

    [Theory]
    [MemberData(nameof(AnyQueueData))]
    public void GivenAnyQueue_WhenEnumerating_ThenReturnsLastInsertedItemsInInsertionOrder(CircularQueue<int> queue, List<int> lastInsertedItems) {
        // Act
        var queueItems = queue.ToList();

        // Assert
        Assert.Equal(lastInsertedItems.Count, queueItems.Count);
        Assert.Equal(lastInsertedItems, queueItems);
    }

    [Theory]
    [MemberData(nameof(AnyQueueData))]
    public void GivenAnyQueue_WhenIndexing_ThenReturnsLastInsertedItemsInInsertionOrder(CircularQueue<int> queue, List<int> lastInsertedItems) {
        // Act
        var queueItems = new List<int>();
        for (var i = 0; i < lastInsertedItems.Count; i++) {
            queueItems.Add(queue[i]);
        }

        // Assert
        Assert.Equal(lastInsertedItems, queueItems);
    }

    [Theory]
    [MemberData(nameof(AnyQueueData))]
    [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters")]
    public void GivenAnyQueue_WhenIndexingOutOfRange_ThenThrows(CircularQueue<int> queue, List<int> _) {
        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => queue[queue.Length]);
    }
}
