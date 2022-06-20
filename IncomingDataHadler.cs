using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace setup_server
{
    class IncomingDataHadler
    {
        private const float TIME_FOR_BAD_DATA_TO_BECOME_BAN_SECONDS = 2f;
        private const float COUNT_FOR_BAD_DATA_TO_BECOME_BAN = 10f;
        private const int HOW_LONG_BASE_BAN_MILISEC = 2000;

        private static Dictionary<string, int> BadDataSuppliers = new Dictionary<string, int>();
        private static HashSet<string> BanListAddresses = new HashSet<string>();
        private static HashSet<string> BannedFirstRowHystoryLOG = new HashSet<string>();
        private static HashSet<string> BannedSecondRowHystoryLOG = new HashSet<string>();
        private static HashSet<string> BannedThirdRowHystoryLOG = new HashSet<string>();

        public static void AddBadDataSupplier(IPEndPoint _address)
        {
            string IP = _address.ToString().Split(':')[0];
            if (!BadDataSuppliers.ContainsKey(IP))
            {
                BadDataSuppliers.TryAdd(IP, 1);
                CheckBadDataSupplierForBanOption(IP);
                Console.WriteLine(DateTime.Now + ": Added new bad data supplier with IP " + IP + " address from " +_address.ToString());                
            }
            else
            {
                BadDataSuppliers[IP] += 1;
            }            
        }

        private static async void CheckBadDataSupplierForBanOption(string IP)
        {
            await Task.Delay((int)(TIME_FOR_BAD_DATA_TO_BECOME_BAN_SECONDS * 1000));

            if (!BadDataSuppliers.ContainsKey(IP))
            {
                return;
            }

            if (BadDataSuppliers.ContainsKey(IP) && BadDataSuppliers[IP] > COUNT_FOR_BAD_DATA_TO_BECOME_BAN)
            {
                
                AddToBanList(IP);
            }
            else if(BadDataSuppliers.ContainsKey(IP) && BadDataSuppliers[IP] <= COUNT_FOR_BAD_DATA_TO_BECOME_BAN)
            {
                BadDataSuppliers.Remove(IP);
            }
        }

        private static async void AddToBanList(string IP)
        {
            if (BanListAddresses.Contains(IP))
            {
                return;
            }

            BanListAddresses.Add(IP);
            int koeff = 1;
            if (BannedFirstRowHystoryLOG.Contains(IP)) koeff = 5;
            if (BannedSecondRowHystoryLOG.Contains(IP)) koeff = 30;
            if (BannedThirdRowHystoryLOG.Contains(IP)) koeff = 1800;

            switch (koeff)
            {
                case 1:
                    Console.WriteLine(DateTime.Now + ": IP added to ban list " + IP);
                    break;
                case 5:
                    Console.WriteLine(DateTime.Now + ": IP added to ban list second time " + IP);
                    break;
                case 30:
                    Console.WriteLine(DateTime.Now + ": IP added to ban list third time " + IP);
                    break;
                case 1800:
                    Console.WriteLine(DateTime.Now + ": IP added to ban list forth or more times " + IP);
                    break;
            }

            await Task.Delay(HOW_LONG_BASE_BAN_MILISEC * koeff);

            if (!BannedFirstRowHystoryLOG.Contains(IP) && !BannedSecondRowHystoryLOG.Contains(IP) && !BannedThirdRowHystoryLOG.Contains(IP)) BannedFirstRowHystoryLOG.Add(IP);
            if (BannedFirstRowHystoryLOG.Contains(IP)  && !BannedSecondRowHystoryLOG.Contains(IP) && !BannedThirdRowHystoryLOG.Contains(IP)) BannedSecondRowHystoryLOG.Add(IP);
            if (BannedFirstRowHystoryLOG.Contains(IP)  && BannedSecondRowHystoryLOG.Contains(IP)  && !BannedThirdRowHystoryLOG.Contains(IP)) BannedThirdRowHystoryLOG.Add(IP);

            if (BanListAddresses.Contains(IP)) BanListAddresses.Remove(IP);
            if (BadDataSuppliers.ContainsKey(IP)) BadDataSuppliers.Remove(IP);
        }

        public static void HandleIncomingTCP(int bytesRead, Socket handler, byte [] buffer)
        {
            try
            {
                if (BanListAddresses.Contains(handler.RemoteEndPoint.ToString().Split(':')[0]))
                {
                    Console.WriteLine(DateTime.Now + ": detected connection try from banned address " + handler.RemoteEndPoint);
                    return;
                }

                if (Encoding.UTF8.GetString(buffer, 0, 4) == "0~71")
                {
                    byte[] d = new byte[bytesRead];
                    for (int i = 0; i < bytesRead; i++)
                    {
                        d[i] = buffer[i];
                    }

                    encryption.Decode(ref d, starter.secret_key_for_game_servers);

                    packet_analyzer.StartSessionTCPInput(Encoding.UTF8.GetString(d), handler);

                }
                else if (Encoding.UTF8.GetString(buffer, 0, 4) == "5~0~")
                {

                    byte[] d = new byte[bytesRead];
                    for (int i = 0; i < bytesRead; i++)
                    {
                        d[i] = buffer[i];
                    }

                    encryption.Decode(ref d, starter.secret_key_for_game_servers);

                    string res = packet_analyzer.ProcessTCPInput(Encoding.UTF8.GetString(d), handler.RemoteEndPoint.ToString());

                    Server.SendDataTCP(handler, $"5~0~{res}");
                }
                else if (!Server.Sessions.ContainsKey(Encoding.UTF8.GetString(buffer, 0, 5)))
                {                    
                    packet_analyzer.StartSessionTCPInput(Encoding.UTF8.GetString(buffer, 0, bytesRead), handler);
                }
                else
                {
                    byte[] d = new byte[bytesRead];
                    for (int i = 0; i < bytesRead; i++)
                    {
                        d[i] = buffer[i];
                    }

                    encryption.Decode(ref d, Server.Sessions[Encoding.UTF8.GetString(buffer, 0, 5)]);

                    string back_result = packet_analyzer.ProcessTCPInput(Encoding.UTF8.GetString(d).Remove(0, 6), handler.RemoteEndPoint.ToString());

                    byte[] t = Encoding.UTF8.GetBytes(back_result);
                    encryption.Encode(ref t, Server.Sessions[Encoding.UTF8.GetString(buffer, 0, 5)]);

                    Server.SendDataTCP(handler, t);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
                AddBadDataSupplier((IPEndPoint)handler.RemoteEndPoint);
            }            
        }



        public static void HandleIncomingUDP(EndPoint endpoint, byte[] buffer)
        {
            try
            {
                if (BanListAddresses.Contains(endpoint.ToString().Split(':')[0]))
                {
                    Console.WriteLine(DateTime.Now + ": detected connection try from banned address " + endpoint);
                    return;
                }

                string data_result = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                string[] packet_data = data_result.Split('~');

                if (packet_data.Length >= 2 && (packet_data[0] + packet_data[1]) == "07")
                {

                    encryption.Decode(ref buffer, starter.secret_key_for_game_servers);
                    data_result = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                    packet_data = data_result.Split('~');

                    if (packet_data.Length == 5 && packet_data[2] == starter.InnerServerConnectionPassword)
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

                string packet_key = Encoding.UTF8.GetString(buffer, 0, 5);

                if (Server.Sessions.ContainsKey(packet_key))
                {
                    encryption.Decode(ref buffer, Server.Sessions[packet_key]);
                    string res = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                    string[] packet = res.Split('~');

               
                    //just pinging 7~0~ticket(3)~char(4)
                    if (packet[1] == "7" && packet[2] == "0")
                    {
                        if (Server.CurrentVisitors.ContainsKey(packet[3]) && Server.CurrentVisitors[packet[3]].GetCharacterName == packet[4])
                        {
                            Server.CurrentVisitors[packet[3]].Update();
                            Server.CurrentVisitors[packet[3]].SetAddress(endpoint);
                        }
                        else if(!Server.CurrentVisitors.ContainsKey(packet[3]))
                        {
                        
                            string[,] get_char_data = mysql.GetMysqlSelect($"SELECT characters.character_id FROM characters WHERE characters.character_name = '{packet[4]}'").Result;

                            if (get_char_data.GetLength(0) == 0 || get_char_data[0, 0] == "error")
                            {
                                return;
                            }

                            //data about visitors==============
                            functions.AddOrUpdateVisitors(packet[3], packet[4], get_char_data[0, 0]);
                            //================================

                            Server.CurrentVisitors[packet[3]].Update();
                            Server.CurrentVisitors[packet[3]].SetAddress(endpoint);
                        }

                    }


                    //packet received OK cheking 7~10~packetID
                    if (packet[1] == "7" && packet[2] == "10" && packet_analyzer.StringChecker(packet[3]))
                    {
                        if (Server.PacketsAwaitingForCheking.ContainsKey(packet[3]))
                        {
                            Server.PacketsAwaitingForCheking.Remove(packet[3]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("==============ERROR================\n" + ex + "\n" + DateTime.Now + "\n" + "==================ERROR_END===========\n");
                AddBadDataSupplier((IPEndPoint)endpoint);
            }


        }
    }
}
