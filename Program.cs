using Aeternum.Commands.Slash;
using Aeternum.config;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Google.Protobuf.WellKnownTypes;
using MySqlConnector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using static Aeternum.Database;

namespace Aeternum
{
    internal class Database
    {
        public enum Channels
        {
            UserLogging,
            MessageLogging,
            Whitelist,
            WhitelistArchive,
            Console,
            Changelog,
            ToDo,
            ServerImages
        }
        public enum Roles
        {
            Admin,
            Whitelisted,
            NonWhitelisted
        }
        public enum Ints
        {
            WhitelistTotal,
            WhitelistSuccess,
            WhitelistFail
        }
        public enum Booleans
        {
            AutomateWhitelist
        }
        public enum Other
        {
            WhitelistThumbnailType
        }

        public class db_channel
        {
            public Channels type { get; set; }
            public string id { get; set; }

            public db_channel(Channels type, string id)
            {
                this.type = type;
                this.id = id;
            }
        }
        public class db_roles
        {
            public Roles type { get; set; }
            public string id { get; set; }

            public db_roles(Roles type, string id)
            {
                this.type = type;
                this.id = id;
            }
        }
        public class db_ints
        {
            public Ints type { get; set; }
            public int value { get; set; }
            public bool set { get; set; }

            public db_ints(Ints type, int value = 1, bool set = false)
            {
                this.type = type;
                this.value = value;
                this.set = set;
            }
        }
        public class db_booleans
        {
            public Booleans type { get; set; }
            public string value { get; set; }

            public db_booleans(Booleans type, bool value)
            {
                this.type = type;
                this.value = value.ToString();
            }
        }
        public class db_other
        {
            public Other type { get; set; }
            public string value { get; set; }

            public db_other(Other type, string value)
            {
                this.type = type;
                this.value = value;
            }
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
            return await Task.FromResult(connection);
        }

        public static async Task Disconnect()
        {
            connection.Close();
            Console.WriteLine("Odpojeno od databáze...");
            await Task.CompletedTask;
        }

    }

    internal class Program
    {
        // First
        public static DiscordClient client { get; private set; }
        public static CommandsNextExtension commands { get; private set; }
        public static DiscordGuild Server { get; private set; }

        // Roles
        public static DiscordRole AdminRole;
        public static DiscordRole WhitelistedRole;
        public static DiscordRole NonWhitelistedRole;

        // Channels
        public static DiscordChannel UserLoggingChannel;
        public static DiscordChannel MessageLoggingChannel;
        public static DiscordChannel WhitelistChannel;
        public static DiscordChannel WhitelistArchiveChannel;
        public static DiscordChannel ConsoleChannel;
        public static DiscordChannel ChangelogChannel;
        public static DiscordChannel ToDoChannel;
        public static DiscordChannel ServerImagesChannel;
        // Emojis
        public static DiscordEmoji ApproveEmoji {  get; private set; }
        public static DiscordEmoji DisApproveEmoji { get; private set; }
        public static DiscordEmoji Btn_ApproveEmoji { get; private set; }
        public static DiscordEmoji Btn_DisApproveEmoji { get; private set; }

        // Ints
        public static int WhitelistTotalCount;
        public static int WhitelistSuccessCount;
        public static int WhitelistFailCount;

        // Booleans
        public static bool AutomateWhitelist;

