# OWO_Valheim
OWO Valheim integration. This mod is based on [this file](https://github.com/brandonmousseau/vhvr-mod/blob/master/ValheimVRMod/Patches/BHapticsPatches.cs) of [brandonmousseau/vhvr-mod](https://github.com/brandonmousseau/vhvr-mod)

## What is OWO?
OWO Skin is a haptic technology that lets you feel everything that happens in a video game.  
OWO is capable of delivering highly realistic and precise sensations, such as the feeling of impact, the recoil of your weapons or even the subtle sensation of insects moving across your skin.

Want to get an OWO Suit? [Look here](https://owogame.com/shop/).

# Installation [BepinEx_v5.4.22]
- Download [BepinEx_v5.4.22](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.22).
- Extract the BepinEx zip data on the same folder of the game and run the game once.
- Download the [lastest release of this mod](https://github.com/OWODevelopers/OWO_Valheim/releases/latest)
- Extract the mod zip and place all files and the owo folder into the BepInEx\Plugins directory of your game installation.
- Enjoy your inmersive experience! 😊

# Featured effect
- Heart Beat
- Attack
- Block
- Impact
- Pet
- Rain
- Thunder
- Teleport
- String Bow
- Eat
- Set/Use Guardian Power
- Boss Spawn
- Boss Attacks
- Ship Damage
- Death
- Corpse Explosion
- Jump
- Landing
- Bad Status Effects
- Building and Repairing
- Grappling and Release
- Perfect Dodge
- Adrenaline Full Bar
- Snow Impact
- Achievement Unlock

# Valheim 1.0 tactile events
Valheim 1.0 marks selected sound effects as tactile events. The mod correlates those events with recent player and boss attacks to discover new weapons and boss moves, then triggers an existing curated OWO sensation when a semantic mapping is known. Valheim's controller vibration strength is never converted into electrostimulation intensity.

Each previously unseen combination is written once to the BepInEx log as `Tactile event discovered`. The entry includes the clip, weapon, boss event, attack animation, ownership, and vibration modifier so new `.owo` sensations can be reviewed and mapped deliberately.

Valheim 1.0 also has dedicated semantic hooks for grappling, perfect dodges, a full adrenaline bar, snow interaction, and achievement unlocks. Their `.owo` files start from existing reviewed patterns and remain separate so each sensation can be tuned independently.

# Manual Connection
If you are having trouble with the automatic connection, create a .txt file called OWO_Manual_IP.txt  
and write the IP of where you placed the OWO mod.
