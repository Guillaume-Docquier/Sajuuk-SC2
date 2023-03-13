using System;
using System.Collections;
using System.Collections.Generic;

namespace Bot;

public class CircularQueue<T> : IEnumerable<T> {
    private readonly T[] _queue;

    private int _startIndex = 0;
    private int _endIndex = 0;

    public int Length { get; private set; } = 0;

    public CircularQueue(int queueSize) {
        _queue = new T[queueSize];
    }

    public T Dequeue() {
        if (Length == 0) {
            throw new InvalidOperationException("Cannot dequeue when the queue is empty.");
        }

        var value = _queue[_startIndex];

        _queue[_startIndex] = default;
        _startIndex = CircularIncrement(_startIndex);
        Length--;

        return value;
    }

    public int Enqueue(T value) {
        if (Length == _queue.Length && _endIndex == _startIndex) {
            _startIndex = CircularIncrement(_startIndex);
        }

        _queue[_endIndex] = value;
        _endIndex = CircularIncrement(_endIndex);
        Length = Math.Min(Length + 1, _queue.Length);

        return Length;
    }

    public T this[int index] => _queue[index % _queue.Length];

    public IEnumerator<T> GetEnumerator() {
        for (var i = 0; i < Length; i++) {
            yield return _queue[_startIndex + i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    private int CircularIncrement(int value) {
        return (value + 1) % _queue.Length;
    }
}
