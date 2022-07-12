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

        public static ushort GetRandomUShort()
        {
            Random rnd = new Random();
            return (ushort)rnd.Next(10000, 65500);
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


        


        public static void AddOrUpdateVisitors(string ticket, string name, string char_id)
        {
            
            //add or refresh data about visitor========

            if (!Server.FindCharacterByID.ContainsKey(char_id))
            {
                Server.FindCharacterByID.Add(char_id, ticket);
            }
            else
            {
                Server.FindCharacterByID[char_id] = ticket;
            }

            

            if (!Server.CurrentVisitors.ContainsKey(ticket))
            {

                Server.CurrentVisitors.Add(ticket, new VisitorData(name, ticket, null, char_id));
            }
            else
            {
                Server.CurrentVisitors[ticket].Update();
            }
            //========================================
        }

        public static bool CheckVisitorStateByID(string ID)
        {
            if (Server.FindCharacterByID.ContainsKey(ID) && Server.CurrentVisitors.ContainsKey(Server.FindCharacterByID[ID]))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
