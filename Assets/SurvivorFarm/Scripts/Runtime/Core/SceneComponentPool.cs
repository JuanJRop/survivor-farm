using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivorFarm.Runtime.Core
{
    /// <summary>
    /// Reuses scene-owned components. Clients configure an inactive lease before activating it,
    /// and clear gameplay references before returning it. The owner disposes on scene teardown.
    /// Cosmetic pools may limit active leases; valuable loot limits retained idle objects only.
    /// </summary>
    public sealed class SceneComponentPool<T> : IDisposable where T : Component
    {
        private readonly Transform storage;
        private readonly Func<Transform, T> factory;
        private readonly Stack<T> idle;
        private readonly HashSet<T> leased;
        private bool disposed;

        public int CreatedCount { get; private set; }
        public int RentCount { get; private set; }
        public int DeniedCount { get; private set; }
        public int ActiveCount => leased.Count;
        public int InactiveCount
        {
            get
            {
                int count = 0;
                // Diagnostic only; external destruction can leave a tombstone until the next rent.
                foreach (T item in idle) if (item != null) count++;
                return count;
            }
        }
        public int MaxRetained { get; }
        public int MaxActive { get; }
        public bool IsDisposed => disposed;

        public SceneComponentPool(Transform storage, Func<Transform, T> factory,
            int prewarm, int maxRetained, int maxActive = int.MaxValue)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            this.storage = storage;
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            MaxRetained = Mathf.Max(0, maxRetained);
            MaxActive = Mathf.Max(1, maxActive);
            int warm = Mathf.Clamp(prewarm, 0, Mathf.Min(MaxRetained, MaxActive));
            idle = new Stack<T>(MaxRetained);
            leased = new HashSet<T>();
            for (int i = 0; i < warm; i++) idle.Push(Create());
        }

        /// <returns>An inactive component, or null when this pool is gone or its active budget is full.</returns>
        public T Rent()
        {
            if (disposed || storage == null) return null;
            if (leased.Count >= MaxActive) { DeniedCount++; return null; }
            T item = null;
            while (idle.Count > 0 && item == null) item = idle.Pop();
            if (item == null) item = Create();
            leased.Add(item);
            RentCount++;
            return item;
        }

        public bool Return(T item)
        {
            // Remove before OnDisable: a client's cleanup can safely attempt a second return.
            if (ReferenceEquals(item, null) || !leased.Remove(item)) return false;
            if (item == null) return true;
            item.gameObject.SetActive(false);
            if (disposed || storage == null || idle.Count >= MaxRetained)
            {
                UnityEngine.Object.Destroy(item.gameObject);
                return true;
            }
            item.transform.SetParent(storage, false);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            item.transform.localScale = Vector3.one;
            idle.Push(item);
            return true;
        }

        /// <summary>Called by a leased component's OnDestroy when an external owner removes it.</summary>
        public void Forget(T item)
        {
            if (!ReferenceEquals(item, null)) leased.Remove(item);
        }

        private T Create()
        {
            T item = factory(storage);
            if (item == null) throw new InvalidOperationException("Pool factory returned no component.");
            item.gameObject.SetActive(false);
            item.transform.SetParent(storage, false);
            CreatedCount++;
            return item;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            // Teardown may invoke clients' OnDestroy/OnDisable. Clear ownership first.
            var remaining = new T[leased.Count];
            leased.CopyTo(remaining);
            leased.Clear();
            foreach (T item in remaining) if (item != null) UnityEngine.Object.Destroy(item.gameObject);
            while (idle.Count > 0)
            {
                T item = idle.Pop();
                if (item != null) UnityEngine.Object.Destroy(item.gameObject);
            }
        }
    }
}
