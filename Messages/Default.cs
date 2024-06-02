using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Embed Message Template
//var embed = new DiscordEmbedBuilder()
//{
//    Color = DiscordColor.White,
//    Author = new DiscordEmbedBuilder.EmbedAuthor()
//    {
//        Name = "",
//        Url = "",
//        IconUrl = "",

//    },
//    Title = "",
//    Url = "",
//    Description = "",
//    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
//    {
//        Url = "",
//    },
//    ImageUrl = "",
//    Footer = new DiscordEmbedBuilder.EmbedFooter()
//    {
//        Text = "",
//        IconUrl = "",
//    },
//    Timestamp = null,
//};
///
// Message Builder Template
//var msg = new DiscordMessageBuilder()
//{
//    Content = "",
//    Embed = embed,
//};


namespace Aeternum.Messages
{
    internal class Default
    {
        //--------------------------------------------------
        //                   Setup zprávy
        // -------------------------------------------------
        public static DiscordMessageBuilder first_Whitelist
        {
            get
            {
                var embed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Yellow,
                    Title = "Jak na přihlášku?",
                    Description = "Přihláška je povinná část pro možnost hrát na serveru.\n\n" +
                    "V přihlášce nám povíš něco o sobě a lidé se dle toho rozhodnou zda ti dávají šanci.\n\n" +
                    "Velmi doporučují si s přihláškou dát záležet, ačkoliv počet pokusů je neomezený tak první dojem u lidí bude klesat s každou další přihláškou.\n\n" + 
                    "**Pokud máš stále zájem klikni na tlačítko níže pro vytvoření tvé přihlášky**",
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                    {
                        Url = "https://openseauserdata.com/files/8e976123d4005649a085afa6abfeace4.gif",
                    },
                };
                var btn = new DiscordButtonComponent(ButtonStyle.Success, "btn_create_whitelist", "Vytvořit Přihlášku", false, new DiscordComponentEmoji(DiscordEmoji.FromName(Program.client, ":pencil:")));
                var msg = new DiscordMessageBuilder()
                {
                    Embed = embed,
                };
                msg.AddComponents(btn);

                return msg;
            }
            private set { }
        }
        public static DiscordMessageBuilder first_ToDo
        {
            get
            {
                var embed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Aquamarine,
                    Title = "To-Do List",
                };
                var msg = new DiscordMessageBuilder()
                {
                    Embed = embed,
                };

                return msg;
            }
            private set { }
        }


        //--------------------------------------------------
        //                   Warning
        // -------------------------------------------------
        public static DiscordMessageBuilder warning_Reaction
        {
            get
            {
                var embed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Title = "Upozornění!",
                    Description = "Hlasoval jsi na svojí nebo jinou přihlášku a to je **zakazáno**.\n\n Prosím tě tedy nehlasuj pro svojí ani jinou přihlášku.",
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                    {
                        Url = "https://upload.wikimedia.org/wikipedia/commons/2/2e/Exclamation_mark_red.png"
                    },
                    Footer = new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = "Aeterum Team",
                        IconUrl = Program.Server.IconUrl,
                    }
                };
                var msg = new DiscordMessageBuilder()
                {
                    Embed = embed,
                };

                return msg;
            }
            private set { }
        }
        public static DiscordMessageBuilder warning_ServerImages
        {
            get
            {
                var embed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Title = "Upozornění!",
                    Description = "Poslal jsi zprávu do kanálu jenž je určený pouze na obrázky. \n\n Pro debatu využij prosím vlákna k jednotlivým obrázkům, děkuji.",
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                    {
                        Url = "https://upload.wikimedia.org/wikipedia/commons/2/2e/Exclamation_mark_red.png"
                    },
                    Footer = new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = "Aeterum Team",
                        IconUrl = Program.Server.IconUrl,
                    }
                };
                var msg = new DiscordMessageBuilder()
                {
                    Embed = embed,
                };

                return msg;
            }
            private set { }
        }


        //--------------------------------------------------
        //                   Whitelist
        // -------------------------------------------------
        public static DiscordMessageBuilder whitelist_Success
        {
            get
            {
                var embed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.SpringGreen,
                    Title = "Čauko, zase my",
                    Description = "Neseme radostné zprávy! Zaujal jsi komunitu a velice rádi ti dáváme šanci se k nám přidat, teď už ti nic nebrání v připojení(IP): mc.aeterum.cz. Těšíme se na tebe :blush:",
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                    {
                        Url = Program.Server.IconUrl,
                    },
                    ImageUrl = "https://cdn.discordapp.com/attachments/1193271233781956732/1237686740060344341/Aeterum_banner.gif?ex=663c8d19&is=663b3b99&hm=dae8f2822b9e356e6376371b77e2451c7fa589683d663e81e63343d90a92a7b9&",
                    Footer = new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = "Aeterum Team",
                        IconUrl = Program.Server.IconUrl,
                    }
                };
                var msg = new DiscordMessageBuilder()
                {
                    Embed = embed,
                };

                return msg;
            }
            private set { }
        }
        public static DiscordMessageBuilder whitelist_Pending
        {
            get
            {
                var msg = new DiscordMessageBuilder()
                {
                };

                return msg;
            }
            private set { }
        }
        public static DiscordMessageBuilder whitelist_Waiting
        {
            get
            {
                var embed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Yellow,
                    Title = "Upozornění!",
                    Description = "Jedná z přihlášek má nerozhodný stav, popožeň lidí ať zahlasují",
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                    {
                        Url = Program.Server.IconUrl,
                    },
                    ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/2/2e/Exclamation_mark_red.png",
                    Footer = new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = "Aeterum Team",
                        IconUrl = Program.Server.IconUrl,
                    }
                };
                var msg = new DiscordMessageBuilder()
                {
                    Embed = embed,
                };

                return msg;
            }
            private set { }
        }
        public static DiscordMessageBuilder whitelist_Mistake_Revoking
        {
            get
            {
                var embed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Yellow,
                    Title = "Nastalo nedorozumnění!",
                    Description = "Velice se celý team Aeterum omlouvá za toto malé nedorozumnění. Neúmyslně a předčasně se vyhodnotila tvá přihláška a jsem tedy nucen ti ji navrátit. Jako odškodné nabízíme 48 hlasovacích bodů jenž se rovná půl dne aktivnímu hlasování. Po přijetí na whitelist si stačí utvořit ticket v kategorii podpora a odškodné vyplatíme. Ještě jednou se velice omlouváme za nedorozumnění!",
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                    {
                        Url = "https://upload.wikimedia.org/wikipedia/commons/2/2e/Exclamation_mark_red.png",
                    },
                    ImageUrl = Program.Server.IconUrl,
                    Footer = new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = "Aeterum Team",
                        IconUrl = Program.Server.IconUrl,
                    }
                };
                var msg = new DiscordMessageBuilder()
                {
                    Embed = embed,
                };

                return msg;
            }
            private set { }
        }

        //--------------------------------------------------
        //                   Member
        // -------------------------------------------------
        public static DiscordMessageBuilder member_Join
        {
            get
            {
                var embed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.SpringGreen,
                    Title = "Ahoj!",
                    Description = "Vítejte na našem Minecraft serveru, ponoř se s námi do světa plného inovací a společenství! Všichni jsme vyrůstali na Vanille, ale  tentokrát volíme Slimefun, rozšíření, které přináší do hry nový rozměr s množstvím unikátních předmětů a strojů, které můžete tvořit a používat. Od základních nástrojů až po složité mechanické konstrukce, Aeterum otevírá dveře k nekonečným možnostem a zážitkům.\r\n\r" +
                                  "Díky whitelistu se můžeš těšit také na naši komunitu, která je srdečná a přátelská, vždy připravená pomoci. Jsme prostě parta která se rozrostla v něco víc než jen občasné společné hraní.  Nic nás netěší víc, než když vidíme eventy, soutěže a společné projekty, které podporují týmového ducha. Připojte se k nám a staňte se součástí naší rostoucí komunity, kde každý den je dobrodružství!",
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                    {
                        Url = Program.Server.IconUrl,
                    },
                    Footer = new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = "Aeterum Team",
                        IconUrl = Program.Server.IconUrl,
                    }
                };
                var msg = new DiscordMessageBuilder()
                {
                    Embed = embed,
                };

                return msg;
            }
            private set { }
        }

    }
}
