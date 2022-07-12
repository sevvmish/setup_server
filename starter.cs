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
        public const string CLIENT_VERSION = "1.1.3";
        public const int MAX_GAME_HUBS = 4;
        
        public static byte[] secret_key_for_game_servers;
        public static string InnerServerConnectionPassword;
        public static string MysqlConnectionData_login;
        public static string address_for_data_config = @"/home/admin/data"; //@"C:\android\data"  @"/home/admin/data"

        public static Stopwatch stopWatch = new Stopwatch();

        //game servers
        public static int GameServerPort = 2328;       
        public static Dictionary<string, GameHubsSpec> GameServerHUBs = new Dictionary<string, GameHubsSpec>();
        

        static void Main(string[] args)
        {            
            data_config.Init_data_config();            
            Thread.Sleep(2000);
            
            
            Server.Server_init_TCP_UDP();
            

            Console.ReadKey();
        }

        private static async void test()
        {
            await Task.Delay(3000);
            /*
            for (int i = 0; i < 500; i++)
            {
                Server.SendTCP_between_servers("hjgvfhjgfcvgh", 2328, "----", false);
                await Task.Delay(50);
            }
            */
            
        }


    }
}
