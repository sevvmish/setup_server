﻿using System;
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
            wizard = 5
        }

                

        public string GetSQLReadyStringForPlayerDataUPDATEByCharName(string char_name, int playerType, string _talents)
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
                    character = new Characters(1, 250, 1, 1, "1-6", 10f, 150, 5, 1f, 1f, 1f, 3f, 1f, 1f, 9, 12, 4, 5, 3, 997, "", "0,1,2,3,4,5,6,7,8,9,10,11,12", "0");
                    warriorImposeTalents(ref character, newTalents);
                    return character;
                
                case 2: //elementalist
                    character = new Characters(1, 200, 1, 2, "1-1", 1f, 0, 0, 1f, 1f, 10f, 1f, 10f, 20f, 56, 55, 52, 53, 65, 997, "", "0,51,52,53,54,55,56,60,61,62,63,65,66,68", "0");
                    warriorImposeTalents(ref character, newTalents);
                    return character;
                

                case 3: //barbarian
                    character = new Characters(1, 280, 1, 1, "5-10", 20f, 80, 0, 10f, 1f, 1f, 3f, 1f, 1f, 101, 102, 103, 106, 105, 997, "", "0,101,102,103,104,105,106,108,109", "0");
                    warriorImposeTalents(ref character, newTalents);
                    return character;
                

                case 4: //rogue
                    character = new Characters(1, 220, 1, 1, "1-4", 7f, 0, 0, 1f, 10f, 1f, 10f, 1f, 1f, 151, 152, 153, 154, 155, 997, "", "0,151,152,153,154,155,156", "0");
                    warriorImposeTalents(ref character, newTalents);
                    return character;
                

                case 5: //wizard
                    character = new Characters(1, 200, 1, 1, "1-1", 1f, 0, 0, 1f, 1f, 1f, 1f, 5f, 20f, 201, 202, 203, 204, 205, 997, "", "0,201,202,203,204,205,206,207,208,209", "0");
                    warriorImposeTalents(ref character, newTalents);
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

        //warrior talents
        private void warriorImposeTalents(ref Characters character, string _talents)
        {
            switch(_talents)
            {
                case "0":

                    break;

                case "1":

                    break;

                case "2":

                    break;

            }            
        }

        //elementalist talents
        private void elementalistImposeTalents(ref Characters character, string _talents)
        {
            switch (_talents)
            {
                case "0":

                    break;

                case "1":

                    break;

                case "2":

                    break;

            }
        }

        //barbarian talents
        private void barbarianImposeTalents(ref Characters character, string _talents)
        {
            switch (_talents)
            {
                case "0":

                    break;

                case "1":

                    break;

                case "2":

                    break;

            }
        }

        //rogue talents
        private void rogueImposeTalents(ref Characters character, string _talents)
        {
            switch (_talents)
            {
                case "0":

                    break;

                case "1":

                    break;

                case "2":

                    break;

            }
        }

        //wizard talents
        private void wizardImposeTalents(ref Characters character, string _talents)
        {
            switch (_talents)
            {
                case "0":

                    break;

                case "1":

                    break;

                case "2":

                    break;

            }
        }



    }
}
