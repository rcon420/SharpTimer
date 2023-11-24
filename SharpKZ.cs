using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace SharpKZ
{
    public partial class SharpKZ : BasePlugin
    {
        private Dictionary<int, PlayerTimerInfo> playerTimers = new Dictionary<int, PlayerTimerInfo>();
        private List<CCSPlayerController> connectedPlayers = new List<CCSPlayerController>();

        public override string ModuleName => "SharpKZ";
        public override string ModuleVersion => "0.0.1";
        public override string ModuleAuthor => "DEAFPS https://github.com/DEAFPS/";
        public override string ModuleDescription => "A simple CSS KZ Plugin";

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
                            $"<font color='green'>{playerTime}</font><br>" +                                                            // Timer
                            $"<font color=\"white\">Speed:</font> <font class='fontSize-l' color='orange'>{playerVel}</font><br>" +    // Speed
                            $"{((buttons & PlayerButtons.Forward) != 0 ? "W" : "_")} " +                                                   // W
                            $"{((buttons & PlayerButtons.Moveleft) != 0 ? "A" : "_")} " +                                                // A
                            $"{((buttons & PlayerButtons.Back) != 0 ? "S" : "_")} " +                                                   // S
                            $"{((buttons & PlayerButtons.Moveright) != 0 ? "D" : "_")} " +                                               // D
                            $"{((buttons & PlayerButtons.Jump) != 0 ? "J" : "_")} " +                                                   // JUMP
                            $"{((buttons & PlayerButtons.Duck) != 0 ? "C" : "_")}</font>");                                        // CROUCH

                        // Check if player.IsTimerRunning, if so, start adding seconds to player.TimerSec
                        if (playerTimers[player.UserId ?? 0].IsTimerRunning)
                        {
                            playerTimers[player.UserId ?? 0].TimerTicks++;
                        }
                    }
                }
            });

            Console.WriteLine("[SharpKZ] Plugin Loaded");
        }

        // Helper method to format seconds into MM:SS format
        private string FormatTime(int ticks)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(ticks / 64.0); // Convert ticks to seconds
            int centiseconds = (int)((ticks % 64) * (100.0 / 64.0)); // Convert remaining ticks to centiseconds

            return $"{timeSpan.Minutes:D1}:{timeSpan.Seconds:D2}.{centiseconds:D2}";
        }

        [ConsoleCommand("kz_start", "Starts the timer")] //placeholder cmd used to test stuff
        public void OnTimerStart(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null) return;

            // Set IsTimerRunning to true for the player
            playerTimers[player.UserId ?? 0].IsTimerRunning = true;
            playerTimers[player.UserId ?? 0].TimerTicks = 0;

        }

        [ConsoleCommand("kz_stop", "Stops the timer")] //placeholder cmd used to test stuff
        public void OnTimerStop(CCSPlayerController? player, CommandInfo? command)
        {
            if (player == null) return;

            // Set IsTimerRunning to false for the player
            playerTimers[player.UserId ?? 0].IsTimerRunning = false;
            playerTimers[player.UserId ?? 0].TimerTicks = 0;

        }
    }

    // Helper class to store player timer information
    public class PlayerTimerInfo
    {
        public bool IsTimerRunning { get; set; }
        public int TimerTicks { get; set; }
    }
}
