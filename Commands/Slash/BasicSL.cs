using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
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
            [Option("titulek", "Titulek o co se jedná")] string title,
            [Option("popisek", "Podtitulek, zde se rozepiš")] string desc,
            [Option("embedcolor", "Barva embedu (na levém boku) ve formátu HEX")] string colorHEX = "FFFF00",
            [Option("tagRole", "Role na označení")] DiscordRole role = null,
            [Option("autor", "Kdo bude jako autor ve zprávě")] DiscordUser autor = null,
            [Option("thumbnail", "Vlož obrázek zde, který se objeví v pravém horním rohu")] DiscordAttachment thumbnailIMG = null,
            [Option("bigImage", "Vlož obrázek zde, který bude ukázán ve zprávě")] DiscordAttachment textIMG = null)
        //[Option("AddField", "Přidat Field (mezi popisek a bigImage/autor)")] bool fieldAdd = false,
        //[Option("FieldCount", "Počet Fieldu")] long fieldCount = 0)
        {
            var msg = new DiscordMessageBuilder();
            var embedMessage = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(colorHEX),
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = "Oznámení",
                    IconUrl = Program.Server.IconUrl,
                },
                Title = title,
                Description = desc,
            };
            if (role != null)
            {
                msg.WithContent(role.Mention);
            }
            if (autor != null) embedMessage.WithFooter(autor.Username, autor.AvatarUrl);
            else embedMessage.WithFooter("Aeterum Team", ctx.Guild.IconUrl);
            if (thumbnailIMG != null) { embedMessage.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail { Url = thumbnailIMG.Url, }; }
            if (textIMG != null) { embedMessage.ImageUrl = textIMG.Url; }

            //if (fieldAdd)
            //{
            //    for (int i = 0; i < fieldCount; i++)
            //    {
            //        string fieldtitle;
            //        string fieldtext;
            //        bool fieldinline;

            //        await ctx.DeferAsync(true);
            //        await ctx.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate, new DiscordInteractionResponseBuilder().WithContent($"Field #{i + 1} Napiš titulek"));
            //        var resultTitle = await ctx.Channel.GetNextMessageAsync(m =>
            //        {
            //            return m.Author.Id == ctx.User.Id;
            //        });
            //        if (resultTitle.TimedOut) return;
            //        fieldtitle = resultTitle.Result.Content;

            //        await ctx.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate, new DiscordInteractionResponseBuilder().WithContent($"Field #{i + 1} Napiš popisek"));
            //        var resultText = await ctx.Channel.GetNextMessageAsync(m =>
            //        {
            //            return m.Author.Id == ctx.User.Id;
            //        });
            //        if (resultText.TimedOut) return;
            //        fieldtext = resultText.Result.Content;

            //        await ctx.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate, new DiscordInteractionResponseBuilder().WithContent($"Field #{i + 1} Bude v jedné řadě? Napiš pouze true nebo false"));
            //        var resultInline = await ctx.Channel.GetNextMessageAsync(m =>
            //        {
            //            return m.Author.Id == ctx.User.Id;
            //        });
            //        if (resultInline.TimedOut) return;
            //        if (resultInline.Result.Content != "true" || resultInline.Result.Content != "false") return;
            //        fieldinline = Convert.ToBoolean(resultInline.Result.Content);
            //    }
            //}

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

            var embedMessage = new DiscordEmbedBuilder
            {
                Color = new DiscordColor(color),
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = msg.Author.Username,
                    IconUrl = msg.Author.AvatarUrl
                },
            };
            if (color != null) embedMessage.WithColor(new DiscordColor(color));
            if (autor != null) embedMessage.WithFooter(autor.Username, autor.AvatarUrl);
            else embedMessage.WithFooter("Aeterum Team", ctx.Guild.IconUrl);
            if (title != null) embedMessage.WithTitle(title);
            if (desc != null) embedMessage.WithDescription(desc);
            if (thumbnailIMG != null) { embedMessage.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail { Url = thumbnailIMG.Url, }; }
            if (textIMG != null) { embedMessage.ImageUrl = textIMG.Url; }

            await msg.ModifyAsync(rolemsg.Content, (DiscordEmbed)embedMessage);
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
                [Option("ServerImages-Channel", "Kde budou jen obrázky Serveru")] DiscordChannel ServerImagesChannel = null)
            {
                if (UserLogging != null) { Properties.Settings.Default.channel_UserLogging = UserLogging.Id.ToString(); }
                if (MessageLogging != null) { Properties.Settings.Default.channel_MessageLogging = MessageLogging.Id.ToString(); }
                if (WhitelistChannel != null) { Properties.Settings.Default.channel_Whitelist = WhitelistChannel.Id.ToString(); }
                if (WhitelistArchiveChannel != null) { Properties.Settings.Default.channel_WhitelistArchive = WhitelistArchiveChannel.Id.ToString(); }
                if (ConsoleChannel != null) { Properties.Settings.Default.channel_Console = ConsoleChannel.Id.ToString(); }
                if (ChangelogChannel != null) { Properties.Settings.Default.channel_changelog = ChangelogChannel.Id.ToString(); }
                if (ToDoChannel != null) { Properties.Settings.Default.channel_todo = ToDoChannel.Id.ToString(); }
                if (ServerImagesChannel != null) { Properties.Settings.Default.channel_ServerImages = ServerImagesChannel.Id.ToString(); }

                await Program.UpdateInitialize();
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
                if (WhitelistRole != null) { Properties.Settings.Default.role_Whitelisted = WhitelistRole.Id.ToString(); }
                if (PendingRole != null) { Properties.Settings.Default.role_PendingWhitelist = PendingRole.Id.ToString(); }
                if (AdminRole != null) { Properties.Settings.Default.role_Admin = AdminRole.Id.ToString(); }

                await Program.UpdateInitialize();
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
                        var channel = Program.GetChannelFromID(Properties.Settings.Default.channel_Whitelist).Result;

                        var sent = await channel.SendMessageAsync(msg);
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
                if (TotalCount > -1) { Properties.Settings.Default.int_WhitelistTotalCount = Convert.ToInt16(TotalCount); }
                if (SuccessCount > -1) { Properties.Settings.Default.int_WhitelistSuccessCount = Convert.ToInt16(SuccessCount); }
                if (FailCount > -1) { Properties.Settings.Default.int_WhitelistFailCount = Convert.ToInt16(FailCount); }
                if (automateWhitelist != Properties.Settings.Default.automateWhitelist) { Properties.Settings.Default.automateWhitelist = automateWhitelist; }
                switch (imgType)
                {
                    case 0:
                        Properties.Settings.Default.imgType_WhitelistThumbnail = "Avatar";
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Hodnoty byly upraveny.\n" +
                            $"https://mc-heads.net/Avatar/Khajit_").AsEphemeral(true));
                        await Task.CompletedTask;
                        break;
                    case 1:
                        Properties.Settings.Default.imgType_WhitelistThumbnail = "Head";
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Hodnoty byly upraveny.\n" +
                            $"https://mc-heads.net/Head/Khajit_").AsEphemeral(true));
                        await Task.CompletedTask;
                        break;
                    case 2:
                        Properties.Settings.Default.imgType_WhitelistThumbnail = "Body";
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Hodnoty byly upraveny.\n" +
                            $"https://mc-heads.net/Body/Khajit_").AsEphemeral(true));
                        await Task.CompletedTask;
                        break;
                    case 3:
                        Properties.Settings.Default.imgType_WhitelistThumbnail = "Player";
                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Hodnoty byly upraveny.\n" +
                            $"https://mc-heads.net/Player/Khajit_").AsEphemeral(true));
                        await Task.CompletedTask;
                        break;
                    default:
                        break;
                }

                await Program.UpdateInitialize();
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Hodnoty byly upraveny.").AsEphemeral(true));
                await Task.CompletedTask;
            }
        }

        //-------------------------------------------------------------------
        //                   Changelog
        //-------------------------------------------------------------------
        [SlashCommand("changelog", "Zpráva ohledně změn")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task changelogMsg(InteractionContext ctx,
            [Option("Nyní", "Zpráva jak je to teď")] string Current,
            [Option("Před", "Zpráva jak to bylo před úpravou")] string Before = null,
            [Option("BigImage", "Obrázek na spodní části zprávy")] DiscordAttachment bigImg = null,
            [Option("Titulek", "Titulek místo Changelog")] string Tittle = "Changelog",
            [Option("DisableCodeFormat", "Zruší kódový formát pro před a po")] bool DisableCode = false)

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
            if (bigImg != null) { embedmsg.WithImageUrl(bigImg.Url); }
            if (Before != null) { embedmsg.AddField("Před", $"{codeFormatSymbol}{Before}{codeFormatSymbol}", false); }
            embedmsg.AddField("Nyní", $"{codeFormatSymbol}{Current}{codeFormatSymbol}", false);
            await ctx.Channel.SendMessageAsync(embed: embedmsg);
            await Task.CompletedTask;
        }

        //-------------------------------------------------------------------
        //                   Context Menu: Unlink Minecraft
        //-------------------------------------------------------------------
        [ContextMenu(ApplicationCommandType.UserContextMenu, "Unlink Minecraft")]
        [RequireUserPermissions(Permissions.Administrator)]
        public async Task unlinkMC(ContextMenuContext ctx)
        {
            await Program.SendMinecraftCommand($"discordsrv unlink {ctx.TargetMember.Id}");

            await Task.CompletedTask;
        }
    }
}
