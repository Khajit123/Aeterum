using Aeternum.Commands.Slash;
using Aeternum.config;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using MySqlConnector;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Timers;
using static Aeternum.Database;

namespace Aeternum
{
    internal class Database
    {
        public enum ChannelNames
        {

            UserLogging,
            MessageLogging,
            Whitelist,
            WhitelistArchive,
            Console,
            Changelog,
            ToDo,
            ServerImages,
            DebugConsole,
            MessageList,
        }

        public enum RoleNames
        {
            Admin,
            Whitelisted,
            NonWhitelisted,
        }

        public enum BooleanNames
        {
            AutomateWhitelist,
        }

        public enum IntNames
        {
            WhitelistTotal,
            WhitelistSuccess,
            WhitelistFail,
        }

        public enum OtherNames
        {
            WhitelistThumbnailType,
        }

        static MySqlConnection connection;

        public static async Task<MySqlConnection> Connect()
        {
            JSONReader jsonReader = new JSONReader();
            await jsonReader.ReadJSON();

            string db_Host = jsonReader.dbhost;
            string db_Username = jsonReader.dbusername;
            string db_Password = jsonReader.dbpassword;
            string db_Name = jsonReader.dbname;

            connection = new MySqlConnection($"Server={db_Host};User ID={db_Username};Password={db_Password};Database={db_Name}");
            connection.Open();
            Console.WriteLine($"Připojeno k databázi {db_Name}...");
            await Program.DebugConsole($"Připojeno k databázi {db_Name}...");
            return await Task.FromResult(connection);
        } 
        public static async Task Disconnect()
        {
            connection.Close();
            Console.WriteLine("Odpojeno od databáze...");
            await Program.DebugConsole($"Odpojeno od databáze...");
            await Task.CompletedTask;
        }

        public static async Task<string> GetValue(MySqlConnection connection, string tableName, string columnName)
        {
            var cmd = new MySqlCommand($"SELECT {columnName} FROM {tableName} LIMIT 1", connection);
            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString();
        }

        public static async Task AddValue(MySqlConnection connection, string tableName, string columnName)
        {
            try
            {
                var cmd = new MySqlCommand($"ALTER TABLE {tableName} ADD COLUMN {columnName} Text NULL");
                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception) { }
        }

        public static async Task UpdateValue(MySqlConnection connection, string tableName, string columnName, string value)
        {
            var cmd = new MySqlCommand($"UPDATE {tableName} SET {columnName} = @value", connection);
            cmd.Parameters.AddWithValue("@value", value);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    internal static class Program
    {
        // First
        public static DiscordClient Client { get; private set; }
        public static CommandsNextExtension Commands { get; private set; }
        public static DiscordGuild Server { get; private set; }


        // Dictionaries to hold database variables
        public static Dictionary<ChannelNames, DiscordChannel> Channels = new Dictionary<ChannelNames, DiscordChannel>();
        public static Dictionary<RoleNames, DiscordRole> Roles = new Dictionary<RoleNames, DiscordRole>();
        public static Dictionary<BooleanNames, bool> Booleans = new Dictionary<BooleanNames, bool>();
        public static Dictionary<IntNames, int> Ints = new Dictionary<IntNames, int>();
        public static Dictionary<OtherNames, string> Others = new Dictionary<OtherNames, string>();

        // Emojis
        public static DiscordEmoji ApproveEmoji { get; private set; }
        public static DiscordEmoji DisApproveEmoji { get; private set; }
        public static DiscordEmoji Btn_ApproveEmoji { get; private set; }
        public static DiscordEmoji Btn_DisApproveEmoji { get; private set; }


        public static List<string> ToDoList = new List<string>();

        //-------------------------------------------------------------------
        //                   Inicializace + Nastavování
        //-------------------------------------------------------------------
        public static async Task Main(string[] args)
        {
            JSONReader jsonReader = new JSONReader();
            await jsonReader.ReadJSON();

            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonReader.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };
            Client = new DiscordClient(discordConfig);
            var slashCommandsConfig = Client.UseSlashCommands();
            var activity = new DiscordActivity()
            {
                Name = "Tebe",
                ActivityType = ActivityType.Watching

            };
            Client.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromMinutes(2)
            });
            Server = await Client.GetGuildAsync(1193271233781956729);


            Client.GuildDownloadCompleted += Client_Ready;
            Client.GuildMemberAdded += EmbedMemberAdd;
            Client.GuildMemberRemoved += EmbedMemberRemove;
            Client.GuildMemberUpdated += OnMemberUpdate;
            Client.MessageDeleted += OnMessageDelete;
            Client.MessageUpdated += OnMessageEdit;
            Client.MessageCreated += OnMessageCreate;
            Client.ComponentInteractionCreated += OnButtonClick;
            Client.ModalSubmitted += OnModalSubmit;
            Client.MessageReactionAdded += OnReactionAdd;
            slashCommandsConfig.RegisterCommands<BasicSL>();

            await LoadVariables();
            await Client.ConnectAsync(activity, UserStatus.Online);
            await Task.Delay(-1);
            try
            {
                var embedmsg = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithDescription("Aeterum Bot byl zastaven nebo samovolně obnoven")
                    .WithThumbnail(Server.IconUrl);
                await GetChannel(ChannelNames.DebugConsole).SendMessageAsync(embed: embedmsg);
            }catch{ };
        }

        #region Database
        //-------------------------------------------------------------------
        //                   LoadFrom: Database
        //-------------------------------------------------------------------
        // Load All
        public static async Task LoadVariables()
        {
            var connection = await Database.Connect();

            // Load channels
            foreach (ChannelNames channelName in Enum.GetValues(typeof(ChannelNames)))
            {
                await LoadChannel(connection, channelName);
            }

            // Load roles
            foreach (RoleNames roleName in Enum.GetValues(typeof(RoleNames)))
            {
                await LoadRole(connection, roleName);
            }

            // Load booleans
            foreach (BooleanNames booleanName in Enum.GetValues(typeof(BooleanNames)))
            {
                await LoadBoolean(connection, booleanName);
            }

            // Load ints
            foreach (IntNames intName in Enum.GetValues(typeof(IntNames)))
            {
                await LoadInt(connection, intName);
            }

            // Load others
            foreach (OtherNames otherName in Enum.GetValues(typeof(OtherNames)))
            {
                await LoadOther(connection, otherName);
            }

            await Database.Disconnect();

            await DebugConsole(GetLoadedVariablesInfo().ToString());
            await Task.CompletedTask;
        }

        // Loaders
        public static async Task LoadChannel(MySqlConnection connection, ChannelNames channelName)
        {
            var channelId = await Database.GetValue(connection, "Channels", channelName.ToString());
            if (string.IsNullOrEmpty(channelId))
            {
                await Database.AddValue(connection, "Channels", channelName.ToString());
                Console.WriteLine($"Kanál {channelName} byl přidán do databáze bez hodnoty (null).");
                await DebugConsole($"Kanál {channelName} byl přidán do databáze bez hodnoty (null).");
            }
            else
            {
                var channel = await GetChannelFromID(channelId);
                Channels[channelName] = channel;
                Console.WriteLine($"{channelName} - {channel.Name} ({channel.Id})");
            }
        }
        public static async Task LoadRole(MySqlConnection connection, RoleNames roleName)
        {
            var roleId = await Database.GetValue(connection, "Roles", roleName.ToString());
            if (string.IsNullOrEmpty(roleId))
            {
                await Database.AddValue(connection, "Roles", roleName.ToString());
                Console.WriteLine($"Role {roleName} byla přidána do databáze bez hodnoty (null).");
                await DebugConsole($"Role {roleName} byla přidána do databáze bez hodnoty (null).");
            }
            else
            {
                var role = Server.GetRole(Convert.ToUInt64(roleId));
                Roles[roleName] = role;
                Console.WriteLine($"{roleName} - {role.Name} ({role.Id})");
            }
        }
        public static async Task LoadBoolean(MySqlConnection connection, BooleanNames booleanName)
        {
            var booleanValue = await Database.GetValue(connection, "Booleans", booleanName.ToString());
            if (string.IsNullOrEmpty(booleanValue))
            {
                await Database.AddValue(connection, "Booleans", booleanName.ToString());
                Console.WriteLine($"Boolean {booleanName} byl přidán do databáze bez hodnoty (null).");
                await DebugConsole($"Boolean {booleanName} byl přidán do databáze bez hodnoty (null).");
            }
            else
            {
                Booleans[booleanName] = bool.Parse(booleanValue);
                Console.WriteLine($"{booleanName} - {Booleans[booleanName]}");
            }
        }
        public static async Task LoadInt(MySqlConnection connection, IntNames intName)
        {
            var intValue = await Database.GetValue(connection, "Ints", intName.ToString());
            if (string.IsNullOrEmpty(intValue))
            {
                await Database.AddValue(connection, "Ints", intName.ToString());
                Console.WriteLine($"Int {intName} byl přidán do databáze s hodnotou (null).");
                await DebugConsole($"Int {intName} byl přidán do databáze s hodnotou (null).");
            }
            else
            {
                Ints[intName] = int.Parse(intValue);
                Console.WriteLine($"{intName} - {Ints[intName]}");
            }
        }
        public static async Task LoadOther(MySqlConnection connection, OtherNames otherName)
        {
            var otherValue = await Database.GetValue(connection, "Other", otherName.ToString());
            if (string.IsNullOrEmpty(otherValue))
            {
                await Database.AddValue(connection, "Other", otherName.ToString());
                Console.WriteLine($"Ostatní {otherName} bylo přidáno do databáze bez hodnoty (null).");
                await DebugConsole($"Ostatní {otherName} bylo přidáno do databáze bez hodnoty (null).");
            }
            else
            {
                Others[otherName] = otherValue;
                Console.WriteLine($"{otherName} - {Others[otherName]}");
            }
        }

