using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace setup_server
{
    class starter
    {
        public const int passlog_min_lenth = 8;
        public const int passlog_max_lenth = 16;
        public const int charname_min_lenth = 6;
        public const int charname_max_lenth = 16;

        public static byte[] secret_key_for_game_servers;
        public static string InnerServerConnectionPassword;
        public static string MysqlConnectionData_login;
        public static string address_for_data_config = @"C:\android\data";

        public static Stopwatch stopWatch = new Stopwatch();

        //game servers
        public static int GameServerPort = 2323;       
        public static Dictionary<string, GameHubsSpec> GameServerHUBs = new Dictionary<string, GameHubsSpec>();
        

        static void Main(string[] args)
        {
            stopWatch.Start();
            data_config.Init_data_config();
          
            Server.Server_init_TCP();

            Console.ReadKey();

        }
    }
}
