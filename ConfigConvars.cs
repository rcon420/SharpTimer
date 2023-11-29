using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;


namespace SharpTimer
{
    public partial class SharpTimer
    {
        [ConsoleCommand("sharptimer_respawn_enabled", "Whether !r is enabled by default or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRespawnConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            respawnEnabled = bool.TryParse(args, out bool respawnEnabledValue) ? respawnEnabledValue : args != "0" && respawnEnabled;
        }

        [ConsoleCommand("sharptimer_top_enabled", "Whether !top is enabled by default or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerTopConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            topEnabled = bool.TryParse(args, out bool topEnabledValue) ? topEnabledValue : args != "0" && topEnabled;
        }

        [ConsoleCommand("sharptimer_rank_enabled", "Whether !rank is enabled by default or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerRankConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            rankEnabled = bool.TryParse(args, out bool rankEnabledValue) ? rankEnabledValue : args != "0" && rankEnabled;
        }

        [ConsoleCommand("sharptimer_checkpoints_enabled", "Whether !cp, !tp and !prevcp are enabled by default or not. Default value: false")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerCPConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            cpEnabled = bool.TryParse(args, out bool cpEnabledValue) ? cpEnabledValue : args != "0" && cpEnabled;
        }

        [ConsoleCommand("sharptimer_connectmsg_enabled", "Whether connect/disconnect messages are enabled by default or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerConnectMSGConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            connectMsgEnabled = bool.TryParse(args, out bool connectMsgEnabledValue) ? connectMsgEnabledValue : args != "0" && connectMsgEnabled;
        }

        [ConsoleCommand("sharptimer_remove_crouch_fatigue", "Whether the player should get no crouch fatigue or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerNoDuckFatigueConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            removeCrouchFatigueEnabled = bool.TryParse(args, out bool removeCrouchFatigueEnabledValue) ? removeCrouchFatigueEnabledValue : args != "0" && removeCrouchFatigueEnabled;
        }


        [ConsoleCommand("sharptimer_sr_ad_enabled", "Whether timed Server Record messages are enabled by default or not. Default value: true")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerSRConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            srEnabled = bool.TryParse(args, out bool srEnabledValue) ? srEnabledValue : args != "0" && srEnabled;
        }

        [ConsoleCommand("sharptimer_sr_ad_timer", "Interval how often SR shall be printed to chat. Default value: 120")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerSRTimerConvar(CCSPlayerController? player, CommandInfo command)
        {
            string args = command.ArgString;

            if (int.TryParse(args, out int interval) && interval > 0)
            {
                srTimer = interval;
                Console.WriteLine($"SharpTimer interval set to {interval} seconds.");
            }
            else
            {
                Console.WriteLine("Invalid interval value. Please provide a positive integer.");
            }
        }

        [ConsoleCommand("sharptimer_chat_prefix", "Default value of chat prefix for SharpTimer messages. Default value: [SharpTimer]")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
        public void SharpTimerChatPrefix(CCSPlayerController? player, CommandInfo command)
        {

            string args = command.ArgString.Trim();

            if (string.IsNullOrEmpty(args))
            {
                msgPrefix = $" {ChatColors.Green} [SharpTimer] {ChatColors.White}";
                return;
            }

            msgPrefix = $" {ChatColors.Green} {args} {ChatColors.White}";
        }
    }
}