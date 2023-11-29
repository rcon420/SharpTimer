using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace SharpTimer
{
    public class MapInfo
    {
        public string? MapStartTrigger { get; set; }
        public string? MapStartC1 { get; set; }
        public string? MapStartC2 { get; set; }
        public string? MapEndTrigger { get; set; }
        public string? MapEndC1 { get; set; }
        public string? MapEndC2 { get; set; }
        public string? RespawnPos { get; set; }
    }

    public class PlayerTimerInfo
    {
        public bool IsTimerRunning { get; set; }
        public int TimerTicks { get; set; }
        public string? TimerRank { get; set; }
        public int CheckpointIndex { get; set; }
        public CCSPlayer_MovementServices? MovementService { get; set; }
    }

    public class PlayerRecord
    {
        public string? PlayerName { get; set; }
        public int TimerTicks { get; set; }
    }

    public class PlayerCheckpoint
    {
        public string? PositionString { get; set; }
        public string? RotationString { get; set; }
    }

    public partial class SharpTimer : BasePlugin
    {
        private Dictionary<int, PlayerTimerInfo> playerTimers = new Dictionary<int, PlayerTimerInfo>();
        private Dictionary<int, List<PlayerCheckpoint>> playerCheckpoints = new Dictionary<int, List<PlayerCheckpoint>>();
        private List<CCSPlayerController> connectedPlayers = new List<CCSPlayerController>();

        public override string ModuleName => "SharpTimer";
        public override string ModuleVersion => "0.0.5";
        public override string ModuleAuthor => "DEAFPS https://github.com/DEAFPS/";
        public override string ModuleDescription => "A simple CSS Timer Plugin";
        public string msgPrefix = $" {ChatColors.Green} [SharpTimer] {ChatColors.White}";
        public string currentMapStartTrigger = "trigger_startzone";
        public string currentMapEndTrigger = "trigger_endzone";
        public Vector currentMapStartC1 = new Vector(0, 0, 0);
        public Vector currentMapStartC2 = new Vector(0, 0, 0);
        public Vector currentMapEndC1 = new Vector(0, 0, 0);
        public Vector currentMapEndC2 = new Vector(0, 0, 0);
        public Vector currentRespawnPos = new Vector(0, 0, 0);

        public bool useTriggers = true;
        public bool respawnEnabled = true;
        public bool topEnabled = true;
        public bool rankEnabled = true;
        public bool cpEnabled = false;
        public bool connectMsgEnabled = true;
        public bool srEnabled = true;
        public int srTimer = 120;
        public bool removeCrouchFatigueEnabled = true;

        public string beepSound = "sounds/ui/csgo_ui_button_rollover_large.vsnd";
        public string respawnSound = "sounds/ui/menu_accept.vsnd";
        public string cpSound = "sounds/ui/counter_beep.vsnd";
        public string cpSoundAir = "sounds/ui/weapon_cant_buy.vsnd";
        public string tpSound = "sounds/ui/buttonclick.vsnd";

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
                    connectedPlayers.Add(player);
                    playerTimers[player.UserId ?? 0] = new PlayerTimerInfo();

                    if (connectMsgEnabled == true)
                    {
                        Server.PrintToChatAll($"{msgPrefix}Player {ChatColors.Red}{player.PlayerName} {ChatColors.White}connected!");
                    }

                    player.PrintToChat($"{msgPrefix}Welcome {ChatColors.Red}{player.PlayerName} {ChatColors.White}to the server!");

                    player.PrintToChat($"{msgPrefix}Avalible Commands:");

                    if (respawnEnabled) player.PrintToChat($"{msgPrefix}!r (css_r) - Respawns you");
                    if (topEnabled) player.PrintToChat($"{msgPrefix}!top (css_top) - Lists top 10 records on this map");
                    if (rankEnabled) player.PrintToChat($"{msgPrefix}!rank (css_rank) - Shows your current rank");

                    if (cpEnabled)
                    {
                        player.PrintToChat($"{msgPrefix}!cp (css_cp) - Sets a Checkpoint");
                        player.PrintToChat($"{msgPrefix}!tp (css_tp) - Teleports you to the last Checkpoint");
                        player.PrintToChat($"{msgPrefix}!prevcp (css_prevcp) - Teleports you to the previous Checkpoint");
                        player.PrintToChat($"{msgPrefix}!nextcp (css_nextcp) - Teleports you to the next Checkpoint");
                    }

                    playerTimers[player.UserId ?? 0].TimerRank = GetPlayerPlacementWithTotal(player);

                    playerTimers[player.UserId ?? 0].MovementService = new CCSPlayer_MovementServices(player.PlayerPawn.Value.MovementServices!.Handle);

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
                    connectedPlayers.Remove(player);
                    playerTimers.Remove(player.UserId ?? 0);
                    Server.PrintToChatAll($"{msgPrefix}Player {ChatColors.Red}{player.PlayerName} {ChatColors.White}disconnected!");

                    return HookResult.Continue;
                }
            });

            RegisterListener<Listeners.OnTick>(() =>
            {
                foreach (var player in connectedPlayers)
                {
                    if (player.IsValid && !player.IsBot && player.PawnIsAlive)
                    {
                        var buttons = player.Buttons;
                        var playerVelV = player.PlayerPawn.Value.AbsVelocity;
                        float playerVel = (float)Math.Sqrt(playerVelV.X * playerVelV.X + playerVelV.Y * playerVelV.Y + playerVelV.Z * playerVelV.Z);
                        string formattedPlayerVel = Math.Round(playerVel).ToString().PadLeft(4, '0');
                        string playerTime = FormatTime(playerTimers[player.UserId ?? 0].TimerTicks);

                        if (playerTimers[player.UserId ?? 0].IsTimerRunning)
                        {
                            player.PrintToCenterHtml(
                                $"<font color='gray'>{GetPlayerPlacement(player)}</font> <font class='fontSize-l' color='green'>{playerTime}</font><br>" +
                                $"<font color='white'>Speed:</font> <font color='orange'>{formattedPlayerVel}</font><br>" +
                                $"<font class='fontSize-s' color='gray'>{playerTimers[player.UserId ?? 0].TimerRank}</font><br>" +
                                $"<font color='white'>{((buttons & PlayerButtons.Forward) != 0 ? "W" : "_")} " +
                                $"{((buttons & PlayerButtons.Moveleft) != 0 ? "A" : "_")} " +
                                $"{((buttons & PlayerButtons.Back) != 0 ? "S" : "_")} " +
                                $"{((buttons & PlayerButtons.Moveright) != 0 ? "D" : "_")} " +
                                $"{((buttons & PlayerButtons.Jump) != 0 ? "J" : "_")} " +
                                $"{((buttons & PlayerButtons.Duck) != 0 ? "C" : "_")}</font>");

                            playerTimers[player.UserId ?? 0].TimerTicks++;
                        }
                        else
                        {
                            player.PrintToCenterHtml(
                                $"<font color='white'>Speed:</font> <font color='orange'>{formattedPlayerVel}</font><br>" +
                                $"<font class='fontSize-s' color='gray'>{playerTimers[player.UserId ?? 0].TimerRank}</font><br>" +
                                $"<font color='white'>{((buttons & PlayerButtons.Forward) != 0 ? "W" : "_")} " +
                                $"{((buttons & PlayerButtons.Moveleft) != 0 ? "A" : "_")} " +
                                $"{((buttons & PlayerButtons.Back) != 0 ? "S" : "_")} " +
                                $"{((buttons & PlayerButtons.Moveright) != 0 ? "D" : "_")} " +
                                $"{((buttons & PlayerButtons.Jump) != 0 ? "J" : "_")} " +
                                $"{((buttons & PlayerButtons.Duck) != 0 ? "C" : "_")}</font>");
                        }

                        if (!useTriggers)
                        {
                            CheckPlayerActions(player);
                        }

                        if (playerTimers[player.UserId ?? 0].MovementService != null && removeCrouchFatigueEnabled == true)
                        {
                            if(playerTimers[player.UserId ?? 0].MovementService.DuckSpeed != 7.0f) playerTimers[player.UserId ?? 0].MovementService.DuckSpeed = 7.0f;
                        }
                    }
                }
            });

            VirtualFunctions.CBaseTrigger_StartTouchFunc.Hook(h =>
            {
                var trigger = h.GetParam<CBaseTrigger>(0);
                var entity = h.GetParam<CBaseEntity>(1);

                if (trigger.DesignerName != "trigger_multiple" || entity.DesignerName != "player" || useTriggers == false) return HookResult.Continue;

                var player = new CCSPlayerController(new CCSPlayerPawn(entity.Handle).Controller.Value.Handle);

                if (!connectedPlayers.Contains(player) || playerTimers[player.UserId ?? 0].IsTimerRunning == false) return HookResult.Continue;

                if (trigger.Entity.Name == currentMapEndTrigger && player.IsValid)
                {
                    OnTimerStop(player);
                    return HookResult.Continue;
                }

                return HookResult.Continue;
            }, HookMode.Post);

            VirtualFunctions.CBaseTrigger_EndTouchFunc.Hook(h =>
            {
                var trigger = h.GetParam<CBaseTrigger>(0);
                var entity = h.GetParam<CBaseEntity>(1);

                if (trigger.DesignerName != "trigger_multiple" || entity.DesignerName != "player" || useTriggers == false) return HookResult.Continue;

                var player = new CCSPlayerController(new CCSPlayerPawn(entity.Handle).Controller.Value.Handle);

                if (!connectedPlayers.Contains(player)) return HookResult.Continue;

                if (trigger.Entity.Name == currentMapStartTrigger && player.IsValid)
                {
                    OnTimerStart(player);
                    return HookResult.Continue;
                }

                return HookResult.Continue;
            }, HookMode.Post);

            Console.WriteLine("[SharpTimer] Plugin Loaded");
        }

        private void ServerRecordADtimer()
        {
            var timer = AddTimer(srTimer, () =>
            {
                Dictionary<string, int> sortedRecords = GetSortedRecords();

                if (sortedRecords.Count == 0)
                {
                    return;
                }

                Server.PrintToChatAll($"{msgPrefix} Current Server Record on {ChatColors.Green}{Server.MapName}{ChatColors.White}: ");

                foreach (var record in sortedRecords.Take(1))
                {
                    string playerName = GetPlayerNameFromSavedSteamID(record.Key); // Get the player name using SteamID
                    Server.PrintToChatAll(msgPrefix + $" {ChatColors.Green}{playerName} {ChatColors.White}- {ChatColors.Green}{FormatTime(record.Value)}");
                }
            }, TimerFlags.REPEAT);
        }

        private static string FormatTime(int ticks)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(ticks / 64.0);
            int centiseconds = (int)((ticks % 64) * (100.0 / 64.0));

            return $"{timeSpan.Minutes:D1}:{timeSpan.Seconds:D2}.{centiseconds:D2}";
        }

        private void CheckPlayerActions(CCSPlayerController? player)
        {
            if (player == null) return;

            Vector incorrectVector = new Vector(0, 0, 0);

            Vector playerPos = player.Pawn.Value.CBodyComponent!.SceneNode.AbsOrigin;

            if (IsVectorInsideBox(playerPos, currentMapStartC1, currentMapStartC2) && currentMapStartC1 != incorrectVector && currentMapStartC2 != incorrectVector && currentMapEndC1 != incorrectVector && currentMapEndC2 != incorrectVector)
            {
                OnTimerStart(player);
            }

            if (IsVectorInsideBox(playerPos, currentMapEndC1, currentMapEndC2) && currentMapStartC1 != incorrectVector && currentMapStartC2 != incorrectVector && currentMapEndC1 != incorrectVector && currentMapEndC2 != incorrectVector)
            {
                OnTimerStop(player);
            }
        }

        static bool IsVectorInsideBox(Vector playerVector, Vector corner1, Vector corner2, float height = 50)
        {
            float minX = Math.Min(corner1.X, corner2.X);
            float minY = Math.Min(corner1.Y, corner2.Y);
            float minZ = Math.Min(corner1.Z, corner2.Z);

            float maxX = Math.Max(corner1.X, corner2.X);
            float maxY = Math.Max(corner1.Y, corner2.Y);
            float maxZ = Math.Max(corner1.Z, corner1.Z);

            return playerVector.X >= minX && playerVector.X <= maxX &&
                   playerVector.Y >= minY && playerVector.Y <= maxY &&
                   playerVector.Z >= minZ && playerVector.Z <= maxZ + height;
        }

        public void OnTimerStart(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid) return;

            // Remove checkpoints for the current player
            if (playerCheckpoints.ContainsKey(player.UserId ?? 0))
            {
                playerCheckpoints.Remove(player.UserId ?? 0);
            }

            playerTimers[player.UserId ?? 0].IsTimerRunning = true;
            playerTimers[player.UserId ?? 0].TimerTicks = 0;
        }

        public void SavePlayerTime(CCSPlayerController? player, int timerTicks)
        {
            if (player == null) return;
            if (playerTimers[player.UserId ?? 0].IsTimerRunning == false) return;

            string currentMapName = Server.MapName;
            string steamId = player.SteamID.ToString();
            string playerName = player.PlayerName;

            string recordsFileName = "SharpTimer/player_records.json";
            string recordsPath = Path.Join(Server.GameDirectory + "/csgo/cfg", recordsFileName);

            Dictionary<string, Dictionary<string, PlayerRecord>> records;
            if (File.Exists(recordsPath))
            {
                string json = File.ReadAllText(recordsPath);
                records = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, PlayerRecord>>>(json) ?? new Dictionary<string, Dictionary<string, PlayerRecord>>();
            }
            else
            {
                records = new Dictionary<string, Dictionary<string, PlayerRecord>>();
            }

            if (!records.ContainsKey(currentMapName))
            {
                records[currentMapName] = new Dictionary<string, PlayerRecord>();
            }

            if (!records[currentMapName].ContainsKey(steamId) || records[currentMapName][steamId].TimerTicks > playerTimers[player.UserId ?? 0].TimerTicks)
            {
                records[currentMapName][steamId] = new PlayerRecord
                {
                    PlayerName = playerName,
                    TimerTicks = playerTimers[player.UserId ?? 0].TimerTicks
                };

                string updatedJson = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(recordsPath, updatedJson);
            }
        }

        public void OnTimerStop(CCSPlayerController? player)
        {
            if (player == null || playerTimers[player.UserId ?? 0].IsTimerRunning == false || !player.IsValid) return;

            int currentTicks = playerTimers[player.UserId ?? 0].TimerTicks;
            int previousRecordTicks = GetPreviousPlayerRecord(player);

            SavePlayerTime(player, playerTimers[player.UserId ?? 0].TimerTicks);
            playerTimers[player.UserId ?? 0].IsTimerRunning = false;

            string timeDifference = "";
            char ifFirstTimeColor = ChatColors.Red;
            if (previousRecordTicks != 0)
            {
                timeDifference = FormatTimeDifference(currentTicks, previousRecordTicks);
                ifFirstTimeColor = ChatColors.Red;
            }
            else
            {
                ifFirstTimeColor = ChatColors.Yellow;
            }

            if (currentTicks < previousRecordTicks)
            {
                Server.PrintToChatAll(msgPrefix + $"{ChatColors.Green}{player.PlayerName} {ChatColors.White}just finished the map in: {ChatColors.Green}[{FormatTime(currentTicks)}]! {timeDifference}");
            }
            else if (currentTicks > previousRecordTicks)
            {
                Server.PrintToChatAll(msgPrefix + $"{ChatColors.Green}{player.PlayerName} {ChatColors.White}just finished the map in: {ifFirstTimeColor}[{FormatTime(currentTicks)}]! {timeDifference}");
            }
            else
            {
                Server.PrintToChatAll(msgPrefix + $"{ChatColors.Green}{player.PlayerName} {ChatColors.White}just finished the map in: {ChatColors.Yellow}[{FormatTime(currentTicks)}]! (No change in time)");
            }


            playerTimers[player.UserId ?? 0].TimerRank = GetPlayerPlacementWithTotal(player);
            NativeAPI.IssueClientCommand((int)player.EntityIndex!.Value.Value - 1, $"play {beepSound}");
        }

        private static int GetPreviousPlayerRecord(CCSPlayerController? player)
        {
            if (player == null) return 0;

            string currentMapName = Server.MapName;
            string steamId = player.SteamID.ToString();

            string recordsFileName = "SharpTimer/player_records.json";
            string recordsPath = Path.Join(Server.GameDirectory + "/csgo/cfg", recordsFileName);

            Dictionary<string, Dictionary<string, PlayerRecord>> records;
            if (File.Exists(recordsPath))
            {
                string json = File.ReadAllText(recordsPath);
                records = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, PlayerRecord>>>(json) ?? new Dictionary<string, Dictionary<string, PlayerRecord>>();

                if (records.ContainsKey(currentMapName) && records[currentMapName].ContainsKey(steamId))
                {
                    return records[currentMapName][steamId].TimerTicks;
                }
            }

            return 0;
        }

        private static string FormatTimeDifference(int currentTicks, int previousTicks)
        {
            int differenceTicks = previousTicks - currentTicks;
            string sign = (differenceTicks > 0) ? "-" : "+";

            TimeSpan timeDifference = TimeSpan.FromSeconds(Math.Abs(differenceTicks) / 64.0);
            int centiseconds = (int)((Math.Abs(differenceTicks) % 64) * (100.0 / 64.0));

            return $"{sign}{timeDifference.Minutes:D1}:{timeDifference.Seconds:D2}.{centiseconds:D2}";
        }

        public static Dictionary<string, int> GetSortedRecords()
        {
            string currentMapName = Server.MapName;

            string recordsFileName = "SharpTimer/player_records.json";
            string recordsPath = Path.Join(Server.GameDirectory + "/csgo/cfg", recordsFileName);

            Dictionary<string, Dictionary<string, PlayerRecord>> records;
            if (File.Exists(recordsPath))
            {
                string json = File.ReadAllText(recordsPath);
                records = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, PlayerRecord>>>(json) ?? new Dictionary<string, Dictionary<string, PlayerRecord>>();
            }
            else
            {
                records = new Dictionary<string, Dictionary<string, PlayerRecord>>();
            }

            if (records.ContainsKey(currentMapName))
            {
                var sortedRecords = records[currentMapName]
                    .OrderBy(record => record.Value.TimerTicks)
                    .ToDictionary(record => record.Key, record => record.Value.TimerTicks);

                return sortedRecords;
            }

            return new Dictionary<string, int>();
        }

        public string GetPlayerPlacement(CCSPlayerController? player)
        {
            if (player == null || !playerTimers.ContainsKey(player.UserId ?? 0) || !playerTimers[player.UserId ?? 0].IsTimerRunning) return "";

            Dictionary<string, int> sortedRecords = GetSortedRecords();
            int currentPlayerTime = playerTimers[player.UserId ?? 0].TimerTicks;

            int placement = 1;

            foreach (var record in sortedRecords)
            {
                if (currentPlayerTime > record.Value)
                {
                    placement++;
                }
                else
                {
                    break;
                }
            }

            return "#" + placement;
        }

        public string GetPlayerPlacementWithTotal(CCSPlayerController? player)
        {
            if (player == null || !playerTimers.ContainsKey(player.UserId ?? 0))
            {
                return "Unranked";
            }

            string steamId = player.SteamID.ToString();
            int savedPlayerTime = GetPreviousPlayerRecord(player);

            if (savedPlayerTime == 0)
            {
                return "Unranked";
            }

            Dictionary<string, int> sortedRecords = GetSortedRecords();

            int placement = 1;

            foreach (var record in sortedRecords)
            {
                if (savedPlayerTime > record.Value)
                {
                    placement++;
                }
                else
                {
                    break;
                }
            }

            int totalPlayers = sortedRecords.Count + 1; // Including the current player

            return $"Rank: {placement}/{totalPlayers}";
        }


        private static string GetPlayerNameFromSavedSteamID(string steamId)
        {
            string currentMapName = Server.MapName;

            string recordsFileName = "SharpTimer/player_records.json";
            string recordsPath = Path.Join(Server.GameDirectory + "/csgo/cfg", recordsFileName);

            if (File.Exists(recordsPath))
            {
                try
                {
                    string json = File.ReadAllText(recordsPath);
                    var records = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, PlayerRecord>>>(json);

                    if (records != null && records.TryGetValue(currentMapName, out var mapRecords) && mapRecords.TryGetValue(steamId, out var playerRecord))
                    {
                        return playerRecord.PlayerName;
                    }
                }
                catch (JsonException ex)
                {
                    // Handle JSON deserialization errors
                    Console.WriteLine($"Error deserializing player records: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // Handle other exceptions
                    Console.WriteLine($"Error reading player records: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"Player records file not found: {recordsPath}");
            }

            return "Unknown"; // Return a default name if the player name is not found or an error occurs
        }

        private static Vector ParseVector(string vectorString)
        {
            var values = vectorString.Split(' ');
            if (values.Length == 3 &&
                float.TryParse(values[0], out float x) &&
                float.TryParse(values[1], out float y) &&
                float.TryParse(values[2], out float z))
            {
                return new Vector(x, y, z);
            }

            return new Vector(0, 0, 0);
        }

        private static QAngle ParseQAngle(string qAngleString)
        {
            var values = qAngleString.Split(' ');
            if (values.Length == 3 &&
                float.TryParse(values[0], out float pitch) &&
                float.TryParse(values[1], out float yaw) &&
                float.TryParse(values[2], out float roll))
            {
                return new QAngle(pitch, yaw, roll);
            }

            return new QAngle(0, 0, 0);
        }

        private Vector FindStartTriggerPos()
        {
            var triggers = Utilities.FindAllEntitiesByDesignerName<CBaseTrigger>("trigger_multiple");

            foreach (var trigger in triggers)
            {
                if (trigger.Entity.Name == currentMapStartTrigger)
                {
                    return trigger.CBodyComponent?.SceneNode?.AbsOrigin;
                }
            }
            return new Vector(0, 0, 0);
        }

        [ConsoleCommand("css_top", "Prints top players of this map")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void PrintTopRecords(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || topEnabled == false) return;

            string currentMapName = Server.MapName;

            Dictionary<string, int> sortedRecords = GetSortedRecords();

            if (sortedRecords.Count == 0)
            {
                player.PrintToChat(msgPrefix + $" No records available for {currentMapName}.");
                return;
            }

            player.PrintToChat(msgPrefix + $" Top 10 Records for {currentMapName}:");
            int rank = 1;

            foreach (var record in sortedRecords.Take(10))
            {
                string playerName = GetPlayerNameFromSavedSteamID(record.Key); // Get the player name using SteamID
                player.PrintToChat(msgPrefix + $" #{rank}: {ChatColors.Green}{playerName} {ChatColors.White}- {ChatColors.Green}{FormatTime(record.Value)}");
                rank++;
            }
        }

        [ConsoleCommand("css_rank", "Tells you your rank on this map")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void RankCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || rankEnabled == false) return;

            player.PrintToChat(msgPrefix + $" You are currently {ChatColors.Green}{GetPlayerPlacementWithTotal(player)}");
        }

        [ConsoleCommand("css_r", "Teleports you to start")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void RespawnPlayer(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || respawnEnabled == false) return;

            if (currentRespawnPos == new Vector(0, 0, 0))
            {
                player.PrintToChat(msgPrefix + $" {ChatColors.LightRed} No RespawnPos found for current map!");
                return;
            }

            // Remove checkpoints for the current player
            if (playerCheckpoints.ContainsKey(player.UserId ?? 0))
            {
                playerCheckpoints.Remove(player.UserId ?? 0);
            }

            if (useTriggers == true)
            {
                player.PlayerPawn.Value.Teleport(FindStartTriggerPos(), new QAngle(0, 90, 0), new Vector(0, 0, 0));
            }
            else
            {
                player.PlayerPawn.Value.Teleport(currentRespawnPos, new QAngle(0, 90, 0), new Vector(0, 0, 0));
            }
            playerTimers[player.UserId ?? 0].IsTimerRunning = false;
            playerTimers[player.UserId ?? 0].TimerTicks = 0;
            NativeAPI.IssueClientCommand((int)player.EntityIndex!.Value.Value - 1, $"play {respawnSound}");
        }

        [ConsoleCommand("css_cp", "Sets a checkpoint")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void SetPlayerCP(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || cpEnabled == false) return;

            if (!player.PlayerPawn.Value.OnGroundLastTick)
            {
                player.PrintToChat(msgPrefix + $"{ChatColors.LightRed}Cant set checkpoint while in air");
                NativeAPI.IssueClientCommand((int)player.EntityIndex!.Value.Value - 1, $"play {cpSoundAir}");
                return;
            }

            // Get the player's current position and rotation
            Vector currentPosition = player.Pawn.Value.CBodyComponent?.SceneNode?.AbsOrigin ?? new Vector(0, 0, 0);
            QAngle currentRotation = player.PlayerPawn.Value.EyeAngles ?? new QAngle(0, 0, 0);

            // Convert position and rotation to strings
            string positionString = $"{currentPosition.X} {currentPosition.Y} {currentPosition.Z}";
            string rotationString = $"{currentRotation.X} {currentRotation.Y} {currentRotation.Z}";

            // Add the current position and rotation strings to the player's checkpoint list
            if (!playerCheckpoints.ContainsKey(player.UserId ?? 0))
            {
                playerCheckpoints[player.UserId ?? 0] = new List<PlayerCheckpoint>();
            }

            playerCheckpoints[player.UserId ?? 0].Add(new PlayerCheckpoint
            {
                PositionString = positionString,
                RotationString = rotationString
            });

            // Get the count of checkpoints for this player
            int checkpointCount = playerCheckpoints[player.UserId ?? 0].Count;

            // Print the chat message with the checkpoint count
            player.PrintToChat(msgPrefix + $"Checkpoint set! {ChatColors.Green}#{checkpointCount}");
            NativeAPI.IssueClientCommand((int)player.EntityIndex!.Value.Value - 1, $"play {cpSound}");
        }

        [ConsoleCommand("css_tp", "Tp to the most recent checkpoint")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void TpPlayerCP(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || cpEnabled == false) return;

            // Check if the player has any checkpoints
            if (!playerCheckpoints.ContainsKey(player.UserId ?? 0) || playerCheckpoints[player.UserId ?? 0].Count == 0)
            {
                player.PrintToChat(msgPrefix + "No checkpoints set!");
                return;
            }

            // Get the most recent checkpoint from the player's list
            PlayerCheckpoint lastCheckpoint = playerCheckpoints[player.UserId ?? 0].Last();

            // Convert position and rotation strings to Vector and QAngle
            Vector position = ParseVector(lastCheckpoint.PositionString ?? "0 0 0");
            QAngle rotation = ParseQAngle(lastCheckpoint.RotationString ?? "0 0 0");

            // Teleport the player to the most recent checkpoint, including the saved rotation
            player.PlayerPawn.Value.Teleport(position, rotation, new Vector(0, 0, 0));

            // Play a sound or provide feedback to the player
            NativeAPI.IssueClientCommand((int)player.EntityIndex!.Value.Value - 1, $"play {tpSound}");
            player.PrintToChat(msgPrefix + "Teleported to most recent checkpoint!");
        }

        [ConsoleCommand("css_prevcp", "Tp to the previous checkpoint")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void TpPreviousCP(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !cpEnabled) return;

            if (!playerCheckpoints.TryGetValue(player.UserId ?? 0, out List<PlayerCheckpoint> checkpoints) || checkpoints.Count == 0)
            {
                player.PrintToChat(msgPrefix + "No checkpoints set!");
                return;
            }

            int index = playerTimers.TryGetValue(player.UserId ?? 0, out var timer) ? timer.CheckpointIndex : 0;

            if (checkpoints.Count == 1)
            {
                TpPlayerCP(player, command);
            }
            else
            {
                // Calculate the index of the previous checkpoint, circling back if necessary
                index = (index - 1 + checkpoints.Count) % checkpoints.Count;

                PlayerCheckpoint previousCheckpoint = checkpoints[index];

                // Update the player's checkpoint index
                playerTimers[player.UserId ?? 0].CheckpointIndex = index;

                // Convert position and rotation strings to Vector and QAngle
                Vector position = ParseVector(previousCheckpoint.PositionString ?? "0 0 0");
                QAngle rotation = ParseQAngle(previousCheckpoint.RotationString ?? "0 0 0");

                // Teleport the player to the previous checkpoint, including the saved rotation
                player.PlayerPawn.Value.Teleport(position, rotation, new Vector(0, 0, 0));

                // Play a sound or provide feedback to the player
                NativeAPI.IssueClientCommand((int)player.EntityIndex!.Value.Value - 1, $"play {tpSound}");
                player.PrintToChat(msgPrefix + "Teleported to the previous checkpoint!");
            }
        }

        [ConsoleCommand("css_nextcp", "Tp to the next checkpoint")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
        public void TpNextCP(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null || !cpEnabled) return;

            if (!playerCheckpoints.TryGetValue(player.UserId ?? 0, out List<PlayerCheckpoint> checkpoints) || checkpoints.Count == 0)
            {
                player.PrintToChat(msgPrefix + "No checkpoints set!");
                return;
            }

            int index = playerTimers.TryGetValue(player.UserId ?? 0, out var timer) ? timer.CheckpointIndex : 0;

            if (checkpoints.Count == 1)
            {
                TpPlayerCP(player, command);
            }
            else
            {
                // Calculate the index of the next checkpoint, circling back if necessary
                index = (index + 1) % checkpoints.Count;

                PlayerCheckpoint nextCheckpoint = checkpoints[index];

                // Update the player's checkpoint index
                playerTimers[player.UserId ?? 0].CheckpointIndex = index;

                // Convert position and rotation strings to Vector and QAngle
                Vector position = ParseVector(nextCheckpoint.PositionString ?? "0 0 0");
                QAngle rotation = ParseQAngle(nextCheckpoint.RotationString ?? "0 0 0");

                // Teleport the player to the next checkpoint, including the saved rotation
                player.PlayerPawn.Value.Teleport(position, rotation, new Vector(0, 0, 0));

                // Play a sound or provide feedback to the player
                NativeAPI.IssueClientCommand((int)player.EntityIndex!.Value.Value - 1, $"play {tpSound}");
                player.PrintToChat(msgPrefix + "Teleported to the next checkpoint!");
            }
        }

        private void LoadConfig()
        {
            Server.ExecuteCommand("exec SharpTimer/config.cfg");

            if (srEnabled == true) ServerRecordADtimer();

            if (removeCrouchFatigueEnabled == true) Server.ExecuteCommand("sv_timebetweenducks 0");

            string currentMapName = Server.MapName;

            string mapdataFileName = "SharpTimer/mapdata.json";
            string mapdataPath = Path.Join(Server.GameDirectory + "/csgo/cfg", mapdataFileName);

            if (File.Exists(mapdataPath))
            {
                string json = File.ReadAllText(mapdataPath);
                var mapData = JsonSerializer.Deserialize<Dictionary<string, MapInfo>>(json);

                if (mapData != null && mapData.TryGetValue(currentMapName, out var mapInfo))
                {
                    if (!string.IsNullOrEmpty(mapInfo.RespawnPos))
                    {
                        currentRespawnPos = ParseVector(mapInfo.RespawnPos);
                    }

                    if (!string.IsNullOrEmpty(mapInfo.MapStartC1) && !string.IsNullOrEmpty(mapInfo.MapStartC2) && !string.IsNullOrEmpty(mapInfo.MapEndC1) && !string.IsNullOrEmpty(mapInfo.MapEndC2))
                    {
                        currentMapStartC1 = ParseVector(mapInfo.MapStartC1);
                        currentMapStartC2 = ParseVector(mapInfo.MapStartC2);
                        currentMapEndC1 = ParseVector(mapInfo.MapEndC1);
                        currentMapEndC2 = ParseVector(mapInfo.MapEndC2);
                        useTriggers = false;
                    }

                    if (!string.IsNullOrEmpty(mapInfo.MapStartTrigger) && !string.IsNullOrEmpty(mapInfo.MapEndTrigger))
                    {
                        currentMapStartTrigger = mapInfo.MapStartTrigger;
                        currentMapEndTrigger = mapInfo.MapEndTrigger;
                        useTriggers = true;
                    }
                }
                else
                {
                    Console.WriteLine($"Map data not found for map: {currentMapName}! Using default trigger names instead!");
                    currentMapStartTrigger = "timer_startzone";
                    currentMapEndTrigger = "timer_endzone";
                    useTriggers = true;
                }
            }
        }
    }
}
