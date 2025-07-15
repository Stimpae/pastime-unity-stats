using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pastime.Stats {
    /// <summary>
    /// Interface for managing a collection of stats identified by enum types.
    /// Provides methods for registration, retrieval, and manipulation of stats.
    /// </summary>
    /// <typeparam name="TE">Enum type used to identify stats</typeparam>
    public interface IStatContainer<in TE> : IDisposable where TE : System.Enum {
        Stat GetStat(TE statType);
        bool TryGetStat(TE statType, out Stat stat);
        float GetStatValue(TE statType);
        bool TryGetStatValue(TE statType, out float? value);
        Stat RegisterStat(TE statType, float baseValue);
        void AddStat(TE statType, Stat value);
        void ResetStats();
        void ResetStat(TE statType);
    }
    
    /// <summary>
    /// Concrete implementation of a stat container that manages stats using enum-based keys.
    /// Maintains both a dictionary for fast lookup and a serializable list for Unity persistence.
    /// </summary>
    /// <typeparam name="TE">Enum type used to identify stats</typeparam>
    [Serializable]
    public class StatContainer<TE> : IStatContainer<TE> where TE : System.Enum {
        // Stat list is an editor only list for Unity serialization. so we can see the stats in the drawer
        [SerializeField] private List<Stat> statsList = new();
        private readonly Dictionary<TE, Stat> m_stats = new();

        /// <summary>
        /// Retrieves a stat by its type identifier.
        /// </summary>
        /// <param name="statType">The enum identifier for the stat</param>
        /// <returns>The stat instance</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the stat type is not found</exception>
        public Stat GetStat(TE statType) {
            if (m_stats.TryGetValue(statType, out var stat)) return stat;
            throw new KeyNotFoundException($"Stat of type {statType} not found");
        }

        /// <summary>
        /// Attempts to retrieve a stat by its type identifier without throwing exceptions.
        /// </summary>
        /// <param name="statType">The enum identifier for the stat</param>
        /// <param name="stat">Output parameter containing the stat if found</param>
        /// <returns>True if the stat was found, false otherwise</returns>
        public bool TryGetStat(TE statType, out Stat stat) => m_stats.TryGetValue(statType, out stat);

        /// <summary>
        /// Gets the current value of a stat by its type identifier.
        /// </summary>
        /// <param name="statType">The enum identifier for the stat</param>
        /// <returns>The current value of the stat</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the stat type is not found</exception>
        public float GetStatValue(TE statType) {
            if (m_stats.TryGetValue(statType, out var stat)) return stat.CurrentValue;
            throw new KeyNotFoundException($"Stat of type {statType} not found");
        }

        /// <summary>
        /// Attempts to get the current value of a stat without throwing exceptions.
        /// </summary>
        /// <param name="statType">The enum identifier for the stat</param>
        /// <param name="value">Output parameter containing the stat value if found</param>
        /// <returns>True if the stat was found, false otherwise</returns>
        public bool TryGetStatValue(TE statType, out float? value) {
            if (m_stats.TryGetValue(statType, out var stat)) {
                value = stat.CurrentValue;
                return true;
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Creates and registers a new stat with the specified base value.
        /// </summary>
        /// <param name="statType">The enum identifier for the new stat</param>
        /// <param name="baseValue">The initial base value for the stat</param>
        /// <returns>The newly created stat instance</returns>
        /// <exception cref="InvalidOperationException">Thrown when a stat with the same type already exists</exception>
        public Stat RegisterStat(TE statType, float baseValue) {
            if (m_stats.ContainsKey(statType)) throw new InvalidOperationException($"Stat of type {statType} already exists");
            var stat = new Stat(baseValue);
            m_stats[statType] = stat;
            statsList.Add(stat);
            return stat;
        }

        /// <summary>
        /// Adds an existing stat instance to the container.
        /// </summary>
        /// <param name="statType">The enum identifier for the stat</param>
        /// <param name="value">The stat instance to add</param>
        /// <exception cref="InvalidOperationException">Thrown when the stat is null or a stat with the same type already exists</exception>
        public void AddStat(TE statType, Stat value) {
            if (value == null) throw new InvalidOperationException(nameof(value));
            if (!m_stats.TryAdd(statType, value)) throw new InvalidOperationException($"Stat of type {statType} already exists");
            statsList.Add(value);
        }

        /// <summary>
        /// Resets all stats in the container to their initial values.
        /// </summary>
        public void ResetStats() {
            foreach (var stat in m_stats.Values) {
                stat.Reset();
            }
        }

        /// <summary>
        /// Resets a specific stat to its initial value if it exists.
        /// </summary>
        /// <param name="statType">The enum identifier for the stat to reset</param>
        public void ResetStat(TE statType) {
            if (m_stats.TryGetValue(statType, out var stat)) stat.Reset();
        }
        
        /// <summary>
        /// Disposes all stats and clears the container.
        /// </summary>
        public void Dispose() {
            foreach (var stat in m_stats.Values) {
                stat.Dispose();
            }
            m_stats.Clear();
            statsList.Clear();
        }
    }
}