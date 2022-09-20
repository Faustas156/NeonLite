# NeonLite
 The Quality of Life Mod for All Your Neon White needs!

Still currently a work in progress and thus it only features very limited amount of mods.

## Current Mods

* PB Tracker Mod (shoutout MOPPI big time for helping me with this mod!!)
  * Displays a time based on whether or not you got a new personal best.
* Boss HP Display
  * Displays the HP of Neon Green in Text Form.


## Installation & Usage

1. Download [MelonLoader](https://github.com/LavaGang/MelonLoader/releases/latest) and install it onto your `Neon White.exe`.
2. Run the game once. This will create required folders; you should see a splash screen if you installed the modloader correctly.
3. Download the **Mono** version of [Melon Preferences Manager](https://github.com/sinai-dev/MelonPreferencesManager/releases/latest), and put the .dlls from that zip into the `mods` folder of your Neon White install (e.g. `SteamLibrary\steamapps\common\Neon White\Mods`)
    * The IL2CPP version **WILL NOT WORK**; you **must** download `MelonPreferencesManager.Mono.zip`. 
4. Download the `NeonWhiteQoL.dll` from the [Releases page](https://github.com/Faustas156/NeonLite/releases/) and drop it in the mods folder.

### Additional Notes

Unless you're a mod developer, make sure to add `--melonloader.hideconsole` to your game launch properties (right click the game in steam -> properties -> launch options at the bottom of that window) to hide the console that melonloader loads in. It makes your game boot up faster xd