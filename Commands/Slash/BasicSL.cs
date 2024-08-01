using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aeternum.Commands.Slash
{
    public class BasicSL : ApplicationCommandModule
    {
        //--------------------------------------------------
        //                   Oznámení
        // -------------------------------------------------
        [Category("Message")]
        [SlashCommand("oznámení", "Zpráva pro oznámení")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task InfoEmbed(InteractionContext ctx,
            [Option("titulek", "Titulek o co se jedná")] string title = null,
            [Option("popisek", "Podtitulek, zde se rozepiš")] string desc = null,
            [Option("embedcolor", "Barva embedu (na levém boku) ve formátu HEX")] string colorHEX = "FFFF00",
            [Option("tagRole", "Role na označení")] DiscordRole role = null,
            [Option("autor", "Kdo bude jako autor ve zprávě")] DiscordUser autor = null,
            [Option("thumbnail", "Vlož obrázek zde, který se objeví v pravém horním rohu")] DiscordAttachment thumbnailIMG = null,
            [Option("bigImage", "Vlož obrázek zde, který bude ukázán ve zprávě")] DiscordAttachment textIMG = null)
        {
            var msg = new DiscordMessageBuilder();
            var embedMessage = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(colorHEX),
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = "Oznámení",
                },
                Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    Text = "Aeterum Team",
                    IconUrl = Program.Server.IconUrl,
                }
            };
            if (title == null && desc == null && role == null && autor == null && thumbnailIMG == null && textIMG == null)
            {
                var modal = new DiscordInteractionResponseBuilder()
                    .WithTitle("Oznámení")
                    .WithCustomId("modal_oznameni");
                modal.AddComponents(new TextInputComponent("Titulek", "oznameniTitleID", null, "Oznámení", true, TextInputStyle.Short));
                modal.AddComponents(new TextInputComponent("Popisek", "oznameniDescID", "Zde se rozepiš", null, true, TextInputStyle.Paragraph));
                modal.AddComponents(new TextInputComponent("Obrázek v právém horním rohu", "oznameniThumbnailimgID", "Pouze url link na obrázek", null, false, TextInputStyle.Short));
                modal.AddComponents(new TextInputComponent("Obrázek pod zprávou", "oznameniBigimgID", "Pouze url link na obrázek", null, false, TextInputStyle.Short));

                await ctx.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);
                await Task.CompletedTask;
                return;
            }
            if (title != null) embedMessage.Title = title;
            if (desc != null) embedMessage.Description = desc.Replace("\\n", "\n");
            if (role != null)
            {
                msg.WithContent(role.Mention);
            }
            if (autor != null) embedMessage.WithFooter(autor.Username, autor.AvatarUrl);
            if (thumbnailIMG != null) { embedMessage.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail { Url = thumbnailIMG.Url, }; }
            if (textIMG != null) { embedMessage.ImageUrl = textIMG.Url; }

            await ctx.Channel.SendMessageAsync(msg.Content, embed: embedMessage);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Zpráva byla úspěšně vytvořena.").AsEphemeral(true));
            await Task.CompletedTask;
        }

        //--------------------------------------------------
        //                   Oznámení Message ID
        // -------------------------------------------------
        [Category("Message")]
        [SlashCommand("oznámeníID", "Zpráva pro oznámení pomocí ID zprávy")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task oznameniMSGID(InteractionContext ctx,
            [Option("channel", "Kanál, kde je zpráva, kterou chceš zkopírovat")] DiscordChannel channel,
            [Option("messageID", "Zkopiruje se obsah zprávy v tomto kanále podle ID")] string messageID,
            [Option("titulek", "Titulek o co se jedná")] string title = null,
            [Option("embedcolor", "Barva embedu (na levém boku) ve formátu HEX")] string colorHEX = "FFFF00",
            [Option("tagRole", "Role na označení")] DiscordRole role = null,
            [Option("autor", "Kdo bude jako autor ve zprávě")] DiscordUser autor = null,
            [Option("thumbnail", "Vlož obrázek zde, který se objeví v pravém horním rohu")] DiscordAttachment thumbnailIMG = null,
            [Option("bigImage", "Vlož obrázek zde, který bude ukázán ve zprávě")] DiscordAttachment textIMG = null)
        {
            ulong ulongMSG = Convert.ToUInt64(messageID);

            var msg = new DiscordMessageBuilder();
            var embedMessage = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(colorHEX),
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = "Oznámení",
                },
                Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    Text = "Aeterum Team",
                    IconUrl = Program.Server.IconUrl,
                }
            };
            if (title != null) embedMessage.Title = title;
            try
            {
                var selectedMSG = await channel.GetMessageAsync(ulongMSG);
                embedMessage.WithDescription(selectedMSG.Content.ToString());
                await Task.CompletedTask;
            }
            catch (Exception)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Nepovadlo se najít zprávu podle ID, zkontroluj to. Pro přepsání zmáčkni šipku nahoru").AsEphemeral(true));
                await Task.CompletedTask;
                return;
            }
            if (role != null)
            {
                msg.WithContent(role.Mention);
            }
            if (autor != null) embedMessage.WithFooter(autor.Username, autor.AvatarUrl);
            if (thumbnailIMG != null) { embedMessage.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail { Url = thumbnailIMG.Url, }; }
            if (textIMG != null) { embedMessage.ImageUrl = textIMG.Url; }

            await ctx.Channel.SendMessageAsync(msg.Content, embed: embedMessage);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Zpráva byla úspěšně vytvořena.").AsEphemeral(true));
            await Task.CompletedTask;
        }

        //--------------------------------------------------
        //           Upravení zprávy od bota
        // -------------------------------------------------
        [Category("Message")]
        [SlashCommand("upravit", "Upravit zprávu od bota")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task EditEmbed(InteractionContext ctx,
            [Option("messageID", "ID zprávy")] string msgID,
            [Option("channel", "Kde je ta zpráva")] DiscordChannel channel,
            [Option("tagRole", "Nová role na označení, avšak to znovu neoznačí")] DiscordRole role = null,
            [Option("autor", "Ty budeš ve zprávě jako autor")] DiscordUser autor = null,
            [Option("title", "Nový titulek")] string title = null,
            [Option("description", "Nový podtitulek")] string desc = null,
            [Option("embedcolor", "Nová barva embedu (na levém boku) ve formátu HEX")] string color = "008000",
            [Option("thumbnail", "Vlož obrázek zde, který se objeví v pravém horním rohu")] DiscordAttachment thumbnailIMG = null,
            [Option("textImage", "Vlož obrázek zde, který bude ukázán ve zprávě")] DiscordAttachment textIMG = null)
        {
            ulong ulongMSG = Convert.ToUInt64(msgID);
            DiscordMessage msg = null;

            try
            {
                msg = await channel.GetMessageAsync(ulongMSG);
            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);
            }
            if (msg == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Nepodařilo se mi najít zprávu. Možná špatné ID zprávy nebo špatný channel").AsEphemeral(true));
                await Task.CompletedTask;
                return;
            }
            var rolemsg = new DiscordMessageBuilder();
            if (role != null)
            {
                rolemsg.WithContent(role.Mention)
                     .WithAllowedMentions(new IMention[] { new RoleMention(role) });
            }
            else
            {
                rolemsg.WithContent(msg.Content)
                    .WithAllowedMentions(new IMention[] { new RoleMention(msg.MentionedRoles[0]) });
            }

            if (msg.Embeds != null)
            {
                var embedMessage = new DiscordEmbedBuilder
                {
                    Color = msg.Embeds[0].Color,
                    Author = new DiscordEmbedBuilder.EmbedAuthor
                    {
                        Name = msg.Embeds[0].Author.Name,
                        IconUrl = msg.Embeds[0].Author.IconUrl.ToString()
                    },
                    Title = msg.Embeds[0].Title,
                    Description = msg.Embeds[0].Description,
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                    {
                        Url = msg.Embeds[0].Thumbnail.Url.ToString(),
                    },
                    ImageUrl = msg.Embeds[0].Image.Url.ToString(),
                    Footer = new DiscordEmbedBuilder.EmbedFooter() 
                    {
                        Text = msg.Embeds[0].Footer.Text,
                        IconUrl = msg.Embeds[0].Footer.IconUrl.ToString()
                    }
                };
                if (color != null) embedMessage.WithColor(new DiscordColor(color));
                if (autor != null) embedMessage.WithFooter(autor.Username, autor.AvatarUrl);
                if (title != null) embedMessage.WithTitle(title);
                if (desc != null) embedMessage.WithDescription(desc);
                if (thumbnailIMG != null) { embedMessage.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail { Url = thumbnailIMG.Url, }; }
                if (textIMG != null) { embedMessage.ImageUrl = textIMG.Url; }
                rolemsg.AddEmbed(embedMessage);
            }

            await msg.ModifyAsync(rolemsg);
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Zpráva byla úspěšně upravená: {msg.JumpLink}.").AsEphemeral(true));
            await Task.CompletedTask;
        }

        //-------------------------------------------------------------------
        //                   Nastavení bota
        //-------------------------------------------------------------------
        [Category("Settings")]
        [SlashCommandGroup("nastavit", "Nastavení")]
        [RequireUserPermissions(Permissions.Administrator)]
        public class GroupContainer
        {

            // --------------------- Channels --------------------
            [SlashCommand("channels", "Nastavení channelu")]
            public async Task msgChannels(InteractionContext ctx,
                [Option("User-Logging", "Připojení/Odpojení hráčů")] DiscordChannel UserLogging = null,
                [Option("Message-Logging", "Upravení/Smazání zprávy")] DiscordChannel MessageLogging = null,
                [Option("Whitelist-Channel", "Kanál s přihláškama")] DiscordChannel WhitelistChannel = null,
                [Option("Archive-Channel", "Kanál pro archiv přihlášek")] DiscordChannel WhitelistArchiveChannel = null,
                [Option("Console-Channel", "Konzole, kde se budou psát příkazy")] DiscordChannel ConsoleChannel = null,
                [Option("Changelog-Channel", "Kde se budou psát changelog zprávy")] DiscordChannel ChangelogChannel = null,
                [Option("ToDo-Channel", "Kde se budou psát To-Do zprávy")] DiscordChannel ToDoChannel = null,
                [Option("ServerImages-Channel", "Kde budou jen obrázky Serveru")] DiscordChannel ServerImagesChannel = null,
                [Option("DebugConsole-Channel", "Konzole, kde se budou ukazovat zprávy od bota")] DiscordChannel DebugConsoleChannel = null)
            {
                List<Database.db_channel> channelsToChange = new List<Database.db_channel>();
                if (UserLogging != null) { channelsToChange.Add(new Database.db_channel(Database.Channels.UserLogging, UserLogging.Id.ToString())); Program.UserLoggingChannel = UserLogging; }
                if (MessageLogging != null) { channelsToChange.Add(new Database.db_channel(Database.Channels.MessageLogging, MessageLogging.Id.ToString())); Program.MessageLoggingChannel = MessageLogging; }
                if (WhitelistChannel != null) { channelsToChange.Add(new Database.db_channel(Database.Channels.Whitelist, WhitelistChannel.Id.ToString())); Program.WhitelistChannel = WhitelistChannel; }
                if (WhitelistArchiveChannel != null) { channelsToChange.Add(new Database.db_channel(Database.Channels.WhitelistArchive, WhitelistArchiveChannel.Id.ToString())); Program.WhitelistArchiveChannel = WhitelistArchiveChannel; }
                if (ConsoleChannel != null) { channelsToChange.Add(new Database.db_channel(Database.Channels.Console, ConsoleChannel.Id.ToString())); Program.ConsoleChannel = ConsoleChannel; }
                if (ChangelogChannel != null) { channelsToChange.Add(new Database.db_channel(Database.Channels.Changelog, ChangelogChannel.Id.ToString())); Program.ChangelogChannel = ChangelogChannel; }
                if (ToDoChannel != null) { channelsToChange.Add(new Database.db_channel(Database.Channels.ToDo, ToDoChannel.Id.ToString())); Program.ToDoChannel = ToDoChannel; }
                if (ServerImagesChannel != null) { channelsToChange.Add(new Database.db_channel(Database.Channels.ServerImages, ServerImagesChannel.Id.ToString())); Program.ServerImagesChannel = ServerImagesChannel; }
                if (DebugConsoleChannel != null) { channelsToChange.Add(new Database.db_channel(Database.Channels.DebugConsole, DebugConsoleChannel.Id.ToString())); Program.DebugConsoleChannel = DebugConsoleChannel; }


                if (channelsToChange.Count > 0) { await Program.UpdateDatabaseChannels(channelsToChange.ToArray()); }

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Nastavení kanálu proběhlo v pořádku.").AsEphemeral(true));
                await Task.CompletedTask;
                return;
            }

            // ---------------------- Roles -----------------------
            [SlashCommand("roles", "Nastavení rolí")]
            public async Task roles(InteractionContext ctx,
                [Option("Whitelist-Role", "Role hráču, kteří již mají whitelist")] DiscordRole WhitelistRole = null,
                [Option("PendingWhitelist-Role", "Role hráčů, co čekají a nemají whitelist")] DiscordRole PendingRole = null,
                [Option("Admin-Role", "Role, která má přístup ke všem admin commandům")] DiscordRole AdminRole = null)
            {
                List<Database.db_roles> rolesToChange = new List<Database.db_roles>();
                if (WhitelistRole != null) { rolesToChange.Add(new Database.db_roles(Database.Roles.Whitelisted, WhitelistRole.Id.ToString())); Program.WhitelistedRole = WhitelistRole; }
                if (PendingRole != null) { rolesToChange.Add(new Database.db_roles(Database.Roles.NonWhitelisted, PendingRole.Id.ToString())); Program.NonWhitelistedRole = PendingRole; }
                if (AdminRole != null) { rolesToChange.Add(new Database.db_roles(Database.Roles.Admin, AdminRole.Id.ToString())); Program.AdminRole = AdminRole; }

                if (rolesToChange.Count > 0) { await Program.UpdateDatabaseRoles(rolesToChange.ToArray()); }

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Nastavení rolí proběhlo v pořádku.").AsEphemeral(true));
                await Task.CompletedTask;
            }

            // ---------------------- Messages ---------------------
            [SlashCommand("messages", "Napíše úvodní zprávu pro správnou funkci")]
            public async Task channelMessages(InteractionContext ctx,
                [Choice("Whitelist Channel", 0)]
                [Option("kanál", "Kde vykonat úvodní zprávu")] long channels)
            {
                switch (channels)
                {
                    // Whitelist Message channel
                    case 0:
                        var msg = Messages.Default.first_Whitelist;

                        var sent = await Program.WhitelistChannel.SendMessageAsync(msg);
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Zpráva byla vygenerována {sent.Channel.Mention}.").AsEphemeral(true));
                        break;

                    default:
                        break;
                }
                await Task.CompletedTask;
            }

            // ---------------------- Others ---------------------
            [SlashCommand("others", "Upraví hodnoty, které nejsou v kategorii")]
            public async Task others(InteractionContext ctx,
                [Option("Počet-Whitelistu", "Upraví celkový počet whitelist žádostí")] long TotalCount = -1,
                [Option("Počet-Úspěšných-Whitelistu", "Upraví počet úspěšných whitelist žádostí")] long SuccessCount = -1,
                [Option("Počet-Neúspěšných-Whitelistu", "Upraví počet neúspěšných whitelist žádostí")] long FailCount = -1,
                [Option("Automatický-Whitelist", "Pokud je hodnota True, whitelist bude sám vyhodnocovat po čase výsledek")] bool automateWhitelist = false,
                [Choice("Avatar", 0)]
                [Choice("Head", 1)]
                [Choice("Body", 2)]
                [Choice("Player", 3)]
                [Option("Typ-Thumbnail-Whitelist", "Typ skinu v pravém rohu na přihlášce")] long imgType = -1)
            {
                List<Database.db_ints> intsToChange = new List<Database.db_ints>();
                List<Database.db_booleans> booleansToChange = new List<Database.db_booleans>();
                List<Database.db_other> otherToChange = new List<Database.db_other>();

                if (TotalCount > -1) { intsToChange.Add(new Database.db_ints(Database.Ints.WhitelistTotal, Int32.Parse(TotalCount.ToString()), true)); }
                if (SuccessCount > -1) { intsToChange.Add(new Database.db_ints(Database.Ints.WhitelistSuccess, Int32.Parse(SuccessCount.ToString()), true)); }
                if (FailCount > -1) { intsToChange.Add(new Database.db_ints(Database.Ints.WhitelistFail, Int32.Parse(FailCount.ToString()), true));}
                if (automateWhitelist != Program.AutomateWhitelist) { booleansToChange.Add(new Database.db_booleans(Database.Booleans.AutomateWhitelist, automateWhitelist)); Program.AutomateWhitelist = automateWhitelist; }
                switch (imgType)
                {
                    case 0:
                        otherToChange.Add(new Database.db_other(Database.Other.WhitelistThumbnailType, "Avatar"));
                        Program.WhitelistThumbnailType = "Avatar";
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Hodnoty byly upraveny.\n" +
                            $"https://mc-heads.net/Avatar/Khajit_").AsEphemeral(true));
                        await Task.CompletedTask;
                        break;
                    case 1:
                        otherToChange.Add(new Database.db_other(Database.Other.WhitelistThumbnailType, "Head"));
                        Program.WhitelistThumbnailType = "Head";
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Hodnoty byly upraveny.\n" +
                            $"https://mc-heads.net/Head/Khajit_").AsEphemeral(true));
                        await Task.CompletedTask;
                        break;
                    case 2:
                        otherToChange.Add(new Database.db_other(Database.Other.WhitelistThumbnailType, "Body"));
                        Program.WhitelistThumbnailType = "Body";
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Hodnoty byly upraveny.\n" +
                            $"https://mc-heads.net/Body/Khajit_").AsEphemeral(true));
                        await Task.CompletedTask;
                        break;
                    case 3:
                        otherToChange.Add(new Database.db_other(Database.Other.WhitelistThumbnailType, "Player"));
                        Program.WhitelistThumbnailType = "Player";
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Hodnoty byly upraveny.\n" +
                            $"https://mc-heads.net/Player/Khajit_").AsEphemeral(true));
                        await Task.CompletedTask;
                        break;
                    default:
                        break;
                }

                if (intsToChange.Count > 0) { await Program.UpdateDatabaseInts(intsToChange.ToArray()); }
                if (booleansToChange.Count > 0) { await Program.UpdateDatabaseBooleans(booleansToChange.ToArray()); }
                if (otherToChange.Count > 0) { await Program.UpdateDatabaseOther(otherToChange.ToArray());}

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Hodnoty byly upraveny.").AsEphemeral(true));
                await Task.CompletedTask;
            }
        }

        //-------------------------------------------------------------------
        //                   To-Do Seznam
        //-------------------------------------------------------------------
        [Category("ToDo")]
        [SlashCommandGroup("todo", "To-Do")]
        [RequireUserPermissions(Permissions.Administrator)]
        public class ToDoGroupContainer
        {
            // ---------------------- Přidat ---------------------
            [SlashCommand("přidat", "Přida zprávu do to-do listu")]
            public async Task addToDo(InteractionContext ctx,
                [Option("Zpráva", "Přidá zprávu do to-do listu")] string addText,
                [Option("Více", "Povolí více zpráv odděluj čárkou")] bool multiple = false)
            {
                if (addText.Contains(',') && multiple)
                {
                    string[] splittedText = addText.Split(',');
                    Program.ToDoList.AddRange(splittedText);
                }
                else
                {
                    Program.ToDoList.Add(addText);
                }

                await Program.ToDoUpdate();


                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Přidal jsi zprávu do to-do listu.").AsEphemeral(true));
                await Task.CompletedTask;
            }
            // ---------------------- Odebrat ---------------------
            [SlashCommand("odebrat", "Smaže zprávu z to-do list")]
            public async Task removeToDo(InteractionContext ctx,
                [Option("Index", "Odebere zprávu na pozici dle uvedeného indexu")] long index)
            {

                Program.ToDoList.RemoveAt((int)index - 1);
                await Program.ToDoUpdate();

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Odebral jsi zprávu z to-do listu.").AsEphemeral(true));
                await Task.CompletedTask;
            }
            // ---------------------- Hotovo ---------------------
            [SlashCommand("hotovo", "Zaznačí zprávu v to-do listu škrtnutím. Pokud je zaškrtlá tak ji vratí do normálu")]
            public async Task doneToDo(InteractionContext ctx,
                [Option("Index", "Zaškrtne zprávu na uvedenem indexu jako hotová")] long index)
            {

                string ToDoText = Program.ToDoList[(int)index - 1];
                bool formattedText = (string.Join(string.Empty, ToDoText.Take(2)) == "~~" ? true : false);
                Program.ToDoList.RemoveAt((int)index - 1);
                if (formattedText)
                {
                    Program.ToDoList.Insert((int)index - 1, ToDoText.TrimStart('~').TrimEnd('~'));
                }
                else
                {
                    Program.ToDoList.Insert((int)index - 1, $"~~{ToDoText}~~");
                }
                await Program.ToDoUpdate();

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Přepnul jsi stav hotovosti v to-do listu.").AsEphemeral(true));
                await Task.CompletedTask;
            }
        }


        //-------------------------------------------------------------------
        //                   Changelog
        //-------------------------------------------------------------------
        [SlashCommand("changelog", "Zpráva ohledně změn")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task changelogMsg(InteractionContext ctx,
            [Option("Před", "Zpráva jak to bylo před úpravou")] string Before = null,
            [Option("Nyní", "Zpráva jak je to teď")] string Current = null,
            [Option("ThumbnailImage", "Obrázek v pravém horním rohu zprávy")] DiscordAttachment thumbnailImg = null,
            [Option("BigImage", "Obrázek na spodní části zprávy")] DiscordAttachment bigImg = null,
            [Option("Titulek", "Titulek místo Changelog")] string Tittle = "Changelog",
            [Option("DisableCodeFormat", "Zruší kódový formát pro před a po")] bool DisableCode = false)

        {
            if (Current == null)
            {
                var modal = new DiscordInteractionResponseBuilder()
                    .WithTitle("Changelog")
                    .WithCustomId("modal_changelog");
                modal.AddComponents(new TextInputComponent("Před", "changelogBeforeID", null, "`Zde vlož text(není nutné)`", false, TextInputStyle.Paragraph));
                modal.AddComponents(new TextInputComponent("Nyní", "changelogNowID", null, "`Zde vlož text(nutné)`", true, TextInputStyle.Paragraph));
                modal.AddComponents(new TextInputComponent("Titulek", "changelogTittleID", null, "Changelog", true, TextInputStyle.Short));
                modal.AddComponents(new TextInputComponent("Obrázek v právém horním rohu", "changelogThumbnailimgID", "Pouze url link na obrázek", null, false, TextInputStyle.Short));
                modal.AddComponents(new TextInputComponent("Obrázek pod zprávou", "changelogBigimgID", "Pouze url link na obrázek", null, false, TextInputStyle.Short));

                await ctx.Interaction.CreateResponseAsync(InteractionResponseType.Modal, modal);
                await Task.CompletedTask;
            }
            else
            {
                string codeFormatSymbol = "`";
                if (DisableCode == true) codeFormatSymbol = "";
                var embedmsg = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Orange,
                    Title = Tittle,
                    Footer = new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = ctx.Member.Nickname,
                        IconUrl = ctx.Member.AvatarUrl.ToString(),
                    }
                };
                if (thumbnailImg != null) { embedmsg.WithThumbnail(thumbnailImg.Url); }
                if (bigImg != null) { embedmsg.WithImageUrl(bigImg.Url); }
                if (Before != null) { embedmsg.AddField("Před", $"{codeFormatSymbol}{Before}{codeFormatSymbol}", false); }
                embedmsg.AddField("Nyní", $"{codeFormatSymbol}{Current}{codeFormatSymbol}", false);
                await ctx.Channel.SendMessageAsync(embed: embedmsg);
                await Task.CompletedTask;
            }
        }

        //-------------------------------------------------------------------
        //                   Context Menu
        //-------------------------------------------------------------------
        // Unlink minecraft
        [ContextMenu(ApplicationCommandType.UserContextMenu, "Unlink Minecraft")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task unlinkMC(ContextMenuContext ctx)
        {
            await Program.SendMinecraftCommand($"discordsrv unlink {ctx.TargetMember.Id}");
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                .WithContent($"unlinknul jsi Discord účet od Minecraft účtu hráči {ctx.TargetUser.Mention}.").AsEphemeral(true));
            await Program.DebugConsole($"Uživateli {ctx.TargetMember.Nickname} ({ctx.TargetMember.Id} byl unlinknut minecraft účet)");
            await Task.CompletedTask;
        }

        // Obnovit přihlášku
        [ContextMenu(ApplicationCommandType.MessageContextMenu, "Obnovit přihlášku")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task revokeWhitelist(ContextMenuContext ctx)
        {
            if (ctx.TargetMessage.Channel == Program.WhitelistArchiveChannel)
            {
                await Program.RevokeWhitelist(ctx.TargetMessage);
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent($"Obnovil jsi přihlášku hráči {ctx.TargetMessage.Content}").AsEphemeral(true));
                await Program.DebugConsole($"Uživateli {ctx.TargetMessage.Content} byla obnovena přihláška)");
                await Task.CompletedTask;
            }
        }
    }
}
