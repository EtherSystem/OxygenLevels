
# OxygenLevels

OxygenLevels introduces altitude-based oxygen deprivation to The Long Dark.

As altitude increases, available oxygen decreases.  
Reduced oxygen impacts stamina, fatigue, and physical performance.

## How It Works

### Altitude States

The system evaluates the player's vertical position and applies one of four states:

- **Normal**
- **Low**
- **Critical**
- **Insufficient**

Each state dynamically modifies gameplay parameters.
Transitions between states trigger localized HUD warnings.
All altitude state thresholds can be modified.

### Gameplay Effects

Depending on altitude tier, the mod adjusts the following values:

- Stamina regeneration rate
- Stamina consumption while sprinting
- Fatigue increase while sprinting
- Delay before stamina recovery
- Fire starting duration
- Dysentery recovery duration
- Food poisoning recovery duration

Effects are modulated according to the altitude state, but are highly customizable.

At extreme altitude, walking depletes endurance, and walking without endurance result in direct condition loss.

### Acclimatization System

Remaining at high altitude gradually builds acclimatization.

Once fully acclimatized, penalties in critical states are partially reduced.

Descending reduces acclimatization over time.

Acclimatization is fully configurable via settings.

### Acute Mountain Sickness (AMS)

Spending extended time at extreme altitude **may** trigger:

- **AMS Risk**
- **AMS**

The system:

- Tracks time spent at critical altitude
- Rolls a configurable chance periodically
- Applies AMS Risk first
- Automatically cures AMS if the player remains below the last altitude state for too long

Acclimatized players are protected from AMS onset.


## Planned Features (SoonTM trust)

A Oxygen Mask / Oxygen Tank item is planned for a future update.

The goal is to allow players to temporarily mitigate altitude penalties.


## Localization

OxygenLevels is currently available in:

- English  
- French  
- German  
- Spanish  
- Turkish  
- Russian  

If you notice inconsistencies or translation issues, feedback is welcome.

## Installation


1.  Install MelonLoader.
2.  Install  [AfflictionComponent](https://github.com/TLD-Mods/AfflictionComponent),  [ModComponent](https://github.com/dommrogers/ModComponent)  and  [ModSettings](https://github.com/DigitalzombieTLD/ModSettings/).
3.  Place  `OxygenLevels.dll`  inside your Mods folder.