        // Getters
        public static DiscordChannel GetChannel(ChannelNames channelName)
        {
            if (Channels.ContainsKey(channelName))
            {
                return Channels[channelName];
            }
            return null;
        }
        public static DiscordRole GetRole(RoleNames roleName)
        {
            if (Roles.ContainsKey(roleName))
            {
                return Roles[roleName];
            }
            return null;
        }
        public static bool GetBoolean(BooleanNames booleanName)
        {
            if (Booleans.ContainsKey(booleanName))
            {
                return Booleans[booleanName];
            }
            return false;
        }
        public static int GetInt(IntNames intName)
        {
            if (Ints.ContainsKey(intName))
            {
                return Ints[intName];
            }
            return 0;
        }
        public static string GetOther(OtherNames otherName)
        {
            if (Others.ContainsKey(otherName))
            {
                return Others[otherName];
            }
            return null;
        }

        //Updaters
        public static async Task UpdateChannel(MySqlConnection connection, ChannelNames channelName, string channelId)
        {
            await Database.UpdateValue(connection, "Channels", channelName.ToString(), channelId);
            var channel = await GetChannelFromID(channelId);
            Channels[channelName] = channel;
            await DebugConsole($"Kanál {channelName} byl aktualizován hodnotou: {channelId}");
        }
        public static async Task UpdateRole(MySqlConnection connection, RoleNames roleName, string roleId)
        {
            await Database.UpdateValue(connection, "Roles", roleName.ToString(), roleId);
            var role = Server.GetRole(Convert.ToUInt64(roleId));
            Roles[roleName] = role;
            await DebugConsole($"Role {roleName} byla aktualizována hodnotou: {roleId}");

        }
        public static async Task UpdateBoolean(MySqlConnection connection, BooleanNames booleanName, bool value)
        {
            await Database.UpdateValue(connection, "Booleans", booleanName.ToString(), value.ToString());
            Booleans[booleanName] = value;
            await DebugConsole($"Boolean {booleanName} byl aktualizován hodnotou: {value}");
        }
        public static async Task UpdateInt(MySqlConnection connection, IntNames intName, int value)
        {
            await Database.UpdateValue(connection, "Ints", intName.ToString(), value.ToString());
            Ints[intName] = value;
            await DebugConsole($"Int {intName} byl aktualizován hodnotou: {value}");
        }
        public static async Task UpdateOther(MySqlConnection connection, OtherNames otherName, string value)
        {
            await Database.UpdateValue(connection, "Other", otherName.ToString(), value);
            Others[otherName] = value;
            await DebugConsole($"Ostatní {otherName} bylo aktualizováno hodnotou: {value}");
        }

        private static StringBuilder GetLoadedVariablesInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("  ╔══════════════════════════ Channels ══════════════════════════╗");
            foreach (var item in Channels)
            {
                sb.AppendLine($"  ║{item.Key} - {item.Value.Name} ({item.Value.Id})");
            }
            sb.AppendLine("  ╠══════════════════════════ Roles ═════════════════════════════╣");
            foreach (var item in Roles)
            {
                sb.AppendLine($"  ║{item.Key} - {item.Value.Name} ({item.Value.Id})");
            }
            sb.AppendLine("  ╠══════════════════════════ Ints ══════════════════════════════╣");
            foreach (var item in Ints)
            {
                sb.AppendLine($"  ║{item.Key} - {item.Value}");
            }
            sb.AppendLine("  ╠══════════════════════════ Booleans ══════════════════════════╣");
            foreach (var item in Booleans)
            {
                sb.AppendLine($"  ║{item.Key} - {item.Value}");
            }

            sb.AppendLine("  ╠══════════════════════════ Others ════════════════════════════╣");
            foreach (var item in Others)
            {
                sb.AppendLine($"  ║{item.Key} - {item.Value}");
            }

