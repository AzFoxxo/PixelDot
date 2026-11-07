/*
 *   Copyright (c) 2026 Az Foxxo (@AzFoxxo on GitHub)
 *   All rights reserved.

 *   Permission is hereby granted, free of charge, to any person obtaining a copy
 *   of this software and associated documentation files (the "Software"), to deal
 *   in the Software without restriction, including without limitation the rights
 *   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *   copies of the Software, and to permit persons to whom the Software is
 *   furnished to do so, subject to the following conditions:
 
 *   The above copyright notice and this permission notice shall be included in all
 *   copies or substantial portions of the Software.
 
 *   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 *   SOFTWARE.
 */

namespace PixelDot.Collision;

/// <summary>
/// Manages registration, broad- and narrow-phase collision updates, and enter/stay/exit lifecycle events
/// for <see cref="PixelDataCollider"/> instances.
/// </summary>
public sealed class CollisionManager
{
    /// <summary>
    /// Unordered pair of colliders used as a dictionary key to track unique interaction pairs.
    /// </summary>
    private readonly struct ColliderPair : IEquatable<ColliderPair>
    {
        public readonly PixelDataCollider A;
        public readonly PixelDataCollider B;

        public ColliderPair(PixelDataCollider a, PixelDataCollider b)
        {
            if (a.Id < b.Id)
            {
                A = a;
                B = b;
            }
            else
            {
                A = b;
                B = a;
            }
        }

        public bool Equals(ColliderPair other) =>
            ReferenceEquals(A, other.A) && ReferenceEquals(B, other.B);

        public override bool Equals(object? obj) =>
            obj is ColliderPair other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(A, B);
    }

    private readonly List<PixelDataCollider> _colliders = [];
    private readonly Dictionary<ColliderPair, CollisionResult> _activeCollisions = new();
    private readonly Dictionary<ColliderPair, CollisionResult> _currentCollisions = new();

    // Reusable buffers to minimize allocations across updates
    private readonly List<CollisionResult> _enterBuffer = [];
    private readonly List<CollisionResult> _stayBuffer = [];
    private readonly List<CollisionResult> _exitBuffer = [];

    /// <summary>
    /// Gets a read-only view of all colliders registered with this manager.
    /// </summary>
    public IReadOnlyList<PixelDataCollider> Colliders => _colliders;

    #region Events

    /// <summary>Fired when a solid collision between two colliders begins.</summary>
    public event Action<CollisionResult>? OnCollisionEnter;

    /// <summary>Fired each update while a solid collision between two colliders persists.</summary>
    public event Action<CollisionResult>? OnCollisionStay;

    /// <summary>Fired when a solid collision between two colliders ends.</summary>
    public event Action<CollisionResult>? OnCollisionExit;

    /// <summary>Fired when an overlap involving at least one trigger collider begins.</summary>
    public event Action<CollisionResult>? OnTriggerEnter;

    /// <summary>Fired each update while an overlap involving at least one trigger collider persists.</summary>
    public event Action<CollisionResult>? OnTriggerStay;

    /// <summary>Fired when an overlap involving at least one trigger collider ends.</summary>
    public event Action<CollisionResult>? OnTriggerExit;

    #endregion

    #region Registration

    /// <summary>
    /// Adds a collider to the manager.
    /// </summary>
    /// <param name="collider">The collider to register.</param>
    public void Add(PixelDataCollider collider)
    {
        ArgumentNullException.ThrowIfNull(collider);

        if (!_colliders.Contains(collider))
        {
            _colliders.Add(collider);
        }
    }

    /// <summary>
    /// Removes a collider from the manager.
    /// Any active collisions involving this collider will immediately fire exit events and be cleaned up.
    /// </summary>
    /// <param name="collider">The collider to unregister.</param>
    /// <returns>True if the collider was found and removed, otherwise false.</returns>
    public bool Remove(PixelDataCollider collider)
    {
        if (collider is null)
        {
            return false;
        }

        var removed = _colliders.Remove(collider);
        if (removed)
        {
            // Collect any active collisions involving this collider
            var exitingPairs = new List<(ColliderPair Pair, CollisionResult Result)>();
            foreach (var kvp in _activeCollisions)
            {
                if (ReferenceEquals(kvp.Key.A, collider) || ReferenceEquals(kvp.Key.B, collider))
                {
                    exitingPairs.Add((kvp.Key, kvp.Value));
                }
            }

            foreach (var (pair, result) in exitingPairs)
            {
                _activeCollisions.Remove(pair);
                DispatchExit(result);
            }
        }

        return removed;
    }

    /// <summary>
    /// Removes all colliders from the manager and fires exit events for any active collisions.
    /// </summary>
    public void Clear()
    {
        var exiting = _activeCollisions.Values.ToList();
        _colliders.Clear();
        _activeCollisions.Clear();
        _currentCollisions.Clear();

        foreach (var result in exiting)
        {
            DispatchExit(result);
        }
    }

    #endregion

    #region Collision Queries

    /// <summary>
    /// Performs a collision test between two colliders, respecting layer masks and enabled states.
    /// </summary>
    /// <param name="a">First collider.</param>
    /// <param name="b">Second collider.</param>
    /// <returns>A <see cref="CollisionResult"/> if a collision occurred, otherwise null.</returns>
    public CollisionResult? CheckCollision(PixelDataCollider a, PixelDataCollider b)
    {
        if (a is null || b is null)
        {
            return null;
        }

        if (!a.Enabled || !b.Enabled)
        {
            return null;
        }

        if (ReferenceEquals(a, b))
        {
            return null;
        }

        if (!CollisionFilter.ShouldCollide(a, b))
        {
            return null;
        }

        return a.CheckCollision(b);
    }

