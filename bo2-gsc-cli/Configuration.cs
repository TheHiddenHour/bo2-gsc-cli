﻿namespace bo2_gsc_cli {
    class Configuration {
        public Gametype MP, ZM;

        public Configuration(Gametype multiplayer, Gametype zombies) {
            MP = multiplayer;
            ZM = zombies;
        }

        public class Gametype {
            public DefaultSettings Defaults;
            public CustomSettings Customs;
            public string ScriptPath;

            public class DefaultSettings {
                public uint PointerAddress, BufferAddress;
            }

            public class CustomSettings {
                public uint BufferAddress;
            }
        }

        public static Gametype GenerateDefaultMPSettings() {
            Gametype mp = new Gametype {
                Defaults = new Gametype.DefaultSettings(),
                Customs = new Gametype.CustomSettings()
            };

            mp.Defaults.PointerAddress = 21021896; // .../gametypes/_clientids.gsc pointer 
            mp.Defaults.BufferAddress = 810181280; // .../gametypes/_clientids.gsc buffer address 
            mp.Customs.BufferAddress = 268697600; // Free location in memory for MP 
            mp.ScriptPath = "maps/mp/gametypes/_clientids.gsc";

            return mp;
        }

        public static Gametype GenerateDefaultZMSettings() {
            Gametype zm = new Gametype {
                Defaults = new Gametype.DefaultSettings(),
                Customs = new Gametype.CustomSettings()
            };

            zm.Defaults.PointerAddress = 21021944; // .../gametypes_zm/_clientids.gsc pointer 
            zm.Defaults.BufferAddress = 810069696; // .../gametypes_zm/_clientids.gsc buffer address  
            zm.Customs.BufferAddress = 13371337; // Need to find a free location in memory for ZM 
            zm.ScriptPath = "maps/mp/gametypes_zm/_clientids.gsc";

            return zm;
        }
    }
}
