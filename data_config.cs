using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace setup_server
{
    class data_config
    {

        //EUROPE 0
        //USA 1
        //Asia 2
        //SA 3


        public static async void Init_data_config()
        {
            try
            {
                using (FileStream fs = new FileStream(starter.address_for_data_config, FileMode.OpenOrCreate))
                {
                    data_config_json Data = await JsonSerializer.DeserializeAsync<data_config_json>(fs);
                    starter.secret_key_for_game_servers = GetByteArrFromStringComma(Data.secret_key_for_game_servers);
                    starter.InnerServerConnectionPassword = Data.InnerServerConnectionPassword;
                    starter.MysqlConnectionData_login = Data.mysql_server_data;
                    
                    Dictionary<string, int> temp = Data.GameHubs;

                    foreach (string keys in temp.Keys)
                    {
                        starter.GameServerHUBs.Add(keys, new GameHubsSpec(keys, temp[keys]));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now + ": error - =========================");
                Console.WriteLine(ex);
                Console.WriteLine(": error ends - =========================");
            }

            
        }

        public static byte[] GetByteArrFromStringComma(string key_in_string)
        {
            List<byte> result = new List<byte>();

            string[] _data = key_in_string.Split(',');

            for (int i = 0; i < _data.Length; i++)
            {
                result.Add(Byte.Parse(_data[i]));
            }

            return result.ToArray();
        }


        class data_config_json
        {
            public string mysql_server_data { get; set; }
            public string secret_key_for_game_servers { get; set; }
            public string InnerServerConnectionPassword { get; set; }

            public Dictionary<string, int> GameHubs { get; set; } //hub name, hub IP
        }



    }
}
