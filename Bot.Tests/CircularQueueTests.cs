namespace Bot.Tests;

public class CircularQueueTests {
    [Fact]
    public void GivenEmptyQueue_WhenEnqueue_ThenAddsToEndOfQueue() {
        // Arrange
        const int numberOfItemsToQueue = 20;
        const int queueSize = 5;
        var circularQueue = new CircularQueue<int>(queueSize);

        // Act
        for (int i = 0; i < numberOfItemsToQueue; i++) {
            var valueToAdd = i + 1;
            circularQueue.Enqueue(valueToAdd);

            // Assert
            Assert.Equal(valueToAdd, circularQueue[i]);
        }
    }

    [Fact]
    public void GivenEmpty_WhenEnqueue_ThenReturnsQueueSize() {
        // Arrange
        const int numberOfItemsToQueue = 20;
        const int queueSize = 5;
        var circularQueue = new CircularQueue<int>(queueSize);

        // Act
        for (int i = 0; i < numberOfItemsToQueue; i++) {
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

    [Fact]
    public void GivenPartiallyFilledQueue_WhenDequeueTooManyTime_ThenThrows() {
        // Arrange
        const int queueSize = 5;
        var circularQueue = new CircularQueue<int>(queueSize);

        // Act
        for (var i = 0; i < queueSize - 1; i++) {
            var valueToAdd = i + 1;
            circularQueue.Enqueue(valueToAdd);
        }

        var numberOfElementsToRemove = circularQueue.Length;
        for (var i = 0; i < numberOfElementsToRemove; i++) {
            circularQueue.Dequeue();
        }

        // Assert
        Assert.Throws<InvalidOperationException>(() => circularQueue.Dequeue());
    }

    [Fact]
    public void GivenPartiallyFilledQueue_WhenDequeue_ThenDequeuesInInsertionOrder() {
        // Arrange
        const int numberOfItemsToQueue = 4;
        const int queueSize = 5;
        var circularQueue = new CircularQueue<int>(queueSize);

        // Act
        for (var i = 0; i < numberOfItemsToQueue; i++) {
            var valueToAdd = i + 1;
            circularQueue.Enqueue(valueToAdd);
        }

        var numberOfElementsToRemove = circularQueue.Length;
        for (var i = 0; i < numberOfElementsToRemove; i++) {
            var expectedValue = i + 1;
            var actualValue = circularQueue.Dequeue();

            // Assert
            Assert.Equal(expectedValue, actualValue);
        }
    }

    [Fact]
    public void GivenOverflownQueue_WhenDequeue_ThenDequeuesLastInsertedItemsInInsertionOrder() {
        // Arrange
        const int numberOfItemsToQueue = 20;
        const int queueSize = 5;
        var circularQueue = new CircularQueue<int>(queueSize);

        // Act
        for (var i = 0; i < numberOfItemsToQueue; i++) {
            var valueToAdd = i + 1;
            circularQueue.Enqueue(valueToAdd);
        }

        var numberOfElementsToRemove = circularQueue.Length;
        for (var i = 0; i < numberOfElementsToRemove; i++) {
            var expectedValue = numberOfItemsToQueue - (queueSize - i) + 1;
            var actualValue = circularQueue.Dequeue();

            // Assert
            Assert.Equal(expectedValue, actualValue);
        }
    }
}
