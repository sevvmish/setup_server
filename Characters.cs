using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace setup_server
{
    class Characters
    {
        //base characteristics
        private float speed;
        private int health;
        private float health_regen;
        private float energy_regen;
        private string weapon_attack;
        private float hit_power;
        private int armor;
        private float shield_block;
        private float magic_resistance;
        private float dodge;
        private float cast_speed;
        private float melee_crit;
        private float magic_crit;
        private float spell_power;
        private int spell1;
        private int spell2;
        private int spell3;
        private int spell4;
        private int spell5;
        private int spell6;
        private string hidden_conds;
        private string spell_book;
        private string talents;

        //player types
        public enum PlayerTypes
        {
            warrior = 1,
            elementalist = 2,
            barbarian = 3,
            rogue = 4,
            wizard = 5,
            gunslinger = 6
        }

                

        public string GetSQLReadyStringForPlayerDataUPDATEByCharName(string char_name, int playerType, string _talents, string [] spells)
        {
            Characters character = CreateDefaultCharacter(playerType, _talents);

       
            return $"UPDATE `character_property` SET {getPlayerCharacteristicsInSQLReadyStringFormatForUpdate(character)} WHERE `character_id`= (SELECT characters.character_id FROM characters WHERE characters.character_name = '{char_name}')";
        }

        public string GetSQLReadyStringForPlayerDataUPDATEByCharID(string charID, int playerType, string _talents)
        {
            Characters character = CreateDefaultCharacter(playerType, _talents);

            return $"UPDATE `character_property` SET {getPlayerCharacteristicsInSQLReadyStringFormatForUpdate(character)} WHERE `character_id`= '{charID}'";
        }

       
        private Characters CreateDefaultCharacter(int playerType, string newTalents)
        {
            Characters character = null;

            switch (playerType)
            {
                case 1: //warrior
                    switch (newTalents)
                    {
                        case "0":
                            return new Characters(1, 300, 1, 0, "1-6", 11, 120, 6, 3, 1, 1, 7, 1, 1, 9, 5, 12, 4, 17, 1020, "", "0,1,2,4,5,6,9,11,12,15,17", newTalents);

                        case "1":
                            return new Characters(1, 275, 1, 0, "2-7", 13, 50, 4, 1, 1, 1, 10, 1, 1, 9, 8, 12, 13, 3, 1020, "", "0,1,3,4,5,6,7,8,9,11,12,13,15", newTalents);

                        case "2":
                            return new Characters(1, 335, 1, 0, "1-6", 9, 200, 12, 5, 1, 1, 5, 1, 1, 5, 6, 10, 11, 19, 1020, "", "0,1,4,5,6,8,9,10,11,12,15,16,19", newTalents);

                    }                    
                    
                    return character;
                
                case 2: //elementalist
                    switch (newTalents)
                    {
                        case "0":
                            return new Characters(1, 240, 1, 1, "1-1", 1, 20, 0, 1, 1, 10, 1, 10, 17, 53, 52, 66, 75, 61, 51, "", "0,52,53,54,55,60,61,63,66,75", newTalents);

                        case "1":
                            return new Characters(1, 240, 1, 1, "1-1", 1, 20, 0, 1, 1, 10, 1, 10, 17, 55, 78, 76, 74, 60, 56, "", "0,52,54,55,60,61,63,74,76,78,79", newTalents);

                        case "2":
                            return new Characters(1, 240, 1, 1, "1-1", 1, 20, 0, 1, 1, 10, 1, 10, 17, 53, 52, 78, 73, 62, 65, "", "0,51,52,53,54,55,56,60,61,62,63,68,73,78", newTalents);
                    }
                                        
                    return character;
                

                case 3: //barbarian
                    switch (newTalents)
                    {
                        case "0":
                            return new Characters(0.9f, 360, 1, 0, "5-10", 17, 80, 0, 10, 1, 1, 5, 1, 1, 101, 102, 103, 106, 104, 1020, "", "0,101,102,103,104,105,106,109", newTalents);

                        case "1":
                            return new Characters(0.9f, 325, 2, 0, "6-11", 17, 80, 0, 10, 1, 1, 5, 1, 1, 109, 111, 103, 108, 104, 1015, "", "0,101,102,103,104,105,106,108,109,111,112", newTalents);

                        case "2":
                            return new Characters(0.9f, 300, 3, 0, "7-12", 17, 80, 0, 10, 1, 1, 5, 1, 1, 109, 111, 110, 112, 104, 1015, "", "0,101,102,103,104,105,106,108,109,110,111,112", newTalents);
                    }
                                        
                    return character;                

                case 4: //rogue
                    switch (newTalents)
                    {
                        case "0":
                            return new Characters(1.1f, 265, 1, 15, "1-4", 6, 50, 0, 1, 10, 1, 10, 1, 1, 152, 154, 156, 175, 158, 153, "", "0,151,152,154,155,156,158,159,160,161,162,165,175,178", newTalents);

                        case "1":
                            return new Characters(1.1f, 240, 1, 15, "1-4", 6, 50, 0, 1, 10, 1, 10, 1, 1, 152, 154, 163, 176, 172, 153, "", "0,151,152,154,155,158,159,160,161,162,163,165,172,175,176,177,178", newTalents);

                        case "2":
                            return new Characters(1.1f, 215, 1, 20, "1-4", 6, 50, 0, 1, 10, 1, 10, 1, 1, 152, 154, 164, 176, 158, 153, "", "0,151,152,154,155,158,159,160,161,162,163,164,165,175,176,177,178", newTalents);
                    }
                    
                    return character;
                

                case 5: //wizard
                    switch (newTalents)
                    {
                        case "0":
                            return new Characters(1, 240, 1, 1, "1-1", 1, 20, 0, 1, 1, 10, 1, 10, 21, 201, 221, 207, 220, 210, 215, "", "0,201,203,204,205,206,207,210,212,214,218,219,220,221,222,223", newTalents);

                        case "1":
                            return new Characters(1, 240, 1, 1, "1-1", 1, 20, 0, 1, 1, 5, 1, 10, 17, 221, 212, 207, 219, 205, 209, "", "0,201,203,204,205,206,207,208,210,212,214,217,218,219,220,221,222,223,227", newTalents);

                        case "2":
                            return new Characters(1, 240, 1, 1, "1-1", 1, 20, 0, 1, 1, 5, 1, 10, 17, 221, 208, 217, 227, 222, 202, "", "0,201,203,204,205,206,207,208,210,212,214,217,218,219,220,221,222,223,227,228", newTalents);
                    }
                    
                    return character;

                case 6: //gunslinger
                    switch (newTalents)
                    {
                        case "0":
                            return new Characters(1, 240, 1, 1, "1-1", 1, 20, 0, 1, 1, 10, 1, 10, 21, 201, 221, 207, 220, 210, 215, "", "0,201,203,204,205,206,207,210,212,214,218,219,220,221,222,223", newTalents);

                        case "1":
                            return new Characters(1, 240, 1, 1, "1-1", 1, 20, 0, 1, 1, 5, 1, 10, 17, 221, 212, 207, 219, 205, 209, "", "0,201,203,204,205,206,207,208,210,212,214,217,218,219,220,221,222,223,227", newTalents);

                        case "2":
                            return new Characters(1, 240, 1, 1, "1-1", 1, 20, 0, 1, 1, 5, 1, 10, 17, 221, 208, 217, 227, 222, 202, "", "0,201,203,204,205,206,207,208,210,212,214,217,218,219,220,221,222,223,227,228", newTalents);
                    }

                    return character;


            }

            return character;
        }


        private Characters(float speed, int health, float health_regen, float energy_regen, string weapon_attack, float hit_power, int armor, float shield_block, float magic_resistance,
            float dodge, float cast_speed, float melee_crit, float magic_crit, float spell_power, int spell1, int spell2, int spell3, int spell4, int spell5, int spell6, string hidden_conds, string spell_book, string talents)
        {
            this.speed = speed;
            this.health = health;
            this.health_regen = health_regen;
            this.energy_regen = energy_regen;
            this.weapon_attack = weapon_attack;
            this.hit_power = hit_power;
            this.armor = armor;
            this.shield_block = shield_block;
            this.magic_resistance = magic_resistance;
            this.dodge = dodge;
            this.cast_speed = cast_speed;
            this.melee_crit = melee_crit;
            this.magic_crit = magic_crit;
            this.spell_power = spell_power;
            this.spell1 = spell1;
            this.spell2 = spell2;
            this.spell3 = spell3;
            this.spell4 = spell4;
            this.spell5 = spell5;
            this.spell6 = spell6;
            this.hidden_conds = hidden_conds;
            this.spell_book = spell_book;
            this.talents = talents;
        }

        public Characters() { }

        private string getPlayerCharacteristicsInSQLReadyStringFormatForInsert(Characters character)
        {
            return $" '{character.speed.ToString("f1").Replace(',', '.')}', '{character.health}', '{character.health_regen.ToString("f1").Replace(',', '.')}', '{character.energy_regen.ToString("f1").Replace(',', '.')}', '{character.weapon_attack}', " +
                $"'{character.hit_power.ToString("f1").Replace(',', '.')}', '{character.armor}', '{character.shield_block.ToString("f1").Replace(',', '.')}', '{character.magic_resistance.ToString("f1").Replace(',', '.')}', " +
                $"'{character.dodge.ToString("f1").Replace(',', '.')}', '{character.cast_speed.ToString("f1").Replace(',', '.')}', '{character.melee_crit.ToString("f1").Replace(',', '.')}', '{character.magic_crit.ToString("f1").Replace(',', '.')}'," +
                $"'{character.spell_power.ToString("f1").Replace(',', '.')}', '{character.spell1}', '{character.spell2}', '{character.spell3}', '{character.spell4}', '{character.spell5}', '{character.spell6}', '{character.hidden_conds}', '{character.spell_book}', '{character.talents}' ";
        }

        private string getPlayerCharacteristicsInSQLReadyStringFormatForUpdate(Characters character)
        {
            return $" `speed`= '{character.speed.ToString("f1").Replace(',', '.')}', `health`= '{character.health}', `health_regen`= '{character.health_regen.ToString("f1").Replace(',', '.')}', `energy_regen`= '{character.energy_regen.ToString("f1").Replace(',', '.')}', `weapon_attack`= '{character.weapon_attack}', " +
                $"`hit_power`= '{character.hit_power.ToString("f1").Replace(',', '.')}', `armor`= '{character.armor}', `shield_block`= '{character.shield_block.ToString("f1").Replace(',', '.')}', `magic_resistance`= '{character.magic_resistance.ToString("f1").Replace(',', '.')}', " +
                $"`dodge`= '{character.dodge.ToString("f1").Replace(',', '.')}', `cast_speed`= '{character.cast_speed.ToString("f1").Replace(',', '.')}', `melee_crit`= '{character.melee_crit.ToString("f1").Replace(',', '.')}', `magic_crit`= '{character.magic_crit.ToString("f1").Replace(',', '.')}'," +
                $"`spell_power`= '{character.spell_power.ToString("f1").Replace(',', '.')}', `spell1`= '{character.spell1}', `spell2`= '{character.spell2}', `spell3`= '{character.spell3}', `spell4`= '{character.spell4}', `spell5`= '{character.spell5}', `spell6`= '{character.spell6}', `hidden_conds`= '{character.hidden_conds}', `spell_book`= '{character.spell_book}', `talents`= '{character.talents}' ";
        }


    }
}
