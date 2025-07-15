# Pastime Unity Stat System

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Made For Unity](https://img.shields.io/badge/Made%20for-Unity-blue)](https://unity3d.com)
[![Maintenance](https://img.shields.io/badge/Maintained%3F-yes-brightgreen.svg)](https://github.com/meredoth/Unity-Fluent-Debug/graphs/commit-activity)

A flexible and extensible stat system for Unity that provides comprehensive attribute management with modifier support, configurable application ordering, and range constraints. Perfect for RPGs, roguelikes, and any game requiring complex numerical systems.

## Requirements

- Unity 2022.3 or later
- UI Toolkit (for stat container property drawer)

## Installation

This can be installed via the Unity Package Manager by adding the following Git URL:
`https://github.com/Stimpae/pastime-unity-stats.git`

## Quick Start

### Creating a Basic Stat

``c#
using Pastime.Stats;

// Create a stat with a base value of 10
Stat strength = new Stat(10f);

// Access the current value
Debug.Log($"Strength: {strength.CurrentValue}"); // Output: 10
```

## Adding Modifiers to Stats
You can then create some modifiers to apply to the stat:
```c#
// Create a modifier that adds 50% to the base value
StatModifier levelBonus = new StatModifier(
    EModifierType.PERCENTAGE_ADD, 
    EModifierTarget.BASE_VALUE, 
    50f
);

// Create a modifier that adds 25 to the current value
StatModifier equipmentBonus = new StatModifier(
    EModifierType.ADD, 
    EModifierTarget.CURRENT_VALUE, 
    25f
);

// Add modifiers to the stat
strength.AddModifier(levelBonus);
strength.AddModifier(equipmentBonus, "equipment"); // With tag for easy removal

// Calculation flow:
// 1. Base value: 10
// 2. Apply BASE_VALUE PERCENTAGE_ADD: 10 * (1 + 50/100) = 15
// 3. Apply CURRENT_VALUE ADD: 15 + 25 = 40
Debug.Log($"Modified Strength: {strength.CurrentValue}"); // Output: 40
```

### Removing Modifiers
```c#
// Remove by reference
strength.RemoveModifier(levelBonus);

// Remove by tag
strength.RemoveModifier("equipment");
```

### Modifiers Application Order
Above we can see how modifiers are applied to a stat and the calculation flow in which base value modifiers are applied first, followed by current value modifiers.
Below is our application order for modifiers, this can be altered in the `m_applicationOrder` array in the `StatModifierApplicationOrder` class:
```c#
private readonly EModifierType[] m_applicationOrder = {
    EModifierType.SUBTRACT, EModifierType.PERCENTAGE_SUBTRACT,
    EModifierType.ADD, EModifierType.PERCENTAGE_ADD,
    EModifierType.MULTIPLY, EModifierType.PERCENTAGE_MULTIPLY
};
```
### Modifier Types
| Type | Description | Formula |
| --- | --- | --- |
| `ADD` | Adds a flat value | `value + modifier` |
| `SUBTRACT` | Subtracts a flat value | `value - modifier` |
| `MULTIPLY` | Multiplies by a value | `value * modifier` |
| `PERCENTAGE_ADD` | Adds a percentage | `value * (1 + modifier/100)` |
| `PERCENTAGE_SUBTRACT` | Subtracts a percentage | `value * (1 - modifier/100)` |
| `PERCENTAGE_MULTIPLY` | Multiplies by a percentage | `value * (1 + modifier/100)` |

## Advanced Usage
### Using Stat Containers
Stat containers provide organized, type-safe access to multiple stats:
```c#
// Define your stat types
public enum PlayerStats {
    Health,
    Mana,
    Strength,
    Agility,
    Intelligence
}

// Create a container
StatContainer<PlayerStats> playerStats = new StatContainer<PlayerStats>();

// Register stats
playerStats.RegisterStat(PlayerStats.Health, 100f);
playerStats.RegisterStat(PlayerStats.Mana, 50f);
playerStats.RegisterStat(PlayerStats.Strength, 10f);

// Access stats
Stat health = playerStats.GetStat(PlayerStats.Health);
float currentHealth = playerStats.GetStatValue(PlayerStats.Health);

// Safe access
if (playerStats.TryGetStat(PlayerStats.Agility, out Stat agility)) {
    // Use agility stat
}
```

Always make sure you dispose of the stat container when done to clean up resources.
```c#
void OnDestroy() {
    playerStats.Dispose(); // Cleans up event subscriptions and resources
}
```

### Range Constraints
Stats can be constrained to which is useful for resource stats like health, mana, etc. 
This allows you to set minimum and maximum values for a stat, and automatically clamps the current value within that range.
```c#
// Create health and max health stats
Stat health = new Stat(100f);
Stat maxHealth = new Stat(100f);
maxhealth.WithMinRange(100f).WithMaxRange(200f); 
// max health can be between 100 and 200

// Set health to be constrained between 0 and maxHealth
health.WithMinRange(0f).WithMaxRange(maxHealth);

// When maxHealth changes, health will automatically be clamped
maxHealth.AddModifier(new StatModifier(EModifierType.ADD, EModifierTarget.CURRENT_VALUE, 50f));
// maxHealth is now 150, health remains 100 (within range)

// If health tries to exceed maxHealth, it will be clamped
health.AddModifier(new StatModifier(EModifierType.ADD, EModifierTarget.CURRENT_VALUE, 100f));
// health would be 200, but gets clamped to 150 (maxHealth's value)
```
## Best Practices
### Listening to Stat Changes
Always subscribe to stat events for reactive gameplay:
```c#
public class PlayerController : MonoBehaviour {
    private StatContainer<PlayerStats> stats;
    private Stat healthStat
    
    void Start() {
        stats = new StatContainer<PlayerStats>();
       
        healthStat = stats.RegisterStat(PlayerStats.Health, 100f);
        health.OnCurrentValueChanged += OnHealthChanged;
    }
    
    private void OnHealthChanged(float newHealth) {
        // Update health bar, check for death, etc.
        healthBar.SetValue(newHealth / stats.GetStat(PlayerStats.Health).CurrentValue);
        // or
        healthBar.SetValue(newHealth / healthStat.CurrentValue);
    }
    
    void OnDestroy() {
        stats?.Dispose(); // Clean up resources
    }
}
```
### Appropriate Modifier Targets
Choose the right target for your modifiers:
- **BASE_VALUE**: For permanent upgrades (level-ups, skill points) these get applied before any other modifiers.
- **CURRENT_VALUE**: For temporary effects (buffs, curses, equipment, consumables) these get applied after base value modifiers.
```c#
// Permanent strength increase from leveling up
var levelBonus = new StatModifier(EModifierType.ADD, EModifierTarget.BASE_VALUE, 5f);

// Temporary strength boost from a potion
var potionBonus = new StatModifier(EModifierType.PERCENTAGE_ADD, EModifierTarget.CURRENT_VALUE, 20f);
```

We have two different targets for modifiers to allow for flexibility in how stats are modified and to
make it so our initial stat is immutable. This way we can have a initial value for a character or enemy in a scriptable object or a database,
and that value will never have to change.

### Debugging Stats
Just make sure that your `StatContainer` as a SerializableField, and Unity's inspector will handle it automatically, showing the stats in a debug view.
Currently there is no way to view what modifiers are applied to a stat, but this is something that will be added in the future. (hopefully)
```c#

// Define your stat types
public enum PlayerStats {
    Health,
    Mana,
    Strength,
    Agility,
    Intelligence
}

// Because our StatContainer is serializable, our property drawer will handle it automatically
// for Unity's inspector and show the stats in a debug view.
public class CharacterController : MonoBehaviour {
    [SerializeField] private StatContainer<PlayerStats> playerStats;
    
    void Start() {
        StatContainer<PlayerStats> playerStats = new StatContainer<PlayerStats>();
        
        // Register stats
        playerStats.RegisterStat(PlayerStats.Health, 100f);
        playerStats.RegisterStat(PlayerStats.Mana, 50f);
        playerStats.RegisterStat(PlayerStats.Strength, 10f);
    }
}
```
## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.