    /// <summary>
    /// Gets all active collisions involving the specified collider from the last <see cref="Update"/> cycle.
    /// </summary>
    /// <param name="collider">The collider to look up.</param>
    /// <returns>A list of active <see cref="CollisionResult"/> objects involving the collider.</returns>
    public IReadOnlyList<CollisionResult> GetCollisions(PixelDataCollider collider)
    {
        if (collider is null)
        {
            return Array.Empty<CollisionResult>();
        }

        var list = new List<CollisionResult>();
        foreach (var (pair, result) in _activeCollisions)
        {
            if (ReferenceEquals(pair.A, collider) || ReferenceEquals(pair.B, collider))
            {
                list.Add(result);
            }
        }

        return list;
    }

    /// <summary>
    /// Immediately tests the specified collider against all other active colliders in this manager.
    /// </summary>
    /// <param name="collider">The collider to query.</param>
    /// <returns>All collisions currently occurring for the collider.</returns>
    public IReadOnlyList<CollisionResult> CheckCollisionsNow(PixelDataCollider collider)
    {
        if (collider is null || !collider.Enabled)
        {
            return Array.Empty<CollisionResult>();
        }

        var results = new List<CollisionResult>();
        for (var i = 0; i < _colliders.Count; i++)
        {
            var other = _colliders[i];
            if (ReferenceEquals(collider, other) || !other.Enabled)
            {
                continue;
            }

            var result = CheckCollision(collider, other);
            if (result is not null)
            {
                results.Add(result);
            }
        }

        return results;
    }

    #endregion

    #region Update Loop

    /// <summary>
    /// Updates all registered colliders: performs broad-phase and narrow-phase collision detection,
    /// and raises Enter, Stay, and Exit lifecycle events.
    /// </summary>
    public void Update()
    {
        _currentCollisions.Clear();
        _enterBuffer.Clear();
        _stayBuffer.Clear();
        _exitBuffer.Clear();

        var count = _colliders.Count;

        // Pairwise comparison: each unique pair (i, j) is tested exactly once
        for (var i = 0; i < count; i++)
        {
            var a = _colliders[i];
            if (!a.Enabled || a.Width <= 0 || a.Height <= 0)
            {
                continue;
            }

            for (var j = i + 1; j < count; j++)
            {
                var b = _colliders[j];
                if (!b.Enabled || b.Width <= 0 || b.Height <= 0)
                {
                    continue;
                }

                // Check collision layer mask filtering first
                if (!CollisionFilter.ShouldCollide(a, b))
                {
                    continue;
                }

                var result = a.CheckCollision(b);
                if (result is not null)
                {
                    var pair = new ColliderPair(a, b);
                    _currentCollisions[pair] = result;

                    if (_activeCollisions.ContainsKey(pair))
                    {
                        _stayBuffer.Add(result);
                    }
                    else
                    {
                        _enterBuffer.Add(result);
                    }
                }
            }
        }

        // Detect Exit events: any pair in _activeCollisions not in _currentCollisions
        foreach (var (pair, previousResult) in _activeCollisions)
        {
            if (!_currentCollisions.ContainsKey(pair))
            {
                _exitBuffer.Add(previousResult);
            }
        }

        // Commit active collisions for the next frame
        _activeCollisions.Clear();
        foreach (var kvp in _currentCollisions)
        {
            _activeCollisions[kvp.Key] = kvp.Value;
        }

        // Dispatch events outside the collection loops to allow safe mutation in callbacks
        for (var i = 0; i < _enterBuffer.Count; i++)
        {
            DispatchEnter(_enterBuffer[i]);
        }

        for (var i = 0; i < _stayBuffer.Count; i++)
        {
            DispatchStay(_stayBuffer[i]);
        }

        for (var i = 0; i < _exitBuffer.Count; i++)
        {
            DispatchExit(_exitBuffer[i]);
        }
    }

    #endregion

    #region Event Dispatchers

    private void DispatchEnter(CollisionResult result)
    {
        if (result.IsTrigger)
        {
            OnTriggerEnter?.Invoke(result);
            result.ColliderA.RaiseTriggerEnter(result);
            result.ColliderB.RaiseTriggerEnter(result);
        }
        else
        {
            OnCollisionEnter?.Invoke(result);
            result.ColliderA.RaiseCollisionEnter(result);
            result.ColliderB.RaiseCollisionEnter(result);
        }
    }

    private void DispatchStay(CollisionResult result)
    {
        if (result.IsTrigger)
        {
            OnTriggerStay?.Invoke(result);
            result.ColliderA.RaiseTriggerStay(result);
            result.ColliderB.RaiseTriggerStay(result);
        }
        else
        {
            OnCollisionStay?.Invoke(result);
            result.ColliderA.RaiseCollisionStay(result);
            result.ColliderB.RaiseCollisionStay(result);
        }
    }

    private void DispatchExit(CollisionResult result)
    {
        if (result.IsTrigger)
        {
            OnTriggerExit?.Invoke(result);
            result.ColliderA.RaiseTriggerExit(result);
            result.ColliderB.RaiseTriggerExit(result);
        }
        else
        {
            OnCollisionExit?.Invoke(result);
            result.ColliderA.RaiseCollisionExit(result);
            result.ColliderB.RaiseCollisionExit(result);
        }
    }

    #endregion
}
