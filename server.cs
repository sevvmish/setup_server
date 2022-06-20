using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using NetCoreServer;

namespace setup_server
{
    
    class Server
    {
        //for checking game servers before init of game session
        public static Dictionary<string, PingProcessor> SendPingsList = new Dictionary<string, PingProcessor>();
        public static Dictionary<string, long> ReceivedPingList = new Dictionary<string, long>();
        //=====================================================

        public static Dictionary<string, byte[]> Sessions = new Dictionary<string, byte[]>();
        
        //working with visitors on the setup server==========
        public static Dictionary<string, VisitorData> CurrentVisitors = new Dictionary<string, VisitorData>();
        public static Dictionary<string, string> FindCharacterByID = new Dictionary<string, string>();
        //====================================================

        //=============packets for UDP waiting for promt OK
        public static Dictionary<string, CheckingIfUDPPacketReceived> PacketsAwaitingForCheking = new Dictionary<string, CheckingIfUDPPacketReceived>();
        //==================================================

        //TIMERS========================
        private static System.Timers.Timer queueTimer, visitorsTimer;
        //==============================
        
        public static Dictionary<string, PlayerForGameSession> PlayersAwaiting = new Dictionary<string, PlayerForGameSession>();
        public static HashSet<GameSessions> GameSessionsAwaiting = new HashSet<GameSessions>();
        public static Dictionary<string, GameSessionResults> GameSessionWaitingForResult = new Dictionary<string, GameSessionResults>();


        public const float LimitForIdlePlayerToLoseQueue = 6f;
        public const float LimitForLonelyPlayerToLoseQueue = 700f;
        public const float TimeForWaitBeforeAddingBot1vs1 = 6f;
        public const float TimeForWaitBeforeAddingBotFor2vs2 = 8f;
        public const float TimeForWaitBeforeAddingBotFor3vs3 = 12f;
        public const float TimeForWaitBeforeAddingBotForBattleRoyale = 20f;        
        public const float TimeForMakingIsChekedToREADY = 4f;
        public const int HowManyPlayersInBattleRoyale = 8;
        public const float TimeForDeleteInactiveVisitor = 3f;
        public const int MaxTrySendingUDPWithPromt = 4;

        //gamehubs cheking
        public static List<int> existingRegionServers = new List<int>();

        //UDP     
        public const int port_udp_for_GameHubs = 2325;
        public const int port_udp_for_SETUP = 2327;
        public static UDPServerConnector ServerUDP;

        //TCP        
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        private static IPAddress ipaddress_tcp;
        private static IPEndPoint localendpoint_tcp;
        private static Socket socket_tcp;
        private const int port_tcp = 2326;
        private const int max_connections = 100000;      
        private static byte[] buffer_send_tcp = new byte[2048];

        

        //START FOR TCP
        public static void Server_init_TCP_UDP()
        {
            //get right region===========================
            //List<int> existingRegionServers = new List<int>();
            foreach (string keys in starter.GameServerHUBs.Keys)
            {
                if (!existingRegionServers.Contains(starter.GameServerHUBs[keys].ServerLocation))
                {
                    existingRegionServers.Add(starter.GameServerHUBs[keys].ServerLocation);
                }
            }

            if (existingRegionServers.Count <= 1)
            {
                AddCheCkQueueTimer(1, 999);
            }
            else
            {
                int _delay = 0;
                foreach (int item in existingRegionServers)
                {                    
                    AddCheCkQueueTimer(_delay, item);
                    _delay += 500;
                }
            }
            //===========================================

            //==============timer for visitors========
            visitorsTimer = new System.Timers.Timer(1000);

            visitorsTimer.Elapsed += delegate {
                checkVisitorsActiveStatus();
            };

            visitorsTimer.AutoReset = true;
            visitorsTimer.Enabled = true;
            //=========================================

            
            Task.Run(() =>
            {
                ServerUDP = new UDPServerConnector(IPAddress.Any, port_udp_for_SETUP);
                ServerUDP.Start();

            });

            //TCP config===================================
            ipaddress_tcp = IPAddress.Any;
            localendpoint_tcp = new IPEndPoint(ipaddress_tcp, port_tcp);
            socket_tcp = new Socket(ipaddress_tcp.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine(DateTime.Now + ": " + "setup server TCP initiated");

            try
            {
                socket_tcp.Bind(localendpoint_tcp);
                socket_tcp.Listen(max_connections);

                while (true)
                {
                    // Set the event to nonsignaled state.  
                    allDone.Reset();
                    
                    socket_tcp.BeginAccept(new AsyncCallback(AcceptCallbackTCP), socket_tcp);

                    allDone.WaitOne();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }
            //TCP config===================================
        }


        public static void AcceptCallbackTCP(IAsyncResult ar)
        {
            try
            {
                // Signal the main thread to continue.  
                allDone.Set();
                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);

                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = handler;
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallbackTCP), state);
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }

        }

