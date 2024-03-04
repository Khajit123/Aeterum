using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Aeternum.config
{
    internal class JSONReader
    {
        public string token {  get; set; }
        public string prefix { get; set; }
        public string dbhost { get; set; }
        public string dbport { get; set; }
        public string dbname { get; set; }
        public string dbusername { get; set; }
        public string dbpassword { get; set; }

        public async Task ReadJSON()
        {
            using (StreamReader sr = new StreamReader("config.json"))
            {
                string json = await sr.ReadToEndAsync();
                JSONStructure data = JsonConvert.DeserializeObject<JSONStructure>(json);

                this.token = data.token;
                this.prefix = data.prefix;
            }

            using (StreamReader sr = new StreamReader("dbconfig.json"))
            {
                string json = await sr.ReadToEndAsync();
                JSONStructure data = JsonConvert.DeserializeObject<JSONStructure>(json);

                this.dbhost = data.Host;
                this.dbport = data.Port;
                this.dbname = data.Database;
                this.dbusername = data.Username;
                this.dbpassword = data.Password;
            }
        }
    }

    internal sealed class JSONStructure
    {
        public string token { get; set; }
        public string prefix { get; set; }
        public string Host { get; set; }
        public string Port { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
