using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace setup_server
{
    class RaitingImplementing
    {             
        public const int XP_FOR_1vs1_score_0 = 1;
        public const int XP_FOR_1vs1_score_1 = 1;
        public const int XP_FOR_1vs1_score_2 = 1;

        public const int XP_FOR_2vs2_score_0 = 1;
        public const int XP_FOR_2vs2_score_1 = 1;
        public const int XP_FOR_2vs2_score_2 = 1;

        public const int XP_FOR_3vs3_score_0 = 1;
        public const int XP_FOR_3vs3_score_1 = 1;
        public const int XP_FOR_3vs3_score_2 = 1;

        public const int XP_FOR_BR_score_0 = 1;
        public const int XP_FOR_BR_score_1 = 1;
       
        public const int RAIT_FOR_1vs1_score_0 = 1;
        public const int RAIT_FOR_1vs1_score_1 = 1;
        public const int RAIT_FOR_1vs1_score_2 = 1;

        public const int RAIT_FOR_2vs2_score_0 = 1;
        public const int RAIT_FOR_2vs2_score_1 = 1;
        public const int RAIT_FOR_2vs2_score_2 = 1;

        public const int RAIT_FOR_3vs3_score_0 = 1;
        public const int RAIT_FOR_3vs3_score_1 = 1;
        public const int RAIT_FOR_3vs3_score_2 = 1;

        public const int RAIT_FOR_BR_score_0 = 1;
        public const int RAIT_FOR_BR_score_1 = 1;


        public static void ReAssessRaitingByCharID(string charID, int score, int session_type_id)
        {
            try
            {
                string[,] old_data_raiting = mysql.GetMysqlSelect($"SELECT `pvp_raiting`, `pvp_played`, `pvp_won`, `pvp_lost`, `pve_raiting`, `xp_points` FROM `character_raiting` WHERE `character_id`='{charID}'").Result;

                if (old_data_raiting.GetLength(0) == 0)
                {
                    Console.WriteLine(DateTime.Now + ": error in getting data from raiting table for character " + charID);
                    return;
                }

                //get old data from raiting
                int PVPraitingToAdd = int.Parse(old_data_raiting[0, 0]);
                int PVPplayed = int.Parse(old_data_raiting[0, 1]);
                int PVP_won = int.Parse(old_data_raiting[0, 2]);
                int PVP_lost = int.Parse(old_data_raiting[0, 3]);
                int PVEraitingToAdd = int.Parse(old_data_raiting[0, 4]);
                int EXPtoAdd = int.Parse(old_data_raiting[0, 5]);

                //change data for played games            
                int winGame = AddGameWinLostpoint(score, session_type_id);
                string foranalyticsRESULT = "";
                if (winGame == 1)
                {
                    foranalyticsRESULT = "win, ";
                    PVP_won++;
                    PVPplayed++;
                }
                else if (winGame == 0)
                {
                    foranalyticsRESULT = "lost, ";
                    PVP_lost++;
                    PVPplayed++;
                }

                //XP to add
                EXPtoAdd += GetXP(score, session_type_id);

                //raitng to add
                bool isPVP = isSessionTypePVP(session_type_id);
                string foranalyticsPVP = "";

                if (isPVP)
                {
                    PVPraitingToAdd += GetRaiting(score, session_type_id);
                    foranalyticsPVP = "PVP, ";
                }
                else
                {
                    PVEraitingToAdd = GetRaiting(score, session_type_id);
                    foranalyticsPVP = "PVE, ";
                }

                //check not to pass minus for raiting
                PVPraitingToAdd = PVPraitingToAdd < 0 ? 0 : PVPraitingToAdd;
                PVEraitingToAdd = PVEraitingToAdd < 0 ? 0 : PVEraitingToAdd;
                EXPtoAdd = EXPtoAdd < 0 ? 0 : EXPtoAdd;

                bool result = mysql.ExecuteSQLInstruction($"UPDATE `character_raiting` SET `pvp_raiting`='{PVPraitingToAdd}',`pvp_played`='{PVPplayed}',`pvp_won`='{PVP_won}',`pvp_lost`='{PVP_lost}',`pve_raiting`='{PVEraitingToAdd}',`xp_points`='{EXPtoAdd}' WHERE `character_id`='{charID}'").Result;

                if (!result)
                {
                    Console.WriteLine(DateTime.Now + ": error trying update new raiting data to character " + charID);
                }

                //analysis==========================
                bool isOK1 = mysql.ExecuteSQLInstruction($"INSERT INTO `events`(`user_id`, `character_id`, `event_type_id`, `datetime`, `data`) VALUES ((SELECT `user_id` FROM `characters` WHERE `character_id`='{charID}'), '{charID}', '8', '{DateTime.Now}', '{foranalyticsPVP}{foranalyticsRESULT}game type:{session_type_id}')").Result;
                //==================================

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        

        public static int GetXP(int _points, int session_type)
        {
            switch (session_type)
            {
                case 1:
                    if (_points == 0) return XP_FOR_1vs1_score_0;
                    if (_points == 1) return XP_FOR_1vs1_score_1;
                    if (_points == 2) return XP_FOR_1vs1_score_2;                    
                    break;
                case 2:
                    if (_points == 0) return XP_FOR_2vs2_score_0;
                    if (_points == 1) return XP_FOR_2vs2_score_1;
                    if (_points == 2) return XP_FOR_2vs2_score_2;
                    break;
                case 3:
                    if (_points == 0) return XP_FOR_3vs3_score_0;
                    if (_points == 1) return XP_FOR_3vs3_score_1;
                    if (_points == 2) return XP_FOR_3vs3_score_2;
                    break;
                case 4:
                    if (_points == 0) return XP_FOR_BR_score_0;
                    if (_points == 1) return XP_FOR_BR_score_1;                  
                    break;
            }

            return 0;
        }


        public static int GetRaiting(int _points, int session_type)
        {
            switch (session_type)
            {
                case 1:
                    if (_points == 0) return RAIT_FOR_1vs1_score_0;
                    if (_points == 1) return RAIT_FOR_1vs1_score_1;
                    if (_points == 2) return RAIT_FOR_1vs1_score_2;
                    break;
                case 2:
                    if (_points == 0) return RAIT_FOR_2vs2_score_0;
                    if (_points == 1) return RAIT_FOR_2vs2_score_1;
                    if (_points == 2) return RAIT_FOR_2vs2_score_2;
                    break;
                case 3:
                    if (_points == 0) return RAIT_FOR_3vs3_score_0;
                    if (_points == 1) return RAIT_FOR_3vs3_score_1;
                    if (_points == 2) return RAIT_FOR_3vs3_score_2;
                    break;
                case 4:
                    if (_points == 0) return RAIT_FOR_BR_score_0;
                    if (_points == 1) return RAIT_FOR_BR_score_1;                    
                    break;
            }
            return 0;
        }


        public static bool isSessionTypePVP(int session_type)
        {
            List<int> PVPtypes = new List<int> { 1, 2, 3, 4 };
            List<int> PVEtypes = new List<int> { 0 };

            if (PVPtypes.Contains(session_type))
            {
                return true;
            }

            if (PVEtypes.Contains(session_type))
            {
                return false;
            }

            Console.WriteLine("error understanding PVP/PVE!");
            return false;

        }


        public static int AddGameWinLostpoint(int score, int session_type)
        {
            switch(session_type)
            {
                case 1:
                    if (score>=2)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                case 2:
                    if (score >= 2)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                case 3:
                    if (score >= 2)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                case 4:
                    if (score >= 1)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }

            }
            Console.WriteLine("error understanding win/lost game");
            return -1;
        }

    }
}
