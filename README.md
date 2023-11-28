# SharpTimer
SharpTimer is a simple Surf/KZ/Bhop/MG/Deathrun/etc. CS2 Timer plugin using CounterStrikeSharp

### Dependencies

[**MetaMod**](https://cs2.poggu.me/metamod/installation/)

[**CounterStrikeSharp**](https://github.com/roflmuffin/CounterStrikeSharp/releases)

[**MovementUnlocker** *(optional)*](https://github.com/Source2ZE/MovementUnlocker)

## Features
<p align="center">
<strong style="font-weight: bold;">----------------- [Timer, Speedometer and Keys] -----------------</strong>
</p>

<p align="center">
  <img src="https://i.imgur.com/cGUjH6m.png">
</p>
<br>
<br>
<br>
<p align="center">
<strong style="font-weight: bold;">-------------------------- [Players PBs] --------------------------</strong>
</p>

<p align="center">
  <img src="https://i.imgur.com/9HGOhRR.png">
</p>

<p align="center">
  <img src="https://i.imgur.com/amVXOHP.png">
</p>
<br>
<br>
<br>
<p align="center">
<strong style="font-weight: bold;">----------------- [Checkpoints (disabled by default)] -----------------</strong>
</p>

<p align="center">
  <img src="https://i.imgur.com/USX5i8C.png">
</p>

<p align="center">
  <img src="https://i.imgur.com/kWiHOlz.png">
</p>

<p align="center">
  <img src="https://i.imgur.com/lXwXNN7.png">
</p>

<p align="center">
  <img src="https://i.imgur.com/nyn76Q4.png">
</p>

### Installing

* Unzip into your servers `game/csgo/` dir
* Its recommended to have a custom server cfg with your desired settings (for example surf or kz)
  
  Like this one for Surf: https://raw.githubusercontent.com/DEAFPS/cs-cfg/main/surf.cfg
  
  You can replace the contents of `gamemode_competitive.cfg` or `gamemode_casual.cfg` with it
  
* Here a collection of maps supported by default: https://steamcommunity.com/sharedfiles/filedetails/?id=3095738559

## Commands

| Command  | What it does |
| ------------- | ------------- |
| `!r`  | Teleports the player back to Spawn |
| `!top`  | Prints the top 10 times on the current map |
| `!rank` | Tells you your rank on the current map |
| `!cp` | Sets a checkpoint |
| `!tp` | Teleports the player to the checkpoint |
| `!prevcp` | Teleports the player to the previous checkpoint |

### Configuration
* See `game/csgo/cfg/SharpTimer/config.cfg` for basic plugin configuration *(yes you can enable checkpoints there)*

* To add Map Start and End zones you can simply add the `targetnames` of the triggers in the `mapdata.json` inside of `game/csgo/cfg/SharpTimer/` using `MapStartTrigger` and  `MapEndTrigger`


  Many maps do not contain any `startzone` or `endzone` triggers. As a workaround you can setup the trigger manually be defining its opposite corner coordinates with `MapStartC1` and `MapStartC2`! if you are using the `getpos` or `cl_showpos 1` to get the coordinates you will have to subtract `64 units` from the Z axis since the coordinates given are at the height of your camera and not your feet! You also need to define the `RespawnPos` for the `!r` command using `RespawmPos`

  Here is a Example of what the `mapdata.json` can look like with both map triggers and manual triggers:

  
```
{
  "surf_kitsune": {
    "MapStartTrigger": "stage1_start",
    "MapEndTrigger": "stage9_end",
  },
  "surf_beginner": {
    "MapStartTrigger": "stage1_trigger",
    "MapEndTrigger": "end_trigger",
  },
  "surf_boomer": {
    "MapStartTrigger": "zippan_start",
    "MapEndTrigger": "zippan_end",
  },
  "surf_mesa_revo": {
    "MapStartC1": "255.63 -1360 8928",
    "MapStartC2": "-259.002686 -832 8928",
    "MapEndC1": "-393 14047 -13759",
    "MapEndC2": "378.517792 13067.639648 -13759",
    "RespawnPos": "-64 -1040 8992"
  },
  "surf_utopia_njv": {
    "MapStartC1": "-13769 512 12800",
    "MapStartC2": "-14319 -512 12800",
    "MapEndC1": "-13825 -512 -6223",
    "MapEndC2": "-14319 527 -6223",
    "RespawnPos": "-13904 336 12864"
  },
}
```
* I will try to add more map data by default in the future :) pull requests are also welcome

## Author
[@DEAFPS_](https://twitter.com/deafps_)
