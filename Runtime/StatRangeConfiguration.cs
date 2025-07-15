using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pastime.Stats {
    /// <summary>
    /// Manages min/max range constraints for stats with automatic value clamping.
    /// Supports both fixed value constraints and dynamic constraints based on other stats.
    /// Handles ownership and lifecycle management of constraint stats.
    /// </summary>
    public class StatRangeConfiguration : IDisposable {
        private Action<float> m_onRangeChanged;
        private bool m_ownsMinStat;
        private bool m_ownsMaxStat;
        
        /// <summary>
        /// The minimum value constraint stat for the stat.
        /// </summary>
        public BaseStat MinStat { get; private set; }
        
        /// <summary>
        /// The maximum value constraint stat for the stat.
        /// </summary>
        public BaseStat MaxStat { get; private set; }
        
        /// <summary>
        /// Gets whether a minimum constraint is currently configured.
        /// </summary>
        public bool HasMinConstraint => MinStat != null;
        
        /// <summary>
        /// Gets whether a maximum constraint is currently configured.
        /// </summary>
        public bool HasMaxConstraint => MaxStat != null;

        
        /// <summary>
        /// Sets the callback to invoke when any range constraint value changes.
        /// </summary>
        /// <param name="onRangeChanged">Action to invoke with the changed value</param>
        public void SetRangeValueChanged(Action<float> onRangeChanged) {
            m_onRangeChanged = onRangeChanged;
        }

        /// <summary>
        /// Sets a fixed minimum value constraint by creating an internal stat.
        /// </summary>
        /// <param name="min">The minimum value to enforce</param>
        public void SetMinValue(float min) => SetMinStat(new BaseStat(min), true);
        
        /// <summary>
        /// Sets a fixed maximum value constraint by creating an internal stat.
        /// </summary>
        /// <param name="max">The maximum value to enforce</param>
        public void SetMaxValue(float max) => SetMaxStat(new BaseStat(max), true);
        
        /// <summary>
        /// Sets a dynamic minimum constraint based on another stat's value.
        /// </summary>
        /// <param name="minStat">The stat to use for minimum constraint</param>
        public void SetMinStat(BaseStat minStat) => SetMinStat(minStat, false);
        
        /// <summary>
        /// Sets a dynamic maximum constraint based on another stat's value.
        /// </summary>
        /// <param name="maxStat">The stat to use for maximum constraint</param>
        public void SetMaxStat(BaseStat maxStat) => SetMaxStat(maxStat, false);
        
        /// <summary>
        /// Internal method to set the minimum stat with ownership tracking.
        /// Handles cleanup of previous constraints and event subscription.
        /// </summary>
        /// <param name="stat">The stat to use for minimum constraint</param>
        /// <param name="ownsInstance">Whether this configuration owns the stat instance</param>
        /// <exception cref="ArgumentNullException">Thrown when stat is null</exception>
        private void SetMinStat(BaseStat stat, bool ownsInstance) {
            if (MinStat != null) {
                if (m_onRangeChanged != null) {
                    MinStat.OnCurrentValueChanged -= m_onRangeChanged;
                    MinStat.OnBaseValueChanged -= m_onRangeChanged;
                } 
                if (m_ownsMinStat) MinStat.Dispose();
            }

            MinStat = stat ?? throw new ArgumentNullException(nameof(stat));
            m_ownsMinStat = ownsInstance;
            if (m_onRangeChanged != null) {
                MinStat.OnCurrentValueChanged += m_onRangeChanged;
                MinStat.OnBaseValueChanged += m_onRangeChanged;
            }
            
            ValidateRanges();
        }

        /// <summary>
        /// Internal method to set the maximum stat with ownership tracking.
        /// Handles cleanup of previous constraints and event subscription.
        /// </summary>
        /// <param name="stat">The stat to use for maximum constraint</param>
        /// <param name="ownsInstance">Whether this configuration owns the stat instance</param>
        /// <exception cref="ArgumentNullException">Thrown when stat is null</exception>
        private void SetMaxStat(BaseStat stat, bool ownsInstance) {
            if (MaxStat != null) {
                if (m_onRangeChanged != null) {
                    MaxStat.OnCurrentValueChanged -= m_onRangeChanged;
                    MaxStat.OnBaseValueChanged -= m_onRangeChanged;
                }
                if (m_ownsMaxStat) MaxStat.Dispose();
            }

            MaxStat = stat ?? throw new ArgumentNullException(nameof(stat));
            m_ownsMaxStat = ownsInstance;
            if (m_onRangeChanged != null) {
                MaxStat.OnCurrentValueChanged += m_onRangeChanged;
                MaxStat.OnBaseValueChanged += m_onRangeChanged;
            }
            
            ValidateRanges();
        }

        /// <summary>
        /// Clamps the given value to the configured min/max constraints.
        /// Returns the original value if no constraints are configured.
        /// </summary>
        /// <param name="value">The value to clamp</param>
        /// <returns>The clamped value within the configured range</returns>
        public float ClampValue(float value) {
            if (HasMinConstraint && value < MinStat.CurrentValue) value = MinStat.CurrentValue;
            if (HasMaxConstraint && value > MaxStat.CurrentValue) value = MaxStat.CurrentValue;
            return value;
        }
        
        /// <summary>
        /// Validates that the minimum constraint doesn't exceed the maximum constraint.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when min range exceeds max range</exception>
        public void ValidateRanges() {
            if (HasMinConstraint && HasMaxConstraint && MinStat.CurrentValue > MaxStat.CurrentValue ||
                MinStat.BaseValue > MaxStat.BaseValue)
                throw new InvalidOperationException("Min range cannot exceed max range");
        }

        /// <summary>
        /// Disposes the configuration, cleaning up event subscriptions and owned stat instances.
        /// </summary>
        public void Dispose() {
            UnsubscribeAndDispose(MinStat, m_ownsMinStat);
            UnsubscribeAndDispose(MaxStat, m_ownsMaxStat);
            MinStat = null;
            MaxStat = null;
            m_onRangeChanged = null;
        }

        /// <summary>
        /// Helper method to unsubscribe from events and dispose stats if owned.
        /// </summary>
        /// <param name="stat">The stat to clean up</param>
        /// <param name="ownsInstance">Whether this configuration owns the stat instance</param>
        private void UnsubscribeAndDispose(IStat stat, bool ownsInstance) {
            if (stat != null && m_onRangeChanged != null) stat.OnCurrentValueChanged -= m_onRangeChanged;
            if (ownsInstance) stat?.Dispose();
        }
    }
}