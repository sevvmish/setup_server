using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace setup_server
{
    
    class Server
    {
        public static Dictionary<string, byte[]> Sessions = new Dictionary<string, byte[]>();
        public static Dictionary<string, DateTime> pvp1vs1 = new Dictionary<string, DateTime>();
        //public static Dictionary<string, Player_data> TemporaryDataForStartingGameSession = new Dictionary<string, Player_data>();
        public static Dictionary<string, PlayerForGameSession> PlayersAwaiting = new Dictionary<string, PlayerForGameSession>();
        public static HashSet<GameSessions> GameSessionsAwaiting = new HashSet<GameSessions>();

        //gamehubs cheking


        //TCP        
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        private static IPAddress ipaddress_tcp;
        private static IPEndPoint localendpoint_tcp;
        private static Socket socket_tcp;
        private const int port_tcp = 2326;
        private const int max_connections = 1000;
        //private static StringBuilder raw_data_received_tcp = new StringBuilder(1024);
        //private static byte[] buffer_received_tcp = new byte[1024];
        private static byte[] buffer_send_tcp = new byte[1024];

        //START FOR TCP
        public static void Server_init_TCP()
        {
            //start checker for PVP
            Task.Run(() => check_queue_for_pvp());

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
                //raw_data_received_tcp.Clear();
                //Socket handler = (Socket)ar.AsyncState;

                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;
                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    //raw_data_received_tcp.Append(Encoding.UTF8.GetString(buffer_received_tcp, 0, bytesRead));
                    //Console.WriteLine(raw_data_received_tcp + " : " + handler.RemoteEndPoint.ToString());
                    state.sb.Append(Encoding.UTF8.GetString(state.buffer, 0, bytesRead));
                    //Console.WriteLine(state.sb + " : " + handler.RemoteEndPoint.ToString());


                    if (!Sessions.ContainsKey(Encoding.UTF8.GetString(state.buffer, 0, 5)))  {
                        //Console.WriteLine(Sessions.ContainsKey(Encoding.UTF8.GetString(state.buffer, 0, 5)) + " - is it in sess cont - NO");
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

                        //Console.WriteLine(back_result);
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
            //int CurrentPort_tcp = general.GameServerTCPPort;
            //string CurrentIP_tcp = general.GameServerIP;
            int CurrentPort_tcp = (int)CurrentPort;
            string CurrentIP_tcp = IP;

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
                byte[] msgBuff = new byte[1024];
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
            //Console.WriteLine(data + " - send");

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


        public static async void check_queue_for_pvp()
        {
            long CurrentTime = starter.stopWatch.ElapsedMilliseconds;

            while (true)
            {
                if (CurrentTime < starter.stopWatch.ElapsedMilliseconds)
                {
                    try
                    {

                        List<PlayerForGameSession> looking_for_2 = new List<PlayerForGameSession>();

                        Console.WriteLine(GameSessionsAwaiting.Count + " !!!!!!!!!!!!!!!CCCCCCCCCCOOOOOOUUUUUUUUUUUNNNNNNNNNNTTTTTTTTTT");

                        foreach (string keys in PlayersAwaiting.Keys)
                        {
                            //cleaning for old unupdated
                            if (PlayersAwaiting[keys].WhenLastUpdated().AddSeconds(10) < DateTime.Now && !PlayersAwaiting[keys].isPlayerBusyForSession())
                            {
                                Console.WriteLine(DateTime.Now + ": removed from queue character - " + PlayersAwaiting[keys].GetCharacterName());
                                if (looking_for_2.Contains(PlayersAwaiting[keys]))
                                {
                                    looking_for_2.Remove(PlayersAwaiting[keys]);
                                }
                                PlayersAwaiting.Remove(keys);
                            }


                            if (!PlayersAwaiting[keys].isPlayerBusyForSession() && PlayersAwaiting[keys].GetPlayerGameType() == GameTypes.PvP_1vs1)
                            {
                                looking_for_2.Add(PlayersAwaiting[keys]);
                                if (looking_for_2.Count == 2)
                                {
                                    GameSessionsAwaiting.Add(new GameSessions(looking_for_2));
                                    looking_for_2.Clear();
                                }



                            }



                        }



                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                    

                    CurrentTime += 2000;
                    if (CurrentTime > starter.stopWatch.ElapsedMilliseconds)
                    {                        
                        await Task.Delay((int)(CurrentTime - starter.stopWatch.ElapsedMilliseconds));
                    }
                }

            }
        }

        public static void CheckGameHubs()
        {
            foreach (string keys in starter.GameServerHUBs.Keys)
            {
                long _pre_ping = starter.stopWatch.ElapsedMilliseconds;
                string result = null;

                try
                {
                    result = Server.SendAndGetTCP_between_servers($"0~7~{starter.InnerServerConnectionPassword}", starter.GameServerPort, starter.GameServerHUBs[keys].GetIP(), true);
                                
                    long ping = starter.stopWatch.ElapsedMilliseconds - _pre_ping;

                    if (result==null || result=="0~7~wp")
                    {
                        Console.WriteLine("ping server is 07wp");
                        starter.GameServerHUBs[keys].SetnonActive();
                    }
                    else
                    {
                        string[] _data = result.Split('~');
                        starter.GameServerHUBs[keys].SetSessions(int.Parse(_data[2]));

                    }

                    Console.WriteLine(result + ": " + starter.GameServerHUBs[keys].GetActiveState() + " - " + starter.GameServerHUBs[keys].GetIP() + " - " + starter.GameServerHUBs[keys].GetPing());

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        public static string GetGameHub()
        {
            foreach (string keys in starter.GameServerHUBs.Keys)
            {
                if (starter.GameServerHUBs[keys].GetActiveState())
                {
                    return starter.GameServerHUBs[keys].GetIP();
                }

            }

            return "0";
        }



    }

    /*
    public class Player_data
    {
        public string character_name;
        public string character_id;
        public string session_id;
        public string player_id;
        public string GameHUB;
        public bool is_ready;
        public List<string> OtherPlayerTickets;

        public Player_data(string charac_n, string char_id, string sess, string player, List<string> pl_tickets)
        {
            character_name = charac_n;
            character_id = char_id;
            session_id = sess;
            player_id = player;
            GameHUB = "1";
            is_ready = false;
            OtherPlayerTickets = new List<string>();
            OtherPlayerTickets = pl_tickets;
        }

        public void PlayerIsReady()
        {
            is_ready = true;
        }        
    }
    */

    public struct GameHubsSpec
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
        public const int BufferSize = 1024;

        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];

        // Received data string.
        public StringBuilder sb = new StringBuilder();

        // Client socket.
        public Socket workSocket = null;
    }

}
