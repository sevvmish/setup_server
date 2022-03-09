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
        private DateTime WhenCheckWasOK;
        private PlayerStatus SessionStatus;
        private int region_id;
        
        public GameSessions(List<PlayerForGameSession> _current_players, GameTypes _gameType, int _region)
        {
            region_id = _region;
           
            for (int i = 0; i < _current_players.Count; i++)
            {
                _current_players[i].SetPlayerGameType(_gameType);
                _current_players[i].MakePlayerBusyForSession();
                CurrentPlayers.Add(_current_players[i]);
            }
                  
            CurrentSessionType = _gameType;
            if (OrganizePVP(CurrentSessionType).Result)
            {
                WhenCheckWasOK = DateTime.Now;
                SessionStatus = PlayerStatus.ischeckedOrganization;
                for (int i = 0; i < CurrentPlayers.Count; i++)
                {
                    CurrentPlayers[i].SetStatusToChecked();
                    WaitAndMakePlayerReadyStatus();
                    
                }
            } 
            else
            {
                for (int i = 0; i < CurrentPlayers.Count; i++)
                {
                    CurrentPlayers[i].ResetPlayerStatusToNonBusy();                    
                }

                Console.WriteLine("game session removed - error creating session");
                Server.GameSessionsAwaiting.Remove(this);
            }

        }


        public async void WaitAndMakePlayerReadyStatus()
        {
            int _delayTime = (int)Server.TimeForMakingIsChekedToREADY * 1000;
            await Task.Delay(_delayTime);

            for (int i = 0; i < CurrentPlayers.Count; i++)
            {
                CurrentPlayers[i].SetStatusToREADY();
            }

            CleanChekedStatusAfterSecondsAnyWay();
        }
      
        public async void CleanChekedStatusAfterSecondsAnyWay()
        {
            int _delayTime = 1000;
            await Task.Delay(_delayTime);

            while(true)
            {
                bool isOK = true;

                for (int i = 0; i < CurrentPlayers.Count; i++)
                {
                    if (CurrentPlayers[i].GetCurrentPlayerStatus()!=PlayerStatus.isGone)
                    {
                        isOK = false;
                        break;
                    }
                }

                if (isOK)
                {
                    break;
                }

                await Task.Delay(500);
            }


            Console.WriteLine(DateTime.Now + ": players deleted and session stopped because to long for waiting");

            foreach (string keyInPlayerWaiting in Server.PlayersAwaiting.Keys)
            {
                if (CurrentPlayers.Contains(Server.PlayersAwaiting[keyInPlayerWaiting]))
                {
                    Server.PlayersAwaiting.Remove(keyInPlayerWaiting);
                }
            }

            Console.WriteLine("game session removed ");
            Server.GameSessionsAwaiting.Remove(this);
        }

        public PlayerStatus GetSessionStatus()
        {
            return SessionStatus;
        }

        public void SetAllPlayersToReadyStatus()
        {
            for (int i = 0; i < CurrentPlayers.Count; i++)
            {
                CurrentPlayers[i].SetStatusToREADY();
            }
        }

        public DateTime GetWhenCheckWasOK()
        {
            return WhenCheckWasOK;
        }

        public List<PlayerForGameSession> GetPlayers()
        {
            return CurrentPlayers;
        }


        private async Task<bool> OrganizePVP(GameTypes CurrentGameType)
        {
            
            List<string> _char_id = new List<string>();
            int _count = CurrentPlayers.Count;

            for (int i = 0; i < CurrentPlayers.Count; i++)
            {
                _char_id.Add(CurrentPlayers[i].GetCharacterID());
            }

            int game_type_id = 0;
            switch ((int)CurrentGameType)
            {
                case 0:
                    game_type_id = 0; //testing
                    break;
                case 1:
                    game_type_id = 1; //type of PVP - 1vs1
                    break;
                case 2:
                    game_type_id = 2; //type of PVP - 2vs2
                    break;
                case 3:
                    game_type_id = 3; //battle royale
                    break;                
            }

            List<string[]> char_d = new List<string[]>(_count);
            List<string[]> char_n = new List<string[]>(_count);
            List<string> pvp_raiting = new List<string>();
            string new_session_id = functions.get_random_set_of_symb(8);
            
            List<string> new_player_id_aka_ticket = new List<string>(_count);
            List<string> player_old_tickets = new List<string>(_count);
            
            try
            {

                //get hub_ip for game:
                
                string Game_hub_IP = await Server.CheckAndGetGameHubs(region_id);
                Console.WriteLine(DateTime.Now + ": server chosen - " + Game_hub_IP);
                if (Game_hub_IP=="error")
                {
                    Console.WriteLine(DateTime.Now + ": error while choosing server");
                    return false;
                }

                for (int i = 0; i < _count; i++)
                {
                    string[,] char_d_ = mysql.GetMysqlSelect($"SELECT `character_id`, `speed`, `health`, `health_regen`, `energy_regen`, `weapon_attack`, `hit_power`, `armor`, `shield_block`, `magic_resistance`, `dodge`, `cast_speed`, `melee_crit`, `magic_crit`, `spell_power`, `spell1`, `spell2`, `spell3`, `spell4`, `spell5`, `spell6`, `hidden_conds`, `spell_book`, `talents` FROM `character_property` WHERE `character_id`='{_char_id[i]}' ").Result;
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

                    //================================PVP raiting================
                    string[,] pvp_r = mysql.GetMysqlSelect($"SELECT `pvp_raiting` FROM `character_raiting` WHERE `character_id`='{_char_id[i]}' ").Result;

                    
                    if (pvp_r.GetLength(0)==0 || pvp_r[0, 0]=="error")
                    {
                        pvp_raiting.Add("0");
                    }
                    else
                    {
                        pvp_raiting.Add(pvp_r[0, 0]);
                    }
                                      

                    string _new_tickets = functions.get_random_set_of_symb(8);

                    new_player_id_aka_ticket.Add(_new_tickets);
                    CurrentPlayers[i].SetNewTicketForPlayer(_new_tickets);
                    CurrentPlayers[i].SetNewSession(new_session_id);
                    CurrentPlayers[i].SetGameHub(Game_hub_IP);
                    //player_old_tickets.Add(CurrentPlayers[i].GetCharacterTicket());

                }

                

                //send data to gamehub1 to create start table
                string send_table_data = $"0~5~{starter.InnerServerConnectionPassword}~CREATE TABLE `{new_session_id}` (`player_order` int(11), `player_id` varchar(10), `player_name` varchar(20),`player_class` tinyint(4),`pvp_raiting` varchar(25),`team_id` int(1), `game_type_id` int(1),`zone_type` tinyint(2),`position_x` float,`position_y` float,`position_z` float,`rotation_x` float,`rotation_y` float,`rotation_z` float,`speed` float,`animation_id` tinyint(2),`conditions` varchar(255),`health_pool` varchar(13),`energy` float,`health_regen` float,`energy_regen` float,`weapon_attack` varchar(10),`hit_power` float,`armor` float,`shield_block` float,`magic_resistance` float,`dodge` float,`cast_speed` float,`melee_crit` float,`magic_crit` float,`spell_power` float,`spell1` smallint(6),`spell2` smallint(6),`spell3` smallint(6),`spell4` smallint(6),`spell5` smallint(6),`spell6` smallint(6),`hidden_conds` varchar(255),`global_button_cooldown` tinyint(2)) ENGINE = InnoDB DEFAULT CHARSET = utf8; ";

                //CHECK IT!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                string res_creating_table = Server.SendAndGetTCP_between_servers(send_table_data, starter.GameServerPort, Game_hub_IP, true);
                //Console.WriteLine(res_creating_table + " =========================!");

                //send data to gamehub1 to add players data
                string send_players_data = $"0~5~{starter.InnerServerConnectionPassword}~INSERT INTO `{new_session_id}` VALUES";


                int zone_type = 1;

                Random rnd = new Random();

                switch(rnd.Next(1,4))
                {
                    case 1: //stone location
                        zone_type = 1;
                        break;
                    case 2: //forest location
                        zone_type = 2;
                        break;
                    case 3: //lava location
                        zone_type = 3;
                        break;
                }

                for (int i = 0; i < _count; i++)
                {
                    int team_id = 0;
                    int x = 0;
                    int z = 0;
                    int rot_y = 0;

                    switch(game_type_id)
                    {
                        case 0: //testing mode
                            team_id = 0;
                            x = 0;
                            z = 0;
                            rot_y = 180;
                            break;

                        case 1: //PvP 1vs1
                            team_id = i;

                            if (i == 0)
                            {
                                x = -10;
                                z = 0;
                                rot_y = 90;
                            }
                            else
                            {
                                x = 10;
                                z = 0;
                                rot_y = 270;
                            }

                            break;
                        case 2: //PvP 2vs2
                            if (i<2)
                            {
                                team_id = 0;

                                if (i == 0)
                                {                                   
                                    x = -10;
                                    z = -2;
                                    rot_y = 90;
                                }
                                else
                                {                                   
                                    x = -10;
                                    z = 2;
                                    rot_y = 90;
                                }
                            } 
                            else
                            {
                                team_id = 1;

                                if (i == 2)
                                {
                                    x = 10;
                                    z = 2;
                                    rot_y = 270;
                                }
                                else
                                {
                                    x = 10;
                                    z = -2;
                                    rot_y = 270;
                                }
                            }
                            break;

                        case 3:
                            team_id = i;
                            switch(i)
                            {
                                case 0:
                                    x = 10;
                                    z = 0;
                                    rot_y = 270;
                                   break;
                                case 1:
                                    x = -10;
                                    z = 0;
                                    rot_y = 90;
                                    break;
                                case 2:
                                    x = 0;
                                    z = 10;
                                    rot_y = 0;
                                    break;
                                case 3:
                                    x = 0;
                                    z = -10;
                                    rot_y = 180;
                                    break;
                                case 4:
                                    x = -8;
                                    z = -8;
                                    rot_y = 180;
                                    break;
                                case 5:
                                    x = 8;
                                    z = 8;
                                    rot_y = -180;
                                    break;
                                case 6:
                                    x = -8;
                                    z = 8;
                                    rot_y = -180;
                                    break;
                                case 7:
                                    x = 8;
                                    z = -8;
                                    rot_y = -180;
                                    break;
                            }


                            break;

                    }

                    if (i > 0) { send_players_data = send_players_data + ","; }
                    send_players_data = send_players_data + $" ('{(i + 1)}', '{new_player_id_aka_ticket[i]}','{char_n[i].GetValue(0)}','{char_n[i].GetValue(1)}','{pvp_raiting[i]}','{team_id}','{game_type_id}','{zone_type}',{x},0,{z},0,{rot_y},0,'{char_d[i].GetValue(1)}',0,'','{char_d[i].GetValue(2)}={char_d[i].GetValue(2)}',100,'{char_d[i].GetValue(3)}','{char_d[i].GetValue(4)}','{char_d[i].GetValue(5)}','{char_d[i].GetValue(6)}','{char_d[i].GetValue(7)}','{char_d[i].GetValue(8)}','{char_d[i].GetValue(9)}','{char_d[i].GetValue(10)}','{char_d[i].GetValue(11)}','{char_d[i].GetValue(12)}','{char_d[i].GetValue(13)}','{char_d[i].GetValue(14)}','{char_d[i].GetValue(15)}','{char_d[i].GetValue(16)}','{char_d[i].GetValue(17)}','{char_d[i].GetValue(18)}','{char_d[i].GetValue(19)}',997,'{char_d[i].GetValue(21)}',0)";
                    if (i == (_count - 1)) { send_players_data = send_players_data + ";"; }
                }

                //CHECK IT!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                string res_sending_players_data = Server.SendAndGetTCP_between_servers(send_players_data, starter.GameServerPort, Game_hub_IP, true);
                //Console.WriteLine(send_players_data + " =========================!");

                //send data to start this session
                //CHECK IT!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                string res_starting_new_session = Server.SendAndGetTCP_between_servers($"0~2~{starter.InnerServerConnectionPassword}~{new_session_id}", starter.GameServerPort, Game_hub_IP, true);


                //preparing awaiting
                if (res_creating_table == "0~5~ok" && res_sending_players_data == "0~5~ok" && res_starting_new_session == "0~2~1")
                {                                        
                    StringBuilder list_of_chars = new StringBuilder();

                    for (int i = 0; i < _count; i++)
                    {
                        list_of_chars.Append(CurrentPlayers[i].GetCharacterName() + ", ");
                    }

                    Console.WriteLine(DateTime.Now + ": started organazing PVP for " + list_of_chars.ToString());
                    return true;
                }
                else
                {
                    StringBuilder list_of_chars = new StringBuilder();

                    for (int i = 0; i < _count; i++)
                    {
                        list_of_chars.Append(CurrentPlayers[i].GetCharacterName() + ", ");
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
        private DateTime WhenPassedCheckOK;
        private string GameHub = "0";
        private int Score;
        private bool isRaitingReassessed;
        private int serverLocation;

        public bool RaitingReassessing
        {
            get
            {
                return isRaitingReassessed;
            }

            set
            {
                isRaitingReassessed = value;
            }
        }



        private int PlayerPVPRaiting;
        private bool isBusyForSession;

        public PlayerForGameSession(string _char_ID, string _char_name, string _char_ticket, GameTypes _player_game_type, int _pvp_rait, int player_server_region)
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
            WhenPassedCheckOK = DateTime.Now;
            serverLocation = player_server_region;
        }

        public int ManageScore
        {
            get
            {
                return Score;
            }

            set
            {
                Score = value;
            }

        }

        public int ServerLocation
        {
            get
            {
                return serverLocation;
            }

            set
            {
                serverLocation = value;
            }
        }


        public DateTime GetTimeOfPassCheckOK()
        {
            return WhenPassedCheckOK;
        }

        public DateTime WhenStarted()
        {
            return WhenEnteredToSearchGame;
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

        public void SetPlayerGameType(GameTypes _playerGametype)
        {
            PlayerGameType = _playerGametype;
        }

        public void SetNewTicketForPlayer(string _new_ticket)
        {
            CharacterNewTicket = _new_ticket;
        }

        public void SetStatusToChecked()
        {
            CurrentPlayerStatus = PlayerStatus.ischeckedOrganization;
            WhenPassedCheckOK = DateTime.Now;
        }

        public void SetStatusToREADY()
        {
            CurrentPlayerStatus = PlayerStatus.isReady;
        }

        public PlayerStatus GetCurrentPlayerStatus()
        {
            return CurrentPlayerStatus;
        }
        public void SetStatusToGONE()
        {
            CurrentPlayerStatus = PlayerStatus.isGone;            
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
        PvE_for_test = 0,
        PvP_1vs1,
        PvP_2vs2,
        PvP_battle_royale,
        PVP_any_battle
                
    }

    public enum PlayerStatus
    {
        free = 0,
        isBusy,
        ischeckedOrganization,
        isReady,
        isGone
    }

    class GameSessionResults
    {
        private string SessionID;
        private bool isKillThisSessionStarted;
        public List<PlayerForGameSession> CurrentPlayers = new List<PlayerForGameSession>();

        public GameSessionResults(string _sessID)
        {
            SessionID = _sessID;
            
        }

        public void RegisterNewSessionDataByPlayerID(string _playerID)
        {
            PlayerForGameSession _player = null;

            for (int i = 0; i < CurrentPlayers.Count; i++)
            {
                if (CurrentPlayers[i].GetCharacterNewGeneratedTicket()==_playerID)
                {
                    _player = CurrentPlayers[i];
                    break;
                }
            }

            if (_player == null)
            {
                Console.WriteLine(DateTime.Now + ": error registring new session data in DB - no such ticket " + _playerID);
                return;
            }

            int typeOfPVP = (int)_player.GetPlayerGameType();
            
            bool result = mysql.ExecuteSQLInstruction($"INSERT INTO `session_archive`(`character_id`, `session_id`, `session_type_id`, `date_n_time`, `player_id`) VALUES('{_player.GetCharacterID()}', '{SessionID}', '{typeOfPVP}', '{DateTime.Now}', '{_player.GetCharacterNewGeneratedTicket()}')").Result;
            
            if (result)
            {
                Console.WriteLine(DateTime.Now + ": registered new session for player " + _playerID);
            }
            else
            {
                Console.WriteLine(DateTime.Now + ": error registring new session data in DB - DB error for " + _playerID);
            }
        }

        public void RegisterNewSessionDataRESULTSByPlayerID(string _playerID)
        {
            
            PlayerForGameSession _player = null;

            for (int i = 0; i < CurrentPlayers.Count; i++)
            {
                Console.WriteLine("all tickets in wating for result: " + CurrentPlayers[i].GetCharacterNewGeneratedTicket());
                if (CurrentPlayers[i].GetCharacterNewGeneratedTicket() == _playerID)
                {
                    _player = CurrentPlayers[i];
                    break;
                }
            }

            if (_player == null)
            {
                Console.WriteLine(DateTime.Now + ": error registring new session data results in DB - no such ticket " + _playerID);
                return;
            } 
            else
            {
                Console.WriteLine("received data to add result for " + _player.GetCharacterName() + " - " +  _player.GetCharacterID());
            }

           

            int typeOfPVP = (int)_player.GetPlayerGameType();

            string [,] pre_result = mysql.GetMysqlSelect($"SELECT `session_archive_id` FROM `session_archive` WHERE (`character_id`='{_player.GetCharacterID()}' AND `session_id`='{SessionID}' AND `player_id`='{_player.GetCharacterNewGeneratedTicket()}')").Result;

            if (pre_result.GetLength(0)>0)
            {
                bool result = mysql.ExecuteSQLInstruction($"UPDATE `session_archive` SET `when_ended`='{DateTime.Now}', `score`='{_player.ManageScore}' WHERE (`character_id`='{_player.GetCharacterID()}' AND `session_id`='{SessionID}' AND `player_id`='{_player.GetCharacterNewGeneratedTicket()}')").Result;

                if (result)
                {
                    Console.WriteLine(DateTime.Now + ": registered new session result for player " + _playerID);
                }
                else
                {
                    Console.WriteLine(DateTime.Now + ": error registring new session data result  in DB - DB error for " + _playerID);
                }
            }
            else
            {
                Console.WriteLine(DateTime.Now + ": error registring new session data result in DB - no such player or session for " + _playerID);
            }

            functions.ReAssessExperienceByCharID(_player.GetCharacterID());
            _player.RaitingReassessing = true;

            if (!isKillThisSessionStarted) 
            {
                isKillThisSessionStarted = true;
                Task.Run(() => KillThisSession());
            }
        }

        private async void KillThisSession()
        {
            await Task.Delay(1000);

            while (true)
            {
                bool isOK = true;

                for (int i = 0; i < CurrentPlayers.Count; i++)
                {
                    if (!CurrentPlayers[i].RaitingReassessing)
                    {
                        isOK = false;
                        break;
                    }
                }

                if (isOK)
                {
                    break;
                }

                await Task.Delay(1000);
            }

            Console.WriteLine(DateTime.Now + ": GameSessionResult ended");

            CurrentPlayers.Clear();
            Server.GameSessionWaitingForResult.Remove(SessionID);
        }

       

        public void AddPlayer(PlayerForGameSession _player)
        {
            if (!CurrentPlayers.Contains(_player))
            {
                CurrentPlayers.Add(_player);
            }
        }
    }
}
