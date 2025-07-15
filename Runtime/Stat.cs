using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pastime.Stats {
    /// <summary>
    /// Core interface for stats that can be modified and observed.
    /// </summary>
    public interface IStat : IDisposable {
        float InitialValue { get; }
        float CurrentValue { get; } 
        float BaseValue { get; } 
        void Reset();
        
        event Action<float> OnCurrentValueChanged;
        event Action<float> OnBaseValueChanged;
    }

    /// <summary>
    /// Interface for managing stat modifiers with optional tagging.
    /// </summary>
    public interface IStatModifierOperations {
        void AddModifier(IStatModifier modifier);
        void AddModifierWithTag(IStatModifier modifier, string tag);
        void RemoveModifier(IStatModifier modifier);
        void RemoveModifierWithTag(string tag);
        List<IStatModifier> GetModifiers();
    }

    /// <summary>
    /// Interface for configuring min/max range constraints on stats.
    /// </summary>
    public interface IStatRangeOperations {
        Stat WithMinRange(float min);
        Stat WithMinRange(Stat min);
        Stat WithMaxRange(float max);
        Stat WithMaxRange(Stat max);
    }
    
    /// <summary>
    /// Base implementation of a stat with modifier support and value caching.
    /// Uses lazy evaluation to recalculate values only when needed.
    /// </summary>
    [Serializable]
    public class BaseStat : IStat, IStatModifierOperations {
        [SerializeField] protected float baseValue;
        [SerializeField] protected float currentValue;
        [SerializeField] protected float initialValue;
        
        private bool m_isDirty;
        protected readonly IStatMediator statMediator;
        
        public float InitialValue => initialValue;

        /// <summary>
        /// Gets the current value, recalculating if dirty.
        /// </summary>
        public float CurrentValue {
            get {
                if (!m_isDirty) return currentValue;
                RecalculateValue();
                m_isDirty = false;
                return currentValue;
            }
        }

        /// <summary>
        /// Gets or sets the base value before modifiers.
        /// Setting this value marks the stat as dirty for recalculation.
        /// </summary>
        public float BaseValue {
            get {
                if (!m_isDirty) return baseValue;
                RecalculateValue();
                m_isDirty = false;
                return baseValue;
            }
        }

        /// <summary>
        /// Event that is fired when the stat value changes.
        /// </summary>
        public event Action<float> OnCurrentValueChanged;
        
        /// <summary>
        /// Event that is fired when the base value changes.
        /// </summary>
        public event Action<float> OnBaseValueChanged;

        /// <summary>
        /// Creates a new BaseStat with the specified initial value.
        /// </summary>
        /// <param name="value">The initial and base value for the stat</param>
        public BaseStat(float value) {
            initialValue = value;
            baseValue = value;
            currentValue = value;
            statMediator = new StatMediator();
            m_isDirty = false;
        }

        /// <summary>
        /// Marks the stat as needing recalculation on next access.
        /// </summary>
        protected void MarkDirty() {
            m_isDirty = true;
        }
        
        /// <summary>
        /// Template method that handles the recalculation flow.
        /// Derived classes can override ProcessValues to apply constraints.
        /// </summary>
        protected virtual void RecalculateValue() {
            // first item is the base value, second is the modified value
            var newValues = statMediator.CalculateValue(baseValue);
            var processedValues = ProcessValues(newValues.Item1, newValues.Item2);
            
            if (!Mathf.Approximately(baseValue, processedValues.baseValue)) {
                baseValue = processedValues.baseValue;
                OnBaseValueChanged?.Invoke(baseValue);
            }
            
            if (!Mathf.Approximately(currentValue, processedValues.currentValue)) {
                currentValue = processedValues.currentValue;
                OnCurrentValueChanged?.Invoke(currentValue);
            }
        }

        /// <summary>
        /// Process the calculated values. Base implementation returns values unchanged.
        /// Override in derived classes to apply constraints like clamping.
        /// </summary>
        protected virtual (float baseValue, float currentValue) ProcessValues(float baseVal, float currentVal) {
            return (baseVal, currentVal);
        }


        public void AddModifier(IStatModifier modifier) {
            statMediator.AddModifier(modifier);
            MarkDirty();
        }

        public void AddModifierWithTag(IStatModifier modifier, string tag) {
            statMediator.AddModifierWithTag(modifier, tag);
            MarkDirty();
        }

        public void RemoveModifier(IStatModifier modifier) {
            statMediator.RemoveModifier(modifier);
            MarkDirty();
        }

        public void RemoveModifierWithTag(string tag) {
            statMediator.RemoveModifierWithTag(tag);
            MarkDirty();
        }

        public List<IStatModifier> GetModifiers() {
            return statMediator.GetModifiers().ToList();
        }

        /// <summary>
        /// Resets the stat to its initial state, removing all modifiers.
        /// </summary>
        public void Reset() {
            var modifiers = statMediator.GetModifiers().ToList();
            foreach (var modifier in modifiers) {
                statMediator.RemoveModifier(modifier);
            }
            
            baseValue = initialValue;
            currentValue = initialValue;
            MarkDirty();
        }

        /// <summary>
        /// Cleans up event subscriptions.
        /// </summary>
        public virtual void Dispose() {
            OnCurrentValueChanged = null;
            OnBaseValueChanged = null;
        }
    }

    /// <summary>
    /// Core Stat implementation that extends BaseStat with range constraints, this class is returned from the StatContainer.
    /// Extended stat implementation with min/max range constraints.
    /// Values are automatically clamped to the configured range.
    /// </summary>
    [Serializable]
    public class Stat : BaseStat, IStatRangeOperations {
        private StatRangeConfiguration m_rangeConfig;
        
        /// <summary>
        /// Gets the stat used for minimum range calculation, if any.
        /// </summary>
        public BaseStat MinRangeStat => m_rangeConfig?.MinStat;
        
        /// <summary>
        /// Gets the stat used for maximum range calculation, if any.
        /// </summary>
        public BaseStat MaxRangeStat => m_rangeConfig?.MaxStat;

        /// <summary>
        /// Creates a new Stat with range constraint support.
        /// </summary>
        /// <param name="baseValue">The initial and base value for the stat</param>
        public Stat(float baseValue) : base(baseValue) {
            m_rangeConfig = new StatRangeConfiguration();
            m_rangeConfig.SetRangeValueChanged(OnRangeStatChanged);
        }
        
        /// <summary>
        /// Applies range constraints to the calculated values.
        /// </summary>
        protected override (float baseValue, float currentValue) ProcessValues(float baseVal, float currentVal) {
            return (m_rangeConfig.ClampValue(baseVal), m_rangeConfig.ClampValue(currentVal));
        }
        
        public Stat WithMinRange(float min) {
            m_rangeConfig.SetMinValue(min);
            MarkDirty();
            return this;
        }
    
        public Stat WithMinRange(Stat min) {
            if (min == null) throw new ArgumentNullException(nameof(min));
            m_rangeConfig.SetMinStat(min);
            MarkDirty();
            return this;
        }
    
        public Stat WithMaxRange(float max) {
            m_rangeConfig.SetMaxValue(max);
            MarkDirty();
            return this;
        }
    
        public Stat WithMaxRange(Stat max) {
            if (max == null) throw new ArgumentNullException(nameof(max));
            m_rangeConfig.SetMaxStat(max);
            MarkDirty();
            return this;
        }

        /// <summary>
        /// Callback for when range constraint stats change their values.
        /// Marks this stat as dirty to recalculate with new constraints.
        /// </summary>
        /// <param name="_">Unused parameter from the event</param>
        private void OnRangeStatChanged(float _) {
            m_rangeConfig.ValidateRanges();
            MarkDirty();
        }
    }
}