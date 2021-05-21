using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace setup_server
{
    class packet_analyzer
    {
        private static Dictionary<string, encryption> TemporarySessionCreator = new Dictionary<string, encryption>();

        public static void test()
        {
            
           

        }

        public static string ProcessTCPInput(string data, string endpoint_address)
        {
            try
            {
            
                string[] packet_data = data.Split('~');
                //Console.WriteLine(data);

                //receive data about character incl all stats
                if (packet_data.Length == 4 && (packet_data[0] + packet_data[1]) == "20")
                {

                    if (!StringChecker(packet_data[2]) || !StringChecker(packet_data[3]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 2~0~wds to user from - " + endpoint_address);
                        return $"2~0~wds"; //wrong digits or signs                    
                    }

                    string[,] get_char_data = mysql.GetMysqlSelect($"SELECT `speed`, `health`, `health_regen`, `energy_regen`, `weapon_attack`, `hit_power`, `armor`, `shield_block`, `magic_resistance`, `dodge`, `cast_speed`, `melee_crit`, `magic_crit`, `spell_power`, `spell1`, `spell2`, `spell3`, `spell4`, `spell5`, `spell6`, `spell_book`, `talents` FROM `character_property` WHERE character_property.character_id = (SELECT characters.character_id FROM characters WHERE characters.character_name = '{packet_data[3]}')  AND((SELECT users.user_id FROM users WHERE users.ticket_id = '{packet_data[2]}') = (SELECT characters.user_id FROM characters WHERE characters.character_name = '{packet_data[3]}')) ").Result;

                    if (get_char_data.GetLength(0)==0)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 2~0~nd to user from - " + endpoint_address);
                        return $"2~0~nd";
                    }

                    string result = "";

                    for (int i = 0; i <= 21; i++)
                    {
                        result = result + get_char_data[0, i] + "~";
                    }

                    Console.WriteLine(DateTime.Now + ": char " + packet_data[3] + " - desciption send to user ticket " + packet_data[2] + " from " + endpoint_address);
                    return $"2~0~{result}";
                }

                //start queue for 1vs1 PVP
                //3~1~ticket~character
                if (packet_data.Length == 4 && (packet_data[0] + packet_data[1]) == "31")
                {

                    if (!StringChecker(packet_data[2]) || !StringChecker(packet_data[3]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 3~1~wds to user from - " + endpoint_address);
                        return $"3~1~wds"; //wrong digits or signs                    
                    }

                    string[,] check_ticket_and_name = mysql.GetMysqlSelect($"SELECT `character_name` FROM `characters` WHERE(`character_name`= '{packet_data[3]}' AND characters.user_id = (SELECT user_id FROM users WHERE users.ticket_id = '{packet_data[2]}'))").Result;
                    if (check_ticket_and_name.GetLength(0) != 1 || check_ticket_and_name[0, 0] != packet_data[3])
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 3~1~eit to user from - " + endpoint_address);
                        return $"3~1~eit";
                    }

                    //check if queue exists and make OFF=======================
                    string[,] char_id = mysql.GetMysqlSelect($"SELECT characters.character_id FROM characters WHERE characters.character_name='{packet_data[3]}'").Result;
                    string[,] check_char_name = mysql.GetMysqlSelect($"SELECT `character_id`,`session_type_id` FROM `session_queue` WHERE `character_id`='{char_id[0,0]}'").Result;
                    if (check_char_name.GetLength(0)!=0)
                    {
                        bool del_char_name = mysql.ExecuteSQLInstruction($"DELETE FROM `session_queue` WHERE `character_id`=(SELECT characters.character_id FROM characters WHERE characters.character_name='{packet_data[3]}')").Result;
                        if (!del_char_name)
                        {
                            Console.WriteLine(DateTime.Now + ": send problem 3~1~dbe to user from - " + endpoint_address);
                            return $"3~1~dbe";
                        } else
                        {
                            Console.WriteLine(DateTime.Now + ": stopped awaiting queue pvp 1vs1 with " + packet_data[2] + " - " + endpoint_address);
                            return $"3~1~out~1";
                        }
                    }
                    //===========================================================OFF

                    //organize new pvp queue for player                    
                    bool ins_char_name = mysql.ExecuteSQLInstruction($"INSERT INTO `session_queue`(`session_type_id`, `character_id`, `character_pvp_rait`, `status`, `time_of_start`) VALUES('1', (SELECT characters.character_id FROM characters WHERE characters.character_name = '{packet_data[3]}'), (SELECT character_raiting.character_pvp_rait FROM character_raiting WHERE character_raiting.character_id = (SELECT characters.character_id FROM characters WHERE characters.character_name = '{packet_data[3]}')), '0', '{DateTime.Now}')").Result;
                    if (!ins_char_name)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 3~1~dbe to user from - " + endpoint_address);
                        return $"3~1~dbe";
                    } else
                    {                        
                        Server.pvp1vs1.Add(char_id[0, 0], DateTime.Now);
                        Console.WriteLine(DateTime.Now + ": started awaiting queue pvp 1vs1 with ticket " + packet_data[2] + " from " + endpoint_address);
                        return $"3~1~in~1";
                    }

                }


                //implement new talents
                //3~4~ticket~character~new talents
                if (packet_data.Length == 5 && (packet_data[0] + packet_data[1]) == "34")
                {

                    if (!StringChecker(packet_data[2]) || !StringChecker(packet_data[3]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 3~4~wds to user from - " + endpoint_address);
                        return $"3~4~wds"; //wrong digits or signs                    
                    }

                    if (!functions.check_new_talents(packet_data[4]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 3~4~eit to user from - " + endpoint_address);
                        return $"3~4~eit";
                    }

                    string[,] check_ticket_and_name = mysql.GetMysqlSelect($"SELECT `character_name` FROM `characters` WHERE(`character_name`= '{packet_data[3]}' AND characters.user_id = (SELECT user_id FROM users WHERE users.ticket_id = '{packet_data[2]}'))").Result;
                    if (check_ticket_and_name.GetLength(0)!=1 || check_ticket_and_name[0,0]!= packet_data[3])
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 3~4~eit to user from - " + endpoint_address);
                        return $"3~4~eit";
                    }

                    string[,] what_type_char = mysql.GetMysqlSelect($"SELECT `character_type` FROM `characters` WHERE `character_name`= '{packet_data[3]}'").Result;
                    string[,] get_pl_data = mysql.GetMysqlSelect($"SELECT `speed`, `health`, `health_regen`, `energy_regen`, `weapon_attack`, `hit_power`, `armor`, `shield_block`, `magic_resistance`, `dodge`, `cast_speed`, `melee_crit`, `magic_crit`, `spell_power`, `spell1`, `spell2`, `spell3`, `spell4`, `spell5`, `spell6`, `hidden_conds`, `spell_book`, `talents` FROM `character_types` WHERE `character_type`='{what_type_char[0,0]}'").Result;

                    List<string> temp_data = new List<string>();
                    for (int i = 0; i < get_pl_data.GetLength(1); i++)
                    {
                        temp_data.Add(get_pl_data[0,i]);
                    }
                    talents_setup new_player_data = new talents_setup(packet_data[4], int.Parse(what_type_char[0,0]), temp_data.ToArray());
                    //new_player_data.talents = packet_data[4];

                    
                    bool creating_result = mysql.ExecuteSQLInstruction(new_player_data.prepare_to_update_sql(packet_data[3])).Result;
                    if (!creating_result)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 3~4~err to user from - " + endpoint_address);
                        return $"3~4~err";
                    }

                    Console.WriteLine(DateTime.Now + ": new talents of char " + packet_data[3] + " implemented to ticket " + packet_data[2] + " from " + endpoint_address);
                    return $"3~4~ok";
                }


                //implement spells in row
                //3~5~ticket~character~new spells
                if (packet_data.Length == 5 && (packet_data[0] + packet_data[1]) == "35")
                {

                    if (!StringChecker(packet_data[2]) || !StringChecker(packet_data[3]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 3~5~wds to user from - " + endpoint_address);
                        return $"3~5~wds"; //wrong digits or signs                    
                    }

                    
                    string[,] check_ticket_and_name = mysql.GetMysqlSelect($"SELECT `character_name` FROM `characters` WHERE(`character_name`= '{packet_data[3]}' AND characters.user_id = (SELECT user_id FROM users WHERE users.ticket_id = '{packet_data[2]}'))").Result;
                    if (check_ticket_and_name.GetLength(0) != 1 || check_ticket_and_name[0, 0] != packet_data[3])
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 3~5~eit to user from - " + endpoint_address);
                        return $"3~5~eit";
                    }

                    //get current spell book
                    string[,] get_spell_book = mysql.GetMysqlSelect($"SELECT `spell_book` FROM `character_property` WHERE character_property.character_id=(SELECT characters.character_id FROM characters WHERE characters.character_name='{packet_data[3]}' )").Result;
                    if (get_spell_book.GetLength(0)!=1)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 3~5~err to user from - " + endpoint_address);
                        return $"3~5~err";
                    }
                    string[] pre_spell_book = get_spell_book[0, 0].Split(',');
                    List<string> spell_book_list = new List<string>();
                    for (int i = 0; i < pre_spell_book.Length; i++)
                    {
                        spell_book_list.Add(pre_spell_book[i]);
                    }

                    //check if received spells exists
                    string[] received_spells = packet_data[4].Split(',');
                    
                    bool isOK = true;
                    for (int i = 0; i < received_spells.Length; i++)
                    {
                        if (!spell_book_list.Contains(received_spells[i]))
                        {
                            isOK = false;
                            Console.WriteLine(DateTime.Now + ": spell is not in spell book from - " + endpoint_address);
                            break;
                        }
                    }
                    if (!isOK)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 3~5~nss to user from - " + endpoint_address);
                        return $"3~5~nss";
                    }

                    //check repeiting spells
                    isOK = true;
                    for (int i = 1; i < 6; i++)
                    {
                        for (int ii = 0; ii < i; ii++)
                        {
                            if (received_spells[i]==received_spells[ii] && received_spells[i]!="0")
                            {
                                
                                isOK = false;
                                break;                                
                            }
                        }
                    }
                    if (!isOK)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 3~5~rsn to user from - " + endpoint_address);
                        return $"3~5~rsn";
                    }

                    //update spells
                    bool creating_result = mysql.ExecuteSQLInstruction($"UPDATE `character_property` SET `spell1`= '{received_spells[0]}', `spell2`= '{received_spells[1]}', `spell3`= '{received_spells[2]}', `spell4`= '{received_spells[3]}', `spell5`= '{received_spells[4]}', `spell6`= '{received_spells[5]}' WHERE `character_id`= (SELECT characters.character_id FROM characters WHERE characters.character_name = '{packet_data[3]}')").Result;
                    if (!creating_result)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 3~5~err to user from - " + endpoint_address);
                        return $"3~5~err";
                    }

                    Console.WriteLine(DateTime.Now + ": new spell set for char "+ packet_data[3] + " implemented to ticket" + packet_data[2] + " from " + endpoint_address);
                    return $"3~5~ok";
                }

                //getting awaiting requests for receiving game session
                //4~0~ticket~char_name
                //answer 4~0~status~ticket~session~HUB
                //status 0 - nothng good, 3 - OK sending data and play
                if (packet_data.Length == 4 && (packet_data[0] + packet_data[1]) == "40")
                {

                    if (!StringChecker(packet_data[2]) || !StringChecker(packet_data[3]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 4~0~0~wds to user from - " + endpoint_address);
                        return $"4~0~0~wds"; //wrong digits or signs                    
                    }

                    

                    //get character id and update pvp1vs1 datetime in queue OR stop queue if no such char in 1vs1pvp queue
                    string[,] get_char_id = mysql.GetMysqlSelect($"SELECT `character_id` FROM `characters` WHERE `character_name`='{packet_data[3]}'").Result;
                    if (get_char_id.GetLength(0) != 1)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 4~0~0~nst to user from - " + endpoint_address);
                        return $"4~0~0~nst"; 
                    }

                    if (Server.pvp1vs1.ContainsKey(get_char_id[0,0]))
                    {
                        Server.pvp1vs1[get_char_id[0, 0]] = DateTime.Now;
                    }

                    if (!Server.pvp1vs1.ContainsKey(get_char_id[0, 0]))
                    {
                        bool creating_result = mysql.ExecuteSQLInstruction($"DELETE FROM `session_queue` WHERE `character_id`= 'get_char_id[0, 0]' ").Result;
                        if (Server.TemporaryDataForStartingGameSession.ContainsKey(packet_data[3])) { 
                            Server.TemporaryDataForStartingGameSession.Remove(packet_data[3]); 
                        }

                        Console.WriteLine(DateTime.Now + ": send problem -no such player awaits queue, queue stopped- to user " + packet_data[3] + " from " + endpoint_address);
                        
                    }
                    //==========================================

                    if (!Server.TemporaryDataForStartingGameSession.ContainsKey(packet_data[2]))
                    {
                        Console.WriteLine(DateTime.Now + ": send 4~0~0~0 to user from - " + endpoint_address);
                        return $"4~0~0~0";
                    }

                    if (Server.TemporaryDataForStartingGameSession[packet_data[2]].character_name != packet_data[3])
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 4~0~0~nsc to user from - " + endpoint_address);
                        return $"4~0~0~nsc";
                    }

                    if (!Server.TemporaryDataForStartingGameSession[packet_data[2]].is_ready)
                    {
                        Console.WriteLine(DateTime.Now + ": player is ready " + packet_data[2] + " - " + endpoint_address);
                        Server.TemporaryDataForStartingGameSession[packet_data[2]].PlayerIsReady();
                    }
                    
                    bool isOK = true;
                    for (int i = 0; i < Server.TemporaryDataForStartingGameSession[packet_data[2]].OtherPlayerTickets.Count; i++)
                    {
                        if (!Server.TemporaryDataForStartingGameSession[Server.TemporaryDataForStartingGameSession[packet_data[2]].OtherPlayerTickets[i]].is_ready)
                        {
                            isOK = false;
                        }
                    }

                    

                    if (!isOK)
                    {
                        Console.WriteLine(DateTime.Now + ": send get ready 4~0~2~0 to user from - " + endpoint_address);
                        return $"4~0~2~0"; //GGEEETTT RRREEAAAADDDYYY
                    } 
                    else
                    {
                        //bool creating_result = mysql.ExecuteSQLInstruction($"DELETE FROM `session_queue` WHERE `character_id`= '{Server.TemporaryDataForStartingGameSession[packet_data[2]].character_id}' ").Result;
                        Server.pvp1vs1.Remove(Server.TemporaryDataForStartingGameSession[packet_data[2]].character_id);
                        functions.ChangeTicketInPlayer(packet_data[2], Server.TemporaryDataForStartingGameSession[packet_data[2]].player_id);
                        Console.WriteLine(DateTime.Now + $": chanched old ticket {packet_data[2]} to new {Server.TemporaryDataForStartingGameSession[packet_data[2]].player_id}, started session {Server.TemporaryDataForStartingGameSession[packet_data[2]].session_id} to {endpoint_address}");
                        return $"4~0~3~{Server.TemporaryDataForStartingGameSession[packet_data[2]].player_id}~{Server.TemporaryDataForStartingGameSession[packet_data[2]].session_id}~{Server.TemporaryDataForStartingGameSession[packet_data[2]].GameHUB}";
                    }


                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }

            return "2~0~err";
            
        }

        public static string FromByteToString(byte [] data)
        {
            StringBuilder d = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                d.Append(data[i]);
            }

            return d.ToString();
        }

        public static void StartSessionTCPInput(string data, Socket player_socket)
        {
            try
            {
            
                string[] packet_data = data.Split('~');

                if (packet_data.Length >= 3 && (packet_data[0] + packet_data[1] + packet_data[2]) == "060")
                {
                    string code = functions.get_random_set_of_symb(5);
                    encryption session_encryption = new encryption();
                    Console.WriteLine(DateTime.Now + ": user requested encryption from - " + player_socket.RemoteEndPoint.ToString());
                    Server.SendDataTCP(player_socket, $"0~6~0~{code}~{session_encryption.publicKeyInString}");

                    if (!TemporarySessionCreator.ContainsKey(code))
                        TemporarySessionCreator.Add(code, session_encryption);
                    Task.Run(() => CleanTempSession(code));
                    
                    return;
                    //return $"0~6~0~{code}~{encoded secret key}";
                }

                if (packet_data.Length >= 3 && (packet_data[0] + packet_data[1] + packet_data[2]) == "061" && TemporarySessionCreator.ContainsKey(packet_data[3]))
                {
                    byte[] secret_key = TemporarySessionCreator[packet_data[3]].GetSecretKey(packet_data[4]);
                    Server.Sessions.Add(packet_data[3], secret_key);
                    TemporarySessionCreator[packet_data[3]].Dispose();
                    TemporarySessionCreator.Remove(packet_data[3]);
                    Console.WriteLine(DateTime.Now + ": user received encryption and accepted - " + player_socket.RemoteEndPoint.ToString());
                    Server.SendDataTCP(player_socket, $"0~6~1~ok");
                    
                    return;
                    //return $"0~6~1~OK";
                }

                if (packet_data.Length == 4 && (packet_data[0] + packet_data[1] + packet_data[2]) == "062" && Server.Sessions.ContainsKey(packet_data[3]))
                {
                    Server.Sessions.Remove(packet_data[3]);
                    Console.WriteLine(DateTime.Now + ": user removed from current encryption - " + player_socket.RemoteEndPoint.ToString());
                    Server.SendDataTCP(player_socket, $"0~6~2~ok");
                    
                    return;                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }
            
            Server.SendDataTCP(player_socket, $"0~0~ns");

            //return "";

        }

        private static async void CleanTempSession(string index)
        {
            await Task.Delay(60000);
            if (TemporarySessionCreator.ContainsKey(index))
            {
                TemporarySessionCreator.Remove(index);
            }
        }

        public static bool StringChecker(string data_to_check)
        {
            if (data_to_check.All(char.IsLetterOrDigit))
            {
                return true;
            }

            return false;
        }

        

        public static bool NumericsChecker(string data_to_check)
        {
            if (data_to_check.All(char.IsDigit))
            {
                return true;
            }

            return false;
        }

    }
}
