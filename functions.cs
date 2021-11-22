using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace setup_server
{
    class functions
    {
        public static string get_random_set_of_symb(int nub_of_symb)
        {
            string[] arr_name = { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p", "a", "s", "d", "f", "g", "h", "j", "k", "l", "z", "x", "c", "v", "b", "n", "m", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "A", "S", "D", "F", "G", "H", "J", "K", "L", "Z", "X", "C", "V", "B", "N", "M", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };
            string result = "";
            Random rnd = new Random();
            for (int i = 0; i < nub_of_symb; i++)
            {
                result = result + arr_name[rnd.Next(0, arr_name.Length - 1)];
            }

            return result;
        }

      
        public static string GetTicketByCharID(string charID)
        {            
            string[,] result = mysql.GetMysqlSelect($"SELECT `ticket_id` FROM `users` WHERE `user_id`= (SELECT characters.user_id FROM characters WHERE characters.character_id = {charID})").Result;
            
            return result[0, 0];
        }

        public static bool ChangePacketInPlayer(string old_packet, string new_packet)
        {
            if (Server.Sessions.ContainsKey(old_packet))
            {
                byte[] secret = Server.Sessions[old_packet];
                Server.Sessions.Remove(old_packet);
                Server.Sessions.Add(new_packet, secret);
                return true;
            }

            return false;
        }

        public static bool ChangeTicketInPlayer(string old_ticket, string new_ticket)
        {
            bool creating_result = mysql.ExecuteSQLInstruction($"UPDATE `users` SET `ticket_id`='{new_ticket}' WHERE `ticket_id`='{old_ticket}'").Result;

            return creating_result;
        }

        public static bool check_new_talents(string talents_data)
        {
            //0:0, 1:0, 2:0, 3:0, 4:0, 5:0, 6:0, 7:0
            //0:1, 1:1, 2:1, 3:1, 4:1, 5:1, 6:1, 7:1
            //0:2, 1:2,      3:2, 4:2,      6:2, 7:2

            int[,] talents_arr;            
            int MaxTalents = 11;

            FromStringToArrTalents(out talents_arr, talents_data);

            

            if (talents_arr.GetLength(0)!=8 || talents_arr.GetLength(1)!=3)
            {
                return false;
            }

            //check max
            int sum = 0;
            foreach (int item in talents_arr)
            {
                sum += item;
            }
            if (sum>MaxTalents)
            {
                return false;
            }
            //uniques
            if ((talents_arr[2, 0] + talents_arr[2, 1])>1)
            {
                return false;
            }
            if ((talents_arr[5, 0] + talents_arr[5, 1]) > 1)
            {
                return false;
            }
            if ((talents_arr[7, 0] + talents_arr[7, 1] + talents_arr[7, 2]) > 1)
            {
                return false;
            }
            //get unique 2
            if ((talents_arr[0,0] + talents_arr[1, 0]) < 2 && talents_arr[2, 0]==1)
            {
                return false;
            }
            if ((talents_arr[0, 1] + talents_arr[1, 1]) < 2 && talents_arr[2, 1] == 1)
            {
                return false;
            }
            //get unique 5
            if ((talents_arr[0, 0] + talents_arr[1, 0] + talents_arr[2, 0] + talents_arr[3, 0] + talents_arr[4, 0]) < 4 && talents_arr[5, 0] == 1)
            {
                return false;
            }
            if ((talents_arr[0, 1] + talents_arr[1, 1] + talents_arr[2, 1] + talents_arr[3, 1] + talents_arr[4, 1]) < 4 && talents_arr[5, 1] == 1)
            {
                return false;
            }
            //check for last talents
            if ((talents_arr[0, 0] + talents_arr[1, 0] + talents_arr[2, 0] + talents_arr[3, 0] + talents_arr[4, 0] + talents_arr[5, 0] + talents_arr[6, 0]) < 6 && talents_arr[7, 0] == 1)
            {
                return false;
            }
            if ((talents_arr[0, 1] + talents_arr[1, 1] + talents_arr[2,1] + talents_arr[3, 1] + talents_arr[4, 1] + talents_arr[5, 1] + talents_arr[6, 1]) < 6 && talents_arr[7, 1] == 1)
            {
                return false;
            }
            if ((talents_arr[0, 2] + talents_arr[1, 2] + talents_arr[3, 2] + talents_arr[4, 2] + talents_arr[6, 2]) < 5 && talents_arr[7, 2] == 1)
            {
                return false;
            }


            return true;
        }


        private static string FromArrToStringTalents(int[,] TalentsSpread)
        {
            string result = "0-0-0,0-0-0,0-0,0-0-0,0-0-0,0-0,0-0-0,0-0-0";

            result = TalentsSpread[0, 0] + "-" + TalentsSpread[0, 1] + "-" + TalentsSpread[0, 2] + "," +
                TalentsSpread[1, 0] + "-" + TalentsSpread[1, 1] + "-" + TalentsSpread[1, 2] + "," +
                TalentsSpread[2, 0] + "-" + TalentsSpread[2, 1] + "," +
                TalentsSpread[3, 0] + "-" + TalentsSpread[3, 1] + "-" + TalentsSpread[3, 2] + "," +
                TalentsSpread[4, 0] + "-" + TalentsSpread[4, 1] + "-" + TalentsSpread[4, 2] + "," +
                TalentsSpread[5, 0] + "-" + TalentsSpread[5, 1] + "," +
                TalentsSpread[6, 0] + "-" + TalentsSpread[6, 1] + "-" + TalentsSpread[6, 2] + "," +
                TalentsSpread[7, 0] + "-" + TalentsSpread[7, 1] + "-" + TalentsSpread[7, 2];

            return result;
        }

        private static void FromStringToArrTalents(out int[,] TalentsSpread, string talents_string)
        {
            TalentsSpread = new int[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } };
            string[] get_talents = talents_string.Split(',');
            List<string[]> Rows = new List<string[]>();

            for (int i = 0; i < get_talents.Length; i++)
            {
                string[] current_row = get_talents[i].Split('-');
                Rows.Add(current_row);
            }

            TalentsSpread[0, 0] = int.Parse(Rows[0].GetValue(0).ToString());
            TalentsSpread[0, 1] = int.Parse(Rows[0].GetValue(1).ToString());
            TalentsSpread[0, 2] = int.Parse(Rows[0].GetValue(2).ToString());

            TalentsSpread[1, 0] = int.Parse(Rows[1].GetValue(0).ToString());
            TalentsSpread[1, 1] = int.Parse(Rows[1].GetValue(1).ToString());
            TalentsSpread[1, 2] = int.Parse(Rows[1].GetValue(2).ToString());

            TalentsSpread[2, 0] = int.Parse(Rows[2].GetValue(0).ToString());
            TalentsSpread[2, 1] = int.Parse(Rows[2].GetValue(1).ToString());

            TalentsSpread[3, 0] = int.Parse(Rows[3].GetValue(0).ToString());
            TalentsSpread[3, 1] = int.Parse(Rows[3].GetValue(1).ToString());
            TalentsSpread[3, 2] = int.Parse(Rows[3].GetValue(2).ToString());

            TalentsSpread[4, 0] = int.Parse(Rows[4].GetValue(0).ToString());
            TalentsSpread[4, 1] = int.Parse(Rows[4].GetValue(1).ToString());
            TalentsSpread[4, 2] = int.Parse(Rows[4].GetValue(2).ToString());

            TalentsSpread[5, 0] = int.Parse(Rows[5].GetValue(0).ToString());
            TalentsSpread[5, 1] = int.Parse(Rows[5].GetValue(1).ToString());

            TalentsSpread[6, 0] = int.Parse(Rows[6].GetValue(0).ToString());
            TalentsSpread[6, 1] = int.Parse(Rows[6].GetValue(1).ToString());
            TalentsSpread[6, 2] = int.Parse(Rows[6].GetValue(2).ToString());

            TalentsSpread[7, 0] = int.Parse(Rows[7].GetValue(0).ToString());
            TalentsSpread[7, 1] = int.Parse(Rows[7].GetValue(1).ToString());
            TalentsSpread[7, 2] = int.Parse(Rows[7].GetValue(2).ToString());

        }

        

    }
}
