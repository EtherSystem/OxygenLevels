
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

All altitude state thresholds can be modified.  

Indoor scenes use the last known outdoor altitude instead of always defaulting to normal oxygen. This prevents oxygen effects from instantly disappearing when entering an indoor scene at high altitude.

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

### Acclimatization System (reworked)

Remaining at high altitude gradually builds acclimatization.

Acclimatization now has two levels:

- **Acclimatized I**, gained from prolonged exposure to **Critical O₂**.
- **Acclimatized II**, gained from prolonged exposure to **Insufficient O₂** after Acclimatized I has already been earned.

Once fully acclimatized, penalties in the specific state are partially reduced.

Descending reduces acclimatization over time and the AMS prevent acclimatization progress while active.

### Acute Mountain Sickness (AMS)

Spending extended time at Insufficient O₂ without Acclimatized I **will** trigger an **AMS Risk** after a configurable exposure time. Once eligible, the chance to develop AMS Risk is rolled every in-game hour.

AMS Risk will evolve into actual AMS if the player does not descend to Critical O₂ or lower in time.

AMS drains condition over time. The drain intensity is controlled by the AMS lethality preset:

- **Forgiving**
- **Standard**
- **Harsh**
- **Brutal**
- **Who Wants to Play Like This?**

Once an AMS is fully developed, natural condition recovery is blocked and an internal timer begins to accumulate, counting the time spent in Insufficient O₂ with the AMS active.

The only way to get rid of the AMS is to descend **at least** to Critical O₂. From there, the AMS will disappear after a certain time calculated from the "AMS disappearance time" setting and the time spent with the actual AMS in Insufficient O₂.

The calculation is simple: `total AMS recovery time = time spent in Insufficient O₂ - (1 hour in-game x AMS disappearance time)`

Therefore, with the default setting, the recovery time is twice as fast as the accumulation time. If you spend 2 hours in Insufficient O₂ with an active AMS, it will take 1 hour to get rid of it once you leave that altitude.

Considering that a condition drain occurs over time, although slow, it does persist. The risk isn't immediate, but it can punish you very quickly.  
With the default settings, remaining at Insufficient O₂ for more than 13 hours and 20 minutes will trigger a lethal condition drain, as it will apply the AMS for a total of 20 hours. By only changing the preset for "Who Wants to Play Like This?", 2 hours with an AMS at Insufficient O₂ is enough to become lethal, as it triggers an AMS for 3 hours with a very severe condition drain. This can kill the player almost as quickly as hypothermia.

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
2.  Install  [AfflictionComponent](https://github.com/TLD-Mods/AfflictionComponent),  [ModComponent](https://github.com/dommrogers/ModComponent),  [ModSettings](https://github.com/DigitalzombieTLD/ModSettings/) and [ModData](https://github.com/dommrogers/ModData).
3.  Place  `OxygenLevels.dll`  inside your Mods folder.