            return sb;
        }
        #endregion

        #region Eventy
        //-------------------------------------------------------------------
        //                   Event: Připraven Bot
        //-------------------------------------------------------------------
        private static async Task Client_Ready(DiscordClient sender, GuildDownloadCompletedEventArgs args)
        {
            #region Safe Initialize
            ApproveEmoji = await GetEmojiFromName(":white_check_mark:");
            DisApproveEmoji = await GetEmojiFromName(":x:");
            Btn_ApproveEmoji = await GetEmojiFromName(":heavy_check_mark:");
            Btn_DisApproveEmoji = await GetEmojiFromName(":heavy_multiplication_x:");
            await SyncToDoDictionary();
            #endregion

            // Timer for periodic update Time on whitelist
            Timer timer = new Timer(TimeSpan.FromMinutes(1).TotalMilliseconds);
            timer.AutoReset = true;
            timer.Elapsed += async (s, ev) => await VoidUpdater(s, ev);
            timer.Start();

            try
            {
                var embedmsg = new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithTitle("Status:")
                    .WithDescription("Aeterum Bot byl uveden do provozu")
                    .WithThumbnail(Server.IconUrl);
                await GetChannel(ChannelNames.DebugConsole).SendMessageAsync(embed: embedmsg);
            }
            catch { };
            await Task.CompletedTask;
        }

        //-------------------------------------------------------------------
        //                   Event: Tlačítka
        //-------------------------------------------------------------------
        private static async Task OnButtonClick(DiscordClient sender, ComponentInteractionCreateEventArgs args)
        {
            DiscordMember member = GetMemberFromUser(args.User).Result;
            bool IsAdmin = member.Roles.Contains(Program.GetRole(RoleNames.Admin));
            //bool IsWhitelisted = member.Roles.Contains(WhitelistedRole);
            bool IsNonWhitelisted = member.Roles.Contains(Program.GetRole(RoleNames.NonWhitelisted));

            await DebugConsole($"Uživatel {args.User.Username} ({args.User.Id}) kliknul na tlačítko {args.Id}", DebugLevel.Warning);

            if (args.Id == "btn_close_id" && IsAdmin) // Neprošel tlačítko u přihlášek
            {
                #region MessageWithSecondApproval
                var secondApprovalMessage = new DiscordMessageBuilder()
                    .WithContent(args.Message.Content);
                secondApprovalMessage.AddComponents(
                    CreateButtonComponent(ButtonStyle.Success, "second_btn_approve_id", "Ano", false, Btn_ApproveEmoji).Result, 
                    CreateButtonComponent(ButtonStyle.Danger, "second_btn_close_id", "Ne", false, Btn_DisApproveEmoji).Result);
                secondApprovalMessage.AddEmbed(new DiscordEmbedBuilder(args.Message.Embeds[0]));
                secondApprovalMessage.AddEmbed(new DiscordEmbedBuilder() { Color = DiscordColor.Red,Title = $"Opravdu chceš uzavřít přihlášku hráče {args.Message.Embeds.First().Author.Name} jako **__NEPROŠEL?__**", });
                #endregion
                #region MessageDefault
                var defaultMessage = new DiscordMessageBuilder()
                    .WithContent(args.Message.Content);
                defaultMessage.AddComponents(
                    CreateButtonComponent(ButtonStyle.Success, "btn_approve_id", "Prošel", false, Btn_ApproveEmoji).Result,
                    CreateButtonComponent(ButtonStyle.Danger, "btn_close_id", "Neprošel", false, Btn_DisApproveEmoji).Result);
                defaultMessage.AddEmbed(new DiscordEmbedBuilder(args.Message.Embeds[0]));
                #endregion

                await args.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                var waitMSG = await args.Message.ModifyAsync(secondApprovalMessage);
                var result = await waitMSG.WaitForButtonAsync(TimeSpan.FromSeconds(10));
                if (!result.TimedOut && result.Result.Id == "second_btn_approve_id" && result.Result.Interaction.User.Id == args.Interaction.User.Id)
                {
                    await WhitelistArchive(args);
                    await args.Message.ModifyAsync(defaultMessage);
                    await Task.CompletedTask;
                }
                else { await args.Message.ModifyAsync(defaultMessage); await Task.CompletedTask; }
            }
            else if (args.Id == "btn_approve_id" && IsAdmin) // Prošel tlačítko u přihlášek
            {
                #region MessageWithSecondApproval
                var secondApprovalMessage = new DiscordMessageBuilder()
                    .WithContent(args.Message.Content);
                secondApprovalMessage.AddComponents(
                    CreateButtonComponent(ButtonStyle.Success, "second_btn_approve_id", "Ano", false, Btn_ApproveEmoji).Result,
                    CreateButtonComponent(ButtonStyle.Danger, "second_btn_close_id", "Ne", false, Btn_DisApproveEmoji).Result);
                secondApprovalMessage.AddEmbed(new DiscordEmbedBuilder(args.Message.Embeds[0]));
                secondApprovalMessage.AddEmbed(new DiscordEmbedBuilder() { Color = DiscordColor.Green,Title = $"Opravdu chceš uzavřít přihlášku hráče {args.Message.Embeds.First().Author.Name} jako **__PROŠEL?__**", });
                #endregion
                #region MessageDefault
                var defaultMessage = new DiscordMessageBuilder()
                    .WithContent(args.Message.Content);
                defaultMessage.AddComponents(
                    CreateButtonComponent(ButtonStyle.Success, "btn_approve_id", "Prošel", false, Btn_ApproveEmoji).Result,
                    CreateButtonComponent(ButtonStyle.Danger, "btn_close_id", "Neprošel", false, Btn_DisApproveEmoji).Result);
                defaultMessage.AddEmbed(new DiscordEmbedBuilder(args.Message.Embeds[0]));
                #endregion

                await args.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                var waitMSG = await args.Message.ModifyAsync(secondApprovalMessage);
                var result = await waitMSG.WaitForButtonAsync(TimeSpan.FromSeconds(10));
                if (!result.TimedOut && result.Result.Id == "second_btn_approve_id" && result.Result.Interaction.User.Id == args.Interaction.User.Id)
                {
                    await WhitelistSuccess(args);
                    await args.Message.ModifyAsync(defaultMessage);
                    await Task.CompletedTask;
                }
                else { await args.Message.ModifyAsync(defaultMessage); await Task.CompletedTask; }
            }
            else if (args.Id == "btn_create_whitelist" && IsNonWhitelisted)
            {
                var messages = await Program.GetChannel(ChannelNames.Whitelist).GetMessagesAsync();
                foreach (var msg in messages) // Pokud už jednu přihlašká má tak zamítnout submit
                {
                    if (msg.MentionedUsers.Count > 0)
                    {
                        if (msg.MentionedUsers[0].Id == args.Interaction.User.Id)
                        {
                            await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Již jednu přihlášku zde máš, počkej až skončí a pak si můžeš podat další " + msg.JumpLink).AsEphemeral(true));
                            await DebugConsole($"Uživatel {args.User} se pokusil vytvořit přihlášku, ačkoliv již má jednu aktivní", DebugLevel.Warning);
                            await Task.CompletedTask;
                            return;
                        }
                    }
                }
                await WhitelistModal(args);
                await Task.CompletedTask;
            }
            else
            {
                await args.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                await Task.CompletedTask;
                return;
            }


        }

        //-------------------------------------------------------------------
        //                   Event: Zprávy / Reakce
        //-------------------------------------------------------------------
        private static async Task OnMessageCreate(DiscordClient sender, MessageCreateEventArgs args)
        {
            // To Do kanál
            if (args.Channel == Program.GetChannel(ChannelNames.ToDo))
            {
                if (!args.Author.IsBot) 
                {
                    await DebugConsole($"Uživatel {args.Author.Username} poslal zprávu, která mu byla smazána do kanálu {args.Channel.Name} s obsahem: {args.Message.Content}", DebugLevel.Warning); 
                    await args.Message.DeleteAsync(); 
                }
            }

            // Obrázky Serveru kanál
            if (args.Channel == Program.GetChannel(ChannelNames.ServerImages) && args.Message.Attachments.Count == 0)
            {
                await SendDMMessage(args.Author, Messages.Default.warning_ServerImages);
                await SendDMMessage(args.Author, new DiscordMessageBuilder().WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Smazána zpráva")
                    .WithColor(DiscordColor.Red)
                    .WithDescription(args.Message.Content)));
                await DebugConsole($"Uživatel {args.Author.Username} poslal zprávu, která mu byla smazána do kanálu {args.Channel.Name} s obsahem: {args.Message.Content}", DebugLevel.Warning);
                await args.Message.DeleteAsync();
            }

            await Task.CompletedTask;
            return;
        }
        private static async Task OnMessageEdit(DiscordClient sender, MessageUpdateEventArgs args)
        {
            if (Program.GetChannel(ChannelNames.MessageLogging) == null || args.Message.Author.IsBot == true || (args.MessageBefore.Embeds.Count == 0 && args.Message.Embeds.Count != 0))
            {
                await Task.CompletedTask;
                return;
            }

            var embedMessage = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Goldenrod,
            };
            if (args.Message.Author != null)
            {
                embedMessage.Footer = new DiscordEmbedBuilder.EmbedFooter { Text = args.Message.Author.Username, IconUrl = args.Message.Author.AvatarUrl };
            }
            embedMessage.Author = new DiscordEmbedBuilder.EmbedAuthor { Name = "Upravená zpráva" };
            embedMessage.AddField("Před úpravou", $"`{args.MessageBefore.Content}`");
            embedMessage.AddField("Po úpravě", $"`{args.Message.Content}`");
            embedMessage.AddField("Vytvořena", args.Message.CreationTimestamp.UtcDateTime.ToString(), true);
            embedMessage.AddField("Upravená", DateTime.UtcNow.ToString(), true);

            await Program.GetChannel(ChannelNames.MessageLogging).SendMessageAsync(embed: embedMessage);
            await Task.CompletedTask;
            return;
        }
        private static async Task OnMessageDelete(DiscordClient sender, MessageDeleteEventArgs args)
        {
            if (args == null) { return; }
            if (args.Message.Author.IsBot) { return; }
            if (args.Message.Author.IsCurrent) { return; }
            if (args.Message.Channel == Program.GetChannel(ChannelNames.ToDo)) { return; }
            if (args.Message.Channel == Program.GetChannel(ChannelNames.Console)) { return; }
            if (args.Message.MessageType != MessageType.Default && args.Message.MessageType != MessageType.Reply) { return; }

            Dictionary<string, Stream> listOfFiles = new Dictionary<string, Stream>();

            var embedMessage = new DiscordEmbedBuilder
            {
                Color = DiscordColor.IndianRed,
                Description = $"`{args.Message.Content}`",
            };
            embedMessage.Author = new DiscordEmbedBuilder.EmbedAuthor { Name = "Smazána Zpráva" };
            if (args.Message.Author != null)
            {
                embedMessage.Footer = new DiscordEmbedBuilder.EmbedFooter { Text = args.Message.Author.Username, IconUrl = args.Message.Author.AvatarUrl };
            }
            embedMessage.AddField("Vytvořena", args.Message.CreationTimestamp.DateTime.ToString(), true);
            embedMessage.AddField("Smazána", DateTime.UtcNow.ToString(), true);
            embedMessage.AddField("Kanál", args.Message.Channel.Mention, true);


            foreach (var attachment in args.Message.Attachments)
            {
                embedMessage.AddField($"Přiloha {args.Message.Attachments.Count}", attachment.Url);
            }

            if (args.Message.Attachments.Count != 0)
            {
                foreach (var attachment in args.Message.Attachments)
                {
                    HttpClient hclient = new HttpClient();
                    Stream stream = await hclient.GetStreamAsync(attachment.Url);
                    listOfFiles.Add(attachment.FileName, stream);
                }
            }

            var attachmentMessage = new DiscordMessageBuilder()
                .AddEmbed(embed: embedMessage)
                .AddFiles(listOfFiles);


            await Program.GetChannel(ChannelNames.MessageLogging).SendMessageAsync(attachmentMessage);
            foreach (var attachment in listOfFiles)
            {
                attachment.Value.Close();
            }

            await Task.CompletedTask;
            return;
        }
        private static async Task OnReactionAdd(DiscordClient sender, MessageReactionAddEventArgs args)
        {
            // Whitelist Channel
            if (args.Channel.Id == Program.GetChannel(ChannelNames.Whitelist).Id)
            {
                DiscordMember[] members = await GetMembersByRole(Program.GetRole(RoleNames.NonWhitelisted));
                foreach (var member in members.Where(member => member.Id == args.User.Id))
                {
                    await args.Message.DeleteReactionAsync(args.Emoji, args.User);
                    await SendDMMessage(args.User, Messages.Default.warning_Reaction);
                    await DebugConsole($"Uživatel {args.User} reagoval na zprávu v kanálu {args.Channel.Name} a neměl dostatečné oprávnění na to", DebugLevel.Warning);
                }
            }

            await Task.CompletedTask;
            return;
        }

        //-------------------------------------------------------------------
        //                   Event: Připojení/Odpojení
        //-------------------------------------------------------------------
        private static async Task EmbedMemberAdd(DiscordClient sender, GuildMemberAddEventArgs args)
        {
            await args.Member.GrantRoleAsync(Program.GetRole(RoleNames.NonWhitelisted), "Připojení");
            if (Program.GetChannel(ChannelNames.UserLogging) == null)
            {
                await Task.CompletedTask;
                return;
            }

            var embedMessage = new DiscordEmbedBuilder
            {
                Color = DiscordColor.SpringGreen,
                Title = args.Member.Username,
                Timestamp = DateTime.Now,
            };
            embedMessage.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail { Url = args.Member.AvatarUrl };
            embedMessage.AddField("User ID", args.Member.Id.ToString(), true);
            embedMessage.AddField("Vytvoření účtů", args.Member.CreationTimestamp.ToString(), true);

            await Program.GetChannel(ChannelNames.UserLogging).SendMessageAsync(embed: embedMessage);
            await SendDMMessage(args.Member, Messages.Default.member_Join);
            await Task.CompletedTask;
        }
        private static async Task EmbedMemberRemove(DiscordClient sender, GuildMemberRemoveEventArgs args)
        {
            if (Program.GetChannel(ChannelNames.UserLogging) == null)
            {
                await Task.CompletedTask;
                return;
            }

            var embedMessage = new DiscordEmbedBuilder
            {
                Color = DiscordColor.IndianRed,
                Title = args.Member.Username,
                Timestamp = DateTime.Now,
            };
            embedMessage.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail { Url = args.Member.AvatarUrl };
            embedMessage.AddField("User ID", args.Member.Id.ToString(), true);
            embedMessage.AddField("Vytvoření účtů", args.Member.CreationTimestamp.ToString(), true);
            embedMessage.AddField("Připojil se na server", args.Member.JoinedAt.ToString(), true);

            await Program.GetChannel(ChannelNames.UserLogging).SendMessageAsync(embed: embedMessage);
            await Task.CompletedTask;
        }

        //-------------------------------------------------------------------
        //                   Event: Modal
        //-------------------------------------------------------------------
        private static async Task OnModalSubmit(DiscordClient sender, ModalSubmitEventArgs args)
        {
            if (args.Interaction.Type == InteractionType.ModalSubmit)
            {
                await DebugConsole($"Uživatel {args.Interaction.User} odeslal modal s id: {args.Interaction.Data.CustomId}");

                if (args.Interaction.Data.CustomId == "modal_whitelist")
                {
                    await NewWhitelist(args);
                }
                else if (args.Interaction.Data.CustomId == "modal_changelog")
                {
                    await CreateChangelog(args);
                }
                else if (args.Interaction.Data.CustomId == "modal_oznameni")
                {
                    await CreateOznameni(args);
                }
            }
            await Task.CompletedTask;
            return;
        }


        //-------------------------------------------------------------------
        //                   Event: Update
        //-------------------------------------------------------------------
        private static async Task OnMemberUpdate(DiscordClient sender, GuildMemberUpdateEventArgs args)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("  Member Before:");
            sb.AppendLine($"  {args.MemberBefore.Username} [{args.MemberBefore.Nickname}] ({args.MemberBefore.Nickname})");
            sb.AppendLine($"  {args.MemberBefore.Email}");
            sb.AppendLine($"  CREATION:{args.MemberBefore.CreationTimestamp.UtcDateTime}");
            sb.AppendLine($"  JOINEDAT:{args.MemberBefore.JoinedAt.UtcDateTime}");
            sb.AppendLine($"  LOCALE:{args.MemberBefore.Locale.ToArray()}");
            sb.AppendLine($"  MFA: {args.MemberBefore.MfaEnabled.Value}");
            sb.AppendLine($"  {args.MemberBefore.Roles.ToArray()}");
            sb.AppendLine("");
            sb.AppendLine("  Member After:");
            sb.AppendLine($"  {args.MemberAfter.Username} [{args.MemberAfter.DisplayName}] ({args.MemberAfter.Nickname})");
            sb.AppendLine($"  {args.MemberAfter.Email}");
            sb.AppendLine($"  CREATION:{args.MemberAfter.CreationTimestamp.UtcDateTime}");
            sb.AppendLine($"  JOINEDAT:{args.MemberAfter.JoinedAt.UtcDateTime}");
            sb.AppendLine($"  LOCALE:{args.MemberAfter.Locale.ToArray()}");
            sb.AppendLine($"  MFA: {args.MemberAfter.MfaEnabled.Value}");
            sb.AppendLine($"  {args.MemberAfter.Roles}");

            await DebugConsole(sb.ToString());
        }
        #endregion

        #region Funkce
        //-------------------------------------------------------------------
        //                   Funkce: Changelog
        //-------------------------------------------------------------------
        private static async Task CreateChangelog(ModalSubmitEventArgs args)
        {
            DiscordMember member = await GetMemberFromUser(args.Interaction.User);
            // Messages
            var embedMessage = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Orange,
                Title = args.Values["changelogTittleID"],
                Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    Text = member.Nickname,
                    IconUrl = member.AvatarUrl,
                }
            };
            if (args.Values["changelogThumbnailimgID"] != null && args.Values["changelogThumbnailimgID"] != string.Empty) { embedMessage.WithThumbnail(args.Values["changelogThumbnailimgID"]); }
            if (args.Values["changelogBigimgID"] != null && args.Values["changelogBigimgID"] != string.Empty) { embedMessage.WithImageUrl(args.Values["changelogBigimgID"]); }
            if (args.Values["changelogBeforeID"] != null && args.Values["changelogBeforeID"] != string.Empty) { embedMessage.AddField("Před", $"{args.Values["changelogBeforeID"]}", false); }
            if (args.Values["changelogNowID"] != null && args.Values["changelogNowID"] != string.Empty) { embedMessage.AddField("Nyní", $"{args.Values["changelogNowID"]}", false); }

            var msg = new DiscordMessageBuilder().WithEmbed(embedMessage);

            // Exit
            await Program.GetChannel(ChannelNames.Changelog).SendMessageAsync(msg);
            await Task.CompletedTask;
        }

        //-------------------------------------------------------------------
        //                   Funkce: Oznamení
        //-------------------------------------------------------------------
        private static async Task CreateOznameni(ModalSubmitEventArgs args)
        {
            var embedMessage = new DiscordEmbedBuilder
            {
                Color = new DiscordColor("FFFF00"),
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = "Oznámení",
                },
                Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    Text = "Aeterum Team",
                    IconUrl = Server.IconUrl,
                }
            };

            if (args.Values["oznameniTitleID"] != null) embedMessage.WithTitle(args.Values["oznameniTitleID"].ToString());
            if (args.Values["oznameniDescID"] != null) embedMessage.WithDescription(args.Values["oznameniDescID"].ToString());
            if (args.Values["oznameniThumbnailimgID"] != null) embedMessage.WithThumbnail(args.Values["oznameniThumbnailimgID"].ToString());
            if (args.Values["oznameniBigimgID"] != null) embedMessage.WithImageUrl(args.Values["oznameniBigimgID"].ToString());

            // Exit
            var msg = new DiscordMessageBuilder().WithEmbed(embedMessage);
            var sentMsg = await args.Interaction.Channel.SendMessageAsync(msg);
            await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Úspěšně jsi vytvořil oznámení " + sentMsg.JumpLink));
            await Task.CompletedTask;
        }

        //-------------------------------------------------------------------
        //                   Funkce: To-Do
        //-------------------------------------------------------------------
        public static async Task ToDoUpdate()
        {
            var toDoMessage = await GetLastBotMessageAsync(Program.GetChannel(ChannelNames.ToDo));

            if (toDoMessage == null) { Console.WriteLine("Couldnt get to-do message"); await DebugConsole("Nepodařilo se získat to-do zprávu", DebugLevel.Error); return; }

            var msg = new DiscordMessageBuilder();

            int neededEmbeds = ToDoList.Count / 25; // Kolik je zapotřebí embedu
            if (ToDoList.Count % 25 > 0) neededEmbeds++; // Pokud je zbytek ve zlomku pričti jeden embed navíc

            List<string> remainingToDos = new List<string>(ToDoList);
            int ToDoPosCounter = 0;

            for (int n = 0; n < neededEmbeds; n++) // Vytvoří embedy s 25 todo nebo zbytkem
            {
                var embedMsg = new DiscordEmbedBuilder().WithColor(DiscordColor.Aquamarine);
                if (n == 0) embedMsg.WithTitle("To-Do Seznam");
                for (int i = 0; i < 25 && remainingToDos.Count > 0; i++) // Ensure we don't exceed the list count
                {
                    ToDoPosCounter++;
                    embedMsg.AddField($"{ToDoPosCounter}:", $"{remainingToDos[0]}");
                    remainingToDos.RemoveAt(0);
                }

                msg.AddEmbed(embedMsg);
            }

            bool messageUpdated = false;
            int retryCount = 0;
            const int maxRetries = 5; // Maximum number of retries

            while (!messageUpdated && retryCount < maxRetries)
            {
                try
                {
                    await toDoMessage.ModifyAsync(msg);
                    messageUpdated = true;
                }
                catch (Exception ex)
                {
                    if (ex is RateLimitException || ex is UnauthorizedException || ex is BadRequestException)
                    {
                        retryCount++;
                        int delay = (int)Math.Pow(2, retryCount); // Exponential backoff
                        Console.WriteLine($"Rate limit hit or API error, retrying after {delay} seconds...");
                        await DebugConsole($"Rate limit hit or API error, retrying after {delay} seconds...", DebugLevel.Warning);
                        await Task.Delay(delay * 1000);
                    }
                    else
                    {
                        Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                        await DebugConsole($"An unexpected error occurred: {ex.Message}", DebugLevel.Error);
                        break; // Break the loop on non-rate limit and non-API exceptions
                    }
                }
            }

            if (!messageUpdated)
            {
                Console.WriteLine("Failed to update message after several retries.");
                await DebugConsole("Failed to update message after several retries.", DebugLevel.Error);
            }

            await Task.CompletedTask;

        }

        //-------------------------------------------------------------------
        //                   Funkce: Přihlášky
        //-------------------------------------------------------------------
        // Manual Mode
        private static async Task WhitelistSuccess(ComponentInteractionCreateEventArgs args)
        {
            await SendMinecraftCommand($"whitelist add {args.Message.Embeds[0].Fields[0].Value}"); // Přidá uživatele na whitelist

            var member = await GetMemberFromUser(args.Message.MentionedUsers[0]);
            await member.RevokeRoleAsync(Program.GetRole(RoleNames.NonWhitelisted), "Zvládnul whitelist"); // Odebere roli NonWhitelisted
            await member.GrantRoleAsync(Program.GetRole(RoleNames.Whitelisted), "Zvládnul whitelist"); // Přidá roli Whitelisted

            await SendDMMessage(member, Messages.Default.whitelist_Success); // Odešle uživateli zprávu o úspěšné přihlášce

            // Archivuje přihlášku
            await WhitelistArchive(args, true);
            await Task.CompletedTask;
        }
        private static async Task WhitelistArchive(ComponentInteractionCreateEventArgs args, bool success = false)
        {
            try
            {
                var yesUsers = await args.Message.GetReactionsAsync(ApproveEmoji, 30);
                var noUsers = await args.Message.GetReactionsAsync(DisApproveEmoji, 30);
                string yesUsersString = "Nikdo";
                string noUsersString = "Nikdo";
                if ((yesUsers.Count - 1) > 0) yesUsersString = "";
                if ((noUsers.Count - 1) > 0) noUsersString = "";


                foreach (var reaction in yesUsers)
                {
                    if (!reaction.IsBot)
                    {
                        yesUsersString += $"{reaction.Username}, ";
                    }
                }
                foreach (var reaction in noUsers)
                {
                    if (!reaction.IsBot)
                    {
                        noUsersString += $"{reaction.Username}, ";
                    }
                }

                // Zkopíruje ještě neuzavřenou přihlášku (pouze embed)
                var embedmsg = args.Message.Embeds[0];
                var embedMessage = new DiscordEmbedBuilder(embedmsg);

                // Změní barvu embedu podle toho zda uspěl či ne
                embedMessage.Color = success ? DiscordColor.Green : DiscordColor.Red;

                // Aktualizuje data v databazi
                var conn = await Database.Connect();
                if (success)
                {
                    int newInt = Program.GetInt(IntNames.WhitelistSuccess) + 1;
                    await Program.UpdateInt(conn, IntNames.WhitelistSuccess, newInt);
                }
                else
                {
                    int newInt = Program.GetInt(IntNames.WhitelistFail) + 1;
                    await Program.UpdateInt(conn, IntNames.WhitelistFail, newInt);
                }
                await Database.Disconnect();

                // Přidá počet hlasů na přihlášce
                embedMessage.AddField($"{ApproveEmoji} ({yesUsers.Count - 1})", yesUsersString, false);
                embedMessage.AddField($"{DisApproveEmoji} ({noUsers.Count - 1})", noUsersString, false);

                // Přidá kdy byla přihláška utvořená a uzavřená
                embedMessage.AddField($"Vytvořená:", args.Message.CreationTimestamp.UtcDateTime.ToString(), true);
                embedMessage.AddField($"Uzavřená:", DateTime.UtcNow.ToString(), true);

                // Přidá kdo uzavřel přihlášku
                embedMessage.Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    IconUrl = args.Interaction.User.AvatarUrl,
                    Text = "Zkontroloval: " + args.Interaction.User.Username
                };

                await Program.GetChannel(ChannelNames.WhitelistArchive).SendMessageAsync(embedMessage);
            }
            catch (Exception ex)
            {
                await DebugConsole($"Něco se pokazilo při dávání přihlášky do archívu a nebyla tedy smazána! - {ex.Message}", DebugLevel.Error);
                return;
            }
            await args.Message.DeleteAsync();
            await Task.CompletedTask;
        }
        // Automatic Mode
        private static async Task WhitelistSuccessAuto(ComponentCrtEventArgs args)
        {
            await SendMinecraftCommand($"whitelist add {args.Message.Embeds[0].Fields[0].Value}"); // Přidá uživatele na whitelist

            var member = await GetMemberFromUser(args.Message.MentionedUsers[0]);
            await member.RevokeRoleAsync(Program.GetRole(RoleNames.NonWhitelisted), "Zvládnul whitelist"); // Odebere roli NonWhitelisted
            await member.GrantRoleAsync(Program.GetRole(RoleNames.Whitelisted), "Zvládnul whitelist"); // Přidá roli Whitelisted

            await SendDMMessage(member, Messages.Default.whitelist_Success); // Odešle uživateli zprávu o úspěšné přihlášce

            // Archivuje přihlášku
            await WhitelistArchiveAuto(args, true);
            await Task.CompletedTask;
        }
        private static async Task WhitelistArchiveAuto(ComponentCrtEventArgs args, bool success = false)
        {
            try
            {
                var yesUsers = await args.Message.GetReactionsAsync(ApproveEmoji, 30);
                var noUsers = await args.Message.GetReactionsAsync(DisApproveEmoji, 30);
                string yesUsersString = "Nikdo";
                string noUsersString = "Nikdo";
                if ((yesUsers.Count - 1) > 0) yesUsersString = "";
                if ((noUsers.Count - 1) > 0) noUsersString = "";


                foreach (var reaction in yesUsers)
                {
                    if (!reaction.IsBot)
                    {
                        yesUsersString += $"{reaction.Username}, ";
                    }
                }
                foreach (var reaction in noUsers)
                {
                    if (!reaction.IsBot)
                    {
                        noUsersString += $"{reaction.Username}, ";
                    }
                }

                // Zkopíruje ještě neuzavřenou přihlášku (pouze embed)
                var embedmsg = args.Message.Embeds[0];
                var embedMessage = new DiscordEmbedBuilder(embedmsg);

                // Změní barvu embedu podle toho zda uspěl či ne
                embedMessage.Color = success ? DiscordColor.Green : DiscordColor.Red;

                // Aktualizuje data v databazi
                var conn = await Database.Connect();
                if (success)
                {
                    int newInt = Program.GetInt(IntNames.WhitelistSuccess) + 1;
                    await Program.UpdateInt(conn, IntNames.WhitelistSuccess, newInt);
                }
                else
                {
                    int newInt = Program.GetInt(IntNames.WhitelistFail) + 1;
                    await Program.UpdateInt(conn, IntNames.WhitelistFail, newInt);
                }
                await Database.Disconnect();

                // Přidá počet hlasů na přihlášce
                embedMessage.AddField($"{ApproveEmoji} ({yesUsers.Count - 1})", yesUsersString, false);
                embedMessage.AddField($"{DisApproveEmoji} ({noUsers.Count - 1})", noUsersString, false);

                // Přidá kdy byla přihláška utvořená a uzavřená
                embedMessage.AddField($"Vytvořená:", args.Message.CreationTimestamp.UtcDateTime.ToString(), true);
                embedMessage.AddField($"Uzavřená:", DateTime.UtcNow.ToString(), true);

                // Přidá kdo uzavřel přihlášku
                embedMessage.Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    IconUrl = args.Interaction.User.AvatarUrl,
                    Text = "Zkontrolováno automaticky"
                };

                await Program.GetChannel(ChannelNames.WhitelistArchive).SendMessageAsync(embedMessage);
            }
            catch (Exception ex)
            {
                await DebugConsole($"Něco se pokazilo při dávání přihlášky do archívu a nebyla tedy smazána! - {ex.Message}", DebugLevel.Error);
                return;
            }
            await args.Message.DeleteAsync();
            await Task.CompletedTask;
        }

        private static async Task WhitelistModal(ComponentInteractionCreateEventArgs args)
        {
            var modal = new DiscordInteractionResponseBuilder()
                .WithTitle("Tvoje přihláška")
                .WithCustomId("modal_whitelist");
            modal.AddComponents(new TextInputComponent("Nickname", "nicknameLabelID", "Tvá přezdívka ve hře"));
            modal.AddComponents(new TextInputComponent("Věk", "ageLabelID", "Tvůj věk"));
            modal.AddComponents(new TextInputComponent("Jak ses o nás dozvěděl/a?", "knowDescID", "Např. Minecraft List, buď specifický..."));
            modal.AddComponents(new TextInputComponent("Co od serveru očekáváš?", "expectationDescID"));
            modal.AddComponents(new TextInputComponent("Něco o sobě", "infoDescID", "Zde se rozepiš", null, true, TextInputStyle.Paragraph));

            await args.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);
            await Task.CompletedTask;
        }
        private static async Task NewWhitelist(ModalSubmitEventArgs args)
        {
            var msg = new DiscordMessageBuilder();
            bool hasResponded = false;  // Track if a response has been sent

            try
            {
                // Buttons
                var approveButton = await CreateButtonComponent(ButtonStyle.Success, "btn_approve_id", "Prošel", false, Btn_ApproveEmoji);
                var closeButton = await CreateButtonComponent(ButtonStyle.Danger, "btn_close_id", "Neprošel", false, Btn_DisApproveEmoji);

                // Messages
                msg = new DiscordMessageBuilder()
                        .WithContent(args.Interaction.User.Mention + " ||" + GetRole(RoleNames.Whitelisted).Mention + "||")
                        .WithAllowedMentions(new IMention[] { new UserMention(args.Interaction.User) })
                        .AddComponents(approveButton, closeButton);

                var embedMessage = new DiscordEmbedBuilder
                {
                    Color = new DiscordColor("89CFF0"),
                    Title = $"Přihláška #{Program.GetInt(IntNames.WhitelistTotal) + 1}",
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = args.Interaction.User.Username,
                        IconUrl = args.Interaction.User.AvatarUrl
                    },
                };

                embedMessage.AddField("Nickname", args.Values["nicknameLabelID"], false);
                embedMessage.AddField("Věk", args.Values["ageLabelID"], false);
                embedMessage.AddField("Jak ses o nás dozvěděl/a?", args.Values["knowDescID"], false);
                embedMessage.AddField("Co od serveru očekáváš?", args.Values["expectationDescID"], false);

                char[] contentArray = args.Values["infoDescID"].ToArray();
                int first = 1;

                if (contentArray.Length <= 1024)
                {
                    embedMessage.AddField("Něco o sobě", args.Values["infoDescID"], false);
                }
                else
                {
                    string stopWhen = ".";
                    int currentCharPos;
                    while (contentArray.Length > 1024)
                    {
                        currentCharPos = 1024;

                        while (contentArray[currentCharPos].ToString() != stopWhen)
                        {
                            currentCharPos--;
                            if (currentCharPos < 300) { stopWhen = string.Empty; currentCharPos = 1024; }
                        }

                        currentCharPos++;

                        if (first == 1)
                        {
                            embedMessage.AddField("Něco o sobě", string.Join("", contentArray.Take(currentCharPos).ToArray()), false);
                            first = 0;
                        }
                        else
                        {
                            embedMessage.AddField("|", string.Join("", contentArray.Take(currentCharPos).ToArray()), false);
                        }
                        contentArray = contentArray.Skip(currentCharPos).ToArray();
                    }

                    if (contentArray.Length <= 1024)
                    {
                        embedMessage.AddField("|", string.Join("", contentArray), false);
                    }
                }

                embedMessage.WithFooter("Zbývá: 2 dny 0 hodin 0 minut");
                string playerUUID = await GetMinecraftUUIDByUsername(args.Values["nicknameLabelID"]);
                embedMessage.WithThumbnail("https://mc-heads.net/" + Program.GetOther(OtherNames.WhitelistThumbnailType) + "/" + playerUUID);
                msg.AddEmbed(embedMessage);

                var sentMSG = await Program.GetChannel(ChannelNames.Whitelist).SendMessageAsync(msg);
                await sentMSG.CreateReactionAsync(ApproveEmoji);
                await sentMSG.CreateReactionAsync(DisApproveEmoji);

                // Respond to the interaction
                await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Přihlášku jsi vytvořil, za celý projekt ti přejeme hodně štěstí s úspěchem! ^-^").AsEphemeral(true));
                hasResponded = true;
            }
            catch (Exception ex)
            {
                if (!hasResponded)
                {
                    await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Něco se pokazilo s přihláškou, prosím utvoř si ticket!").AsEphemeral(true));
                    hasResponded = true;
                }
                await DebugConsole($"Přihlášku pro uživatele {args.Interaction.User.Username} nebylo možné utvořit! - {ex.Message}", DebugLevel.Error);
                await Program.GetChannel(ChannelNames.DebugConsole).SendMessageAsync(msg);
                return;
            }

            // Aktualizace databáze
            var conn = await Database.Connect();
            int newInt = Program.GetInt(IntNames.WhitelistTotal) + 1;
            await Program.UpdateInt(conn, IntNames.WhitelistTotal, newInt);
            await Database.Disconnect();

            Console.WriteLine("Here");
            Console.WriteLine("Here2");
            await Task.CompletedTask;
        }

        public static async Task RevokeWhitelist(DiscordMessage archivedWhitelist)
        {
            // Buttons
            var approveButton = CreateButtonComponent(ButtonStyle.Success, "btn_approve_id", "Prošel", false, Btn_ApproveEmoji).Result;
            var closeButton = CreateButtonComponent(ButtonStyle.Danger, "btn_close_id", "Neprošel", false, Btn_DisApproveEmoji).Result;

            //Revoke info
            var originalEmbed = archivedWhitelist.Embeds[0];
            DiscordMember originalAuthor = Server.GetAllMembersAsync().Result.Where(x => x.Username == originalEmbed.Author.Name).First();
            string originalNickname = originalEmbed.Fields.Where(x => x.Name == "Nickname").FirstOrDefault().Value;

            // Messages
            var msg = new DiscordMessageBuilder()
                    .WithContent(originalAuthor.Mention)
                    .WithAllowedMentions(new IMention[] { new UserMention(originalAuthor) })
                    .AddComponents(approveButton, closeButton);
            var embedMessage = new DiscordEmbedBuilder
            {
                Color = new DiscordColor("89CFF0"),
                Title = $"Přihláška #{Program.GetInt(IntNames.WhitelistTotal)}",
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = originalAuthor.Username,
                    IconUrl = originalAuthor.AvatarUrl
                },
            };

            for (int i = 0; i < originalEmbed.Fields.Count - 4; i++)// Add all fields but not last 4
            {
                embedMessage.AddField(originalEmbed.Fields[i].Name, originalEmbed.Fields[i].Value.ToString(), originalEmbed.Fields[i].Inline);
            }

            embedMessage.WithFooter("Zbývá: 2 dny 0 hodin 0 minut"); // Footer with timer
            string playerUUID = await GetMinecraftUUIDByUsername(originalEmbed.Fields.FirstOrDefault().Value);
            embedMessage.WithThumbnail("https://mc-heads.net/" + Program.GetOther(OtherNames.WhitelistThumbnailType) + "/" + playerUUID); // Add Thumbnail with player skin by player nickname
            msg.AddEmbed(embedMessage); // Add embed to tag username

            // Exit
            var sentMSG = await Program.GetChannel(ChannelNames.Whitelist).SendMessageAsync(msg);
            await sentMSG.CreateReactionAsync(ApproveEmoji); // Create :white_checked_approve: reaction
            await sentMSG.CreateReactionAsync(DisApproveEmoji); // Create :x: reaction

            // Aktualizace Databáze
            var conn = await Database.Connect();
            if (originalEmbed.Color == DiscordColor.Green)
            {
                int newInt = Program.GetInt(IntNames.WhitelistSuccess) - 1;
                await Program.UpdateInt(conn, IntNames.WhitelistSuccess, newInt);
            } // Remove from WhitelistSuccess 1
            else 
            {
                int newInt = Program.GetInt(IntNames.WhitelistFail) - 1;
                await Program.UpdateInt(conn, IntNames.WhitelistFail, newInt);
            } // Remove from WhitelistFail 1
            await Database.Disconnect();

            await originalAuthor.RevokeRoleAsync(Program.GetRole(RoleNames.Whitelisted), "Odebrán z nedorozumnění"); // Odebrána role kvůli obnovení přihlášky
            await originalAuthor.GrantRoleAsync(Program.GetRole(RoleNames.NonWhitelisted), "Přidán z nedorozumnění"); // Přidána role kvůli obnovení přihlášky

            await SendDMMessage(originalAuthor, Messages.Default.whitelist_Mistake_Revoking); // Odešle uživateli zprávu ohledně obnovení přihlášky s omluvou


            await SendMinecraftCommand("whitelist remove " + originalNickname); // Odebere uživatele z whitelistu

            await archivedWhitelist.DeleteAsync();
            await Task.CompletedTask;
        }

        public static async Task VoidUpdater(object sender, ElapsedEventArgs e)
        {
            const int maxRetries = 3;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    var messages = await Program.GetChannel(ChannelNames.Whitelist).GetMessagesAsync(20);

                    if (messages.Count == 1)
                    {
                        await Task.CompletedTask;
                        return;
                    }

                    await UpdateWhitelistTime(sender, e);
                    break; // Exit the loop if successful
                }
                catch (DSharpPlus.Exceptions.ServerErrorException ex) when ((int)ex.HResult == 503)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        // Log the error (consider using a logging framework)
                        Console.WriteLine($"Failed to fetch messages after {maxRetries} attempts: {ex.Message}");
                        await DebugConsole($"Failed to fetch messages after {maxRetries} attempts: {ex.Message}");
                    }
                    else
                    {
                        // Wait before retrying (e.g., exponential backoff)
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
                    }
                }
                catch (Exception ex)
                {
                    // Log other exceptions and handle them accordingly
                    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                    break;
                }
            }
        }
        public static async Task UpdateWhitelistTime(object sender, ElapsedEventArgs e)
        {
            var messages = Program.GetChannel(ChannelNames.Whitelist).GetMessagesAsync().Result.Where(x => x.Embeds[0].Author != null).ToList();
            if (messages.Count == 0) { await Task.CompletedTask; return; }
            foreach (var message in messages)
            {
                var whitelistTime = message.Timestamp.DateTime.AddHours(48);
                int minutes = Convert.ToInt16(whitelistTime.Subtract(DateTime.Now).Minutes);
                int hours = Convert.ToInt16(whitelistTime.Subtract(DateTime.Now).Hours);
                int days = Convert.ToInt16(whitelistTime.Subtract(DateTime.Now).Days);
                string minutesString = string.Empty;
                string hoursString = string.Empty;
                string daysString = string.Empty;
                if (minutes >= 5) { minutesString = "minut"; } else if (minutes <= 4 && minutes != 1) { minutesString = "minuty"; } else if (minutes == 1) { minutesString = "minuta"; }
                if (hours >= 5) { hoursString = "hodin"; } else if (hours <= 4 && hours != 1) { hoursString = "hodiny"; } else if (hours == 1) { hoursString = "hodina"; }
                if (days >= 5) { daysString = "dnů"; } else if (days <= 4 && days != 1) { daysString = "dny"; } else if (days == 1) { daysString = "den"; }
                string timeFieldValue = $"Zbývá: {days} {daysString} {hours} {hoursString} {minutes} {minutesString}";
                // 1 minuta 2,3,4, minuty 5,6,7,8 minut
                // 1 hodina 2,3,4 hodiny 5,6,7,8... hodin

                if (days <= 0 && hours <= 0 && minutes <= 0)
                {
                    timeFieldValue = "Čeká na schválení!";
                    if (Program.GetBoolean(BooleanNames.AutomateWhitelist))
                    {
                        var members = await GetMembersByRole(Program.GetRole(RoleNames.Admin));
                        int yesCount = message.GetReactionsAsync(DiscordEmoji.FromName(Client, ":white_check_mark:", false), 30).Result.Count;
                        int noCount = message.GetReactionsAsync(DiscordEmoji.FromName(Client, ":x:", false), 30).Result.Count;
                        if (yesCount > noCount)
                        {
                            await WhitelistSuccessAuto(new ComponentCrtEventArgs(message));
                        }
                        else if (yesCount == noCount)
                        {
                            var _msg = Messages.Default.whitelist_Pending;
                            _msg.WithEmbed(new DiscordEmbedBuilder(_msg.Embed).AddField("Zpráva", message.JumpLink.ToString(), false));
                            _msg.WithContent(message.Content);

                            foreach (var member in members)
                            {
                                var DMChannel = await member.CreateDmChannelAsync();

                                if (DMChannel.GetMessagesAsync(5).Result.First().Content != message.Content)
                                {
                                    await DMChannel.SendMessageAsync(_msg);
                                }
                            }
                        }
                        else
                        {
                            await WhitelistArchiveAuto(new ComponentCrtEventArgs(message));
                        }
                    }
                    else
                    {
                        var _msg = Messages.Default.whitelist_Pending;
                        _msg.WithEmbed(new DiscordEmbedBuilder(_msg.Embed).AddField("Zpráva", message.JumpLink.ToString(), false));
                        _msg.WithContent(message.Content);

                        DiscordMember[] members = await GetMembersByRole(Program.GetRole(RoleNames.Admin));
 
                        if (message.Embeds.First().Footer.Text != "Čeká na schválení")
                        {
                            foreach (var member in members)
                            {
                                if (!member.IsBot)
                                {
                                    await SendDMMessage(member, _msg);
                                    Console.WriteLine($"[INFO] - {member.Username} byla poslána zpráva whitelist_pending");
                                    await DebugConsole($"{member.Username} byla poslána zpráva whitelist_pending");
                                }
                            }
                        }
                    }
                }
                var embed = message.Embeds[0];
                var newEmbed = new DiscordEmbedBuilder()
                {
                    Color = embed.Color,
                    Title = embed.Title,
                    Author = new DiscordEmbedBuilder.EmbedAuthor()
                    {
                        Name = embed.Author.Name,
                        IconUrl = embed.Author.IconUrl.ToString(),
                    },
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                    {
                        Url = embed.Thumbnail.Url.ToString(),
                    },
                    Footer = new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = timeFieldValue,
                    }
                };
                for (int i = 0; i < embed.Fields.Count; i++)
                {
                    newEmbed.AddField(embed.Fields[i].Name, embed.Fields[i].Value.ToString(), embed.Fields[i].Inline);
                }
                var msg = new DiscordMessageBuilder()
                {
                    Content = message.Content,
                    Embed = newEmbed
                };
                msg.AddComponents(message.Components);

                await message.ModifyAsync(msg);
                await Task.CompletedTask;
            }
        }
        #endregion

        #region Utility
        //-------------------------------------------------------------------
        //                   Funkce: Utility
        //-------------------------------------------------------------------
        public static async Task SyncToDoDictionary()
        {
            DiscordMessage ToDoMessage = null;

            try
            {
                ToDoMessage = Program.GetChannel(ChannelNames.ToDo).GetMessagesAsync(10).Result.Where(x => x.Author.IsBot == true).ToList().FirstOrDefault();
            }
            catch
            {
                Console.WriteLine("Couldn't get to-do message creating one");
                await DebugConsole("Nepodařilo se získat to-do zprávu, vytvářím novou", DebugLevel.Warning);
                await Task.CompletedTask;
            }

            if (ToDoMessage == null)
            {
                var msg = new DiscordEmbedBuilder().WithTitle("To-Do Seznam").WithColor(DiscordColor.Aquamarine);
                for (int i = 0; i < ToDoList.Count; i++)
                {
                    msg.AddField($"{i + 1}:", $"{ToDoList[i]}");
                }

                ToDoMessage = await Program.GetChannel(ChannelNames.ToDo).SendMessageAsync(msg);
                await Task.CompletedTask;
                return;
            }

            if (ToDoMessage.Embeds.FirstOrDefault().Fields == null) { await Task.CompletedTask; return; }
            ToDoList.Clear();
            foreach (var embed in ToDoMessage.Embeds)
            {
                foreach (var field in embed.Fields)
                {
                    string msg = field.Value;
                    ToDoList.Add(msg);
                }
            }

            await Task.CompletedTask;
        }

        //
        // Souhrn: 
        //      Jednoduché tvoření tlačítek
        public static async Task<DiscordButtonComponent> CreateButtonComponent(ButtonStyle style, string customID, string buttonText, bool disabled = false, DiscordEmoji emoji = null)
        {
            try
            {
                var btn = new DiscordButtonComponent(style, customID, buttonText, disabled, new DiscordComponentEmoji(emoji));
                return await Task.FromResult(btn);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"({ex.Source}) - {ex.Message}");
                await DebugConsole($"({ex.Source}) - {ex.Message}", DebugLevel.Error);
                return null;
            }
        }

        //
        // Souhrn: 
        //      Jednoduché načtení emoji podle jména
        public static async Task<DiscordEmoji> GetEmojiFromName(string emojiName)
        {
            try
            {
                var emoji = DiscordEmoji.FromName(Client, emojiName);
                return await Task.FromResult(emoji);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"({ex.Source}) - {ex.Message}");
                await DebugConsole($"(Nepodařilo se získat emoji {emojiName}", DebugLevel.Error);
                return null;
            }
        }

        //
        // Souhrn: 
        //      Jednoduché načtení kanálu podle ID
        public static async Task<DiscordChannel> GetChannelFromID(string channelID)
        {
            try
            {
                DiscordChannel channel = await Client.GetChannelAsync(Convert.ToUInt64(channelID));
                return await Task.FromResult(channel);
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"({channelID}) - {ex.Message}");
                await DebugConsole($"Nepodařilo se získat kanál s Id: {channelID}", DebugLevel.Error);
                return null;
            }
        }

        //
        // Souhrn: 
        //      Vrátí poslední zprávu v kanálu od aktuálního bota
        public static async Task<DiscordMessage> GetLastBotMessageAsync(DiscordChannel channel, int limit = 10)
        {
            try
            {
                var messages = await channel.GetMessagesAsync(limit);
                if (messages.Count == 0) { return null; }
                return messages.First(x => x.Author.IsBot);
            }
            catch
            {
                Console.WriteLine("Nepodařilo se najít poslední zprávu od bota");
                await DebugConsole($"Nepodařilo se najít poslední zprávu od bota v kanálu: {channel.Name} ({channel.Id}), bylo zkontrolováno {limit} zpráv");
                return null;
            }
        }

        //
        // Souhrn: 
        //      Jednoduché načtení role podle ID
        public static async Task<DiscordRole> GetRoleFromID(string roleID)
        {
            try
            {
                DiscordRole role = Server.GetRole(Convert.ToUInt64(roleID));
                return await Task.FromResult(role);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"({roleID}) - {ex.Message}");
                await DebugConsole($"Nepodařilo se získat roli s Id: {roleID}", DebugLevel.Error);
                return null;
            }
        }

        //
        // Souhrn: 
        //      Vrátí všechny uživatelé podle role
        public static async Task<DiscordMember[]> GetMembersByRole(DiscordRole role)
        {
            try
            {
                var members = Server.GetAllMembersAsync().Result.Where(x => x.Roles.Contains(role)).ToArray<DiscordMember>();
                return await Task.FromResult(members);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"({ex.Source}) - {ex.Message}");
                await DebugConsole($"Nepodařilo se získat Serverové Uživatele kteří mají roli: {role.Name} ({role.Id})", DebugLevel.Error);
                return null;
            }
        }

        //
        // Souhrn: 
        //      Vrátí Server Uživatele z Uživatele
        public static async Task<DiscordMember> GetMemberFromUser(DiscordUser user)
        {
            try
            {
                var member = Server.GetAllMembersAsync().Result.Where(x => x.Id == user.Id).First();
                return await Task.FromResult(member);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"({ex.Source}) - {ex.Message}");
                await DebugConsole($"Nepodařilo se získat Serverového Uživatele za pomocí uživatele: {user.Username} ({user.Id})", DebugLevel.Error);
                return null;
            }
        }

        //
        // Souhrn: 
        //      Pošle minecraft příkaz (bez /)
        public static async Task SendMinecraftCommand(string command)
        {
            try
            {
                await Program.GetChannel(ChannelNames.Console).SendMessageAsync(command);
            }
            catch
            {
                Console.WriteLine($"Could not send /{command}");
                await DebugConsole($"Nepodařilo se odeslat příkaz do minecraftu /{command}", DebugLevel.Error);
            }
        }

        //
        // Souhrn: 
        //      Pošle soukromou zprávu Server Uživateli / Uživateli
        public static async Task SendDMMessage(DiscordMember member, DiscordMessageBuilder message)
        {
            try
            {
                var DMChannel = await member.CreateDmChannelAsync();
                await DMChannel.SendMessageAsync(message);

                await DebugConsole($"Byla odeslána zpráva uživateli: {member.Username} ({member.Id}) s obsahem: {message.Content}");

                await Task.CompletedTask;
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"({ex.Source}) - {ex.Message}");
                await DebugConsole($"Nepodařilo se odeslat soukromou zprávu Serverovému Uživateli: {member.Username} ({member.Id}) s obsahem: {message.Content})", DebugLevel.Error);
                return;
            }
        }
        public static async Task SendDMMessage(DiscordUser user, DiscordMessageBuilder message)
        {
            try
            {
                var member = GetMemberFromUser(user).Result;
                var DMChannel = await member.CreateDmChannelAsync();
                await DMChannel.SendMessageAsync(message);

                await DebugConsole($"Byla odeslána zpráva uživateli: {user.Username} ({user.Id}) s obsahem: {message.Content}");

                await Task.CompletedTask;
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"({ex.Source}) - {ex.Message}");
                await DebugConsole($"Nepodařilo se odeslat soukromou zprávu uživateli: {user.Username} ({user.Id}) s obsahem: {message.Content})", DebugLevel.Error);
                return;
            }
        }

        //
        // Souhrn: 
        //      Vrátí minecraft UUID podle minecraft nickname
        public static async Task<string> GetMinecraftUUIDByUsername(string username)
        {
            try
            {
                HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(string.Format("https://api.mojang.com/users/profiles/minecraft/" + username));
                webReq.Method = "GET";

                HttpWebResponse webResp = (HttpWebResponse)webReq.GetResponse();


                string jsonString;
                using (Stream stream = webResp.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                    jsonString = reader.ReadToEnd();
                }

                var jsonID = JObject.Parse(jsonString);
                string userID = jsonID.GetValue("id").ToString();
                return await Task.FromResult(userID);
            }
            catch (WebException ex)
            {
                Console.WriteLine("[ERROR] Couldn't get player UUID with this nickname: " + username + " - "+  ex.ToString());
                await DebugConsole($"Nepodařilo se získat UUID minecraft hráče s jeho přezdívkou: {username} - {ex.Message}", DebugLevel.Error);
                return null;
            }
        }

        //
        // Souhrn: 
        //      Vrátí zprávu podle customId, které je v embeds jako author
        public static async Task<List<DiscordMessageBuilder>> GetMessagesWithCustomId(string customId)
        {
            var messages = await Program.GetChannel(ChannelNames.MessageList).GetMessagesAsync();
            var resultMessages = new List<DiscordMessageBuilder>();

            foreach (var message in messages)
            {
                var embed = message.Embeds.FirstOrDefault();
                if (embed != null && embed.Author != null && embed.Author.Name == customId)
                {
                    var newEmbed = new DiscordEmbedBuilder(embed)
                    {
                        Author = null
                    }.Build();

                    var newMessage = new DiscordMessageBuilder()
                        .WithContent(message.Content)
                        .AddEmbed(newEmbed);

                    resultMessages.Add(newMessage);
                }
            }

            return resultMessages;

        }

        //
        // Souhrn: 
        //      Pošle záznam do DebugLog kanálu na discordu
        public enum DebugLevel
        {
            Info,
            Warning,
            Error
        }
        public static async Task DebugConsole(string text, DebugLevel debugType = DebugLevel.Info, [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                string prefix = "  ";
                if (debugType == DebugLevel.Info) { prefix = "  "; }
                else if (debugType == DebugLevel.Warning) { prefix = "! "; }
                else if (debugType == DebugLevel.Error) { prefix = "- "; }

                string currentTime = DateTime.Now.ToString("dddd, dd MMMM yyyy HH:mm:ss");
                string formattedText = $"{prefix}[{currentTime}] ({lineNumber}) [{debugType.ToString()}] - {text}";

                DiscordMessage lastMessage = await GetLastBotMessageAsync(Program.GetChannel(Database.ChannelNames.DebugConsole));

                if (lastMessage != null && ((lastMessage.Content.Length + formattedText.Length) < 2000) && 
                    lastMessage.Embeds.Count == 0)
                {
                    string fixedLastMessage = lastMessage.Content.Trim('`').Replace("diff", "");
                    string newMessage = $"```diff{Environment.NewLine}{fixedLastMessage}{Environment.NewLine}{formattedText}```";
                    await lastMessage.ModifyAsync(newMessage);
                }
                else
                {
                    string newMessage = $"```diff{Environment.NewLine}{formattedText}```";
                    await Program.GetChannel(Database.ChannelNames.DebugConsole).SendMessageAsync(newMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Nepodařilo se odeslat zprávu do debug konzole - {ex.Message}");
            }
        }
        #endregion
    }
}