        public static void ReadCallbackTCP(IAsyncResult ar)
        {
            try
            {
              
                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    Task.Run(()=>IncomingDataHadler.HandleIncomingTCP(bytesRead, handler, state.buffer));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }

        }

        public static string SendAndGetTCP_between_servers(string DataForSending, int CurrentPort, string IP, bool is_it_encoded)
        {
          
            int CurrentPort_tcp = (int)CurrentPort;
            string CurrentIP_tcp = IP;

            if (string.IsNullOrEmpty(IP))
            {
                return "error";
            }

            string result = null;

            IPEndPoint endpoint_tcp = new IPEndPoint(IPAddress.Parse(CurrentIP_tcp), CurrentPort_tcp);
            Socket sck_tcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //=============================CONNECT======================================
            try
            {
                sck_tcp.Connect(endpoint_tcp);
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
                result = ex.ToString();

                //sck_tcp.Shutdown(SocketShutdown.Both);
                //sck_tcp.Close();
                return result;
            }
            //===============================SEND======================================
            try
            {
                byte[] data_to_s = Encoding.UTF8.GetBytes(DataForSending);

                if (is_it_encoded)
                {
                    encryption.Encode(ref data_to_s, starter.secret_key_for_game_servers);
                }

                sck_tcp.Send(data_to_s);
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
                result = ex.ToString();

                sck_tcp.Shutdown(SocketShutdown.Both);
                sck_tcp.Close();
                return result;
            }
            //================================GET======================================
            try
            {
                StringBuilder messrec = new StringBuilder();
                byte[] msgBuff = new byte[2048];
                int size = 0;

                {
                    size = sck_tcp.Receive(msgBuff);
                    messrec.Append(Encoding.UTF8.GetString(msgBuff, 0, size));
                }
                while (sck_tcp.Available > 0) ;



                if (messrec.ToString() == "nsc")
                {
                    sck_tcp.Shutdown(SocketShutdown.Both);
                    sck_tcp.Close();
                    return "nsc";
                }

                if (is_it_encoded)
                {
                    byte[] data_r = new byte[size];

                    for (int i = 0; i < size; i++)
                    {
                        data_r[i] = msgBuff[i];
                    }
                    encryption.Decode(ref data_r, starter.secret_key_for_game_servers);
                    messrec.Clear();
                    messrec.Append(Encoding.UTF8.GetString(data_r, 0, data_r.Length));
                }

                if (messrec.ToString() != "" && messrec.ToString() != null)
                {
                    result = messrec.ToString();
                }
                else
                {
                    result = "error in received data";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
                result = ex.ToString();

                sck_tcp.Shutdown(SocketShutdown.Both);
                sck_tcp.Close();
                return result;
            }
            //error case
            sck_tcp.Shutdown(SocketShutdown.Both);
            sck_tcp.Close();
            return result;

        }


        public static string SendTCP_between_servers(string DataForSending, int CurrentPort, string IP, bool is_it_encoded)
        {

            int CurrentPort_tcp = (int)CurrentPort;
            string CurrentIP_tcp = IP;

            if (string.IsNullOrEmpty(CurrentIP_tcp))
            {
                return "error";
            }

            string result = null;

            IPEndPoint endpoint_tcp = new IPEndPoint(IPAddress.Parse(CurrentIP_tcp), CurrentPort_tcp);
            Socket sck_tcp = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //=============================CONNECT======================================
            try
            {
                sck_tcp.Connect(endpoint_tcp);
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
                result = ex.ToString();

                //sck_tcp.Shutdown(SocketShutdown.Both);
                //sck_tcp.Close();
                return result;
            }
            //===============================SEND======================================
            try
            {
                byte[] data_to_s = Encoding.UTF8.GetBytes(DataForSending);

                if (is_it_encoded)
                {
                    encryption.Encode(ref data_to_s, starter.secret_key_for_game_servers);
                }

                sck_tcp.Send(data_to_s);
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
                result = ex.ToString();

                sck_tcp.Shutdown(SocketShutdown.Both);
                sck_tcp.Close();
                return result;
            }

            //error case
            sck_tcp.Shutdown(SocketShutdown.Both);
            sck_tcp.Close();
            return result;

        }

        public static Task SendDataTCP(Socket handler, String data)
        {
          
            try
            {
                // Convert the string data to byte data using ASCII encoding.  
                buffer_send_tcp = Encoding.UTF8.GetBytes(data);

                // Begin sending the data to the remote device.  
                handler.BeginSend(buffer_send_tcp, 0, buffer_send_tcp.Length, 0, new AsyncCallback(SendCallback), handler);
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }

            return Task.CompletedTask;

        }


        public static Task SendDataTCP(Socket handler, byte [] data)
        {
            //Console.WriteLine(Encoding.UTF8.GetString(data) + " - send");

            try
            {
                // Convert the string data to byte data using ASCII encoding.  
                buffer_send_tcp = data;

                // Begin sending the data to the remote device.  
                handler.BeginSend(buffer_send_tcp, 0, buffer_send_tcp.Length, 0, new AsyncCallback(SendCallback), handler);
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }

            return Task.CompletedTask;

        }


        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = handler.EndSend(ar);
                

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }
        }



        public static async void SendDataUDPWithChekReceiving(EndPoint ipEnd, string data)
        {
            

            try
            {
                string _key = functions.get_random_set_of_symb(8);
                PacketsAwaitingForCheking.Add(_key, new CheckingIfUDPPacketReceived(data, ipEnd));

                for (int i = 0; i < MaxTrySendingUDPWithPromt; i++)
                {
                    if (PacketsAwaitingForCheking.ContainsKey(_key))
                    {
                        ServerUDP.Send(ipEnd, data);
                    }

                    await Task.Delay(250);
                }

                
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }
            
        }


        public static Task SendDataUDP(EndPoint ipEnd, string data)
        {
            try
            {                
                ServerUDP.Send(ipEnd, data);                
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }
            return Task.CompletedTask;
        }

        public static Task SendDataUDP(EndPoint ipEnd, byte[] data)
        {
            try
            {                
                ServerUDP.Send(ipEnd, data);                
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }
            return Task.CompletedTask;
        }


        public static void CheckAndSendUDPPacketsControl()
        {
            foreach (string keys in PacketsAwaitingForCheking.Keys)
            {
                SendDataUDP(PacketsAwaitingForCheking[keys].address, PacketsAwaitingForCheking[keys].packet);
            }
        }


        public static void checkVisitorsActiveStatus()
        {
            foreach (string key in CurrentVisitors.Keys)
            {
                if (CurrentVisitors[key].GetLastUpdate.AddSeconds(TimeForDeleteInactiveVisitor)<DateTime.Now)
                {
                    CurrentVisitors.Remove(key);
                }
            }
        }


        //start main timer for checking PVP
        public static async void AddCheCkQueueTimer(int delay, int region)
        {
            await Task.Delay(delay);

            queueTimer = new System.Timers.Timer(2000);

            queueTimer.Elapsed += delegate {
                check_queue_for_pvp(region);
            };

            queueTimer.AutoReset = true;
            queueTimer.Enabled = true;            
        }



        public static void check_queue_for_pvp(int region_id)
        {
                       
            try
            {
                        
                if (GameSessionsAwaiting.Count > 0)
                {                            
                    foreach (GameSessions item in GameSessionsAwaiting)
                    {
                       
                        //cheking for gamesession with allready got away players=======================
                        if (item.GetPlayers().Count == 0)
                        {

                            GameSessionsAwaiting.Remove(item);
                        }
                    }
                    //===============================================================================                    
                }
          
                List<PlayerForGameSession> looking_for_2 = new List<PlayerForGameSession>(2);
                List<PlayerForGameSession> looking_for_4 = new List<PlayerForGameSession>(4);
                List<PlayerForGameSession> looking_for_6 = new List<PlayerForGameSession>(6);
                List<PlayerForGameSession> looking_for_BR = new List<PlayerForGameSession>(HowManyPlayersInBattleRoyale);
                List<PlayerForGameSession> looking_for_Any = new List<PlayerForGameSession>();

                foreach (string keys in PlayersAwaiting.Keys)
                {
                    //filter for region game===============================
                    if (region_id != 999)
                    {
                        if (PlayersAwaiting[keys].ServerLocation != region_id && existingRegionServers.Contains(PlayersAwaiting[keys].ServerLocation))
                        {
                            continue;
                        }
                        else if((region_id == 0 || region_id == 3) && PlayersAwaiting[keys].ServerLocation == 2 && !existingRegionServers.Contains(2))
                        {
                            continue;
                        }
                        else if((region_id == 0 || region_id == 2) && PlayersAwaiting[keys].ServerLocation == 3 && !existingRegionServers.Contains(3))
                        {
                            continue;
                        }
                    }
                    //======================================================

                    //cleaning for old unupdated============================
                    if (PlayersAwaiting[keys].WhenLastUpdated().AddSeconds(LimitForIdlePlayerToLoseQueue) < DateTime.Now && PlayersAwaiting[keys].GetCurrentPlayerStatus()!=PlayerStatus.isGone) 
                    {
                        Console.WriteLine(DateTime.Now + ": removed from queue character - " + PlayersAwaiting[keys].GetCharacterName());
                       
                        PlayersAwaiting[keys].ResetPlayerStatusToNonBusy();

                        PlayersAwaiting.Remove(keys);

                        continue;
                    }
                    //=======================================================

                    if (!PlayersAwaiting[keys].isPlayerBusyForSession())
                    {
                        switch((int)PlayersAwaiting[keys].GetPlayerGameType())
                        {
                            case 1:
                                looking_for_2.Add(PlayersAwaiting[keys]);
                                //Console.WriteLine(region_id + ": " + PlayersAwaiting[keys].GetCharacterName() + " - " + PlayersAwaiting[keys].ServerLocation);
                                break;

                            case 2:
                                looking_for_4.Add(PlayersAwaiting[keys]);
                                break;

                            case 3:
                                looking_for_6.Add(PlayersAwaiting[keys]);
                                break;

                            case 4:
                                //looking_for_BR.Add(PlayersAwaiting[keys]);
                                break;

                            case 5:
                                looking_for_Any.Add(PlayersAwaiting[keys]);
                                break;

                            case 6:
                                GameSessionsAwaiting.Add(new GameSessions(new List<PlayerForGameSession> { PlayersAwaiting[keys] }, GameTypes.training_room, region_id));

                                break;
                        }
                    }

                    
                }

                //Console.WriteLine(looking_for_2.Count + " - 1vs1..." + looking_for_4.Count + " - 2vs2..." + looking_for_BR.Count + " - BR...."  + looking_for_Any.Count + " - Any...");

                

                //dealing with 1vs1=================================================
                if (looking_for_2.Count>1)
                {
                    int _count = looking_for_2.Count / 2;
                    if (_count > 0)
                    {
                        for (int i = 0; i < _count; i += 2)
                        {                            
                            GameSessionsAwaiting.Add(new GameSessions(new List<PlayerForGameSession> { looking_for_2[i], looking_for_2[i+1] }, GameTypes.PvP_1vs1, region_id));
                          
                        }
                    }
                        
                } 
                else if (looking_for_2.Count == 1 && looking_for_Any.Count>0)
                {                    
                    GameSessionsAwaiting.Add(new GameSessions(new List<PlayerForGameSession> { looking_for_2[0], looking_for_Any[0] }, GameTypes.PvP_1vs1, region_id));
                
                }
                else if (looking_for_2.Count == 1 && looking_for_Any.Count == 0)
                {
                    if (looking_for_2[0].WhenStarted().AddSeconds(TimeForWaitBeforeAddingBot1vs1) < DateTime.Now)
                    {
                        GameSessionsAwaiting.Add(new GameSessions(new List<PlayerForGameSession> { looking_for_2[0], GetBotForPvP(GameTypes.PvP_1vs1) }, GameTypes.PvP_1vs1, region_id));
                        
                    }
                }
                //================================================================

                //dealing with 2vs2================================================
                if (looking_for_4.Count > 3)
                {
                    int _count = looking_for_4.Count / 4;
                    if (_count > 0)
                    {
                        for (int i = 0; i < _count; i+=4)
                        {
                            GameSessionsAwaiting.Add(new GameSessions(new List<PlayerForGameSession> { looking_for_4[i], looking_for_4[i+1], looking_for_4[i+2], looking_for_4[i+3] }, GameTypes.PvP_2vs2, region_id));
                           
                        }
                    }

                }
                else if (looking_for_4.Count > 0 && looking_for_4.Count <= 3)
                {
                    int howMany = 0;
                    List<PlayerForGameSession> _temporary2vs2 = new List<PlayerForGameSession>();
                    
                    for (int i = 0; i < looking_for_4.Count; i++)
                    {
                        howMany++;
                        _temporary2vs2.Add(looking_for_4[i]);
                        Console.WriteLine("added : " + looking_for_4[i].GetCharacterName());
                       
                    }

                    int delta = looking_for_Any.Count > (4 - howMany) ? (4 - howMany) : looking_for_Any.Count;

                    for (int i = 0; i < delta; i++)
                    {
                        howMany++;
                        _temporary2vs2.Add(looking_for_Any[i]);
                        
                    }

                    if (howMany == 4)
                    {
                        GameSessionsAwaiting.Add(new GameSessions(_temporary2vs2, GameTypes.PvP_2vs2, region_id));
                    } 
                    else if (howMany < 4 && _temporary2vs2[0].WhenStarted().AddSeconds(TimeForWaitBeforeAddingBotFor2vs2) < DateTime.Now)
                    {
                        for (int i = 0; i < (4 - howMany); i++)
                        {
                            _temporary2vs2.Add(GetBotForPvP(GameTypes.PvP_2vs2));
                            
                        }

                        GameSessionsAwaiting.Add(new GameSessions(_temporary2vs2, GameTypes.PvP_2vs2, region_id));
                    }                   
                }
                //==================================================================================================

                //dealing with 3vs3================================================
                if (looking_for_6.Count > 5)
                {
                    int _count = looking_for_6.Count / 6;
                    if (_count > 0)
                    {
                        for (int i = 0; i < _count; i += 6)
                        {
                            GameSessionsAwaiting.Add(new GameSessions(new List<PlayerForGameSession> { looking_for_6[i], looking_for_6[i + 1], looking_for_6[i + 2], looking_for_6[i + 3], looking_for_6[i + 4], looking_for_6[i + 5] }, GameTypes.PvP_3vs3, region_id));

                        }
                    }

                }
                else if (looking_for_6.Count >= 3 && looking_for_6.Count <= 5)
                {
                    int howManyPlayers = 0;
                    List<PlayerForGameSession> _temporary3vs3 = new List<PlayerForGameSession>(6);

                    for (int i = 0; i < looking_for_6.Count; i++)
                    {
                        _temporary3vs3.Add(looking_for_6[i]);
                        howManyPlayers++;
                    }

                    if (howManyPlayers < 6 && looking_for_Any.Count > 0)
                    {
                        for (int i = 0; i < looking_for_Any.Count; i++)
                        {
                            _temporary3vs3.Add(looking_for_Any[i]);
                            howManyPlayers++;
                            if (_temporary3vs3.Count == 6)
                            {
                                GameSessionsAwaiting.Add(new GameSessions(_temporary3vs3, GameTypes.PvP_3vs3, region_id));
                                break;
                            }
                        }
                    }
                    
                    if (howManyPlayers < 6 && _temporary3vs3[0].WhenStarted().AddSeconds(TimeForWaitBeforeAddingBotFor3vs3) < DateTime.Now)
                    {
                        //Console.WriteLine("yes 111111111111111111111111111" + howManyPlayers  + " - " + _temporary3vs3.Count);
                        int delta = 6 - howManyPlayers;
                        for (int i = 0; i < delta; i++)
                        {
                            //Console.WriteLine("yes 22222222222222222" + howManyPlayers + " - " + _temporary3vs3.Count);
                            _temporary3vs3.Add(GetBotForPvP(GameTypes.PvP_3vs3));
                            howManyPlayers++;
                            if (_temporary3vs3.Count == 6)
                            {
                                
                                GameSessionsAwaiting.Add(new GameSessions(_temporary3vs3, GameTypes.PvP_3vs3, region_id));
                                break;
                            }
                        }
                    }                   
                }
                //==================================================================================================



                //dealing with Battle Royale============================================================================
                if (looking_for_BR.Count >= HowManyPlayersInBattleRoyale)
                {
                    int _count = looking_for_BR.Count / HowManyPlayersInBattleRoyale;
                    if (_count > 0)
                    {
                        for (int i = 0; i < _count; i+=HowManyPlayersInBattleRoyale)
                        {
                            List<PlayerForGameSession> _temporaryBR = new List<PlayerForGameSession>();
                            for (int u = 0; u < HowManyPlayersInBattleRoyale; u++)
                            {
                                _temporaryBR.Add(looking_for_BR[i+u]);
                                
                            }
                            GameSessionsAwaiting.Add(new GameSessions(_temporaryBR, GameTypes.PvP_battle_royale, region_id));
                         
                        }
                    }

                }
                else if (looking_for_BR.Count > 0 && looking_for_BR.Count < HowManyPlayersInBattleRoyale)
                {
                    int howMany = 0;
                    List<PlayerForGameSession> _temporaryBR = new List<PlayerForGameSession>();

                    for (int i = 0; i < looking_for_BR.Count; i++)
                    {
                        howMany++;
                        _temporaryBR.Add(looking_for_BR[i]);
                        
                    }

                    int delta = looking_for_Any.Count > (HowManyPlayersInBattleRoyale - howMany) ? (HowManyPlayersInBattleRoyale - howMany) : looking_for_Any.Count;

                    for (int i = 0; i < delta; i++)
                    {
                        howMany++;
                        _temporaryBR.Add(looking_for_Any[i]);
                        
                    }

                    if (howMany == HowManyPlayersInBattleRoyale)
                    {
                        GameSessionsAwaiting.Add(new GameSessions(_temporaryBR, GameTypes.PvP_battle_royale, region_id));
                    }
                    else if (howMany < HowManyPlayersInBattleRoyale && _temporaryBR[0].WhenStarted().AddSeconds(TimeForWaitBeforeAddingBotForBattleRoyale) < DateTime.Now)
                    {
                        for (int i = 0; i < (HowManyPlayersInBattleRoyale - howMany); i++)
                        {
                            _temporaryBR.Add(GetBotForPvP(GameTypes.PvP_battle_royale));                            
                        }

                        GameSessionsAwaiting.Add(new GameSessions(_temporaryBR, GameTypes.PvP_battle_royale, region_id));
                    }

                }
                //=================================



                //dealing with any game=================================================
                if (looking_for_Any.Count > 0)
                {
                    if (looking_for_Any.Count == 1 && looking_for_Any[0].WhenStarted().AddSeconds(TimeForWaitBeforeAddingBot1vs1) < DateTime.Now)
                    {
                        GameSessionsAwaiting.Add(new GameSessions(new List<PlayerForGameSession> { looking_for_Any[0], GetBotForPvP(GameTypes.PvP_1vs1) }, GameTypes.PvP_1vs1, region_id));
                    }
                    else if (looking_for_Any.Count >= 4)
                    {
                        List<PlayerForGameSession> _temporaryAny = new List<PlayerForGameSession>();

                        for (int i = 0; i < 4; i++)
                        {
                            _temporaryAny.Add(looking_for_Any[i]);
                        }

                        GameSessionsAwaiting.Add(new GameSessions(_temporaryAny, GameTypes.PvP_2vs2, region_id));
                    }
                    else
                    {
                        List<PlayerForGameSession> _temporaryAny = new List<PlayerForGameSession>();

                        for (int i = 0; i < 2; i++)
                        {
                            _temporaryAny.Add(looking_for_Any[i]);
                        }

                        GameSessionsAwaiting.Add(new GameSessions(_temporaryAny, GameTypes.PvP_1vs1, region_id));
                    }

                }
                
                //================================================================


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }

        private static PlayerForGameSession GetBotForPvP(GameTypes _type)
        {            
            Random rnd = new Random();

            switch (rnd.Next(1, 3))
            {
                case 1:
                    return new PlayerForGameSession("-1", "warrior bot", "botttt", _type, 0, 0, true);
                    
                case 2:
                    return new PlayerForGameSession("-2", "elem bot", "botttt", _type, 0, 0, true);
                    
            }

            return null;
        }

        public static async Task SendPing(EndPoint EP, string ID)
        {
            try
            {                
                PingProcessor _ping_p = new PingProcessor();
                SendPingsList.Add(ID, _ping_p);
                _ping_p.SendTime = starter.stopWatch.ElapsedMilliseconds;
                _ping_p.ID = ID;

                string result = $"0~7~{starter.InnerServerConnectionPassword}~{ID}";
                byte[] b = Encoding.UTF8.GetBytes(result);
                encryption.Encode(ref b, starter.secret_key_for_game_servers);

                await Server.SendDataUDP(EP, b);
                KillSendPingKey(ID);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static async Task<string> CheckAndGetGameHubs(int region_id)
        {
            Dictionary<string, GameHubsSpec> currentHubs = new Dictionary<string, GameHubsSpec>();
            if (region_id==999)
            {
                currentHubs = starter.GameServerHUBs;
            }
            else
            {
                foreach (string keys in starter.GameServerHUBs.Keys)
                {
                    if (starter.GameServerHUBs[keys].ServerLocation==region_id)
                    {
                        currentHubs.Add(keys, starter.GameServerHUBs[keys]);
                    }
                }
            }


            foreach (string keys in currentHubs.Keys)
            {               
                try
                {
                    string ID = functions.get_random_set_of_symb(6);

                    await SendPing(IPEndPoint.Parse($"{currentHubs[keys].GetIP()}:{port_udp_for_GameHubs}"), ID);
                    await Task.Delay(1000);

                    if (SendPingsList.ContainsKey(ID) && SendPingsList[ID].isItOK())
                    {

                        currentHubs[keys].SetActive();
                        currentHubs[keys].SetPing(SendPingsList[ID].GetPing());
                        currentHubs[keys].SetSessions(SendPingsList[ID].GetSessions());
                        SendPingsList.Remove(ID);
                    } 
                    else
                    {
                        
                        Console.WriteLine(DateTime.Now + ": server is inactive - no ping - " + currentHubs[keys].GetIP());
                        currentHubs[keys].SetnonActive();
                        
                    }

                  
                    Console.WriteLine(currentHubs[keys].GetActiveState() + " - " + currentHubs[keys].GetIP() + " - " + currentHubs[keys].GetPing());

                }
                catch (Exception ex)
                {
                    currentHubs[keys].SetnonActive();
                    Console.WriteLine(DateTime.Now + ": server is inactive - no ping - " + currentHubs[keys].GetIP());
                }
            }

            int sessions = 100000;
            string IP = null;
            foreach (string keys in currentHubs.Keys)
            {
                if (currentHubs[keys].GetActiveState() && currentHubs[keys].GetSessions()<sessions)
                {
                    IP = currentHubs[keys].GetIP();
                    sessions = currentHubs[keys].GetSessions();
                }
            }

            if (!string.IsNullOrEmpty(IP))
            {
                return IP;
            }
            else
            {
                return "error";
            }            
        }

        public static async void KillSendPingKey(string ID)
        {
            await Task.Delay(5000);

            if (SendPingsList.ContainsKey(ID)) SendPingsList.Remove(ID);
        }
    }

   
    public class GameHubsSpec
    {
        private string hub_IP;
        private bool isActive;
        private List<long> ping;
        private int number_of_sessions;
        private int serverLocation;

        //EUROPE 0
        //USA 1
        //Asia 2
        //SA 3

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


        public GameHubsSpec(string _ip, int server)
        {
            hub_IP = _ip;
            isActive = true;
            ping = new List<long>();
            number_of_sessions = 0;
            serverLocation = server;
        }

        public void SetnonActive()
        {
            isActive = false;
        }

        public void SetActive()
        {
            isActive = true;
        }

        public void SetSessions(int _sessions)
        {
            number_of_sessions = _sessions;
        }

        public int GetSessions()
        {
            return number_of_sessions;
        }

        public void SetPing(long _ping)
        {
            ping.Add(_ping);

            if (ping.Count>15)
            {
                ping.Remove(ping[0]);
            }
        }

        public int GetPing()
        {
            if (ping.Count>0)
            {
                return (int)(ping.Sum() / ping.Count);
            } 
            else
            {
                return 0;
            }
            
        }

        public bool GetActiveState()
        {
            return isActive;
        }

        public string GetIP()
        {
            return hub_IP;
        }
        
    }


    // State object for reading client data asynchronously  
    public class StateObject
    {
        // Size of receive buffer.  
        public const int BufferSize = 2048;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Received data string.
        public StringBuilder sb = new StringBuilder();

        // Client socket.
        public Socket workSocket = null;
    }

    class PingProcessor
    {
        public string ID;
        public long SendTime;
        public long ReceivedTime;
        public int Sessions;
        public bool isOK;

        public void SetReceivedTime(long _data)
        {
            ReceivedTime = _data;
        }

        public void SetSessions(int _data)
        {
            Sessions = _data;
        }

        public void SetOKGood()
        {
            isOK = true;
        }

        public bool isItOK()
        {
            return isOK;
        }

        public int GetSessions()
        {
            return Sessions;
        }

        public int GetPing()
        {
            return (int)(ReceivedTime - SendTime);
        }

    }


    class UDPServerConnector : UdpServer
    {

        private StringBuilder raw_data_received_udp = new StringBuilder(2048);

        public UDPServerConnector(IPAddress address, int port) : base(address, port) { }


        protected override void OnStarted()
        {
            // Start receive datagrams
            try
            {
                Console.WriteLine(DateTime.Now + ": " + "game server UDP initiated");
                ReceiveAsync();

            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }

        }

        protected override void OnReceived(EndPoint endpoint, byte[] buffer, long offset, long size)
        {

            if (size == 0)
            {

                // Important: Receive using thread pool is necessary here to avoid stack overflow with Socket.ReceiveFromAsync() method!
                //ThreadPool.QueueUserWorkItem(o => { ReceiveAsync(); });

            }
            else
            {
                                
                byte[] t = new byte[(int)size];

                for (int i = 0; i < (int)size; i++)
                {                    
                    t[i] = buffer[i];
                }

                IncomingDataHadler.HandleIncomingUDP(endpoint, t);
            }

            ReceiveAsync();

        }


        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Server caught an error with code {error} ");

        }
    }

    public struct CheckingIfUDPPacketReceived
    {
        public EndPoint address;
        public string packet;

        public CheckingIfUDPPacketReceived(string _packet, EndPoint _address)
        {
            packet = _packet;
            address = _address;
        }

    }

    public class VisitorData
    {
        private string characterName;
        private string characterID;
        private string ticket;
        private DateTime lastUpdate;
        private EndPoint address;

        public VisitorData(string name, string ticket_id, EndPoint _address, string char_id)
        {
            Console.WriteLine(DateTime.Now +  ": added new visitor " + name + " with ticket " + ticket_id);
            characterName = name;
            ticket = ticket_id;
            lastUpdate = DateTime.Now;
            address = _address;
            characterID = char_id;
        }

        public string CharacterID
        {
            get
            {
                return characterID;
            }
            private set
            {
                characterID = value;
            }
        }

        public EndPoint Address
        {
            get
            {
                return address;
            }

        }

        public void SetAddress(EndPoint _address)
        {
            address = _address;
        }

        public string GetCharacterName
        {
            get
            {
                return characterName;
            }
        }

        public string GetTicket
        {
            get
            {
                return ticket;
            }
        }

        public DateTime GetLastUpdate
        {
            get
            {
                return lastUpdate;
            }
        }

        public void Update()
        {
            lastUpdate = DateTime.Now;
        }

    }

}
