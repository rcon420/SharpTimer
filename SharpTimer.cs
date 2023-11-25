using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace SharpTimer
{
    public class MapInfo
    {
        public string MapStartC1 { get; set; }
        public string MapStartC2 { get; set; }
        public string MapEndC1 { get; set; }
        public string MapEndC2 { get; set; }
    }

    public class PlayerTimerInfo
    {
        public bool IsTimerRunning { get; set; }
        public int TimerTicks { get; set; }
    }

    public partial class SharpTimer : BasePlugin
    {
        private Dictionary<int, PlayerTimerInfo> playerTimers = new Dictionary<int, PlayerTimerInfo>();
        private List<CCSPlayerController> connectedPlayers = new List<CCSPlayerController>();

        public override string ModuleName => "SharpTimer";
        public override string ModuleVersion => "0.0.1";
        public override string ModuleAuthor => "DEAFPS https://github.com/DEAFPS/";
        public override string ModuleDescription => "A simple CSS KZ Plugin";
        
        public string msgPrefix = "\x06 [SharpTimer] >>> \x0D";
        public Vector currentMapStartC1 = new Vector(0, 0, 0);
        public Vector currentMapStartC2 = new Vector(0, 0, 0);
        public Vector currentMapEndC1 = new Vector(0, 0, 0);
        public Vector currentMapEndC2 = new Vector(0, 0, 0);

        public bool configLoaded = false;

        public override void Load(bool hotReload)
        {
            RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
            {
                var player = @event.Userid;

                if (player.IsBot || !player.IsValid)
                {
                    return HookResult.Continue;
                }
                else
                {
                    // Append player to the connected players list
                    connectedPlayers.Add(player);

                    // Initialize player-specific timer properties
                    playerTimers[player.UserId ?? 0] = new PlayerTimerInfo();

                    return HookResult.Continue;
                }
            });

            RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                    LoadConfig();
                    return HookResult.Continue;
            });

            RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
            {
                var player = @event.Userid;

                if (player.IsBot || !player.IsValid)
                {
                    return HookResult.Continue;
                }
                else
                {
                    // Remove player from the connected players list
                    connectedPlayers.Remove(player);

                    // Remove player timer info
                    playerTimers.Remove(player.UserId ?? 0);

                    return HookResult.Continue;
                }
            });

            RegisterListener<Listeners.OnTick>(() =>
            {
                // Iterate through each player in connectedPlayers
                foreach (var player in connectedPlayers)
                {
                    if (player.IsValid && !player.IsBot && player.PawnIsAlive)
                    {
                        var buttons = player.Buttons;
                        string playerVel = Math.Round(player.PlayerPawn.Value.AbsVelocity.Length2D()).ToString().PadLeft(3, '0');
                        string playerTime = FormatTime(playerTimers[player.UserId ?? 0].TimerTicks); // Format the timer display

                        player.PrintToCenterHtml(
                            $"<font color='green'>{playerTime}</font><br>" +
                            $"<font color=\"white\">Speed:</font> <font class='fontSize-l' color='orange'>{playerVel}</font><br>" +
                            $"{((buttons & PlayerButtons.Forward) != 0 ? "W" : "_")} " +
                            $"{((buttons & PlayerButtons.Moveleft) != 0 ? "A" : "_")} " +
                            $"{((buttons & PlayerButtons.Back) != 0 ? "S" : "_")} " +
                            $"{((buttons & PlayerButtons.Moveright) != 0 ? "D" : "_")} " +
                            $"{((buttons & PlayerButtons.Jump) != 0 ? "J" : "_")} " +
                            $"{((buttons & PlayerButtons.Duck) != 0 ? "C" : "_")}</font>");

                        // Check if player.IsTimerRunning, if so, start adding seconds to player.TimerSec
                        if (playerTimers[player.UserId ?? 0].IsTimerRunning)
                        {
                            playerTimers[player.UserId ?? 0].TimerTicks++;
                        }

                        // add a function that checks if the player is doing certain actions
                        CheckPlayerActions(player);
                    }
                }
            });

            Console.WriteLine("[SharpTimer] Plugin Loaded");
        }

        // Helper method to format seconds into MM:SS format
        private string FormatTime(int ticks)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(ticks / 64.0); // Convert ticks to seconds
            int centiseconds = (int)((ticks % 64) * (100.0 / 64.0)); // Convert remaining ticks to centiseconds

            return $"{timeSpan.Minutes:D1}:{timeSpan.Seconds:D2}.{centiseconds:D2}";
        }

        private void CheckPlayerActions(CCSPlayerController? player)
        {
            Vector playerPos = player.Pawn.Value.CBodyComponent!.SceneNode.AbsOrigin;
            if (IsVectorInsideBox(playerPos, currentMapStartC1, currentMapStartC2))
            {
                // start the timer
                OnTimerStart(player);
            }
            //Console.WriteLine($"Player is in Start: {CheckPlayerIsInStart(player)}");

            if (IsVectorInsideBox(playerPos, currentMapEndC1, currentMapEndC2))
            {
                // print time to chat and end the timer
                OnTimerStop(player);
            }
        }

        static bool IsVectorInsideBox(Vector vector, Vector corner1, Vector corner2, float height = 50)
        {
            float minX = Math.Min(corner1.X, corner2.X);
            float minY = Math.Min(corner1.Y, corner2.Y);
            float minZ = Math.Min(corner1.Z, corner2.Z);

            float maxX = Math.Max(corner1.X, corner2.X);
            float maxY = Math.Max(corner1.Y, corner2.Y);
            float maxZ = Math.Max(corner1.Z, corner2.Z);

            return vector.X >= minX && vector.X <= maxX &&
                   vector.Y >= minY && vector.Y <= maxY &&
                   vector.Z >= minZ && vector.Z <= maxZ + height;
        }

        public void OnTimerStart(CCSPlayerController? player)
        {
            if (player == null) return;

            // Set IsTimerRunning to true for the player
            playerTimers[player.UserId ?? 0].IsTimerRunning = true;
            playerTimers[player.UserId ?? 0].TimerTicks = 0;
        }

        public void OnTimerStop(CCSPlayerController? player)
        {
            if (player == null || playerTimers[player.UserId ?? 0].IsTimerRunning == false) return;

            // Set IsTimerRunning to false for the player
            Server.PrintToChatAll($"{msgPrefix} {player.PlayerName} just finished the map in: [{FormatTime(playerTimers[player.UserId ?? 0].TimerTicks)}]!");
            playerTimers[player.UserId ?? 0].IsTimerRunning = false;
        }

        // Helper method to parse a Vector from a string
        private Vector ParseVector(string vectorString)
        {
            var values = vectorString.Split(' ');
            if (values.Length == 3 &&
                float.TryParse(values[0], out float x) &&
                float.TryParse(values[1], out float y) &&
                float.TryParse(values[2], out float z))
            {
                return new Vector(x, y, z);
            }

            // If parsing fails, return a vector with zeros
            return new Vector(0, 0, 0);
        }

        private void LoadConfig()
        {
            // Get map name
            string currentMapName = Server.MapName;

            // Define the json file path
            string mapdataFileName = "SharpTimer/mapdata.json";
            string mapdataPath = Path.Join(Server.GameDirectory + "/csgo/cfg", mapdataFileName);

            // Load map data from JSON for the current map.
            if (File.Exists(mapdataPath))
            {
                string json = File.ReadAllText(mapdataPath);
                var mapData = JsonSerializer.Deserialize<Dictionary<string, MapInfo>>(json);

                if (mapData.TryGetValue(currentMapName, out var mapInfo))
                {
                    // Set currentMapStart
                    currentMapStartC1 = ParseVector(mapInfo.MapStartC1);
                    currentMapStartC2 = ParseVector(mapInfo.MapStartC2);

                    // Set currentMapEnd
                    currentMapEndC1 = ParseVector(mapInfo.MapEndC1);
                    currentMapEndC2 = ParseVector(mapInfo.MapEndC2);
                }
            }
        }
    }
}
