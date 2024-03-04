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
                    Title = "Zvládl jsi to!",
                    Description = "Tvá žádost o přidání na whitelist na Aeterum byla přijata.\r\n\r\nNež tě tam však přidám, chtěl jsem ještě předat nějaké to info, které se může hodit. \r\n\r\n- Již od tvého prvního připojení máš přístup k příkazu “/co i”, díky kterému zjistíš jakékoliv interakce s truhlami nebo blocky. Je to pro takový klid na duši, krádeže a nehlášené “vypůjčování” zde mají velice zřídkavý výskyt.\r\n\r\n- Po odehrání 100 hodin na serveru máš nárok na pozici hráče. Pokud si sami nevšimneme, neváhej nám dát vědět [odehraný čas na serveru najdeš v statistikách.] Tento rank nám slouží hlavně při rozhodování u dlouhodobější neaktivity, neaktivní hráči pak mají větší šanci zůstat na whitelistu. \r\n\r\n- Následně pokud každý měsíc odehraješ alespoň 15 hodin po získání ranku hráče, získáváš navíc privilegia rozhodovat o dění na serveru - hlasovat v přihláškách, anketách a podobně. Víc informací je potom přímo na discordu -> doporučuju alespoň koutkem oka projít. \r\n\r\nTo by mělo být to nejdůležitější, při jakékoliv otázce se neboj napsat, jsme tu téměř nonstop. Vítej na Orbisu 😊",
                    Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail()
                    {
                        Url = Program.Server.IconUrl,
                    },
                    ImageUrl = "https://www.icegif.com/wp-content/uploads/2023/10/icegif-170.gif",
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
                var embed = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Title = "Upozornění!",
                    Description = "Někdo čeká na zkontrolování a ověření žádosti, utíkej mu kliknout na tlačítko",
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

        //--------------------------------------------------
        //                   Member
        // -------------------------------------------------

    }
}
