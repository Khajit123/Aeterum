using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Channels;
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
                [Option("Whitelist-Channel", "Kanál s přihláškama")] DiscordChannel Whitelist = null,
                [Option("Archive-Channel", "Kanál pro archiv přihlášek")] DiscordChannel WhitelistArchive = null,
                [Option("Console-Channel", "Konzole, kde se budou psát příkazy")] DiscordChannel Console = null,
                [Option("Changelog-Channel", "Kde se budou psát changelog zprávy")] DiscordChannel Changelog = null,
                [Option("ToDo-Channel", "Kde se budou psát To-Do zprávy")] DiscordChannel ToDo = null,
                [Option("ServerImages-Channel", "Kde budou jen obrázky Serveru")] DiscordChannel ServerImages = null,
                [Option("DebugConsole-Channel", "Konzole, kde se budou ukazovat zprávy od bota")] DiscordChannel DebugConsole = null,
                [Option("MessageList-Channel", "Seznam zpráv")] DiscordChannel MessageList = null)
            {
                var conn = await Database.Connect();
                if (UserLogging != null) { await Program.UpdateChannel(conn, Database.ChannelNames.UserLogging, UserLogging.Id.ToString()); }
                if (MessageLogging != null) { await Program.UpdateChannel(conn, Database.ChannelNames.MessageLogging, MessageLogging.Id.ToString()); }
                if (Whitelist != null) { await Program.UpdateChannel(conn, Database.ChannelNames.Whitelist, Whitelist.Id.ToString()); }
                if (WhitelistArchive != null) { await Program.UpdateChannel(conn, Database.ChannelNames.WhitelistArchive, WhitelistArchive.Id.ToString()); }
                if (Console != null) { await Program.UpdateChannel(conn, Database.ChannelNames.Console, Console.Id.ToString()); }
                if (Changelog != null) { await Program.UpdateChannel(conn, Database.ChannelNames.Changelog, Changelog.Id.ToString()); }
                if (ToDo != null) { await Program.UpdateChannel(conn, Database.ChannelNames.ToDo, ToDo.Id.ToString()); }
                if (ServerImages != null) { await Program.UpdateChannel(conn, Database.ChannelNames.ServerImages, ServerImages.Id.ToString()); }
                if (DebugConsole != null) { await Program.UpdateChannel(conn, Database.ChannelNames.DebugConsole, DebugConsole.Id.ToString()); }
                if (MessageList != null) { await Program.UpdateChannel(conn, Database.ChannelNames.MessageList, MessageList.Id.ToString()); }
                await Database.Disconnect();

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Nastavení kanálu proběhlo v pořádku.").AsEphemeral(true));
                await Task.CompletedTask;
            }

            // ---------------------- Roles -----------------------
            [SlashCommand("roles", "Nastavení rolí")]
            public async Task roles(InteractionContext ctx,
                [Option("Whitelist-Role", "Role hráču, kteří již mají whitelist")] DiscordRole Whitelisted = null,
                [Option("PendingWhitelist-Role", "Role hráčů, co čekají a nemají whitelist")] DiscordRole NonWhitelisted = null,
                [Option("Admin-Role", "Role, která má přístup ke všem admin commandům")] DiscordRole Admin = null)
            {
                var conn = await Database.Connect();
                if (Whitelisted != null) { await Program.UpdateRole(conn, Database.RoleNames.Whitelisted, Whitelisted.Id.ToString()); }
                if (NonWhitelisted != null) { await Program.UpdateRole(conn, Database.RoleNames.NonWhitelisted, NonWhitelisted.Id.ToString()); }
                if (Admin != null) { await Program.UpdateRole(conn, Database.RoleNames.Admin, Admin.Id.ToString()); }
                await Database.Disconnect();

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Nastavení rolí proběhlo v pořádku.").AsEphemeral(true));
                await Task.CompletedTask;
            }

            // ---------------------- Others ---------------------
            [SlashCommand("others", "Upraví hodnoty, které nejsou v kategorii")]
            public async Task others(InteractionContext ctx,
                [Option("Počet-Whitelistu", "Upraví celkový počet whitelist žádostí")] long WhitelistTotal = -1,
                [Option("Počet-Úspěšných-Whitelistu", "Upraví počet úspěšných whitelist žádostí")] long WhitelistSuccess = -1,
                [Option("Počet-Neúspěšných-Whitelistu", "Upraví počet neúspěšných whitelist žádostí")] long WhitelistFail = -1,
                [Option("Automatický-Whitelist", "Pokud je hodnota True, whitelist bude sám vyhodnocovat po čase výsledek")] bool AutomateWhitelist = false,
                [Choice("Avatar", 0)]
                [Choice("Head", 1)]
                [Choice("Body", 2)]
                [Choice("Player", 3)]
                [Option("Typ-Thumbnail-Whitelist", "Typ skinu v pravém rohu na přihlášce")] long WhitelistThumbnailType = -1)
            {
                var conn = await Database.Connect();
                // Ints
                if (WhitelistTotal > -1) { await Program.UpdateInt(conn, Database.IntNames.WhitelistTotal, Int32.Parse(WhitelistTotal.ToString())); }
                if (WhitelistSuccess > -1) { await Program.UpdateInt(conn, Database.IntNames.WhitelistSuccess, Int32.Parse(WhitelistSuccess.ToString())); }
                if (WhitelistFail > -1) { await Program.UpdateInt(conn, Database.IntNames.WhitelistFail, Int32.Parse(WhitelistFail.ToString())); }

                // Booleans
                if (AutomateWhitelist != Program.GetBoolean(Database.BooleanNames.AutomateWhitelist)) { await Program.UpdateBoolean(conn, Database.BooleanNames.AutomateWhitelist, AutomateWhitelist); }

                // Others
                switch (WhitelistThumbnailType)
                {
                    case 0:
                        await Program.UpdateOther(conn, Database.OtherNames.WhitelistThumbnailType, "Avatar");
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Hodnoty byly upraveny.\n" +
                            $"https://mc-heads.net/Avatar/Khajit_").AsEphemeral(true));
                        break;
                    case 1:
                        await Program.UpdateOther(conn, Database.OtherNames.WhitelistThumbnailType, "Head");
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Hodnoty byly upraveny.\n" +
                            $"https://mc-heads.net/Head/Khajit_").AsEphemeral(true));
                        break;
                    case 2:
                        await Program.UpdateOther(conn, Database.OtherNames.WhitelistThumbnailType, "Body");
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Hodnoty byly upraveny.\n" +
                            $"https://mc-heads.net/Body/Khajit_").AsEphemeral(true));
                        break;
                    case 3:
                        await Program.UpdateOther(conn, Database.OtherNames.WhitelistThumbnailType, "Player");
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Hodnoty byly upraveny.\n" +
                            $"https://mc-heads.net/Player/Khajit_").AsEphemeral(true));
                        break;
                    default:
                        break;
                }
                await Database.Disconnect();

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
        //                   Zprávy
        //-------------------------------------------------------------------
        [SlashCommand("preview", "Aktualizuje seznam zpráv")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task previewMsg(InteractionContext ctx)
        {
            DiscordChannel messageListChannel = Program.GetChannel(Database.ChannelNames.MessageList);
            var messagesInChannel = await messageListChannel.GetMessagesAsync();

            Type messagesType = typeof(Messages.Default);
            PropertyInfo[] properties = messagesType.GetProperties(BindingFlags.Public | BindingFlags.Static);

            await ctx.DeferAsync(true);

            foreach (var message in messagesInChannel)
            {
                await message.DeleteAsync();
                await Task.Delay(TimeSpan.FromSeconds(5).Milliseconds);
            }

            foreach (PropertyInfo property in properties)
            {
                if (property.PropertyType == typeof(DiscordMessageBuilder))
                {
                    var message = (DiscordMessageBuilder)property.GetValue(null);

                    // Add the field name to the message content
                    var fieldName = property.Name;

                    var msgToBeSent = new DiscordMessageBuilder(message)
                        .WithContent($"{fieldName}\r\n\r{message.Content}")
                        .WithEmbed(new DiscordEmbedBuilder(message.Embed));

                    // Send the message to the channel
                    await messageListChannel.SendMessageAsync(msgToBeSent);
                    await Task.Delay(TimeSpan.FromSeconds(5).Milliseconds);
                }
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"Aktualizoval jsi preview zpráv."));
            await Task.CompletedTask;
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
            await Program.DebugConsole($"Uživateli {ctx.TargetMember.DisplayName} ({ctx.TargetMember.Id} byl unlinknut minecraft účet)");
            await Task.CompletedTask;
        }

        // Obnovit přihlášku
        [ContextMenu(ApplicationCommandType.MessageContextMenu, "Obnovit přihlášku")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task revokeWhitelist(ContextMenuContext ctx)
        {
            if (ctx.TargetMessage.Channel == Program.GetChannel(Database.ChannelNames.WhitelistArchive))
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
