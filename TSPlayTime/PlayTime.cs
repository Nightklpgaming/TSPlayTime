using Microsoft.Data.Sqlite;
using Microsoft.Xna.Framework;
using System.ComponentModel;
using System.Data;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace TSPlayTime
{
    [ApiVersion(2, 1)]
    public class PlayTime : TerrariaPlugin
    {
        public override string Name => "PlayTime";
        public override Version Version => new Version(1, 2, 4, 1);
        public override string Author => "Soofa, Nightklp";
        public override string Description => "indicates the amount of time players spend on this server.";

        public static DateTime lastTime = DateTime.UtcNow;
        private IDbConnection db;
        public static Database.DatabaseManager dbManager;
        public static Dictionary<string, int> onlinePlayers = new();
        public PlayTime(Main game) : base(game)
        {
        }
        public override void Initialize()
        {
            db = new SqliteConnection(("Data Source=" + Path.Combine(TShock.SavePath, "TSPlayTime.sqlite")));
            dbManager = new Database.DatabaseManager(db);

            //ServerApi.Hooks.ServerJoin.Register(this, OnServerJoin);
            TShockAPI.Hooks.PlayerHooks.PlayerPostLogin += OnPlayerPostLogin;
            ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);

            Commands.ChatCommands.Add(new Command("playtime.default", PlayTimeCmd, "playtime", "pt")
            {
                AllowServer = false,
                HelpText = "Shows how much playtime you have."
            });
            Commands.ChatCommands.Add(new Command("playtime.default", PlayTimeLBCmd, "playtimeleaderboard", "playtimelb", "ptlb")
            {
                AllowServer = false,
                HelpText = "Displays the player playtime leaderboards."
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);
                TShockAPI.Hooks.PlayerHooks.PlayerPostLogin -= OnPlayerPostLogin;
                //ServerApi.Hooks.ServerJoin.Deregister(this, OnServerJoin);
            }
            base.Dispose(disposing);
        }

        #region Hooks

        private void OnPlayerPostLogin(PlayerPostLoginEventArgs args)
        {
            UpdateTime();
            try
            {
                onlinePlayers.Add(args.Player.Name, dbManager.GetPlayerTime(args.Player.Name));
            }
            catch (NullReferenceException)
            {
                dbManager.InsertPlayer(args.Player.Name);
                onlinePlayers.Add(args.Player.Name, 0);
            }
        }

        #region unused
        private void OnServerJoin(JoinEventArgs args)
        {
            UpdateTime();
            try
            {
                onlinePlayers.Add(Main.player[args.Who].name, dbManager.GetPlayerTime(Main.player[args.Who].name));
            }
            catch (NullReferenceException)
            {
                dbManager.InsertPlayer(Main.player[args.Who].name);
                onlinePlayers.Add(Main.player[args.Who].name, 0);
            }
        }
        #endregion

        private void OnServerLeave(LeaveEventArgs args)
        {
            UpdateTime();
            onlinePlayers.Remove(Main.player[args.Who].name);
        }

        #endregion

        #region Commands
        private void PlayTimeCmd(CommandArgs args)
        {
            UpdateTime();
            
            if (args.Parameters.Count != 0)
            {
                string targetname = string.Join(" ", args.Parameters.ToArray(), 0, args.Parameters.Count);

                List<TSPlayer> gettargets = TSPlayer.FindByNameOrID(targetname);

                if (gettargets.Count == 0)
                {
                    try
                    {
                        MinuteTime get = MinutesToTotal(dbManager.GetPlayerTime(targetname));
                        args.Player.SendInfoMessage($"[i:3099] {targetname} PlayTime is {get.Days}days {get.Hours}hours {get.Minutes}minutes");
                    }
                    catch (NullReferenceException)
                    {
                        args.Player.SendErrorMessage("Invalid Player!");
                    }
                } else
                {
                    MinuteTime get = MinutesToTotal(onlinePlayers[gettargets[0].Name]);
                    args.Player.SendInfoMessage($"[i:3099] {gettargets[0].Name} PlayTime is {get.Days}days {get.Hours}hours {get.Minutes}minutes");
                }

            } else
            {

                MinuteTime get = MinutesToTotal(onlinePlayers[args.Player.Name]);
                args.Player.SendInfoMessage($"[i:3099] Your PlayTime is {get.Days}days {get.Hours}hours {get.Minutes}minutes");

            }

        }

        public void PlayTimeLBCmd(CommandArgs args)
        {
            try
            {
                string result = "";
                int index = 1;


                if (args.Parameters.Count != 0)
                {
                    int page = int.Parse(args.Parameters[0])-1;
                    if (page < 0) { page = 0; }

                    foreach (var check in dbManager.GetLeaderBoard(page+1))
                    {
                        if (((page*10)+1) <= index && ((page*10) + 10) >= index)
                        {
                            MinuteTime get = MinutesToTotal(check.Value);
                            result += $"{index}# [c/00fbd6:{check.Key}] : [c/f3fb00:{get.Days}days {get.Hours}hours {get.Minutes}minutes]\n";
                        }
                        index++;
                    }
                    page++;
                    if (result == "")
                    {
                        args.Player.SendMessage("[i:3099] [c/a3fff1:Time-based player leaderboards] [i:4601]" +
                            $"\nPage: {page}" +
                            $"\n\n[c/ffa834:no results...]", Color.WhiteSmoke);
                    } else if (index == ((page-1)*10)+10)
                    {
                        args.Player.SendMessage("[i:3099] [c/a3fff1:Time-based player leaderboards] [i:4601]" +
                            $"\nPage: {page}" +
                            $"\n\n{result}", Color.WhiteSmoke);
                    } else
                    {
                        args.Player.SendMessage("[i:3099] [c/a3fff1:Time-based player leaderboards] [i:4601]" +
                            $"\nPage: {page}" +
                            $"\n\n{result}" +
                            $"\ndo /playtimelb {page + 1} for more", Color.WhiteSmoke);
                    }
                    

                } else
                {

                    foreach (var check in dbManager.GetLeaderBoard(1))
                    {
                        if (1 <= index && 10 >= index)
                        {
                            MinuteTime get = MinutesToTotal(check.Value);
                            result += $"{index}# [c/00fbd6:{check.Key}] : [c/f3fb00:{get.Days}days {get.Hours}hours {get.Minutes}minutes]\n";
                        }
                        index++;
                    }

                    if (index == 10)
                    {
                        args.Player.SendMessage("[i:3099] [c/a3fff1:Time-based player leaderboards] [i:4601]" +
                            $"\n\n{result}", Color.WhiteSmoke);
                    } else
                    {
                        args.Player.SendMessage("[i:3099] [c/a3fff1:Time-based player leaderboards] [i:4601]" +
                            $"\n\n{result}" +
                            $"\ndo /playtimelb 2 for more", Color.WhiteSmoke);
                    }
                    

                }
                
                
            } catch (Exception e) { }
            
        }

        #endregion


        public void UpdateTime()
        {
            if ((DateTime.UtcNow - lastTime).TotalMinutes < 1)
            {
                return;
            }
            foreach (var plr in onlinePlayers)
            {
                onlinePlayers[plr.Key] += (int)(DateTime.UtcNow - lastTime).TotalMinutes;
                dbManager.SavePlayer(plr.Key, onlinePlayers[plr.Key]);
            }
            lastTime = DateTime.UtcNow;
        }

        public MinuteTime MinutesToTotal(int minutes)
        {
            int check = minutes;
            int Days = check / 1440;
            check -= Days * 1440;
            int Hours = check / 60;
            check -= Hours * 60;

            return new MinuteTime(Days, Hours, check);
        }
    }

    public class MinuteTime
    {
        public int Days;
        public int Hours;
        public int Minutes;

        public MinuteTime(int Days, int Hours,int Minutes)
        {
            this.Days = Days;
            this.Hours = Hours;
            this.Minutes = Minutes;
        }
    }
}