using Aeternum.Commands.Slash;
using Aeternum.config;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Aeternum
{
    static class Program
    {
        // Main
        public static DiscordClient client { get; private set; }
        public static CommandsNextExtension commands { get; private set; }
        public static DiscordGuild Server { get; private set; }

        // Roles
        public static DiscordRole AdminRole { get; private set; }
        public static DiscordRole WhitelistedRole { get; private set; }
        public static DiscordRole WhitelistedPendingRole { get; private set; }

        // Channels
        public static DiscordChannel UserLoggingChannel { get; private set; }
        public static DiscordChannel MessageLoggingChannel { get; private set; }
        public static DiscordChannel WhitelistChannel { get; private set; }
        public static DiscordChannel WhitelistArchiveChannel { get; private set; }
        public static DiscordChannel ConsoleChannel { get; private set; }
        public static DiscordChannel ChangelogChannel { get; private set; }
        public static DiscordChannel ToDoChannel { get; private set; }

        // Emojis
        public static DiscordEmoji ApproveEmoji {  get; private set; }
        public static DiscordEmoji DisApproveEmoji { get; private set; }

        // Others
        private static List<string> ToDoList = new List<string>();

        //-------------------------------------------------------------------
        //                   Inicializace + Nastavování
        //-------------------------------------------------------------------
        static async Task Main(string[] args)
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


            client.Ready += Client_Ready;
            client.GuildMemberAdded += EmbedMemberAdd;
            client.GuildMemberRemoved += EmbedMemberRemove;
            client.MessageDeleted += OnMessageDelete;
            client.MessageUpdated += OnMessageEdit;
            client.MessageCreated += OnMessageCreate;
            client.ComponentInteractionCreated += OnButtonClick;
            client.ModalSubmitted += OnModalSubmit;
            client.MessageReactionAdded += OnReactionAdd;

            slashCommandsConfig.RegisterCommands<BasicSL>();


            await client.ConnectAsync(activity, UserStatus.Online);
            await Task.Delay(-1);
        }


        #region Eventy
        //-------------------------------------------------------------------
        //                   Event: Připraven Bot
        //-------------------------------------------------------------------
        private static async Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
        {
            #region Safe Initialize
            ApproveEmoji = GetEmojiFromName(":white_check_mark:").Result;
            DisApproveEmoji = GetEmojiFromName(":x:").Result;
            await UpdateInitialize();
            await UpdateToDoDictionary();
            #endregion

            // Timer for periodic update Time on whitelist
            Timer timer = new Timer(TimeSpan.FromSeconds(60).TotalMilliseconds);
            timer.AutoReset = true;
            timer.Elapsed += new ElapsedEventHandler(VoidUpdater);
            timer.Start();


            await Task.CompletedTask;
        }

        //-------------------------------------------------------------------
        //                   Event: Tlačítka
        //-------------------------------------------------------------------
        private static async Task OnButtonClick(DiscordClient sender, ComponentInteractionCreateEventArgs args)
        {
            DiscordMember[] members = GetMembersByRole(WhitelistedPendingRole).Result;
            var bufferArgs = args;

            if (bufferArgs.Id == "btn_close_id")
            {
                foreach (var member in members.Where(member => member.Roles.Contains(AdminRole)))
                {
                    if (member.Id == args.User.Id)
                    {
                        await WhitelistArchive(bufferArgs);
                        await Task.CompletedTask;
                    }
                }
            }
            else if (bufferArgs.Id == "btn_approve_id")
            {
                foreach (var member in members.Where(member => member.Roles.Contains(AdminRole)))
                {
                    if (member.Id == args.User.Id)
                    {
                        await WhitelistSuccess(bufferArgs);
                        await Task.CompletedTask;
                    }
                }
            }
            else if (bufferArgs.Id == "btn_create_whitelist")
            {
                foreach (var member in members)
                {
                    if (member.Id == bufferArgs.User.Id)
                    {
                        await WhitelistModal(bufferArgs);
                        await Task.CompletedTask;
                    }
                }
                await Task.CompletedTask;
            }

            await Task.CompletedTask;
            return;

        }

        //-------------------------------------------------------------------
        //                   Event: Zprávy / Reakce
        //-------------------------------------------------------------------
        private static async Task OnMessageCreate(DiscordClient sender, MessageCreateEventArgs args)
        {
            // To Do kanál
            if (args.Channel == GetChannelFromID(Properties.Settings.Default.channel_todo).Result)
            {
                var ToDoMessage = ToDoChannel.GetMessagesAsync().Result.Last();
                string prefix = args.Message.Content.Split(' ')[0];
                if (prefix == "+")
                {
                    ToDoList.Add(args.Message.Content.Substring(1));

                    // Update
                    await args.Message.DeleteAsync();
                    var msg = new DiscordEmbedBuilder().WithTitle("To-Do Seznam").WithColor(DiscordColor.Aquamarine);
                    for (int i = 0; i < ToDoList.Count; i++)
                    {
                        msg.AddField($"{i + 1}:", $"`{ToDoList[i]}`");
                    }

                    try
                    {
                        await ToDoMessage.ModifyAsync(new DiscordMessageBuilder().WithEmbed(msg));
                    }
                    catch { Console.WriteLine("Couldn't edit ToDo message"); }

                    await Task.CompletedTask;
                    return;
                }
                else if (prefix == "-" && args.Message.Content.Split(' ')[1].All(char.IsDigit))
                {
                    int index = Convert.ToInt16(args.Message.Content.Split(' ')[1]);
                    ToDoList.RemoveAt(index - 1);

                    // Update
                    await args.Message.DeleteAsync();
                    var msg = new DiscordEmbedBuilder().WithTitle("To-Do Seznam").WithColor(DiscordColor.Aquamarine);
                    for (int i = 0; i < ToDoList.Count; i++)
                    {
                        msg.AddField($"{i + 1}:", $"`{ToDoList[i]}`");
                    }

                    try
                    {
                        await ToDoMessage.ModifyAsync(new DiscordMessageBuilder().WithEmbed(msg));
                    }
                    catch { Console.WriteLine("Couldn't edit ToDo message"); }

                    await Task.CompletedTask;
                    return;
                }

                await args.Message.DeleteAsync();
                await Task.CompletedTask;
                return;

            }
            // Obrázky Serveru kanál
            if (args.Channel == GetChannelFromID(Properties.Settings.Default.channel_ServerImages).Result && args.Message.Attachments.Count == 0)
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
            if (Properties.Settings.Default.channel_MessageLogging == null || args.Message.Author.IsBot == true || (args.MessageBefore.Embeds.Count == 0 && args.Message.Embeds.Count != 0))
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
            if (MessageLoggingChannel == null || args.Message.Channel == ToDoChannel || args.Message.Channel == ConsoleChannel || args.Message.Author.IsBot == true || (args.Message.MessageType != MessageType.Default && args.Message.MessageType != MessageType.Reply))
            {
                await Task.CompletedTask;
                return;
            }

            Dictionary<string, Stream> listOfFiles = new Dictionary<string, Stream>();

            var embedMessage = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Rose,
                Description = $"`{args.Message.Content}`",
            };
                embedMessage.Author = new DiscordEmbedBuilder.EmbedAuthor { Name = "Smazána Zpráva"};
            if (args.Message.Author != null)
            {
                embedMessage.Footer = new DiscordEmbedBuilder.EmbedFooter { Text = args.Message.Author.Username, IconUrl = args.Message.Author.AvatarUrl };
            }
            embedMessage.AddField("Vytvořena", args.Message.CreationTimestamp.ToString(), true);
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
                await SendDMMessage(args.User, Messages.Default.warning_Reaction);
            }

            await Task.CompletedTask;
            return;
        }

        //-------------------------------------------------------------------
        //                   Event: Připojení/Odpojení
        //-------------------------------------------------------------------
        private static async Task EmbedMemberAdd(DiscordClient sender, GuildMemberAddEventArgs args)
        {
            await args.Member.GrantRoleAsync(WhitelistedPendingRole, "Připojení");
            if (Properties.Settings.Default.channel_UserLogging == null)
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

            await GetChannelFromID(Properties.Settings.Default.channel_UserLogging).Result.SendMessageAsync(embed: embedMessage);
            await Task.CompletedTask;
        }
        private static async Task EmbedMemberRemove(DiscordClient sender, GuildMemberRemoveEventArgs args)
        {
            if (Properties.Settings.Default.channel_UserLogging == null)
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

            await GetChannelFromID(Properties.Settings.Default.channel_UserLogging).Result.SendMessageAsync(embed: embedMessage);
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
                    var messages = await GetChannelFromID(Properties.Settings.Default.channel_Whitelist).Result.GetMessagesAsync();
                    foreach (var msg in messages)
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
            // Messages
            var embedMessage = new DiscordEmbedBuilder
            {
                Color = DiscordColor.Orange,
                Title = args.Values["titleID"],
                Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    Text = args.Interaction.User.Username,
                    IconUrl = args.Interaction.User.AvatarUrl,
                }
            };
            if (args.Values["beforeChangeID"] != "")
            {
                embedMessage.AddField("Před", $"`{args.Values["beforeChangeID"]}`", false);
            }
            embedMessage.AddField("Po", $"`{args.Values["afterChangeID"]}`", false);
            var msg = new DiscordMessageBuilder().WithEmbed(embedMessage);

            // Exit
            await GetChannelFromID(Properties.Settings.Default.channel_changelog).Result.SendMessageAsync(msg);
            await Task.CompletedTask;
        }


        //-------------------------------------------------------------------
        //                   Funkce: Přihlášky
        //-------------------------------------------------------------------
        // Click Button
        private static async Task WhitelistSuccess(ComponentInteractionCreateEventArgs args)
        {
            await WhitelistArchive(args, true);
            await SendMinecraftCommand($"whitelist add {args.Message.Embeds[0].Fields[0].Value}");

            var member = GetMemberFromUser(args.Message.MentionedUsers[0]).Result;
            await member.RevokeRoleAsync(WhitelistedPendingRole, "Zvládnul whitelist");
            await member.GrantRoleAsync(WhitelistedRole, "Zvládnul whitelist");
            var DMChannel = await member.CreateDmChannelAsync();

            await DMChannel.SendMessageAsync(Messages.Default.whitelist_Success);
            Properties.Settings.Default.int_WhitelistSuccessCount++;
            await UpdateInitialize();
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
            foreach (var field in embedmsg.Fields)
            {
                embedMessage.AddField(field.Name, field.Value.ToString(), field.Inline);
            }
            if (sucess) { embedMessage.Color = DiscordColor.Green; }
            else { embedMessage.Color = DiscordColor.Red; Properties.Settings.Default.int_WhitelistFailCount++; }

            await UpdateInitialize();
            embedMessage.AddField($"{ApproveEmoji} ({yesUsers.Count - 1})", yesUsersString, false);
            embedMessage.AddField($"{ApproveEmoji} ({noUsers.Count - 1})", noUsersString, false);
            embedMessage.Footer = new DiscordEmbedBuilder.EmbedFooter()
            {
                Text = "Vytvořená přihláška: " + args.Message.CreationTimestamp.ToString()
            };
            await WhitelistArchiveChannel.SendMessageAsync(embedMessage);
            await args.Message.DeleteAsync();
            await Task.CompletedTask;
        }
        // Automatic
        private static async Task WhitelistSuccessAuto(ComponentCrtEventArgs args)
        {
            await WhitelistArchiveAuto(args, true);
            await ConsoleChannel.SendMessageAsync($"whitelist add {args.Message.Embeds[0].Fields[0].Value}");

            DiscordMember member = GetMemberFromUser(args.Message.MentionedUsers[0]).Result;
            await member.RevokeRoleAsync(WhitelistedPendingRole, "Zvládnul whitelist");
            await member.GrantRoleAsync(WhitelistedRole, "Zvládnul whitelist");
            var DMChannel = await member.CreateDmChannelAsync();

            await DMChannel.SendMessageAsync(Messages.Default.whitelist_Success);
            Properties.Settings.Default.int_WhitelistSuccessCount++;
            await UpdateInitialize();
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
            else { embedMessage.Color = DiscordColor.Red; Properties.Settings.Default.int_WhitelistFailCount++; }

            await UpdateInitialize();
            embedMessage.AddField($"{ApproveEmoji} ({yesUsers.Count - 1})", yesUsersString, false);
            embedMessage.AddField($"{DisApproveEmoji} ({noUsers.Count - 1})", noUsersString, false);
            embedMessage.Footer = new DiscordEmbedBuilder.EmbedFooter()
            {
                Text = "Vytvořená přihláška: " + args.Message.CreationTimestamp.ToString()
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
            var approveButton = CreateButtonComponent(ButtonStyle.Success, "btn_approve_id", "Prošel", false, ApproveEmoji).Result;
            var closeButton = CreateButtonComponent(ButtonStyle.Danger, "btn_close_id", "Neprošel", false, DisApproveEmoji).Result;

            // Messages
            var msg = new DiscordMessageBuilder()
                    .WithContent(args.Interaction.User.Mention)
                    .WithAllowedMentions(new IMention[] { new UserMention(args.Interaction.User) })
                    .AddComponents(approveButton, closeButton);
            var embedMessage = new DiscordEmbedBuilder
            {
                Color = new DiscordColor("89CFF0"),
                Title = $"Přihláška #{Properties.Settings.Default.int_WhitelistTotalCount + 1}",
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
            embedMessage.WithFooter("Zbývá: 48 hodin 0 minut");
            embedMessage.WithThumbnail("https://mc-heads.net/" + Properties.Settings.Default.imgType_WhitelistThumbnail + "/" + args.Values["nicknameLabelID"]);
            msg.AddEmbed(embedMessage);

            // Exit
            var sentMSG = await WhitelistChannel.SendMessageAsync(msg);
            await sentMSG.CreateReactionAsync(ApproveEmoji);
            await sentMSG.CreateReactionAsync(DisApproveEmoji);
            await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Přihlášku jsi vytvořil, za celý projekt ti přejeme hodně štěstí s úspěchem! ^-^").AsEphemeral(true));
            Properties.Settings.Default.int_WhitelistTotalCount++;
            await UpdateInitialize();
            await Task.CompletedTask;
        }
        public static async void VoidUpdater(object sender, ElapsedEventArgs e)
        {
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
                    if (Properties.Settings.Default.automateWhitelist == true)
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
                            foreach (var member in members)
                            {
                                var DMChannel = await member.CreateDmChannelAsync();

                                if (DMChannel.GetMessagesAsync(5).Result.First().Content != message.JumpLink.ToString())
                                {
                                    var _msg = Messages.Default.whitelist_Pending;
                                    _msg.WithContent(message.JumpLink.ToString());

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
                        var members = await GetMembersByRole(AdminRole);
                        foreach (var member in members)
                        {
                            var DMChannel = await member.CreateDmChannelAsync();

                            if (DMChannel.GetMessagesAsync(5).Result.First().Content != message.JumpLink.ToString())
                            {
                                var _msg = Messages.Default.whitelist_Pending;
                                _msg.WithContent(message.JumpLink.ToString());


                                await DMChannel.SendMessageAsync(_msg);
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

        //-------------------------------------------------------------------
        //                   Funkce: Util
        //-------------------------------------------------------------------
        public static async Task UpdateInitialize()
        {
            Properties.Settings.Default.Save();
            Properties.Settings.Default.Reload();


            StringBuilder Wrapper_sb = new StringBuilder();
            StringBuilder Channels_sb = new StringBuilder();
            StringBuilder Roles_sb = new StringBuilder();
            StringBuilder Booleans_sb = new StringBuilder();
            StringBuilder Ints_sb = new StringBuilder();
            StringBuilder Others_sb = new StringBuilder();

            foreach (SettingsProperty currentProperty in Properties.Settings.Default.Properties)
            {
                var type = currentProperty.Name.Split('_');

                if (type[0] == "channel")
                {
                    try
                    {
                        Channels_sb.AppendLine($"{type[1]} - {GetChannelFromID(Properties.Settings.Default[currentProperty.Name].ToString()).Result.Name} ({Properties.Settings.Default[currentProperty.Name]})");
                    }
                    catch
                    {
                        Channels_sb.AppendLine($"{type[1]} - couldn't get channel ({Properties.Settings.Default[currentProperty.Name]})");
                    }
                }
                else if (type[0] == "role")
                {
                    try
                    {
                        Roles_sb.AppendLine($"{type[1]} - {GetRoleFromID(Properties.Settings.Default[currentProperty.Name].ToString()).Result.Name} ({Properties.Settings.Default[currentProperty.Name]})");
                    }
                    catch
                    {
                        Roles_sb.AppendLine($"{type[1]} - couldn't get role ({Properties.Settings.Default[currentProperty.Name]})");
                    }
                }
                else if (type[0] == "bool")
                {
                    Booleans_sb.AppendLine($"{type[1]} - {Properties.Settings.Default[currentProperty.Name]}");
                }
                else if (type[0] == "int")
                {
                    Ints_sb.AppendLine($"{type[1]} - {Properties.Settings.Default[currentProperty.Name]}");
                }
                else
                {
                    Others_sb.AppendLine($"{currentProperty.Name} - {Properties.Settings.Default[currentProperty.Name]}");
                }
            }

            Wrapper_sb.AppendLine("");
            Wrapper_sb.AppendLine("------------------- Channels -------------------");
            Wrapper_sb.AppendLine(Channels_sb.ToString());
            Wrapper_sb.AppendLine("------------------- Roles -------------------");
            Wrapper_sb.AppendLine(Roles_sb.ToString());
            Wrapper_sb.AppendLine("------------------- Booleans -------------------");
            Wrapper_sb.AppendLine(Booleans_sb.ToString());
            Wrapper_sb.AppendLine("------------------- Ints -------------------");
            Wrapper_sb.AppendLine(Ints_sb.ToString());
            Wrapper_sb.AppendLine("------------------- Others -------------------");
            Wrapper_sb.AppendLine(Others_sb.ToString());
            Wrapper_sb.AppendLine("");
            Console.WriteLine(Wrapper_sb.ToString());


            //Role
            #region Roles
            try
            {
                AdminRole = GetRoleFromID(Properties.Settings.Default.role_Admin).Result;
            } catch { }
            try
            {
                WhitelistedRole = GetRoleFromID(Properties.Settings.Default.role_Whitelisted).Result;
            } catch { }
            try
            {
                WhitelistedPendingRole = GetRoleFromID(Properties.Settings.Default.role_PendingWhitelist).Result;
            } catch { }
            #endregion

            //Channels
            #region Channels
            try
            {
                WhitelistChannel = GetChannelFromID(Properties.Settings.Default.channel_Whitelist).Result;
            } catch { }
            try
            {
                WhitelistArchiveChannel = GetChannelFromID(Properties.Settings.Default.channel_WhitelistArchive).Result;
            } catch { }
            try
            {
                ConsoleChannel = GetChannelFromID(Properties.Settings.Default.channel_Console).Result;
            } catch { }
            try
            {
                UserLoggingChannel = GetChannelFromID(Properties.Settings.Default.channel_UserLogging).Result;
            }
            catch { }
            try
            {
                MessageLoggingChannel = GetChannelFromID(Properties.Settings.Default.channel_MessageLogging).Result;
            }
            catch { }
            try
            {
                ChangelogChannel = GetChannelFromID(Properties.Settings.Default.channel_changelog).Result;
            }
            catch { }
            try
            {
                ToDoChannel = GetChannelFromID(Properties.Settings.Default.channel_todo).Result;
            }
            catch { }
            #endregion

            await Task.CompletedTask;
            return;
        }
        public static async Task UpdateToDoDictionary()
        {
            DiscordMessage ToDoMessage = ToDoChannel.GetMessagesAsync().Result.Where(x => x.Author.IsBot == true).First();
            if (ToDoMessage == null)
            {
                var msg = new DiscordEmbedBuilder().WithTitle("To-Do Seznam").WithColor(DiscordColor.Aquamarine);
                for (int i = 0; i < ToDoList.Count; i++)
                {
                    msg.AddField($"{i + 1}:", $"`{ToDoList[i]}`");
                }

                await ToDoChannel.SendMessageAsync(msg);
            }

            if (ToDoMessage.Embeds[0].Fields == null) { await Task.CompletedTask; return; }
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
                await WhitelistChannel.SendMessageAsync(command);
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

        #endregion
    }
}
