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
        //public static byte[] secret_key_for_game_servers;
        //public static string InnerServerConnectionPassword;
        //public static string MysqlConnectionData_login;
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

        public static async Task<byte[]> secret_key_for_game_servers()
        {
            byte[] result;

            using (FileStream fs = new FileStream(starter.address_for_data_config, FileMode.OpenOrCreate))
            {
                data_config_json Data = await JsonSerializer.DeserializeAsync<data_config_json>(fs);
                result = GetByteArrFromStringComma(Data.secret_key_for_game_servers);
            }

            return result;
        }


        public static async Task<string> InnerServerConnectionPassword()
        {
            string result = null;


            using (FileStream fs = new FileStream(starter.address_for_data_config, FileMode.OpenOrCreate))
            {
                data_config_json Data = await JsonSerializer.DeserializeAsync<data_config_json>(fs);
                result = Data.InnerServerConnectionPassword;

            }

            return result;
        }

        public static async Task<string> MysqlConnectionData_login()
        {
            string result = null;

            using (FileStream fs = new FileStream(starter.address_for_data_config, FileMode.OpenOrCreate))
            {
                data_config_json Data = await JsonSerializer.DeserializeAsync<data_config_json>(fs);
                result = Data.mysql_server_data;
            }

            return result;
        }




        struct data_config_json
        {
            public string mysql_server_data { get; set; }
            public string secret_key_for_game_servers { get; set; }
            public string InnerServerConnectionPassword { get; set; }


        }

    }
}
