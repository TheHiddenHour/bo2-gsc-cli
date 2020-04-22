using CommandLine;
using Newtonsoft.Json;
using PS3Lib;
using System;
using System.Data;
using System.IO;

namespace bo2_gsc_cli {
    enum Gametype {
        MP,
        ZM
    }

    enum PathType {
        File,
        Directory,
        String
    }

    class Program {
        static void Main(string[] args) {
            // Parse configuration JSON file 
            Configuration config = LoadConfigurationFile();

            /*
                Parse the command line parameters.
                Conflicting commands shouldn't be allowed due to their configured exclusivity in the ParameterOptions class.
            */
            Parser.Default.ParseArguments<ParameterOptions>(args).WithParsed(o => {
                // Validate selected PS3 API 
                SelectAPI selectedAPI = ValidateAPISelection(o.SelectedPS3API);
                // Validate selected gametype for resetting the ScriptParseTree and injection 
                Gametype selectedGametype = ValidateGametypeSelection(o.SelectedGametype);

                // Parse Reset ScriptParseTree parameter 
                if (o.ResetScriptParseTree) {
                    ConsoleWriteInfo("Resetting ScriptParseTree");

                    // Connect and attach to PS3 
                    PS3API PS3 = new PS3API(selectedAPI);
                    bool connectedAndAttached = ConnectAndAttachPS3(PS3);
                    if(!connectedAndAttached) { // If target could not connect or process could not attach 
                        return;
                    }

                    // Reset script pointer in ScriptParseTree for selected gametype 
                    ResetScriptParseTree(PS3, selectedGametype, config);
                    ConsoleWriteSuccess("ScriptParseTree reset");

                    return;
                }

                // Parse Syntax check parameter 
                if(!string.IsNullOrEmpty(o.SyntaxCheckPath)) {
                    ConsoleWriteInfo("Checking syntax...");

                    // Determine path type to syntax check 
                    PathType pathType = ValidatePathType(o.SyntaxCheckPath);
                    switch(pathType) {
                        default:
                        case PathType.File:
                            // TODO 
                            break;
                        case PathType.Directory:
                            // TODO 
                            break;
                        case PathType.String:
                            // TODO 
                            break;
                    }

                    return;
                }

                // Parse Compile parameter 
                if(!string.IsNullOrEmpty(o.CompilePath)) {
                    ConsoleWriteInfo("Compiling...");

                    // Determine path type to compile 
                    PathType pathType = ValidatePathType(o.CompilePath);
                    switch (pathType) {
                        default:
                        case PathType.File:
                            // TODO 
                            break;
                        case PathType.Directory:
                            // TODO 
                            break;
                        case PathType.String:
                            // TODO 
                            break;
                    }

                    return;
                }

                // Parse Injection parameter 
                if(!string.IsNullOrEmpty(o.InjectPath)) {
                    ConsoleWriteInfo("Injecting...");

                    // Connect and attach to PS3 
                    PS3API PS3 = new PS3API(selectedAPI);
                    bool connectedAndAttached = ConnectAndAttachPS3(PS3);
                    if (!connectedAndAttached) { // If target could not connect or process could not attach 
                        return;
                    }

                    // Buffer to be injected is a compiled .gsc file 
                    if (o.InjectCompiledScript) {
                        // TODO 
                    }
                    else {
                        // TODO 
                    }

                    return;
                }
            });
        }

        static void ConsoleWriteError(string msg) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[ERROR] {0}", msg);
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void ConsoleWriteInfo(string msg) {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("[INFO] {0}", msg);
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void ConsoleWriteSuccess(string msg) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[SUCCESS] {0}", msg);
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void ResetScriptParseTree(PS3API PS3, Gametype gametype, Configuration config) {
            switch (gametype) {
                default:
                case Gametype.MP:
                    PS3.Extension.WriteUInt32(config.MP.Defaults.PointerAddress, config.MP.Defaults.BufferAddress);
                    break;
                case Gametype.ZM:
                    PS3.Extension.WriteUInt32(config.ZM.Defaults.PointerAddress, config.ZM.Defaults.BufferAddress);
                    break;
            }
        }

        static bool ConnectAndAttachPS3(PS3API PS3) {
            try {
                if(PS3.ConnectTarget()) {
                    if(PS3.AttachProcess()) {
                        string msg = string.Format("[INFO] Connected and attached to {0}", PS3.GetConsoleName());
                        ConsoleWriteInfo(msg);

                        return true;
                    }
                    else {
                        ConsoleWriteError("Could not attach to process");

                        return false;
                    }
                }
                else {
                    ConsoleWriteError("Could not connect to target");

                    return false;
                }
            }
            catch {
                ConsoleWriteError("An exception occurred trying to connect to the target");

                return false;
            }
        }

        static Configuration LoadConfigurationFile() {
            string exe_dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location); // Get executable directory 
            string config_path = Path.Combine(exe_dir, "config.json"); // Get exe dir + config.json 

            try {
                string config_text = File.ReadAllText(config_path); // Read JSON file from disk 

                return JsonConvert.DeserializeObject<Configuration>(config_text); // Deserialize JSON string into configuration obj 
            }
            catch {
                Configuration new_config = new Configuration(Configuration.GenerateDefaultMPSettings(), Configuration.GenerateDefaultZMSettings()); // Create an new config obj 
                string new_config_str = JsonConvert.SerializeObject(new_config, Formatting.Indented); // Serialize config obj into a string 

                File.WriteAllText(config_path, new_config_str); // Write the serialized config obj to file 

                return new_config;
            }
        }

        static PathType ValidatePathType(string path) {
            if(File.Exists(path)) { // Path is a file 
                return PathType.File;
            }
            else if(Directory.Exists(path)) { // Path is a directory 
                return PathType.Directory;
            }

            return PathType.String; // Path is a raw string 
        }

        static Gametype ValidateGametypeSelection(string gametype) {
            switch(gametype) {
                default:
                case "m":
                case "mp":
                case "M":
                case "MP":
                case "multiplayer":
                case "Multiplayer":
                    return Gametype.MP;
                case "z":
                case "zm":
                case "Z":
                case "ZM":
                case "zombies":
                case "Zombies":
                    return Gametype.ZM;
            }
        }

        static SelectAPI ValidateAPISelection(string api) {
            switch(api) {
                default:
                case "t":
                case "tm":
                case "T":
                case "TM":
                case "TargetManager":
                    return SelectAPI.TargetManager;
                case "c":
                case "cc":
                case "C":
                case "CC":
                case "CCAPI":
                case "ControlConsole":
                    return SelectAPI.ControlConsole;
            }
        }
    }
}
