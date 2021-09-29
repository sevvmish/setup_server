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

        /*
        public static void OrganizePVP(int _count, List<string> _char_id)
        {
                        
            List<string[]> char_d = new List<string[]>(_count);
            List<string[]> char_n = new List<string[]>(_count);
            string new_session_id = functions.get_random_set_of_symb(8);
            List<string> new_player_id_aka_ticket = new List<string>(_count);
            List<string> player_old_tickets = new List<string>(_count);
            int game_type_id = 0;

            switch(_count)
            {
                case 2:
                    game_type_id = 1; //type of PVP - 1vs1
                    break;
            }


            //Console.WriteLine("started organazing PVP!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

            try
            {
                for (int i = 0; i < _count; i++)
                {
                    string[,] char_d_ = mysql.GetMysqlSelect($"SELECT * FROM `character_property` WHERE `character_id`='{_char_id[i]}' ").Result;
                    string[] temp = new string[char_d_.GetLength(1)];
                    for (int ii = 0; ii < char_d_.GetLength(1); ii++)
                    {
                        temp[ii] = char_d_[0, ii];
                    }
                    char_d.Add(temp);
                    //================================
                    string[,] char_n_ = mysql.GetMysqlSelect($"SELECT `character_name`,`character_type` FROM `characters` WHERE `character_id`='{_char_id[i]}' ").Result;
                    string[] temp1 = new string[char_n_.GetLength(1)];
                    for (int ii = 0; ii < char_n_.GetLength(1); ii++)
                    {
                        temp1[ii] = char_n_[0, ii];
                    }
                    char_n.Add(temp1);
                    
                    new_player_id_aka_ticket.Add(functions.get_random_set_of_symb(8));
                    player_old_tickets.Add(functions.GetTicketByCharID(_char_id[i]));                    
                }

                //send data to gamehub1 to create start table
                string send_table_data = $"0~5~{starter.InnerServerConnectionPassword}~CREATE TABLE `{new_session_id}` (`player_order` int(11), `player_id` varchar(10), `player_name` varchar(20),`player_class` tinyint(4),`connection_number` varchar(25),`team_id` int(1), `game_type_id` int(1),`zone_type` tinyint(2),`position_x` float,`position_y` float,`position_z` float,`rotation_x` float,`rotation_y` float,`rotation_z` float,`speed` float,`animation_id` tinyint(2),`conditions` varchar(255),`health_pool` varchar(13),`energy` float,`health_regen` float,`energy_regen` float,`weapon_attack` varchar(10),`hit_power` float,`armor` float,`shield_block` float,`magic_resistance` float,`dodge` float,`cast_speed` float,`melee_crit` float,`magic_crit` float,`spell_power` float,`spell1` smallint(6),`spell2` smallint(6),`spell3` smallint(6),`spell4` smallint(6),`spell5` smallint(6),`spell6` smallint(6),`hidden_conds` varchar(255),`global_button_cooldown` tinyint(2)) ENGINE = InnoDB DEFAULT CHARSET = utf8; ";
                
                //CHECK IT!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                string res_creating_table = Server.SendAndGetTCP_between_servers(send_table_data, starter.GameServerPort, starter.GameServerHUBs["HUB_2"], true);
                Console.WriteLine(res_creating_table + " =========================!");

                //send data to gamehub1 to add players data
                string send_players_data = $"0~5~{starter.InnerServerConnectionPassword}~INSERT INTO `{new_session_id}` VALUES";
                

                int zone_type = 1;

                for (int i = 0; i < _count; i++)
                {
                    if (i > 0) { send_players_data = send_players_data + ","; }

                    send_players_data = send_players_data + $" ('{(i+1)}', '{new_player_id_aka_ticket[i]}','{char_n[i].GetValue(0)}','{char_n[i].GetValue(1)}','0','{i}','{game_type_id}','{zone_type}',2,0,2,0,0,0,'{char_d[i].GetValue(1)}',0,'','{char_d[i].GetValue(2)}={char_d[i].GetValue(2)}',100,'{char_d[i].GetValue(3)}','{char_d[i].GetValue(4)}','{char_d[i].GetValue(5)}','{char_d[i].GetValue(6)}','{char_d[i].GetValue(7)}','{char_d[i].GetValue(8)}','{char_d[i].GetValue(9)}','{char_d[i].GetValue(10)}','{char_d[i].GetValue(11)}','{char_d[i].GetValue(12)}','{char_d[i].GetValue(13)}','{char_d[i].GetValue(14)}','{char_d[i].GetValue(15)}','{char_d[i].GetValue(16)}','{char_d[i].GetValue(17)}','{char_d[i].GetValue(18)}','{char_d[i].GetValue(19)}',997,'{char_d[i].GetValue(21)}',0)";

                    if (i == (_count - 1)) { send_players_data = send_players_data + ";"; }
                }

                //CHECK IT!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                string res_sending_players_data = Server.SendAndGetTCP_between_servers(send_players_data, starter.GameServerPort, starter.GameServerHUBs["HUB_2"], true);
                Console.WriteLine(send_players_data + " =========================!");

                //send data to start this session
                //CHECK IT!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                string res_starting_new_session = Server.SendAndGetTCP_between_servers($"0~2~{starter.InnerServerConnectionPassword}~{new_session_id}", starter.GameServerPort, starter.GameServerHUBs["HUB_2"], true);
                

                //preparing awaiting
                if (res_creating_table=="0~5~ok" && res_sending_players_data== "0~5~ok" && res_starting_new_session=="0~2~1")
                {
                   
                    for (int i = 0; i < _count; i++)
                    {                        
                        Server.TemporaryDataForStartingGameSession.Add(player_old_tickets[i], new Player_data(char_n[i].GetValue(0).ToString(), _char_id[i], new_session_id, new_player_id_aka_ticket[i], player_old_tickets));
                        
                        
                    }
                    string sess_ty = "";
                    switch (_count)
                    {
                        case 2:
                            sess_ty = "1";
                            break;
                    }
                    Console.WriteLine(player_old_tickets.Count + " - dimention");
                    Task.Run(() => CheckForStartingGameSession(player_old_tickets, sess_ty));

                    StringBuilder list_of_chars = new StringBuilder();

                    for (int i = 0; i < _count; i++)
                    {
                        list_of_chars.Append(char_n[i] + ", ");
                        
                    }

                    Console.WriteLine(DateTime.Now + ": started organazing PVP for " + list_of_chars);
                } 
                else
                {
                                        
                }
            }
            catch (Exception ex)
            {
               
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }


        }
        

        public async static void CheckForStartingGameSession(List<string> _tickets, string session_type_id)
        {
            await Task.Delay(10000);

            for (int i = 0; i < _tickets.Count; i++)
            {
                if (!Server.TemporaryDataForStartingGameSession.ContainsKey(_tickets[i]))
                {
                    _tickets.Remove(_tickets[i]);
                }
            }

           
            bool isOK = true;
            for (int i = 0; i < _tickets.Count; i++)
            {
                if (!Server.TemporaryDataForStartingGameSession[_tickets[i]].is_ready)
                {
                    isOK = false;
                    break;
                }
            }

            if (isOK)
            {
                for (int i = 0; i < _tickets.Count; i++)
                {
                        
                    bool creating_result = mysql.ExecuteSQLInstruction($"DELETE FROM `session_queue` WHERE `character_id`= '{Server.TemporaryDataForStartingGameSession[_tickets[i]].character_id}' ").Result;
                    bool creating_result1 = mysql.ExecuteSQLInstruction($"INSERT INTO `session_archive`(`session_id`, `session_arch_type_id`, `characters_ids`, `session_type_id`, `when_created`, `last_change_date`) VALUES('{Server.TemporaryDataForStartingGameSession[_tickets[i]].session_id}',1,'{Server.TemporaryDataForStartingGameSession[_tickets[i]].character_id}','{session_type_id}','{DateTime.Now}','{DateTime.Now}')").Result;
                    Server.TemporaryDataForStartingGameSession.Remove(_tickets[i]);
                }                    
            } 
            else
            {
                for (int i = 0; i < _tickets.Count; i++)
                {

                    bool creating_result = mysql.ExecuteSQLInstruction($"DELETE FROM `session_queue` WHERE `character_id`= '{Server.TemporaryDataForStartingGameSession[_tickets[i]].character_id}' ").Result;
                    Server.TemporaryDataForStartingGameSession.Remove(_tickets[i]);

                }
            }           
        }
        */

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
