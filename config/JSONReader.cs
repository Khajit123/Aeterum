using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace Aeternum.config
{
    internal class JSONReader
    {
        public string token { get; set; }
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
        public string token;
        public string Host;
        public string Port;
        public string Database;
        public string Username;
        public string Password;
    }
}