# Tanks 3D
This is the Assets folder for my Tanks 3D Unity game.

The game can be played via this link: [https://viper1618.itch.io/tanks-3d](url)

## About
3 different campaigns with unique levels were handcrafted. All textures and models were also handcrafted using Blender, making use of procedural shaders.

The game has multiplayer features allowing players to compete against each other in FFA or teams modes, or work together in the campaigns in Co-Op mode.

There are also level editing features where players can create custom maps and can also use their custom maps in multiplayer.

All features from the original Wii Play Tanks game were implemented in 3D, including the original 100 levels. An additional 2 campaigns and 55 custom levels were created.

## Custom Tank AIs
6 custom tanks were created that are not part of the original Wii Play Tanks

### Lime Tank
An upgraded version of the green tank that is also stationary and has the same 2-ricochet rocket bullets. However, its barrel is not restricted from pivoting fully vertically, allowing it to utilize ricochets off the ceiling.

### Aquamarine Tank
A further upgrade to the Lime Tank, inheriting all features of the Lime Tank but with the added ability to move.

### Blue Tank
Shoots in volleys of 3 rocket bullets that each ricochet once. Extremely tricky to deal with since it can predict its bullet ricochets. Does not place mines.

### Orange Tank
Places mines in smart locations near destructible elements as traps and waits for a player to go within the mine's blast radius to shoot at the mine to trigger it. However, it is fairly dumb when shooting directly at the player, and it does not calculate bullet ricochets.

### Gold Tank
Similar trapping logic to the Orange Tank, but it will aggressively use mines to clear paths to get closer to the player. It will also place mines behind any walls instead of only near destructible elements, and can calculate bullet ricochets to trigger its mines or kill the player. It can also factor in the future position of the player. Additionally, this tank can determine if a chain of mines will eventually kill a player and trigger them if so.

### Silver Tank
Uses missiles instead of bullets and will intelligently use its missiles to kill the player within the missile blast radius, even if not directly visible or behind walls. This tank will also factor in the future position of the player, making its missiles and area of effect even more dangerous. 
