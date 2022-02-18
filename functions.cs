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



            if (talents_arr.GetLength(0) != 8 || talents_arr.GetLength(1) != 3)
            {
                return false;
            }

            //check max
            int sum = 0;
            foreach (int item in talents_arr)
            {
                sum += item;
            }
            if (sum > MaxTalents)
            {
                return false;
            }
            //uniques
            if ((talents_arr[2, 0] + talents_arr[2, 1]) > 1)
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
            if ((talents_arr[0, 0] + talents_arr[1, 0]) < 2 && talents_arr[2, 0] == 1)
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
            if ((talents_arr[0, 1] + talents_arr[1, 1] + talents_arr[2, 1] + talents_arr[3, 1] + talents_arr[4, 1] + talents_arr[5, 1] + talents_arr[6, 1]) < 6 && talents_arr[7, 1] == 1)
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

        public static void ReAssessExperienceByCharID(string charID)
        {
                        
            string[,] all_data = mysql.GetMysqlSelect($"SELECT `session_archive_id`, `session_type_id`, `score`, `when_ended` FROM `session_archive` WHERE (`character_id`='{charID}' AND `is_checked`='0')").Result;

            if (all_data.GetLength(0) == 0 || all_data[0, 0]=="error")
            {                
                return;
            }
                                          
            string[,] old_data_raiting = mysql.GetMysqlSelect($"SELECT `pvp_raiting`, `pve_raiting`, `xp_points` FROM `character_raiting` WHERE `character_id`='{charID}'").Result;

            if (old_data_raiting.GetLength(0) == 0 || all_data[0, 0] == "error")
            {
                Console.WriteLine(DateTime.Now + ": error in getting data from raiting table for character " + charID);
                return;
            }

            int PVPscoreToAdd = int.Parse(old_data_raiting[0, 0]);
            int PVEscoreToAdd = int.Parse(old_data_raiting[0, 1]);
            int EXPtoAdd = int.Parse(old_data_raiting[0, 2]);

            for (int i = 0; i < all_data.GetLength(0); i++)
            {
                try
                {

                    if (all_data[i, 3] == "awaiting")
                    {
                        Console.WriteLine(DateTime.Now + " error - no end date for archive game for player " + charID);
                        continue;
                    }

                    string session_archive_id = all_data[i, 0];
                    int session_type_id = int.Parse(all_data[i, 1]);
                    int score = int.Parse(all_data[i, 2]);

                    //make is_checked to true
                    Task.Run(() => mysql.ExecuteSQLInstruction($"UPDATE `session_archive` SET `is_checked`='1' WHERE `session_archive_id`='{session_archive_id}'").Result);
                                      
                    EXPtoAdd += GetXP(score, session_type_id);
                    bool isPVP = isSessionTypePVP(session_type_id);

                    if (isPVP)
                    {
                        PVPscoreToAdd += GetRaiting(score, session_type_id);
                    }
                    else
                    {
                        PVEscoreToAdd = GetRaiting(score, session_type_id);
                    }                                        

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    return;
                }
            }

            PVPscoreToAdd = PVPscoreToAdd < 0 ? 0 : PVPscoreToAdd;
            PVEscoreToAdd = PVEscoreToAdd < 0 ? 0 : PVEscoreToAdd;
            EXPtoAdd = EXPtoAdd < 0 ? 0 : EXPtoAdd;

            bool result = mysql.ExecuteSQLInstruction($"UPDATE `character_raiting` SET `pvp_raiting`='{PVPscoreToAdd}',`pve_raiting`='{PVEscoreToAdd}',`xp_points`='{EXPtoAdd}' WHERE `character_id`='{charID}'").Result;

            if (!result)
            {
                Console.WriteLine(DateTime.Now + ": error trying update new raiting data to character " + charID);
            }
        }


        //DATA for XP and raiting assessing===============================
        public static int XPforLostBattle = 100;
        //================================================================

        public static int GetXP(int _points, int session_type)
        {
            switch (session_type)
            {
                case 1:
                    if (_points == 0) return 100;
                    if (_points == 1) return 200;
                    if (_points == 2) return 300;
                    if (_points == 3) return 400;
                    break;
                case 2:

                    break;
                case 3:

                    break;
            }

            return 0;
        }

        public static int GetRaiting(int _points, int session_type)
        {
            switch(session_type)
            {
                case 1:
                    if (_points == 0) return -1;
                    if (_points == 1) return 0;
                    if (_points == 2) return 2;
                    if (_points == 3) return 3;
                    break;
                case 2:

                    break;
                case 3:

                    break;
            }

            return 0;
        }

        public static bool isSessionTypePVP(int session_type)
        {
            List<int> types = new List<int> { 1, 2 };

            if (types.Contains(session_type))
            {
                return true;
            }
            else
            {
                return false;
            }
                        
        }

        public static bool isSessionTypePVE(int session_type)
        {
            List<int> types = new List<int> { 5, 6 };

            if (types.Contains(session_type))
            {
                return true;
            }
            else
            {
                return false;
            }

        }


        public static void AddOrUpdateVisitors(string ticket, string name)
        {

            //add or refresh data about visitor========

            if (!Server.FindCharacterNameByTicket.ContainsKey(name))
            {
                Server.FindCharacterNameByTicket.Add(name, ticket);
            }
            else
            {
                Server.FindCharacterNameByTicket[name] = ticket;
            }



            if (!Server.CurrentVisitors.ContainsKey(ticket))
            {

                Server.CurrentVisitors.Add(ticket, new VisitorData(name, ticket, null));
            }
            else
            {
                Server.CurrentVisitors[ticket].Update();
            }
            //========================================
        }

    }
}
