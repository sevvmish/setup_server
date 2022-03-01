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

      
        public static string ProcessTCPInput(string data, string endpoint_address)
        {
            try
            {
                Console.WriteLine(data);
            
                string[] packet_data = data.Split('~');
                //Console.WriteLine(data);

                
                //get current friend list  7~1~ticket~char
                if (packet_data.Length == 4 && (packet_data[0] + packet_data[1]) == "71")
                {
                    if (!StringChecker(packet_data[2]) || !StringChecker(packet_data[3]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 7~1~wds to user from - " + endpoint_address);
                        return $"7~1~wds"; //wrong digits or signs                    
                    }

                    //SELECT characters.character_id,characters.character_name FROM characters, friends WHERE friends.friend_character_id=characters.character_id AND friends.character_id=(SELECT characters.character_id FROM characters WHERE characters.character_name='warkostyapc' AND characters.user_id=(SELECT users.user_id FROM users WHERE users.ticket_id='aXhT2f8vCo'))

                    string[,] get_data = mysql.GetMysqlSelect($"SELECT characters.character_id,characters.character_name FROM characters, friends " +
                        $"WHERE friends.friend_character_id=characters.character_id AND " +
                        $"friends.character_id=(SELECT characters.character_id FROM characters WHERE characters.character_name='{packet_data[3]}' " +
                        $"AND characters.user_id=(SELECT users.user_id FROM users WHERE users.ticket_id='{packet_data[2]}'))").Result;

                    if (get_data.GetLength(0) == 0 || get_data[0, 0] == "error")
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 7~1 error getting friend list to user from - " + endpoint_address);
                        return $"7~1~err";
                    }

                    string result = $"7~1~{get_data.GetLength(0)}";

                    for (int i = 0; i < get_data.GetLength(0); i++)
                    {
                        int isActive = 0;

                        for (int u = 0; u < 3; u++)
                        {                            
                            if (u==0 && functions.CheckVisitorStateByID(int.Parse(get_data[i, u])))
                            {
                                isActive = 1;
                            }

                            if (u==2)
                            {
                                result += $"~{isActive}";
                            }
                            else
                            {
                                result += $"~{get_data[i, u]}";
                            }
                            
                        }                        
                    }

                    return result;                
                }
                

                //verify and get client version 2~3~ticket
                if (packet_data.Length == 3 && (packet_data[0] + packet_data[1]) == "23")
                {
                    if (!StringChecker(packet_data[2]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 2~3~wds to user from - " + endpoint_address);
                        return $"2~3~wds"; //wrong digits or signs                    
                    }

                    string[,] check_ticket = mysql.GetMysqlSelect($"SELECT `user_id` FROM `users` WHERE `ticket_id`='{packet_data[2]}'").Result;

                    if (check_ticket.GetLength(0) == 0 || check_ticket[0, 0] == "error")
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 2~3~nst to user from - " + endpoint_address);
                        return $"2~3~nst";
                    }
                                        
                    return $"2~3~{starter.CLIENT_VERSION}";
                }



                //receive data about player any INFO MESSAGES
                if (packet_data.Length == 3 && (packet_data[0] + packet_data[1]) == "22")
                {
                    if (!StringChecker(packet_data[2]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 2~2~wds to user from - " + endpoint_address);
                        return $"2~2~wds"; //wrong digits or signs                    
                    }

                    string[,] check_ticket = mysql.GetMysqlSelect($"SELECT `user_id` FROM `users` WHERE `ticket_id`='{packet_data[2]}'").Result;

                    if (check_ticket.GetLength(0) == 0 || check_ticket[0, 0] == "error")
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 2~2~nst to user from - " + endpoint_address);
                        return $"2~2~nst";
                    }

                    string[,] result = mysql.GetMysqlSelect($"SELECT `char_info_id`,`information_id` FROM `character_info` WHERE `date_informed`='' AND `user_id`='{check_ticket[0,0]}'").Result;

                    if (result.GetLength(0) == 0 || result[0, 0] == "error")
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 2~2~nsc to user from - " + endpoint_address);
                        return $"2~2~no";
                    }

                    string char_info_id = result[0, 0];
                    string information_id = result[0, 1];

                    //make exact information line outdated
                    bool isOK = mysql.ExecuteSQLInstruction($"UPDATE `character_info` SET `date_informed`='{DateTime.Now}' WHERE `char_info_id`='{char_info_id}'").Result;

                    if (!isOK) Console.WriteLine(DateTime.Now + ": error setting date to char informed for - " + endpoint_address);

                    //read exact message from INFORMATION
                    result = mysql.GetMysqlSelect($"SELECT `info_topic`, `info_body` FROM `information` WHERE `information_id`='{information_id}'").Result;
                    
                    if (result.GetLength(0) == 0 || result[0, 0] == "error")
                    {
                        Console.WriteLine(DateTime.Now + ": send problem of getting message body to - " + endpoint_address);
                        return $"2~2~nd";
                    }

                    return $"2~2~{result[0, 0]}~{result[0, 1]}";
                }


                //receive data about player raitings and XP 2~1~ticket~charname
                if (packet_data.Length == 4 && (packet_data[0] + packet_data[1]) == "21")
                {
                    if (!StringChecker(packet_data[2]) || !StringChecker(packet_data[3]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 2~1~wds to user from - " + endpoint_address);
                        return $"2~1~wds"; //wrong digits or signs                    
                    }

                    string[,] check_ticket = mysql.GetMysqlSelect($"SELECT `user_id` FROM `users` WHERE `ticket_id`='{packet_data[2]}'").Result;

                    if (check_ticket.GetLength(0) == 0 || check_ticket[0, 0] == "error")
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 2~1~nst to user from - " + endpoint_address);
                        return $"2~1~nst";
                    }

                    string[,] result = mysql.GetMysqlSelect($"SELECT `pvp_raiting`, `pve_raiting`, `xp_points` FROM `character_raiting` WHERE `character_id`=(SELECT characters.character_id FROM characters WHERE characters.character_name='{packet_data[3]}')").Result;

                    if (result.GetLength(0) == 0 || result[0,0]=="error")
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 2~1~nsc to user from - " + endpoint_address);
                        return $"2~1~nsc";
                    }

                    return $"2~1~{result[0,0]}~{result[0, 1]}~{result[0, 2]}";

                }


                //receive data about character incl all stats
                if (packet_data.Length == 4 && (packet_data[0] + packet_data[1]) == "20")
                {

                    if (!StringChecker(packet_data[2]) || !StringChecker(packet_data[3]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 2~0~wds to user from - " + endpoint_address);
                        return $"2~0~wds"; //wrong digits or signs                    
                    }

                    string[,] get_char_data = mysql.GetMysqlSelect($"SELECT `character_id`,`speed`, `health`, `health_regen`, `energy_regen`, `weapon_attack`, `hit_power`, `armor`, `shield_block`, `magic_resistance`, `dodge`, `cast_speed`, `melee_crit`, `magic_crit`, `spell_power`, `spell1`, `spell2`, `spell3`, `spell4`, `spell5`, `spell6`, `spell_book`, `talents` FROM `character_property` WHERE character_property.character_id = (SELECT characters.character_id FROM characters WHERE characters.character_name = '{packet_data[3]}')  AND((SELECT users.user_id FROM users WHERE users.ticket_id = '{packet_data[2]}') = (SELECT characters.user_id FROM characters WHERE characters.character_name = '{packet_data[3]}')) ").Result;

                    if (get_char_data.GetLength(0)==0 || get_char_data[0,0]=="error")
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 2~0~nd to user from - " + endpoint_address);
                        return $"2~0~nd";
                    }

                    //data about visitors==============
                    functions.AddOrUpdateVisitors(packet_data[2], packet_data[3], int.Parse(get_char_data[0, 0]));
                    //================================

                    string result = "";

                    for (int i = 1; i <= 22; i++)
                    {                        
                        result = result + get_char_data[0, i] + "~";
                    }

                    Console.WriteLine(DateTime.Now + ": char " + packet_data[3] + " - desciption send to user ticket " + packet_data[2] + " from " + endpoint_address);

                    return $"2~0~{result}";
                }

                //start queue for any PVP
                //3~1~ticket~character~1
                if (packet_data.Length == 5 && (packet_data[0] + packet_data[1]) == "31")
                {

                    if (!StringChecker(packet_data[2]) || !StringChecker(packet_data[3]) || !NumericsChecker(packet_data[4]))
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

                    //get server location
                    int region_id = 0;
                    string[,] server_number = mysql.GetMysqlSelect($"SELECT `region_id` FROM `users` WHERE `ticket_id`='{packet_data[2]}'").Result;
                    if (server_number.GetLength(0) != 1 || server_number[0,0]=="error")
                    {
                        Console.WriteLine(DateTime.Now + ": error getting region_ID form - " + endpoint_address);                        
                    }
                    region_id = int.Parse(server_number[0, 0]);


                    string[,] char_id = mysql.GetMysqlSelect($"SELECT characters.character_id FROM characters WHERE characters.character_name='{packet_data[3]}'").Result;
                    if (char_id.GetLength(0) == 0)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 3~1~eit to user from - " + endpoint_address);
                        return $"3~1~eit";
                    }

                    if (Server.PlayersAwaiting.ContainsKey(char_id[0, 0]))
                    {
                        
                        Console.WriteLine(DateTime.Now + $": player {packet_data[3]} allready in queue for PVP, to user from - " + endpoint_address);
                        return $"3~1~aiq";
                    }


                    //organize new pvp queue for player
                    //get PVP Raiting???

                    GameTypes WhatPVP = GameTypes.PvP_1vs1;

                    switch(int.Parse(packet_data[4]))
                    {
                        case 0:
                            WhatPVP = GameTypes.PvE_for_test;
                            break;
                        case 1:
                            WhatPVP = GameTypes.PvP_1vs1;
                            break;
                        case 2:
                            WhatPVP = GameTypes.PvP_2vs2;
                            break;
                        case 3:
                            WhatPVP = GameTypes.PvP_battle_royale;
                            break;
                        case 4:
                            WhatPVP = GameTypes.PVP_any_battle;
                            break;
                    }



                    if (!Server.PlayersAwaiting.ContainsKey(char_id[0, 0])) {
                        
                        //cheking raiting
                        string[,] PVPraiting = mysql.GetMysqlSelect($"SELECT `pvp_raiting` FROM `character_raiting` WHERE `character_id`='{char_id[0, 0]}'").Result;
                        if (PVPraiting[0, 0] == "error") PVPraiting[0, 0] = "0";
                        
                        //INIT
                        Server.PlayersAwaiting.Add(char_id[0, 0], new PlayerForGameSession(char_id[0, 0], packet_data[3], packet_data[2], WhatPVP, int.Parse(PVPraiting[0, 0]), region_id));
                        return $"3~1~ok";
                    }
                               
                }

                //reset ALL QUEUES
                //3~101~ticket~character
                if (packet_data.Length == 4 && (packet_data[0] + packet_data[1]) == "3101")
                {

                    if (!StringChecker(packet_data[2]) || !StringChecker(packet_data[3]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 3~1~wds to user from - " + endpoint_address);
                        return $"3~101~wds"; //wrong digits or signs                    
                    }

                    string[,] check_ticket_and_name = mysql.GetMysqlSelect($"SELECT `character_name` FROM `characters` WHERE(`character_name`= '{packet_data[3]}' AND characters.user_id = (SELECT user_id FROM users WHERE users.ticket_id = '{packet_data[2]}'))").Result;
                    if (check_ticket_and_name.GetLength(0) != 1 || check_ticket_and_name[0, 0] != packet_data[3])
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 3~101~eit to user from - " + endpoint_address);
                        return $"3~101~eit";
                    }

                    string[,] char_id = mysql.GetMysqlSelect($"SELECT characters.character_id FROM characters WHERE characters.character_name='{packet_data[3]}'").Result;
                    if (char_id.GetLength(0) == 0)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 3~101~eit to user from - " + endpoint_address);
                        return $"3~101~eit";
                    }

                    //remove visitor
                    Server.CurrentVisitors.Remove(packet_data[2]);
                    //=================

                    if (Server.PlayersAwaiting.ContainsKey(char_id[0, 0]) && Server.PlayersAwaiting[char_id[0, 0]].GetCurrentPlayerStatus() != PlayerStatus.isGone /*!Server.PlayersAwaiting[char_id[0, 0]].isPlayerBusyForSession()*/)
                    {
                        Server.PlayersAwaiting.Remove(char_id[0, 0]);
                        Console.WriteLine(DateTime.Now + $": player {packet_data[3]} removed from any queues, to user from - " + endpoint_address);
                        return $"3~101~out";
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

                    CheckPVP1();

                    if (!StringChecker(packet_data[2]) || !StringChecker(packet_data[3]))
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 4~0~0~wds to user from - " + endpoint_address);
                        return $"4~0~0~wds"; //wrong digits or signs                    
                    }


                    //get character id and update datetime in queue OR stop queue if no such char in 1vs1pvp queue
                    //string[,] get_char_id = mysql.GetMysqlSelect($"SELECT `character_id` FROM `characters` WHERE `character_name`='{packet_data[3]}'").Result;
                    string get_char_id = null;

                    foreach (string _keys in Server.PlayersAwaiting.Keys)
                    {
                        if (Server.PlayersAwaiting[_keys].GetCharacterName()== packet_data[3] && Server.PlayersAwaiting[_keys].GetCharacterTicket() == packet_data[2])
                        {
                            get_char_id = _keys;
                            break;
                        }
                    }
                    
                    if (get_char_id==null)
                    {
                        Console.WriteLine(DateTime.Now + ": send problem 4~0~0~nst to user from - " + endpoint_address);
                        return $"4~0~0~nst"; 
                    }

                    if (!Server.PlayersAwaiting.ContainsKey(get_char_id))
                    {
                        Console.WriteLine(DateTime.Now + ": send error - no such player in queue - to user from - " + endpoint_address);
                        return $"4~0~0~nst";
                    }


                    //remove player for timeout
                    if (Math.Abs(Server.PlayersAwaiting[get_char_id].WhenStarted().Subtract(DateTime.Now).TotalSeconds)> Server.LimitForLonelyPlayerToLoseQueue)
                    {
                        Server.PlayersAwaiting.Remove(get_char_id);
                        Console.WriteLine(DateTime.Now + $": player {packet_data[3]} removed from any queues for timeout, to user from - " + endpoint_address);
                        return $"3~101~timeout";
                    }

                    if (Server.PlayersAwaiting.ContainsKey(get_char_id) && (Server.PlayersAwaiting[get_char_id].GetCurrentPlayerStatus()==PlayerStatus.free || Server.PlayersAwaiting[get_char_id].GetCurrentPlayerStatus() == PlayerStatus.isBusy))
                    {
                        Server.PlayersAwaiting[get_char_id].Update();
                        return $"4~0~0~0";
                    }                    

                    if (Server.PlayersAwaiting.ContainsKey(get_char_id) && Server.PlayersAwaiting[get_char_id].GetCurrentPlayerStatus() == PlayerStatus.ischeckedOrganization)
                    {
                        Server.PlayersAwaiting[get_char_id].Update();
                        //Console.WriteLine(DateTime.Now + ": send get ready 4~0~2~0 to user from - " + endpoint_address);
                        return $"4~0~2~0"; //GGEEETTT RRREEAAAADDDYYY
                    }

                    if (Server.PlayersAwaiting.ContainsKey(get_char_id) && Server.PlayersAwaiting[get_char_id].GetCurrentPlayerStatus() == PlayerStatus.isReady)
                    {
                        string _new_ticket = Server.PlayersAwaiting[get_char_id].GetCharacterNewGeneratedTicket();
                        string _old_ticket = Server.PlayersAwaiting[get_char_id].GetCharacterTicket();
                        string _new_session = Server.PlayersAwaiting[get_char_id].GetNewSession();
                        string _game_hub = Server.PlayersAwaiting[get_char_id].GetGameHub();
                        functions.ChangeTicketInPlayer(_old_ticket, _new_ticket);
                        Console.WriteLine(DateTime.Now + $": changed old ticket {_old_ticket} to new {_new_ticket}, started session {_new_session} to {endpoint_address}");
                        
                        if (!Server.GameSessionWaitingForResult.ContainsKey(_new_session))
                        {
                            Server.GameSessionWaitingForResult.Add(_new_session, new GameSessionResults(_new_session));
                        }
                        Server.GameSessionWaitingForResult[_new_session].AddPlayer(Server.PlayersAwaiting[get_char_id]);
                        Task.Run(() => Server.GameSessionWaitingForResult[_new_session].RegisterNewSessionDataByPlayerID(_new_ticket));
                        
                        Server.PlayersAwaiting[get_char_id].SetStatusToGONE();
                        Server.PlayersAwaiting.Remove(get_char_id);
                        
                        return $"4~0~3~{_new_ticket}~{_new_session}~{_game_hub}";
                    }


                }

                //receive data about played games for statistics    5~0~session~number of players~ticket pl1~ score pl1~ticket pl2 ~ score pl2
                if (packet_data.Length >= 6 && (packet_data[0] + packet_data[1]) == "50")
                {
                    for (int i = 0; i < packet_data.Length; i++)
                    {
                        if (!StringChecker(packet_data[i]))
                        {
                            Console.WriteLine(DateTime.Now + ": problem with getting data about played session -> " + packet_data[2]);
                            return $"5~0~er1"; //wrong digits or signs                    
                        }
                    }

                    if (!Server.GameSessionWaitingForResult.ContainsKey(packet_data[2]))
                    {
                        Console.WriteLine(DateTime.Now + ": problem with getting data about played session - no such session awaiting -> " + packet_data[2]);
                        return $"5~0~er2"; //wrong session ID
                    }

                    int _number = int.Parse(packet_data[3]);

                    int x = 4;
                    for (int i = 0; i < _number; i++)
                    {
                        foreach (var items in Server.GameSessionWaitingForResult[packet_data[2]].CurrentPlayers)
                        {
                            

                            if (items.GetCharacterNewGeneratedTicket() == packet_data[x])
                            {
                                Console.WriteLine(items.GetCharacterNewGeneratedTicket() + " = " + packet_data[x] + " :"+ int.Parse(packet_data[x + 1]));
                                items.ManageScore = int.Parse(packet_data[x + 1]);
                                string ID = packet_data[x];
                                Task.Run(()=> Server.GameSessionWaitingForResult[packet_data[2]].RegisterNewSessionDataRESULTSByPlayerID(ID));
                            }
                        }

                        x += 2;
                    }

                    Console.WriteLine(DateTime.Now + ": data about played session received OK -> " + packet_data[2]);
                    return $"5~0~OK";
                }

                


            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }

            return "2~0~err";
            
        }



        public static void CheckPVP1()
        {
            foreach (var item in Server.PlayersAwaiting.Keys)
            {
                Console.WriteLine(item +" - "+ Server.PlayersAwaiting[item].WhenLastUpdated() +" - " + Server.PlayersAwaiting[item].GetCurrentPlayerStatus() +  "!!!!!!!!!");
            }
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

                //universal ping process
                if ((packet_data[0] + packet_data[1]) == "071")
                {

                    if (packet_data[2] != starter.InnerServerConnectionPassword)
                    {
                        Console.WriteLine(DateTime.Now + ": error 0~71 in password for another server from " + player_socket.RemoteEndPoint.ToString());
                        Server.SendDataTCP(player_socket, $"0~71~error");
                        return;
                    }

                    string result = $"0~71~{Server.PlayersAwaiting.Count}~{Server.GameSessionsAwaiting.Count}~{Server.GameSessionWaitingForResult.Count}";

                    byte[] t = Encoding.UTF8.GetBytes(result);
                    encryption.Encode(ref t, starter.secret_key_for_game_servers);
                    Console.WriteLine(DateTime.Now + $": send Ping Answer {result} to " + player_socket.RemoteEndPoint);
                    Server.SendDataTCP(player_socket, t);
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

        public static void ProcessPing(string data, EndPoint EP)
        {
            try
            {
                string[] packet_data = data.Split('~');

                if ((packet_data[0] + packet_data[1]) == "07")
                {
                    Console.WriteLine("what data received " + data);
                    if (packet_data[2] != starter.InnerServerConnectionPassword)
                    {
                        Console.WriteLine(DateTime.Now + ": error 0~7~wp for another server from " + EP.ToString());
                        Server.SendDataUDP(EP, $"0~7~wp");
                        return;
                    }

                    Console.WriteLine(DateTime.Now + ": received ping from " + EP.ToString());
                    Server.SendDataUDP(EP, $"0~7~{starter.GameServerHUBs.Count}");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + data + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }
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
