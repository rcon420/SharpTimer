# SharpTimer
SharpTimer is a simple Surf/KZ CS2 Timer plugin using CounterStrikeSharp

### Dependencies

MetaMod: https://cs2.poggu.me/metamod/installation/

CounterStrikeSharp: https://docs.cssharp.dev/

## Features
Timer, Speedometer and Keys

![alt text](https://i.imgur.com/v6zmECN.png)

Players PBs

![alt text](https://i.imgur.com/9Sfhq0S.png)

### Installing

* Unzip into your servers `game/csgo`
* Its recommended to have a custom server cfg with your desired settings (for example surf or kz)

## Commands
- `!r` --Teleports the player back to Spawn
- `!top` --Prints the top 10 times on the current map

### Configuration

* To add map Start, Respawn and End Areas head over to `game/csgo/cfg/SharpTimer/mapdata.json`

  Add the coordinates of opposite corners for each of the Start and End Areas using `MapStartC1`, `MapStartC2` & `MapEndC1`, `MapEndC2`

  After that set the `RespawnPos` to the coordinates you with the player will get teleported to once they use `!r`

  To get the coordinates you can use either the console command `cl_showpos 1` or `getpos` to get the coordinates of where your player currently is.
  
```
{
  "surf_kitsune": {
    "MapStartC1": "-15744 -15343 816",
    "MapStartC2": "-14974 -14832 816",
    "MapEndC1": "-15540 10560 -11807",
    "MapEndC2": "-16047 9793 -11807",
    "RespawnPos": "-15360 -15068 817"
  },
  "surf_someothermap": {
    "MapStartC1": "-15744 -15343 816",
    "MapStartC2": "-14974 -14832 816",
    "MapEndC1": "-15540 10560 -11807",
    "MapEndC2": "-16047 9793 -11807",
    "RespawnPos": "-15360 -15068 817"
  }
}
```
* To whitelist players add their SteamID3 to the allowedPlayers table in whitelist.lua

* To add admins edit the `adminPlayers` table in `pug_cfg.lua`

* To add custom nade lineups for the Pracc mode navigate to game/csgo/cfg/dea_pugplugin_praccnades.cfg` and follow the example given there

## Author
[@DEAFPS_](https://twitter.com/deafps_)
