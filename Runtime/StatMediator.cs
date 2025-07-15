using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pastime.Stats {
    /// <summary>
    /// Interface for mediating stat modifier operations and value calculations.
    /// Handles the application of modifiers to base stat values.
    /// </summary>
    public interface IStatMediator {
        void AddModifier(IStatModifier modifier);
        void AddModifierWithTag(IStatModifier modifier, string tag);
        void RemoveModifier(IStatModifier modifier);
        void RemoveModifierWithTag(string tag);
        (float,float) CalculateValue(float baseValue);
        IEnumerable<IStatModifier> GetModifiers();
    }
    
    /// <summary>
    /// Concrete implementation of stat mediator that manages modifier collections and value calculation.
    /// Supports both untagged and tagged modifiers for flexible modifier management.
    /// Uses an application order strategy to determine how modifiers are applied to base values.
    /// </summary>
    public class StatMediator : IStatMediator {
        private readonly List<IStatModifier> m_modifiers = new();
        private readonly Dictionary<string, IStatModifier> m_taggedModifiers = new();
        private readonly IStatModifierApplicationOrder m_applicationOrder;
        
        /// <summary>
        /// Creates a new StatMediator with optional custom application order.
        /// </summary>
        /// <param name="applicationOrder">Strategy for determining modifier application order. Uses default if null.</param>
        public StatMediator() {
            m_applicationOrder = new StatModifierApplicationOrder();
        }
        
        /// <summary>
        /// Adds a modifier to the collection without a tag.
        /// </summary>
        /// <param name="modifier">The modifier to add</param>
        /// <exception cref="ArgumentNullException">Thrown when modifier is null</exception>
        public void AddModifier(IStatModifier modifier) {
            if (modifier == null) throw new ArgumentNullException(nameof(modifier));
            m_modifiers.Add(modifier);
        }
        
        /// <summary>
        /// Adds a modifier with an associated tag for easier identification and removal.
        /// </summary>
        /// <param name="modifier">The modifier to add</param>
        /// <param name="tag">Unique tag to associate with the modifier</param>
        /// <exception cref="ArgumentNullException">Thrown when modifier is null</exception>
        /// <exception cref="ArgumentException">Thrown when tag is null or empty</exception>
        /// <exception cref="InvalidOperationException">Thrown when a modifier with the same tag already exists</exception>
        public void AddModifierWithTag(IStatModifier modifier, string tag) {
            if (modifier == null) throw new ArgumentNullException(nameof(modifier));
            if (string.IsNullOrEmpty(tag)) throw new ArgumentException("Tag cannot be null or empty", nameof(tag));
            
            m_modifiers.Add(modifier);
            if (!m_taggedModifiers.TryAdd(tag, modifier)) {
                throw new InvalidOperationException($"A modifier with tag '{tag}' already exists");
            }
        }
        
        /// <summary>
        /// Removes a specific modifier from the collection and its associated tag if any.
        /// </summary>
        /// <param name="modifier">The modifier to remove</param>
        /// <exception cref="ArgumentNullException">Thrown when modifier is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when the modifier is not found</exception>
        public void RemoveModifier(IStatModifier modifier) {
            if (modifier == null) throw new ArgumentNullException(nameof(modifier));
            if (!m_modifiers.Remove(modifier)) {
                throw new InvalidOperationException("Modifier not found");
            }
            
            var tagToRemove = m_taggedModifiers.FirstOrDefault(kvp => kvp.Value == modifier).Key;
            if (tagToRemove != null) {
                m_taggedModifiers.Remove(tagToRemove);
            }
        }
        
        /// <summary>
        /// Removes a modifier by its associated tag.
        /// </summary>
        /// <param name="tag">The tag identifying the modifier to remove</param>
        /// <exception cref="ArgumentException">Thrown when tag is null or empty</exception>
        /// <exception cref="InvalidOperationException">Thrown when no modifier with the specified tag is found</exception>
        public void RemoveModifierWithTag(string tag) {
            if (string.IsNullOrEmpty(tag)) throw new ArgumentException("Tag cannot be null or empty", nameof(tag));
            
            if (m_taggedModifiers.TryGetValue(tag, out var modifier)) {
                m_modifiers.Remove(modifier);
                m_taggedModifiers.Remove(tag);
            } else {
                throw new InvalidOperationException($"No modifier found with tag '{tag}'");
            }
        }
        
        /// <summary>
        /// Calculates the final value by applying all modifiers to the base value.
        /// Returns the base value unchanged if no modifiers are present.
        /// </summary>
        /// <param name="baseValue">The base value before modifier application</param>
        /// <returns>The calculated value after applying all modifiers</returns>
        public  (float,float) CalculateValue(float baseValue) {
            return m_modifiers.Count == 0 ? (baseValue, baseValue) : m_applicationOrder.ApplyInOrder(m_modifiers, baseValue);
        }

        /// <summary>
        /// Gets all currently registered modifiers, as read-only.
        /// </summary>
        /// <returns>An enumerable collection of all modifiers</returns>
        public IEnumerable<IStatModifier> GetModifiers() => m_modifiers.ToList().AsReadOnly();
    }
}