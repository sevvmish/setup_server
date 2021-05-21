using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace setup_server
{
    class talents_setup
    {
       
        public float speed;
        public float health;
        public float health_regen;
        public float energy_regen;
        public string weapon_attack;
        public float hit_power;
        public float armor;
        public float shield_block;
        public float magic_resistance;
        public float dodge;
        public float cast_speed;
        public float melee_crit;
        public float magic_crit;
        public float spell_power;
        public int spell1;
        public int spell2;
        public int spell3;
        public int spell4;
        public int spell5;
        public int spell6;
        public string hidden_conds;
        public string spell_book;
        public string talents;
        int character_type;

        public talents_setup base_player;
        
        public talents_setup(string _talents, int char_type, params string[] data)
        {
            speed = float.Parse(data[0]);
            health = float.Parse(data[1]);
            health_regen = float.Parse(data[2]);
            energy_regen = float.Parse(data[3]);
            weapon_attack = data[4];
            hit_power = float.Parse(data[5]);
            armor = float.Parse(data[6]);
            shield_block = float.Parse(data[7]);
            magic_resistance = float.Parse(data[8]);
            dodge = float.Parse(data[9]);
            cast_speed = float.Parse(data[10]);
            melee_crit = float.Parse(data[11]);
            magic_crit = float.Parse(data[12]);
            spell_power = float.Parse(data[13]);
            spell1 = int.Parse(data[14]);
            spell2 = int.Parse(data[15]);
            spell3 = int.Parse(data[16]);
            spell4 = int.Parse(data[17]);
            spell5 = int.Parse(data[18]);
            spell6 = int.Parse(data[19]);
            //hidden_conds = data[20];
            //spell_book = data[21];
            //talents = data[22];
            hidden_conds = "";
            spell_book = "";
            talents = _talents;
            character_type = char_type;
        }

        public string prepare_to_update_sql(string char_name)
        {
            string result = "";
            string spell_b = $"0,{spell1},{spell2},{spell3},{spell4},{spell5},{spell6}";
            result = $"UPDATE `character_property` SET `speed`= '{speed.ToString("f0")}',  `health`= '{health.ToString("f0")}',`health_regen`= '{health_regen.ToString("f0")}', `energy_regen`= '{energy_regen.ToString("f0")}', `weapon_attack`= '{weapon_attack}', `hit_power`= '{hit_power.ToString("f0")}', `armor`= '{armor.ToString("f0")}', `shield_block`= '{shield_block.ToString("f0")}', `magic_resistance`= '{magic_resistance.ToString("f0")}', `dodge`= '{dodge.ToString("f0")}', `cast_speed`= '{cast_speed.ToString("f0")}', `melee_crit`= '{melee_crit.ToString("f0")}', `magic_crit`= '{magic_crit.ToString("f0")}', `spell_power`= '{spell_power.ToString("f0")}', `spell1`= '{spell1}', `spell2`= '{spell2}', `spell3`= '{spell3}', `spell4`= '{spell4}', `spell5`= '{spell5}', `spell6`= '{spell6}', `hidden_conds`= '{hidden_conds}', `spell_book`= '{spell_b}', `talents`= '{talents}' WHERE `character_id`= (SELECT characters.character_id FROM characters WHERE characters.character_name = '{char_name}')";

            return result;
        }


    }
}
