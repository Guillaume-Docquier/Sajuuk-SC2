using System;
using System.Collections;
using System.Collections.Generic;

namespace Bot.DataStructures;

public class CircularQueue<T> : IEnumerable<T> {
    private readonly T[] _queue;

    private int _dequeueIndex = 0;
    private int _enqueueIndex = 0;

    public int Length { get; private set; } = 0;

    public CircularQueue(int queueSize) {
        _queue = new T[queueSize];
    }

    public T Dequeue() {
        if (Length == 0) {
            throw new InvalidOperationException("Cannot dequeue when the queue is empty.");
        }

        var value = _queue[_dequeueIndex];

        _queue[_dequeueIndex] = default;
        _dequeueIndex = CircularIncrement(_dequeueIndex);
        Length--;

        return value;
    }

    public int Enqueue(T value) {
        if (Length == _queue.Length && _enqueueIndex == _dequeueIndex) {
            _dequeueIndex = CircularIncrement(_dequeueIndex);
        }

        _queue[_enqueueIndex] = value;
        _enqueueIndex = CircularIncrement(_enqueueIndex);
        Length = Math.Min(Length + 1, _queue.Length);

        return Length;
    }

    public void Clear() {
        Length = 0;
        _dequeueIndex = 0;
        _enqueueIndex = 0;
    }

    public T this[int index] {
        get {
            if (index >= Length) {
                throw new IndexOutOfRangeException($"Trying to access element at index {index} in a queue of length {Length}");
            }

            return _queue[(_dequeueIndex + index) % _queue.Length];
        }
    }

    public IEnumerator<T> GetEnumerator() {
        for (var index = 0; index < Length; index++) {
            yield return this[index];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    private int CircularIncrement(int value) {
        return (value + 1) % _queue.Length;
    }
}
