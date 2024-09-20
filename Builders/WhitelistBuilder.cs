using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Aeternum.Database;

namespace Aeternum.Model
{
    public class WhitelistBuilder
    {
        // Properties for the different fields
        public DiscordUser DiscordNickname { get; set; }
        public string Nickname { get; set; }
        public string Age { get; set; }
        public string HowDidYouFindOutAboutUs { get; set; }
        public string Expectations { get; set; }
        public string AboutYourself { get; set; }

        // This will store the generated DiscordMessageBuilder
        private DiscordMessageBuilder _messageBuilder;

        // Constructor
        public WhitelistBuilder()
        {
            // Initialize the message builder
            _messageBuilder = new DiscordMessageBuilder();
        }

        // Method to convert the properties to DiscordMessageBuilder with specific field order
        public DiscordMessageBuilder ToDiscordMessage()
        {
            // Ensure the fields are added in the order you want
            var embedBuilder = new DiscordEmbedBuilder()
                .AddField("Nickname", this.Nickname) // 1. field
                .AddField("Věk", this.Age.ToString()) // 2. field
                .AddField("Jak ses o nás dozvěděl/a?", this.HowDidYouFindOutAboutUs) // 3. field
                .AddField("Co od serveru očekáváš?", this.Expectations); // 4. field

            // Removing limit from 1024(discord embed field) chars to 4000(discord modal) chars 
            char[] contentArray = this.AboutYourself.ToArray();
            int first = 1;

            if (contentArray.Length <= 1024)
            {
                embedBuilder.AddField("Něco o sobě", this.AboutYourself, false);
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
                        embedBuilder.AddField("Něco o sobě", string.Join("", contentArray.Take(currentCharPos).ToArray()), false);
                        first = 0;
                    }
                    else
                    {
                        embedBuilder.AddField("|", string.Join("", contentArray.Take(currentCharPos).ToArray()), false);
                    }
                    contentArray = contentArray.Skip(currentCharPos).ToArray();
                }

                if (contentArray.Length <= 1024)
                {
                    embedBuilder.AddField("|", string.Join("", contentArray), false);
                }
            }

            // Adding some properties for embed
            embedBuilder.WithColor(new DiscordColor("89CFF0")); // Adding light blue color to embed
            embedBuilder.WithAuthor(this.DiscordNickname.Username, null, this.DiscordNickname.AvatarUrl); // Adding user as author
            embedBuilder.WithTitle($"Přihláška #{Program.GetInt(Database.IntNames.WhitelistTotal) + 1}"); // Updating whitelist with total count
            embedBuilder.WithFooter("Zbývá: 2 dny"); // Placeholder for remaining time

            // Adding thumbnail with player skin by specified Nickname in case user doesnt own original account then fail
            string playerUUID = Program.GetMinecraftUUIDByUsername(this.Nickname).Result;
            if (playerUUID == null)
            {
                DiscordMessageBuilder warningMessage = new DiscordMessageBuilder().WithEmbed(new DiscordEmbedBuilder(Messages.Default.warning_Reaction.Embeds.First())
                    .WithDescription("Tvůj pokus o vytvoření přihlášky byl neúspěšný kvůli tomu, že se nepodařilo ověřit nickname, který si zadal jako vlastníka originalního minecraftu a bez něj si u nás nezahraješ. Je ti poslána takhle soukromá zpráva ať dotyčnou zprávu nemusíš psát celou znovu, ale můžeš změnit pouze nickname."));
                Program.SendDMMessage(this.DiscordNickname, warningMessage).Wait();
                return null;
            }
            embedBuilder.WithThumbnail("https://mc-heads.net/" + Program.GetOther(OtherNames.WhitelistThumbnailType) + "/" + playerUUID);

            // Attach the embed to the message builder
            _messageBuilder.AddEmbed(embedBuilder);
            _messageBuilder.WithContent(this.DiscordNickname.Mention + " ||" + Program.GetRole(RoleNames.Whitelisted).Mention + "||")
                .WithAllowedMentions(new IMention[] { new UserMention(this.DiscordNickname) });

            return _messageBuilder;
        }

        // Static method to parse a DiscordEmbed back to a WhitelistModel
        public static WhitelistBuilder FromDiscordEmbed(DiscordMessage message)
        {
            var whitelist = new WhitelistBuilder();

            if (message.MentionedUsers.Count > 0) { whitelist.DiscordNickname = message.MentionedUsers.FirstOrDefault(); } // Setting DiscordNickname by embed content mention

            foreach (var field in message.Embeds.FirstOrDefault().Fields)
            {
                // Map fields back to model properties based on their name
                if (field.Name == "Nickname")
                {
                    whitelist.Nickname = field.Value;
                } 
                else if (field.Name == "Věk")
                {
                    whitelist.Age = field.Value;
                }
                else if (field.Name == "Jak ses o nás dozvěděl/a?")
                {
                    whitelist.HowDidYouFindOutAboutUs = field.Value;
                }
                else if (field.Name == "Co od serveru očekáváš?")
                {
                    whitelist.Expectations = field.Value;
                }
                else if (field.Name == "Něco o sobě" || field.Name == "|")
                {
                    whitelist.AboutYourself += field.Value;
                }
            }

            return whitelist;
        }
    }
}
