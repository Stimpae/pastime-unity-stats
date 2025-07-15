using System;
using System.Collections.Generic;
using System.Linq;


namespace Pastime.Stats {
    /// <summary>
    /// Interface for defining the order in which stat modifiers are applied to values.
    /// </summary>
    public interface IStatModifierApplicationOrder {
        (float,float) ApplyInOrder(IEnumerable<IStatModifier> statModifiers, float baseValue);
    }

    /// <summary>
    /// Default implementation that applies stat modifiers in a predefined order.
    /// First applies base value modifiers, then current value modifiers.
    /// Within each group, modifiers are applied by type in the order: subtract, percentage subtract, add, percentage add, multiply, percentage multiply.
    /// </summary>
    public class StatModifierApplicationOrder : IStatModifierApplicationOrder {
        private readonly EModifierType[] m_applicationOrder = {
            EModifierType.SUBTRACT,
            EModifierType.PERCENTAGE_SUBTRACT,
            EModifierType.ADD,
            EModifierType.PERCENTAGE_ADD,
            EModifierType.MULTIPLY,
            EModifierType.PERCENTAGE_MULTIPLY
        };
    
        /// <summary>
        /// Applies modifiers to the base value in the defined order.
        /// Base value modifiers are applied first, followed by current value modifiers.
        /// </summary>
        /// <param name="modifiers">Collection of modifiers to apply</param>
        /// <param name="baseValue">The initial base value before any modifications</param>
        /// <returns>The final calculated value after applying all modifiers</returns>
        public  (float,float) ApplyInOrder(IEnumerable<IStatModifier> modifiers, float baseValue) {
            var statModifiers = modifiers.ToList();
            var currentValueModifiers = statModifiers.Where(m => m.Target == EModifierTarget.CURRENT_VALUE).ToList();
            var baseValueModifiers = statModifiers.Where(m => m.Target == EModifierTarget.BASE_VALUE).ToList();

            // base value modifiers to get the modified base value
            var modifiedBaseValue = ApplyModifiersToValue(baseValueModifiers, baseValue);

            // Apply current value modifiers starting from the modified base value
            var finalValue = ApplyModifiersToValue(currentValueModifiers, modifiedBaseValue);

            return (modifiedBaseValue, finalValue);
        }

        /// <summary>
        /// Applies a collection of modifiers to a value in the predefined type order.
        /// Groups modifiers by type and applies them sequentially according to the application order array.
        /// </summary>
        /// <param name="modifiers">Collection of modifiers to apply</param>
        /// <param name="value">The initial value to modify</param>
        /// <returns>The value after applying all modifiers</returns>
        private float ApplyModifiersToValue(IEnumerable<IStatModifier> modifiers, float value) {
            var modifiersByType = modifiers.GroupBy(m => m.ModifierType)
                .ToDictionary(g => g.Key, g => g.ToList());

            var currentValue = value;
            foreach (var modifierType in m_applicationOrder) {
                if (!modifiersByType.TryGetValue(modifierType, out var typeModifiers)) continue;
                currentValue =
                    typeModifiers.Aggregate(currentValue, (current, modifier) => modifier.Calculate(current));
            }

            return currentValue;
        }
    }
}