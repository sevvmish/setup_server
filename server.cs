﻿using System;
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
        public static Dictionary<string, PingProcessor> SendPingsList = new Dictionary<string, PingProcessor>();
        public static Dictionary<string, long> ReceivedPingList = new Dictionary<string, long>();
        public static Dictionary<string, byte[]> Sessions = new Dictionary<string, byte[]>();
        public static Dictionary<string, DateTime> pvp1vs1 = new Dictionary<string, DateTime>();
     
        public static Dictionary<string, PlayerForGameSession> PlayersAwaiting = new Dictionary<string, PlayerForGameSession>();

        public static System.Timers.Timer _timer;

        public static HashSet<GameSessions> GameSessionsAwaiting = new HashSet<GameSessions>();
        public static Dictionary<string, GameSessionResults> GameSessionWaitingForResult = new Dictionary<string, GameSessionResults>();

        public const float LimitForIdlePlayerToLoseQueue = 6f;
        public const float LimitForLonelyPlayerToLoseQueue = 700f;
        public const float TimeForWaitBeforeAddingBot1vs1 = 10f;
        public const float TimeForWaitBeforeAddingBotFor2vs2 = 15f;
        public const float TimeForWaitBeforeAddingBotForBattleRoyale = 20f;        
        public const float TimeForMakingIsChekedToREADY = 10f;
        public const int HowManyPlayersInBattleRoyale = 8;

        //gamehubs cheking

        //UDP     
        private const int port_udp_for_GameHubs = 2325;    
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
        public static void Server_init_TCP()
        {
            //start checker for PVP
            //Task.Run(() => check_queue_for_pvp());

            _timer = new System.Timers.Timer(2000);

            _timer.Elapsed += delegate {
                check_queue_for_pvp();
            };

            _timer.AutoReset = true;
            _timer.Enabled = true;

            Task.Run(() =>
            {
                ServerUDP = new UDPServerConnector(IPAddress.Any, 2327);
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
                    state.sb.Append(Encoding.UTF8.GetString(state.buffer, 0, bytesRead));
                    
                    if (Encoding.UTF8.GetString(state.buffer, 0, 4) == "5~0~")
                    {

                        byte[] d = new byte[bytesRead];
                        for (int i = 0; i < bytesRead; i++)
                        {
                            d[i] = state.buffer[i];
                        }

                        encryption.Decode(ref d, starter.secret_key_for_game_servers);

                        string res = packet_analyzer.ProcessTCPInput(Encoding.UTF8.GetString(d), handler.RemoteEndPoint.ToString());

                        SendDataTCP(handler, $"5~0~{res}");
                    } else if (!Sessions.ContainsKey(Encoding.UTF8.GetString(state.buffer, 0, 5)))  
                    {
                 
                        packet_analyzer.StartSessionTCPInput(Encoding.UTF8.GetString(state.buffer, 0, bytesRead), handler);                        
                    }                    
                    else
                    {
                        byte[] d = new byte[bytesRead];
                        for (int i = 0; i < bytesRead; i++)
                        {
                            d[i] = state.buffer[i];
                        }

                        encryption.Decode(ref d, Sessions[Encoding.UTF8.GetString(state.buffer, 0, 5)]);

                        string back_result = packet_analyzer.ProcessTCPInput(Encoding.UTF8.GetString(d).Remove(0, 6), handler.RemoteEndPoint.ToString());
                                           
                        byte[] t = Encoding.UTF8.GetBytes(back_result);
                        encryption.Encode(ref t, Sessions[Encoding.UTF8.GetString(state.buffer, 0, 5)]);

                        SendDataTCP(handler, t);                                            
                        
                    }


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

                sck_tcp.Shutdown(SocketShutdown.Both);
                sck_tcp.Close();
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


        public static Task SendDataUDP(EndPoint ipEnd, string data)
        {
            try
            {
                //ServerUDP.SendAsync(ipEnd, data);
                ServerUDP.Send(ipEnd, data);
                //Console.WriteLine("out&" + data + "$" + starter.stopWatch.ElapsedMilliseconds);
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
                //ServerUDP.SendAsync(ipEnd, data);
                ServerUDP.Send(ipEnd, data);
                //Console.WriteLine("out&" + data + "$" + starter.stopWatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
            }
            return Task.CompletedTask;
        }

        

        public static void check_queue_for_pvp()
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
                List<PlayerForGameSession> looking_for_BR = new List<PlayerForGameSession>(HowManyPlayersInBattleRoyale);
                List<PlayerForGameSession> looking_for_Any = new List<PlayerForGameSession>();

                foreach (string keys in PlayersAwaiting.Keys)
                {
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
                                break;

                            case 2:
                                looking_for_4.Add(PlayersAwaiting[keys]);
                                break;

                            case 3:
                                looking_for_BR.Add(PlayersAwaiting[keys]);
                                break;

                            case 4:
                                looking_for_Any.Add(PlayersAwaiting[keys]);
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
                            GameSessionsAwaiting.Add(new GameSessions(new List<PlayerForGameSession> { looking_for_2[i], looking_for_2[i+1] }, GameTypes.PvP_1vs1));
                            //looking_for_2.Remove(looking_for_2[0]);
                            //looking_for_2.Remove(looking_for_2[1]);
                        }
                    }
                        
                } 
                else if (looking_for_2.Count == 1 && looking_for_Any.Count>0)
                {                    
                    GameSessionsAwaiting.Add(new GameSessions(new List<PlayerForGameSession> { looking_for_2[0], looking_for_Any[0] }, GameTypes.PvP_1vs1));
                    //looking_for_2.Remove(looking_for_2[0]);
                    //looking_for_Any.Remove(looking_for_Any[0]);
                }
                else if (looking_for_2.Count == 1 && looking_for_Any.Count == 0)
                {
                    if (looking_for_2[0].WhenStarted().AddSeconds(TimeForWaitBeforeAddingBot1vs1) < DateTime.Now)
                    {
                        GameSessionsAwaiting.Add(new GameSessions(new List<PlayerForGameSession> { looking_for_2[0], GetBotForPvP(GameTypes.PvP_1vs1) }, GameTypes.PvP_1vs1));
                        //looking_for_2.Remove(looking_for_2[0]);                        
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
                            GameSessionsAwaiting.Add(new GameSessions(new List<PlayerForGameSession> { looking_for_4[i], looking_for_4[i+1], looking_for_4[i+2], looking_for_4[i+3] }, GameTypes.PvP_2vs2));
                            //looking_for_4.Remove(looking_for_4[0]);
                            //looking_for_4.Remove(looking_for_4[1]);
                            //looking_for_4.Remove(looking_for_4[2]);
                            //looking_for_4.Remove(looking_for_4[3]);
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
                        //looking_for_4.Remove(looking_for_4[i]);
                    }

                    int delta = looking_for_Any.Count > (4 - howMany) ? (4 - howMany) : looking_for_Any.Count;

                    for (int i = 0; i < delta; i++)
                    {
                        howMany++;
                        _temporary2vs2.Add(looking_for_Any[i]);
                        //looking_for_Any.Remove(looking_for_Any[i]);
                    }

                    if (howMany == 4)
                    {
                        GameSessionsAwaiting.Add(new GameSessions(_temporary2vs2, GameTypes.PvP_2vs2));
                    } 
                    else if (howMany < 4 && _temporary2vs2[0].WhenStarted().AddSeconds(TimeForWaitBeforeAddingBotFor2vs2) < DateTime.Now)
                    {
                        for (int i = 0; i < (4 - howMany); i++)
                        {
                            _temporary2vs2.Add(GetBotForPvP(GameTypes.PvP_2vs2));
                            
                        }

                        GameSessionsAwaiting.Add(new GameSessions(_temporary2vs2, GameTypes.PvP_2vs2));
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
                                //looking_for_BR.Remove(looking_for_BR[u]);
                            }
                            GameSessionsAwaiting.Add(new GameSessions(_temporaryBR, GameTypes.PvP_battle_royale));
                         
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
                        //looking_for_BR.Remove(looking_for_BR[i]);
                    }

                    int delta = looking_for_Any.Count > (HowManyPlayersInBattleRoyale - howMany) ? (HowManyPlayersInBattleRoyale - howMany) : looking_for_Any.Count;

                    for (int i = 0; i < delta; i++)
                    {
                        howMany++;
                        _temporaryBR.Add(looking_for_Any[i]);
                        //looking_for_Any.Remove(looking_for_Any[i]);
                    }

                    if (howMany == HowManyPlayersInBattleRoyale)
                    {
                        GameSessionsAwaiting.Add(new GameSessions(_temporaryBR, GameTypes.PvP_battle_royale));
                    }
                    else if (howMany < HowManyPlayersInBattleRoyale && _temporaryBR[0].WhenStarted().AddSeconds(TimeForWaitBeforeAddingBotForBattleRoyale) < DateTime.Now)
                    {
                        for (int i = 0; i < (HowManyPlayersInBattleRoyale - howMany); i++)
                        {
                            _temporaryBR.Add(GetBotForPvP(GameTypes.PvP_battle_royale));                            
                        }

                        GameSessionsAwaiting.Add(new GameSessions(_temporaryBR, GameTypes.PvP_battle_royale));
                    }

                }
                //=================================



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
                    return new PlayerForGameSession("-1", "warrior bot", "botttt", _type, 0);
                    break;
                case 2:
                    return new PlayerForGameSession("-2", "elem bot", "botttt", _type, 0);
                    break;
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

        public static async Task<string> CheckAndGetGameHubs()
        {
            foreach (string keys in starter.GameServerHUBs.Keys)
            {
               
                try
                {
                    string ID = functions.get_random_set_of_symb(6);

                    await SendPing(IPEndPoint.Parse($"{starter.GameServerHUBs[keys].GetIP()}:{port_udp_for_GameHubs}"), ID);
                    await Task.Delay(1000);

                    if (SendPingsList.ContainsKey(ID) && SendPingsList[ID].isItOK())
                    {
                        
                        starter.GameServerHUBs[keys].SetActive();
                        starter.GameServerHUBs[keys].SetPing(SendPingsList[ID].GetPing());
                        starter.GameServerHUBs[keys].SetSessions(SendPingsList[ID].GetSessions());
                        SendPingsList.Remove(ID);
                    } 
                    else
                    {
                        
                        Console.WriteLine(DateTime.Now + ": server is inactive - no ping - " + starter.GameServerHUBs[keys].GetIP());
                        starter.GameServerHUBs[keys].SetnonActive();
                        
                    }

                  
                    Console.WriteLine(starter.GameServerHUBs[keys].GetActiveState() + " - " + starter.GameServerHUBs[keys].GetIP() + " - " + starter.GameServerHUBs[keys].GetPing());

                }
                catch (Exception ex)
                {
                    starter.GameServerHUBs[keys].SetnonActive();
                    Console.WriteLine(DateTime.Now + ": server is inactive - no ping - " + starter.GameServerHUBs[keys].GetIP());
                }
            }

            foreach (string keys in starter.GameServerHUBs.Keys)
            {
                if (starter.GameServerHUBs[keys].GetActiveState())
                {
                    return starter.GameServerHUBs[keys].GetIP();
                }

            }

            return "error";
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

        public GameHubsSpec(string _ip)
        {
            hub_IP = _ip;
            isActive = true;
            ping = new List<long>();
            number_of_sessions = 0;
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
                ThreadPool.QueueUserWorkItem(o => { ReceiveAsync(); });

            }
            else
            {

                //List<byte> b = new List<byte>();
                byte[] t = new byte[(int)size];

                for (int i = 0; i < (int)size; i++)
                {
                    //b.Add(buffer[i]);
                    t[i] = buffer[i];
                }


                    
                string data_result = Encoding.UTF8.GetString(t, 0, t.Length);

                Console.WriteLine("received " + data_result);

                try
                {
                    string[] packet_data = data_result.Split('~');

                    if (packet_data.Length>=2 && (packet_data[0] + packet_data[1]) == "07")
                    {

                        encryption.Decode(ref t, starter.secret_key_for_game_servers);
                        data_result = Encoding.UTF8.GetString(t, 0, t.Length);
                        packet_data = data_result.Split('~');

                        if (packet_data.Length == 5 && packet_data[2]== starter.InnerServerConnectionPassword)
                        {
                            if (Server.SendPingsList.ContainsKey(packet_data[4]))
                            {
                                Console.WriteLine(packet_data[4]);
                                Server.SendPingsList[packet_data[4]].SetReceivedTime(starter.stopWatch.ElapsedMilliseconds);
                                Server.SendPingsList[packet_data[4]].SetSessions(int.Parse(packet_data[3]));
                                Server.SendPingsList[packet_data[4]].SetOKGood();
                            }
                            
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            //ThreadPool.QueueUserWorkItem(o => { ReceiveAsync(); });


            try
            {
                ReceiveAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Onreceived - " + ex);
            }

            // Echo the message back to the sender
            //SendAsync(endpoint, buffer, offset, size);
        }


        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Server caught an error with code {error} ");

        }

    }

}
