using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace setup_server
{
    class GameSessions
    {
        private GameTypes CurrentSessionType;
        private List<PlayerForGameSession> CurrentPlayers = new List<PlayerForGameSession>();
        

        public GameSessions(List<PlayerForGameSession> _current_players)
        {
            for (int i = 0; i < _current_players.Count; i++)
            {
                _current_players[i].MakePlayerBusyForSession();
            }
            CurrentPlayers = _current_players;
            CurrentSessionType = CurrentPlayers[0].GetPlayerGameType();
            if (OrganizePVP())
            {
                for (int i = 0; i < _current_players.Count; i++)
                {
                    CurrentPlayers[i].SetStatusToChecked();
                }

                //Task.Run(() => StartSession());
                StartSession();
                Console.WriteLine("SSSSSSSSSSSTTTTTTTTTTTTEEEEEEEEPPPPPPPPPPPPP 1111111111111111");

            } 
            else
            {
                for (int i = 0; i < _current_players.Count; i++)
                {
                    _current_players[i].ResetPlayerStatusToNonBusy();                    
                }
                Server.GameSessionsAwaiting.Remove(this);
            }

        }

        public void StartSession()
        {
            Console.WriteLine("SSSSSSSSSSSTTTTTTTTTTTTEEEEEEEEPPPPPPPPPPPPP 2222222222222222");
            //await Task.Delay(5000);

            for (int i = 0; i < CurrentPlayers.Count; i++)
            {
                Console.WriteLine(CurrentPlayers[i].GetCharacterName());
                CurrentPlayers[i].SetStatusToREADY();
            }
            Console.WriteLine("SSSSSSSSSSSTTTTTTTTTTTTEEEEEEEEPPPPPPPPPPPPP 33333333333333333");
            Server.GameSessionsAwaiting.Remove(this);
        }


        public bool OrganizePVP()
        {
            
            List<string> _char_id = new List<string>();
            int _count = CurrentPlayers.Count;

            for (int i = 0; i < CurrentPlayers.Count; i++)
            {
                _char_id.Add(CurrentPlayers[i].GetCharacterID());
            }

            int game_type_id = 0;
            switch (_count)
            {
                case 2:
                    game_type_id = 1; //type of PVP - 1vs1
                    break;
            }

            List<string[]> char_d = new List<string[]>(_count);
            List<string[]> char_n = new List<string[]>(_count);
            string new_session_id = functions.get_random_set_of_symb(8);
            
            List<string> new_player_id_aka_ticket = new List<string>(_count);
            List<string> player_old_tickets = new List<string>(_count);
            
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

                    string _new_tickets = functions.get_random_set_of_symb(8);

                    new_player_id_aka_ticket.Add(_new_tickets);
                    CurrentPlayers[i].SetNewTicketForPlayer(_new_tickets);
                    CurrentPlayers[i].SetNewSession(new_session_id);
                    //player_old_tickets.Add(CurrentPlayers[i].GetCharacterTicket());

                }

                //send data to gamehub1 to create start table
                string send_table_data = $"0~5~{starter.InnerServerConnectionPassword}~CREATE TABLE `{new_session_id}` (`player_order` int(11), `player_id` varchar(10), `player_name` varchar(20),`player_class` tinyint(4),`connection_number` varchar(25),`team_id` int(1), `game_type_id` int(1),`zone_type` tinyint(2),`position_x` float,`position_y` float,`position_z` float,`rotation_x` float,`rotation_y` float,`rotation_z` float,`speed` float,`animation_id` tinyint(2),`conditions` varchar(255),`health_pool` varchar(13),`energy` float,`health_regen` float,`energy_regen` float,`weapon_attack` varchar(10),`hit_power` float,`armor` float,`shield_block` float,`magic_resistance` float,`dodge` float,`cast_speed` float,`melee_crit` float,`magic_crit` float,`spell_power` float,`spell1` smallint(6),`spell2` smallint(6),`spell3` smallint(6),`spell4` smallint(6),`spell5` smallint(6),`spell6` smallint(6),`hidden_conds` varchar(255),`global_button_cooldown` tinyint(2)) ENGINE = InnoDB DEFAULT CHARSET = utf8; ";

                //CHECK IT!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                string res_creating_table = Server.SendAndGetTCP_between_servers(send_table_data, starter.GameServerPort, starter.GameServerHUBs["HUB_2"], true);
                //Console.WriteLine(res_creating_table + " =========================!");

                //send data to gamehub1 to add players data
                string send_players_data = $"0~5~{starter.InnerServerConnectionPassword}~INSERT INTO `{new_session_id}` VALUES";


                int zone_type = 1;

                for (int i = 0; i < _count; i++)
                {
                    if (i > 0) { send_players_data = send_players_data + ","; }
                    send_players_data = send_players_data + $" ('{(i + 1)}', '{new_player_id_aka_ticket[i]}','{char_n[i].GetValue(0)}','{char_n[i].GetValue(1)}','0','{i}','{game_type_id}','{zone_type}',2,0,2,0,0,0,'{char_d[i].GetValue(1)}',0,'','{char_d[i].GetValue(2)}={char_d[i].GetValue(2)}',100,'{char_d[i].GetValue(3)}','{char_d[i].GetValue(4)}','{char_d[i].GetValue(5)}','{char_d[i].GetValue(6)}','{char_d[i].GetValue(7)}','{char_d[i].GetValue(8)}','{char_d[i].GetValue(9)}','{char_d[i].GetValue(10)}','{char_d[i].GetValue(11)}','{char_d[i].GetValue(12)}','{char_d[i].GetValue(13)}','{char_d[i].GetValue(14)}','{char_d[i].GetValue(15)}','{char_d[i].GetValue(16)}','{char_d[i].GetValue(17)}','{char_d[i].GetValue(18)}','{char_d[i].GetValue(19)}',997,'{char_d[i].GetValue(21)}',0)";
                    if (i == (_count - 1)) { send_players_data = send_players_data + ";"; }
                }

                //CHECK IT!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                string res_sending_players_data = Server.SendAndGetTCP_between_servers(send_players_data, starter.GameServerPort, starter.GameServerHUBs["HUB_2"], true);
                //Console.WriteLine(send_players_data + " =========================!");

                //send data to start this session
                //CHECK IT!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                string res_starting_new_session = Server.SendAndGetTCP_between_servers($"0~2~{starter.InnerServerConnectionPassword}~{new_session_id}", starter.GameServerPort, starter.GameServerHUBs["HUB_2"], true);


                //preparing awaiting
                if (res_creating_table == "0~5~ok" && res_sending_players_data == "0~5~ok" && res_starting_new_session == "0~2~1")
                {                                        
                    StringBuilder list_of_chars = new StringBuilder();

                    for (int i = 0; i < _count; i++)
                    {
                        list_of_chars.Append(CurrentPlayers[i] + ", ");
                    }

                    Console.WriteLine(DateTime.Now + ": started organazing PVP for " + list_of_chars.ToString());
                    return true;
                }
                else
                {
                    StringBuilder list_of_chars = new StringBuilder();

                    for (int i = 0; i < _count; i++)
                    {
                        list_of_chars.Append(CurrentPlayers[i] + ", ");
                    }

                    Console.WriteLine(DateTime.Now + ": failed organizing PVP -" + list_of_chars.ToString());
                    return false;
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
                return false;
            }

            return false;

        }




    }

    class PlayerForGameSession
    {
        private string Character_ID;
        private string CharacterName;
        private string CharacterTicket;
        private string CharacterNewSession;
        private string CharacterNewTicket;
        private DateTime WhenEnteredToSearchGame;
        private DateTime WhenLastUpdateSignal;
        private GameTypes PlayerGameType;
        private PlayerStatus CurrentPlayerStatus;
        private string GameHub = "0";

        private int PlayerPVPRaiting;
        private bool isBusyForSession;

        public PlayerForGameSession(string _char_ID, string _char_name, string _char_ticket, GameTypes _player_game_type, int _pvp_rait)
        {
            Character_ID = _char_ID;
            CharacterName = _char_name;
            CharacterTicket = _char_ticket;
            WhenEnteredToSearchGame = DateTime.Now;
            PlayerGameType = _player_game_type;
            PlayerPVPRaiting = _pvp_rait;
            isBusyForSession = false;
            WhenLastUpdateSignal = DateTime.Now;
            CurrentPlayerStatus = PlayerStatus.free;
        }

        public bool isPlayerBusyForSession()
        {
            return isBusyForSession;
        }

        public void MakePlayerBusyForSession()
        {
            isBusyForSession = true;
            CurrentPlayerStatus = PlayerStatus.isBusy;
        }

        public void ResetPlayerStatusToNonBusy()
        {
            isBusyForSession = false;
            CurrentPlayerStatus = PlayerStatus.free;
        }

        public void Update()
        {
            WhenLastUpdateSignal = DateTime.Now;
        }

        public DateTime WhenLastUpdated()
        {
            return WhenLastUpdateSignal;
        }

        public string GetCharacterName()
        {
            return CharacterName;
        }

        public string GetCharacterTicket()
        {
            return CharacterTicket;
        }

        public string GetCharacterNewGeneratedTicket()
        {
            return CharacterNewTicket;
        }

        public string GetCharacterID()
        {
            return Character_ID;
        }

        public GameTypes GetPlayerGameType()
        {
            return PlayerGameType;
        }

        public void SetNewTicketForPlayer(string _new_ticket)
        {
            CharacterNewTicket = _new_ticket;
        }

        public void SetStatusToChecked()
        {
            CurrentPlayerStatus = PlayerStatus.ischeckedOrganization;
        }

        public void SetStatusToREADY()
        {
            CurrentPlayerStatus = PlayerStatus.isReady;
        }

        public PlayerStatus GetCurrentPlayerStatus()
        {
            return CurrentPlayerStatus;
        }

        public void SetNewSession(string _session)
        {
            CharacterNewSession = _session;
        }

        public string GetNewSession()
        {
            return CharacterNewSession;
        }

        public void SetGameHub(string _game_hub)
        {
            GameHub = _game_hub;
        }

        public string GetGameHub()
        {
            return GameHub;
        }

    }

    public enum GameTypes
    {
        PvP_1vs1 = 0,
        PvP_2vs2,
        PvP_3vs3,
        PvE_for_test
    }

    public enum PlayerStatus
    {
        free = 0,
        isBusy,
        ischeckedOrganization,
        isReady
    }
}