        // Other
        public static string WhitelistThumbnailType;
        private static List<string> ToDoList = new List<string>();

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
            client = new DiscordClient(discordConfig);
            var slashCommandsConfig = client.UseSlashCommands();
            var activity = new DiscordActivity()
            {
                Name = "Tebe",
                ActivityType = ActivityType.Watching

            };
            client.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromMinutes(2)
            });
            Server = await client.GetGuildAsync(1193271233781956729);


            client.GuildDownloadCompleted += Client_Ready;
            client.GuildMemberAdded += EmbedMemberAdd;
            client.GuildMemberRemoved += EmbedMemberRemove;
            client.MessageDeleted += OnMessageDelete;
            client.MessageUpdated += OnMessageEdit;
            client.MessageCreated += OnMessageCreate;
            client.ComponentInteractionCreated += OnButtonClick;
            client.ModalSubmitted += OnModalSubmit;
            client.MessageReactionAdded += OnReactionAdd;
            client.UnknownEvent += MuteEvent;

            slashCommandsConfig.RegisterCommands<BasicSL>();


            await client.ConnectAsync(activity, UserStatus.Online);
            await Task.Delay(-1);
        }


        #region Eventy
        //-------------------------------------------------------------------
        //                   Event: Připraven Bot
        //-------------------------------------------------------------------
        private static async Task Client_Ready(DiscordClient sender, GuildDownloadCompletedEventArgs args)
        {
            #region Safe Initialize
            ApproveEmoji = GetEmojiFromName(":white_check_mark:").Result;
            DisApproveEmoji = GetEmojiFromName(":x:").Result;
            Btn_ApproveEmoji = GetEmojiFromName(":heavy_check_mark:").Result;
            Btn_DisApproveEmoji = GetEmojiFromName(":heavy_multiplication_x:").Result;
            await DatabaseStartInitialize();
            await UpdateToDoDictionary();
            #endregion

            // Timer for periodic update Time on whitelist
            Timer timer = new Timer(TimeSpan.FromMinutes(1).TotalMilliseconds);
            timer.AutoReset = true;
            timer.Elapsed += new ElapsedEventHandler(VoidUpdater);
            timer.Start();


            await Task.CompletedTask;
        }
        private static async Task MuteEvent(DiscordClient sender, UnknownEventArgs args)
        {
            await Task.CompletedTask;
            return;
        }

        //-------------------------------------------------------------------
        //                   Event: Tlačítka
        //-------------------------------------------------------------------
        private static async Task OnButtonClick(DiscordClient sender, ComponentInteractionCreateEventArgs args)
        {
            DiscordMember member = GetMemberFromUser(args.User).Result;
            bool IsAdmin = member.Roles.Contains(AdminRole);
            //bool IsWhitelisted = member.Roles.Contains(WhitelistedRole);
            bool IsNonWhitelisted = member.Roles.Contains(NonWhitelistedRole);
            

            if (args.Id == "btn_close_id" && IsAdmin)
            {
                await WhitelistArchive(args);
                await Task.CompletedTask;
            }
            else if (args.Id == "btn_approve_id" && IsAdmin)
            {
                await WhitelistSuccess(args);
                await Task.CompletedTask;
            }
            else if (args.Id == "btn_create_whitelist" && IsNonWhitelisted)
            {
                await WhitelistModal(args);
                await Task.CompletedTask;
            }
            else
            {
                await Task.CompletedTask; 
            }


        }

        //-------------------------------------------------------------------
        //                   Event: Zprávy / Reakce
        //-------------------------------------------------------------------
        private static async Task OnMessageCreate(DiscordClient sender, MessageCreateEventArgs args)
        {
            // To Do kanál
            if (args.Channel == ToDoChannel)
            {
                var ToDoMessage = ToDoChannel.GetMessagesAsync(10).Result.Last();
                string prefix = args.Message.Content.Split(' ')[0];

                if (prefix == "+")
                {
                    ToDoList.Add(args.Message.Content.Substring(2));
                }
                else if (prefix == "-" && args.Message.Content.Split(' ')[1].All(char.IsDigit))
                {
                    int index = Convert.ToInt16(args.Message.Content.Split(' ')[1]);
                    ToDoList.RemoveAt(index - 1);
                }
                else { await Task.CompletedTask; return; }

                // Update
                var msg = new DiscordEmbedBuilder().WithTitle("To-Do Seznam").WithColor(DiscordColor.Aquamarine);
                for (int i = 0; i < ToDoList.Count; i++)
                {
                    msg.AddField($"{i + 1}:", $"`{ToDoList[i]}`");
                }

                try
                {
                    var messages = ToDoChannel.GetMessagesAsync(10).Result;
                    foreach (var message in messages)
                    {
                        message.DeleteAsync().Wait();
                    }
                    await ToDoChannel.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(msg));
                }
                catch { Console.WriteLine("Couldn't edit ToDo message"); }

                await Task.CompletedTask;
                return;
            }

            // Obrázky Serveru kanál
            if (args.Channel == ServerImagesChannel && args.Message.Attachments.Count == 0)
            {
                await SendDMMessage(args.Author, Messages.Default.warning_ServerImages);
                await SendDMMessage(args.Author, new DiscordMessageBuilder().WithEmbed(new DiscordEmbedBuilder()
                    .WithTitle("Smazána zpráva")
                    .WithColor(DiscordColor.Red)
                    .WithDescription(args.Message.Content)));
                await args.Message.DeleteAsync();
            }

            await Task.CompletedTask;
            return;
        }
        private static async Task OnMessageEdit(DiscordClient sender, MessageUpdateEventArgs args)
        {
            if (MessageLoggingChannel == null || args.Message.Author.IsBot == true || (args.MessageBefore.Embeds.Count == 0 && args.Message.Embeds.Count != 0))
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
                embedMessage.Author = new DiscordEmbedBuilder.EmbedAuthor { Name = "Upravená zpráva"};
                embedMessage.AddField("Před úpravou", $"`{args.MessageBefore.Content}`");
                embedMessage.AddField("Po úpravě", $"`{args.Message.Content}`");
                embedMessage.AddField("Vytvořena", args.Message.CreationTimestamp.UtcDateTime.ToString(), true);
                embedMessage.AddField("Upravená", DateTime.UtcNow.ToString(), true);

            await MessageLoggingChannel.SendMessageAsync(embed: embedMessage);
            await Task.CompletedTask;
            return;
        }
        private static async Task OnMessageDelete(DiscordClient sender, MessageDeleteEventArgs args)
        {
            if (args.Message.Channel == ToDoChannel || args.Message.Channel == ConsoleChannel || args.Message.Author.IsBot == true || (args.Message.MessageType != MessageType.Default && args.Message.MessageType != MessageType.Reply))
            {
                await Task.CompletedTask;
                return;
            }

            Dictionary<string, Stream> listOfFiles = new Dictionary<string, Stream>();

            var embedMessage = new DiscordEmbedBuilder
            {
                Color = DiscordColor.IndianRed,
                Description = $"`{args.Message.Content}`",
            };
                embedMessage.Author = new DiscordEmbedBuilder.EmbedAuthor { Name = "Smazána Zpráva"};
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


            await MessageLoggingChannel.SendMessageAsync(attachmentMessage);
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
            if (args.Channel.Id == WhitelistChannel.Id)
            {
                DiscordMember[] members = GetMembersByRole(NonWhitelistedRole).Result;
                foreach (var member in members.Where(member => member.Id == args.User.Id))
                {
                    await args.Message.DeleteReactionAsync(args.Emoji, args.User);
                    await SendDMMessage(args.User, Messages.Default.warning_Reaction);
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
            await args.Member.GrantRoleAsync(NonWhitelistedRole, "Připojení");
            if (UserLoggingChannel == null)
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

            await UserLoggingChannel.SendMessageAsync(embed: embedMessage);
            await SendDMMessage(args.Member, Messages.Default.member_Join);
            await Task.CompletedTask;
        }
        private static async Task EmbedMemberRemove(DiscordClient sender, GuildMemberRemoveEventArgs args)
        {
            if (UserLoggingChannel == null)
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

            await UserLoggingChannel.SendMessageAsync(embed: embedMessage);
            await Task.CompletedTask;
        }

        //-------------------------------------------------------------------
        //                   Event: Modal
        //-------------------------------------------------------------------
        private static async Task OnModalSubmit(DiscordClient sender, ModalSubmitEventArgs args)
        {
            if (args.Interaction.Type == InteractionType.ModalSubmit)
            {
                if (args.Interaction.Data.CustomId == "modal_whitelist")
                {
                    var messages = await WhitelistChannel.GetMessagesAsync();
                    foreach (var msg in messages) // Pokud už jednu přihlašká má tak zamítnout submit
                    {
                        if (msg.MentionedUsers.Count > 0)
                        {
                            if (msg.MentionedUsers[0].Id == args.Interaction.User.Id)
                            {
                                await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Již jednu přihlášku zde máš, počkej až skončí a pak si můžeš podat další " + msg.JumpLink).AsEphemeral(true));
                                await Task.CompletedTask;
                                return;
                            }
                        }
                    }
                    await CreateWhitelist(args);
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
        #endregion

        #region Funkce
        //-------------------------------------------------------------------
        //                   Funkce: Changelog
        //-------------------------------------------------------------------
        private static async Task CreateChangelog(ModalSubmitEventArgs args)
        {
            DiscordMember member = GetMemberFromUser(args.Interaction.User).Result;
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
            await ChangelogChannel.SendMessageAsync(msg);
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
            await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource ,new DiscordInteractionResponseBuilder().WithContent("Úspěšně jsi vytvořil oznámení " + sentMsg.JumpLink));
            await Task.CompletedTask;
        }

        //-------------------------------------------------------------------
        //                   Funkce: Přihlášky
        //-------------------------------------------------------------------
        // Manual Mode
        private static async Task WhitelistSuccess(ComponentInteractionCreateEventArgs args)
        {
            await SendMinecraftCommand($"whitelist add {args.Message.Embeds[0].Fields[0].Value}");

            var member = GetMemberFromUser(args.Message.MentionedUsers[0]).Result;
            await member.RevokeRoleAsync(NonWhitelistedRole, "Zvládnul whitelist");
            await member.GrantRoleAsync(WhitelistedRole, "Zvládnul whitelist");
            var DMChannel = await member.CreateDmChannelAsync();

            await DMChannel.SendMessageAsync(Messages.Default.whitelist_Success);
            await UpdateDatabaseInts(new db_ints(Ints.WhitelistSuccess, 1, false));
            await WhitelistArchive(args, true);
            await Task.CompletedTask;
        }
        private static async Task WhitelistArchive(ComponentInteractionCreateEventArgs args, bool sucess = false)
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

            var embedmsg = args.Message.Embeds[0];
            var embedMessage = new DiscordEmbedBuilder
            {
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = embedmsg.Author.Name,
                    IconUrl = embedmsg.Author.IconUrl.ToString()
                },
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                {
                    Url = embedmsg.Thumbnail.Url.ToString()
                }
            };
            embedMessage.WithTitle($"Přihláška #{embedmsg.Title.Split('#')[1]}");
            foreach (var field in embedmsg.Fields)
            {
                embedMessage.AddField(field.Name, field.Value.ToString(), field.Inline);
            }
            if (sucess) { embedMessage.Color = DiscordColor.Green; }
            else { embedMessage.Color = DiscordColor.Red; await UpdateDatabaseInts(new db_ints(Ints.WhitelistFail, 1, false)); }

            embedMessage.AddField($"{ApproveEmoji} ({yesUsers.Count - 1})", yesUsersString, false);
            embedMessage.AddField($"{DisApproveEmoji} ({noUsers.Count - 1})", noUsersString, false);
            embedMessage.AddField($"Vytvořená:", args.Message.CreationTimestamp.UtcDateTime.ToString(), true);
            embedMessage.AddField($"Uzavřená:", DateTime.UtcNow.ToString(), true);
            embedMessage.Footer = new DiscordEmbedBuilder.EmbedFooter()
            {
                IconUrl = args.Interaction.User.AvatarUrl,
                Text = "Zkontroloval: " + args.Interaction.User.Username
            };
            await WhitelistArchiveChannel.SendMessageAsync(embedMessage);
            await args.Message.DeleteAsync();
            await Task.CompletedTask;
        }
        // Automatic Mode
        private static async Task WhitelistSuccessAuto(ComponentCrtEventArgs args)
        {
            await WhitelistArchiveAuto(args, true);
            await ConsoleChannel.SendMessageAsync($"whitelist add {args.Message.Embeds[0].Fields[0].Value}");

            DiscordMember member = GetMemberFromUser(args.Message.MentionedUsers[0]).Result;
            await member.RevokeRoleAsync(NonWhitelistedRole, "Zvládnul whitelist");
            await member.GrantRoleAsync(WhitelistedRole, "Zvládnul whitelist");
            var DMChannel = await member.CreateDmChannelAsync();

            await DMChannel.SendMessageAsync(Messages.Default.whitelist_Success);
            await UpdateDatabaseInts(new db_ints(Ints.WhitelistSuccess, 1, false));
            await Task.CompletedTask;
        }
        private static async Task WhitelistArchiveAuto(ComponentCrtEventArgs args, bool sucess = false)
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

            var embedmsg = args.Message.Embeds[0];
            var embedMessage = new DiscordEmbedBuilder
            {
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = embedmsg.Author.Name,
                    IconUrl = embedmsg.Author.IconUrl.ToString()
                },
                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                {
                    Url = embedmsg.Thumbnail.Url.ToString()
                }
            };
            foreach (var field in embedmsg.Fields)
            {
                embedMessage.AddField(field.Name, field.Value.ToString(), field.Inline);
            }
            if (sucess) { embedMessage.Color = DiscordColor.Green; }
            else { embedMessage.Color = DiscordColor.Red; await UpdateDatabaseInts(new db_ints(Ints.WhitelistFail, 1, false)); }

            embedMessage.AddField($"{ApproveEmoji} ({yesUsers.Count - 1})", yesUsersString, false);
            embedMessage.AddField($"{DisApproveEmoji} ({noUsers.Count - 1})", noUsersString, false);
            embedMessage.AddField($"Vytvořená:", args.Message.CreationTimestamp.UtcDateTime.ToString(), true);
            embedMessage.AddField($"Uzavřená:", DateTime.UtcNow.ToString(), true);
            embedMessage.Footer = new DiscordEmbedBuilder.EmbedFooter()
            {
                Text = "Zkontrolováno Automaticky"
            };
            await WhitelistArchiveChannel.SendMessageAsync(embedMessage);
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
            modal.AddComponents(new TextInputComponent("Jak ses o nás dozvěděl/a?", "knowDescID", "Např. Minecraft List, Kamarád, atd..."));
            modal.AddComponents(new TextInputComponent("Co od serveru očekáváš?", "expectationDescID"));
            modal.AddComponents(new TextInputComponent("Něco o sobě", "infoDescID", "Zde se rozepiš", null, true, TextInputStyle.Paragraph));

            await args.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);
            await Task.CompletedTask;
        }
        private static async Task CreateWhitelist(ModalSubmitEventArgs args)
        {
            // Buttons
            var approveButton = CreateButtonComponent(ButtonStyle.Success, "btn_approve_id", "Prošel", false, Btn_ApproveEmoji).Result;
            var closeButton = CreateButtonComponent(ButtonStyle.Danger, "btn_close_id", "Neprošel", false, Btn_DisApproveEmoji).Result;

            // Messages
            var msg = new DiscordMessageBuilder()
                    .WithContent(args.Interaction.User.Mention)
                    .WithAllowedMentions(new IMention[] { new UserMention(args.Interaction.User) })
                    .AddComponents(approveButton, closeButton);
            var embedMessage = new DiscordEmbedBuilder
            {
                Color = new DiscordColor("89CFF0"),
                Title = $"Přihláška #{WhitelistTotalCount + 1}",
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
            embedMessage.AddField("Něco o sobě", args.Values["infoDescID"], false);
            embedMessage.WithFooter("Zbývá: 2 dny 0 hodin 0 minut");
            embedMessage.WithThumbnail("https://mc-heads.net/" + WhitelistThumbnailType + "/" + args.Values["nicknameLabelID"]);
            msg.AddEmbed(embedMessage);

            // Exit
            var sentMSG = await WhitelistChannel.SendMessageAsync(msg);
            await sentMSG.CreateReactionAsync(ApproveEmoji);
            await sentMSG.CreateReactionAsync(DisApproveEmoji);
            await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Přihlášku jsi vytvořil, za celý projekt ti přejeme hodně štěstí s úspěchem! ^-^").AsEphemeral(true));
            await UpdateDatabaseInts(new db_ints(Ints.WhitelistTotal, 1, false));
            await Task.CompletedTask;
        }
        public static async void VoidUpdater(object sender, ElapsedEventArgs e)
        {
            if (WhitelistChannel.GetMessagesAsync(20).Result.Count == 1) { await Task.CompletedTask; return; }
            await UpdateWhitelistTime(sender, e);
        }
        public static async Task UpdateWhitelistTime(object sender, ElapsedEventArgs e)
        {
            var messages = WhitelistChannel.GetMessagesAsync().Result.Where(x => x.Embeds[0].Author != null).ToList();
            if (messages.Count == 0) return;
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
                    if (AutomateWhitelist == true)
                    {
                        var members = await GetMembersByRole(AdminRole);
                        int yesCount = message.GetReactionsAsync(DiscordEmoji.FromName(client, ":white_check_mark:", false), 30).Result.Count;
                        int noCount = message.GetReactionsAsync(DiscordEmoji.FromName(client, ":x:", false), 30).Result.Count;
                        if (yesCount > noCount)
                        {
                            await WhitelistSuccessAuto(new ComponentCrtEventArgs(message));
                        }
                        else if (yesCount == noCount)
                        {
                            var _msg = Messages.Default.whitelist_Pending;
                            var _embed = new DiscordEmbedBuilder()
                            {
                                Color = DiscordColor.White,
                                Title = "Upozornění!",
                                Description = "Někdo čeká na zkontrolování a ověření žádosti",
                                Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                                {
                                    Url = "https://4vector.com/i/free-vector-admin-tools-icon_098300_Admin_tools_icon.png",
                                },
                                Footer = new DiscordEmbedBuilder.EmbedFooter()
                                {
                                    Text = "Aeterum Team",
                                    IconUrl = Program.Server.IconUrl,
                                }
                            };
                            _embed.AddField("Zpráva", message.JumpLink.ToString(), false);
                            _msg.AddEmbed(_embed);
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
                        var _embed = new DiscordEmbedBuilder()
                        {
                            Color = DiscordColor.White,
                            Title = "Upozornění!",
                            Description = "Někdo čeká na zkontrolování a ověření žádosti, utíkej mu kliknout na tlačítko",
                            Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                            {
                                Url = "https://4vector.com/i/free-vector-admin-tools-icon_098300_Admin_tools_icon.png",
                            },
                            Footer = new DiscordEmbedBuilder.EmbedFooter()
                            {
                                Text = "Aeterum Team",
                                IconUrl = Program.Server.IconUrl,
                            }
                        };
                        _embed.AddField("Zpráva", message.JumpLink.ToString(), false);
                        _msg.AddEmbed(_embed);
                        _msg.WithContent(message.Content);

                        var members = await GetMembersByRole(AdminRole);
                        foreach (var member in members)
                        {
                            var DMChannel = await member.CreateDmChannelAsync();
                            try
                            {
                                if (DMChannel.GetMessagesAsync(5).Result.First().Content != message.Content)
                                {
                                    await DMChannel.SendMessageAsync(_msg);
                                    Console.WriteLine($"[INFO] - {member.Username} byla poslána zpráva whitelist_pending");
                                }
                            }
                            catch (Exception ex) { Console.WriteLine(ex); }
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

        //-------------------------------------------------------------------
        //                   Funkce: Util
        //-------------------------------------------------------------------
        public static async Task DatabaseStartInitialize()
        {
            StringBuilder infoSB = new StringBuilder();
            try
            {
                var conn = Database.Connect().Result;

                var channelCommand = new MySqlCommand("SELECT * FROM Channels", conn);
                var rolesCommand = new MySqlCommand("SELECT * FROM Roles", conn);
                var intsCommand = new MySqlCommand("SELECT * FROM Ints", conn);
                var booleansCommand = new MySqlCommand("SELECT * FROM Booleans", conn);
                var otherCommand = new MySqlCommand("SELECT * FROM Other", conn);

                infoSB.AppendLine();
                infoSB.AppendLine("------------------- Channels -------------------");
                using (MySqlDataReader channel_reader = channelCommand.ExecuteReader())
                {
                    while (channel_reader.Read())
                    {
                        try
                        {
                            UserLoggingChannel = GetChannelFromID(channel_reader["UserLogging"].ToString()).Result;
                            infoSB.AppendLine($"UserLoggingChannel - {UserLoggingChannel.Name} ({UserLoggingChannel.Id})");
                        } catch { infoSB.AppendLine("UserLoggingChannel - couldnt be set"); }
                        try
                        {
                            MessageLoggingChannel = GetChannelFromID(channel_reader["MessageLogging"].ToString()).Result;
                            infoSB.AppendLine($"MessageLoggingChannel - {MessageLoggingChannel.Name} ({MessageLoggingChannel.Id})");
                        } catch { infoSB.AppendLine("MessageLoggingChannel - couldnt be set"); }
                        try
                        {
                            WhitelistChannel = GetChannelFromID(channel_reader["Whitelist"].ToString()).Result;
                            infoSB.AppendLine($"WhitelistChannel - {WhitelistChannel.Name} ({WhitelistChannel.Id})");
                        } catch { infoSB.AppendLine("WhitelistChannel - couldnt be set"); }
                        try
                        {
                            WhitelistArchiveChannel = GetChannelFromID(channel_reader["WhitelistArchive"].ToString()).Result;
                            infoSB.AppendLine($"WhitelistArchiveChannel - {WhitelistArchiveChannel.Name} ({WhitelistArchiveChannel.Id})");
                        } catch { infoSB.AppendLine("WhitelistArchiveChannel - couldnt be set"); }
                        try
                        {
                            ConsoleChannel = GetChannelFromID(channel_reader["Console"].ToString()).Result;
                            infoSB.AppendLine($"ConsoleChannel - {ConsoleChannel.Name} ({ConsoleChannel.Id})");
                        } catch { infoSB.AppendLine("ConsoleChannel - couldnt be set"); }
                        try
                        {
                            ChangelogChannel = GetChannelFromID(channel_reader["Changelog"].ToString()).Result;
                            infoSB.AppendLine($"ChangelogChannel - {ChangelogChannel.Name} ({ChangelogChannel.Id})");
                        } catch { infoSB.AppendLine("ChangelogChannel - couldnt be set"); }
                        try
                        {
                            ToDoChannel = GetChannelFromID(channel_reader["ToDo"].ToString()).Result;
                            infoSB.AppendLine($"ToDoChannel - {ToDoChannel.Name} ({ToDoChannel.Id})");
                        } catch { infoSB.AppendLine("ToDoChannel - couldnt be set"); }
                        try
                        {
                            ServerImagesChannel = GetChannelFromID(channel_reader["ServerImages"].ToString()).Result;
                            infoSB.AppendLine($"ServerImagesChannel - {ServerImagesChannel.Name} ({ServerImagesChannel.Id})");
                        } catch { infoSB.AppendLine("ServerImagesChannel - couldnt be set"); }
                    }
                    channel_reader.Close();
                }

                infoSB.AppendLine("------------------- Roles -------------------");
                using (MySqlDataReader roles_reader = rolesCommand.ExecuteReader())
                {
                    while (roles_reader.Read())
                    {
                        try
                        {
                            AdminRole = GetRoleFromID(roles_reader["Admin"].ToString()).Result;
                            infoSB.AppendLine($"AdminRole - {AdminRole.Name} ({AdminRole.Id})");
                        }
                        catch (Exception ex ) { infoSB.AppendLine($"AdminRole - couldnt be set"); Console.WriteLine(ex.Message); }
                        try
                        {
                            WhitelistedRole = GetRoleFromID(roles_reader["Whitelisted"].ToString()).Result;
                            infoSB.AppendLine($"WhitelistedRole - {WhitelistedRole.Name} ({WhitelistedRole.Id})");
                        }
                        catch { infoSB.AppendLine($"WhitelistedRole - couldnt be set"); }
                        try
                        {
                            NonWhitelistedRole = GetRoleFromID(roles_reader["NonWhitelisted"].ToString()).Result;
                            infoSB.AppendLine($"NonWhitelistedRole - {NonWhitelistedRole.Name} ({NonWhitelistedRole.Id})");
                        }
                        catch { infoSB.AppendLine($"NonWhitelistedRole - couldnt be set"); }
                    }
                    roles_reader.Close();
                }

                infoSB.AppendLine("------------------- Ints -------------------");
                using (MySqlDataReader ints_reader = intsCommand.ExecuteReader())
                {
                    while (ints_reader.Read())
                    {
                        try
                        {
                            WhitelistTotalCount = Int32.Parse(ints_reader["WhitelistTotal"].ToString());
                            infoSB.AppendLine($"WhitelistTotalCount - {WhitelistTotalCount}");
                        }
                        catch { infoSB.AppendLine($"WhitelistTotalCount - couldnt be set"); }
                        try
                        {
                            WhitelistSuccessCount = Int32.Parse(ints_reader["WhitelistSuccess"].ToString());
                            infoSB.AppendLine($"WhitelistSuccessCount - {WhitelistSuccessCount}");
                        }
                        catch { infoSB.AppendLine($"WhitelistSuccessCount - couldnt be set"); }
                        try
                        {
                            WhitelistFailCount = Int32.Parse(ints_reader["WhitelistFail"].ToString());
                            infoSB.AppendLine($"WhitelistFailCount - {WhitelistFailCount}");
                        }
                        catch { infoSB.AppendLine($"WhitelistFailCount - couldnt be set"); }

                    }
                    ints_reader.Close();
                }

                infoSB.AppendLine("------------------- Booleans -------------------");
                using (MySqlDataReader booleans_reader = booleansCommand.ExecuteReader())
                {
                    while (booleans_reader.Read())
                    {
                        try
                        {
                            AutomateWhitelist = bool.Parse(booleans_reader["AutomateWhitelist"].ToString());
                            infoSB.AppendLine($"AutomateWhitelist - {AutomateWhitelist}");
                        }
                        catch (Exception ex) { infoSB.AppendLine($"AutomateWhitelist - couldnt be set"); Console.WriteLine(ex.Message); }
                    }
                    booleans_reader.Close();
                }

                infoSB.AppendLine("------------------- Others -------------------");
                using (MySqlDataReader other_reader = otherCommand.ExecuteReader())
                {
                    while (other_reader.Read())
                    {
                        try
                        {
                            WhitelistThumbnailType = other_reader["WhitelistThumbnailType"].ToString();
                            infoSB.AppendLine($"WhitelistThumbnailType - {WhitelistThumbnailType}");
                        }
                        catch (Exception ex) { infoSB.AppendLine($"WhitelistThumbnailType - couldnt be set"); Console.WriteLine(ex.Message); }
                    }
                    other_reader.Close();
                }
                infoSB.AppendLine();
            }
            catch ( Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally 
            {
                await Database.Disconnect();
                Console.WriteLine(infoSB.ToString());

            }
            await Task.CompletedTask;
        }
        public static async Task UpdateToDoDictionary()
        {
            DiscordMessage ToDoMessage = null;

            try
            {
                ToDoMessage = ToDoChannel.GetMessagesAsync(10).Result.Where(x => x.Author.IsBot == true).ToList()[0];
            }
            catch
            {
                Console.WriteLine("Couldn't get to-do message creating one");
            }

            if (ToDoMessage == null)
            {
                Console.WriteLine("Is nul;l");
                var msg = new DiscordEmbedBuilder().WithTitle("To-Do Seznam").WithColor(DiscordColor.Aquamarine);
                for (int i = 0; i < ToDoList.Count; i++)
                {
                    msg.AddField($"{i + 1}:", $"`{ToDoList[i]}`");
                }

                ToDoMessage = await ToDoChannel.SendMessageAsync(msg);
                await Task.CompletedTask;
                return;
            }

            //// Pokud není to-do list, udělat nový
            //if (ToDoMessage == null)
            //{
            //    var msg = new DiscordEmbedBuilder().WithTitle("To-Do Seznam").WithColor(DiscordColor.Aquamarine);
            //    for (int i = 0; i < ToDoList.Count; i++)
            //    {
            //        msg.AddField($"{i + 1}:", $"`{ToDoList[i]}`");
            //    }

            //    await ToDoChannel.SendMessageAsync(msg);
            //}

            if (ToDoMessage.Embeds[0].Fields.Count == 0) { await Task.CompletedTask; return; }
            ToDoList.Clear();
            for (int i = 0; i < ToDoMessage.Embeds[0].Fields.Count; i++)
            {
                ToDoList.Add(ToDoMessage.Embeds[0].Fields[i].Value);
            }

            await Task.CompletedTask;
            return;
        }

        // Components
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
                return null;
            }
        }

        // Emoji
        public static async Task<DiscordEmoji> GetEmojiFromName(string emojiName)
        {
            try
            {
                var emoji = DiscordEmoji.FromName(client, emojiName);
                return await Task.FromResult(emoji);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"({ex.Source}) - {ex.Message}");
                return null;
            }
        }

        // Channels
        public static async Task<DiscordChannel> GetChannelFromID(string channelID)
        {
            try
            {
                DiscordChannel channel = client.GetChannelAsync(Convert.ToUInt64(channelID)).Result;
                return await Task.FromResult(channel);
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"({channelID}) - {ex.Message}");
                return null;
            }
        }

        // Roles
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
                return null;
            }
        }
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
                return null;
            }
        }
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
                return null;
            }
        }

        // Minecraft Command
        public static async Task SendMinecraftCommand(string command)
        {
            try
            {
                await ConsoleChannel.SendMessageAsync(command);
            }
            catch
            {
                Console.WriteLine($"Could not send /{command}");
            }
        }

        // Send DM Message
        public static async Task SendDMMessage(DiscordMember member, DiscordMessageBuilder message)
        {
            try
            {
                var DMChannel = await member.CreateDmChannelAsync();
                await DMChannel.SendMessageAsync(message);

                await Task.CompletedTask;
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"({ex.Source}) - {ex.Message}");
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

                await Task.CompletedTask;
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"({ex.Source}) - {ex.Message}");
                return;
            }
        }

        // Dabatase Management
        public static async Task UpdateDatabaseChannels(params db_channel[] CurrentChannels)
        {
            MySqlConnection conn = Database.Connect().Result;
            foreach (var current in CurrentChannels)
            {
                try
                {
                    var cmd = new MySqlCommand($"UPDATE Channels SET {current.type} = {current.id}", conn);
                    cmd.ExecuteNonQuery();
                    Console.WriteLine($"Updated channel {current.type} with ID: {current.id}");
                }
                catch
                {
                    Console.WriteLine($"Couldnt update {current.type} channels in database with value: {current.id}");
                }
            }
            Database.Disconnect().Wait();
            await Task.CompletedTask;
        }
        public static async Task UpdateDatabaseInts(params db_ints[] currentInts)
        {
            MySqlConnection conn = Database.Connect().Result;
            foreach (var current in currentInts)
            {
                try
                {
                    if (current.type == Ints.WhitelistTotal)
                    {
                        if (current.set) { WhitelistTotalCount = current.value; }
                        else { WhitelistTotalCount += current.value; }
                        var cmd = new MySqlCommand($"UPDATE Ints SET WhitelistTotal = {WhitelistTotalCount}", conn);
                        cmd.ExecuteNonQuery();
                        Console.WriteLine($"Updated Int {current.type} is now: {WhitelistTotalCount}");
                    }
                    else if (current.type == Ints.WhitelistSuccess)
                    {
                        if (current.set) { WhitelistSuccessCount = current.value; }
                        else { WhitelistSuccessCount += current.value; }
                        var cmd = new MySqlCommand($"UPDATE Ints SET WhitelistSuccess = {WhitelistSuccessCount}", conn);
                        cmd.ExecuteNonQuery();
                        Console.WriteLine($"Updated Int {current.type} is now: {WhitelistSuccessCount}");
                    }
                    else if (current.type == Ints.WhitelistFail)
                    {
                        if (current.set) { WhitelistFailCount = current.value; }
                        else { WhitelistFailCount += current.value; }
                        var cmd = new MySqlCommand($"UPDATE Ints SET WhitelistFail = {WhitelistFailCount}", conn);
                        cmd.ExecuteNonQuery();
                        Console.WriteLine($"Updated Int {current.type} is now: {WhitelistFailCount}");
                    }
                }
                catch
                {
                    Console.WriteLine($"Couldnt Update Int {current.type} in Database with value: {current.value}");
                }
            }
            Database.Disconnect().Wait();
            await Task.CompletedTask;
        }
        public static async Task UpdateDatabaseRoles(params db_roles[] currentRoles)
        {
            MySqlConnection conn = Database.Connect().Result;
            foreach (var current in currentRoles)
            {
                try
                {
                    var cmd = new MySqlCommand($"UPDATE Roles SET {current.type} = {current.id}", conn);
                    cmd.ExecuteNonQuery();
                    Console.WriteLine($"Updated role {current.type} with ID: {current.id}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Couldnt update {current.type} role in database with value: {current.id}");
                }
            }
            Database.Disconnect().Wait();
            await Task.CompletedTask;
        }
        public static async Task UpdateDatabaseBooleans(params db_booleans[] currentBooleans)
        {
            MySqlConnection conn = Database.Connect().Result;
            foreach (var current in currentBooleans)
            {
                try
                {
                    var cmd = new MySqlCommand($"UPDATE Booleans SET {current.type} = '{current.value}'", conn);
                    cmd.ExecuteNonQuery();
                    Console.WriteLine($"Updated boolean {current.type} with value: {current.value}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Couldnt update {current.type} boolean in database with value: {current.value}");
                }
            }
            Database.Disconnect().Wait();
            await Task.CompletedTask;
        }
        public static async Task UpdateDatabaseOther(params db_other[] currentOther)
        {
            MySqlConnection conn = Database.Connect().Result;
            foreach (var current in currentOther)
            {
                try
                {
                    var cmd = new MySqlCommand($"UPDATE Other SET {current.type} = '{current.value}'", conn);
                    cmd.ExecuteNonQuery();
                    Console.WriteLine($"Updated {current.type} with value: {current.value}");
                }
                catch
                {
                    Console.WriteLine($"[ERROR] Couldnt update {current.type} in database with value: {current.value}");
                }
            }
            Database.Disconnect().Wait();
            await Task.CompletedTask;
        }
        #endregion
    }
}
