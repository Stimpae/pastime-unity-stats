using System;

namespace Pastime.Stats {
    /// <summary>
    /// Defines which value the modifier should target during calculation.
    /// </summary>
    public enum EModifierTarget {
        CURRENT_VALUE,
        BASE_VALUE
    }
    
    /// <summary>
    /// Defines the type of mathematical operation to perform on the stat value.
    /// </summary>
    public enum EModifierType {
        ADD,
        MULTIPLY,
        SUBTRACT,
        PERCENTAGE_ADD,
        PERCENTAGE_MULTIPLY,
        PERCENTAGE_SUBTRACT,
    } 
    
    /// <summary>
    /// Interface for stat modifiers that can perform calculations on stat values.
    /// </summary>
    public interface IStatModifier {
        public EModifierType ModifierType { get; }
        EModifierTarget Target { get; }
        float Calculate(float statValue);
    }
    
    /// <summary>
    /// Concrete implementation of a stat modifier that applies mathematical operations to stat values.
    /// Supports various operation types including basic arithmetic and percentage-based calculations.
    /// </summary>
    public class StatModifier : IStatModifier {
        public EModifierType ModifierType { get; }
        public EModifierTarget Target { get; }
        private readonly float m_value;

        /// <summary>
        /// Creates a new StatModifier with the specified operation type, target, and value.
        /// </summary>
        /// <param name="modifierType">The type of mathematical operation to perform</param>
        /// <param name="target">Which value (base or current) the modifier should target</param>
        /// <param name="value">The value to use in the calculation</param>
        public StatModifier(EModifierType modifierType, EModifierTarget target, float value) {
            ModifierType = modifierType;
            Target = target;
            m_value = value;
        }

        /// <summary>
        /// Calculates the modified value based on the modifier type and input value.
        /// Percentage operations treat the modifier value as a percentage (e.g., 50 = 50%).
        /// </summary>
        /// <param name="value">The input value to modify</param>
        /// <returns>The calculated result after applying the modifier</returns>
        public float Calculate(float value) {
            return ModifierType switch {
                EModifierType.ADD => value + m_value,
                EModifierType.SUBTRACT => value - m_value,
                EModifierType.MULTIPLY => value * m_value,
                EModifierType.PERCENTAGE_ADD => value * (1f + m_value / 100f),
                EModifierType.PERCENTAGE_SUBTRACT => value * (1f - m_value / 100f),
                EModifierType.PERCENTAGE_MULTIPLY => value * (1f + m_value / 100f),
                _ => value
            };
        }
    }
}