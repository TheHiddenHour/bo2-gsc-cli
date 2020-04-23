using CommandLine;
using Newtonsoft.Json;
using PS3Lib;
using BO2GSCCompiler;
using Irony.Parsing;
using System;
using System.Data;
using System.IO;
using Irony;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace bo2_gsc_cli {
    enum Gametype {
        MP,
        ZM
    }

    class Program {
        static void Main(string[] args) {
            // Parse configuration JSON file 
            Configuration config = LoadConfigurationFile();

            /*
                Parse the command line parameters.
                Conflicting commands shouldn't be allowed due to their configured exclusivity in the ParameterOptions class.
            */
            CommandLine.Parser.Default.ParseArguments<ParameterOptions>(args).WithParsed(o => {
                // Validate selected PS3 API 
                SelectAPI selectedAPI = ValidateAPISelection(o.SelectedPS3API);
                // Validate selected gametype for resetting the ScriptParseTree and injection 
                Gametype selectedGametype = ValidateGametypeSelection(o.SelectedGametype);
                // Create GSC grammar and parser objects 
                Grammar grammar = new GSCGrammar();
                Irony.Parsing.Parser parser = new Irony.Parsing.Parser(grammar);

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
                    if (Directory.Exists(o.SyntaxCheckPath)) { // Path is a directory 
                        bool errorsFound = false;
                        string[] files = Directory.GetFiles(o.SyntaxCheckPath, "*.gsc", SearchOption.AllDirectories);

                        foreach(string file in files) { // Iterate over every script in the directory 
                            string scriptText = File.ReadAllText(file);
                            ParseTree scriptTree = parser.Parse(scriptText);

                            if (scriptTree.ParserMessages.Count > 0) { // If the script had parsing errors 
                                LogMessage parserMsg = scriptTree.ParserMessages[0];
                                string msg = string.Format("Bad syntax at line {0} in {1}", parserMsg.Location.Line, file);
                                ConsoleWriteError(msg);
                                errorsFound = true;
                            }
                        }

                        if(errorsFound) { // Prevent success message from printing 
                            return;
                        }

                        ConsoleWriteSuccess("No syntax errors found");

                        return;
                    }
                    else if(File.Exists(o.SyntaxCheckPath)) { // Path is a file 
                        string scriptText = File.ReadAllText(o.SyntaxCheckPath);
                        ParseTree scriptTree = parser.Parse(scriptText);

                        if(scriptTree.ParserMessages.Count > 0) { // If the file had parsing errors 
                            LogMessage parserMsg = scriptTree.ParserMessages[0];
                            string msg = string.Format("Bad syntax at line {0} in {1}", parserMsg.Location.Line, o.SyntaxCheckPath);
                            ConsoleWriteError(msg);

                            return;
                        }

                        ConsoleWriteSuccess("No syntax errors found");

                        return;
                    }
                    else { // Path is unrecognized 
                        ConsoleWriteError("Path to file or directory not recognized");

                        return;
                    }
                }

                // Parse Compile parameter 
                if(!string.IsNullOrEmpty(o.CompilePath)) {
                    // Determine path type to compile  
                    if (Directory.Exists(o.CompilePath)) { // Path is a directory 
                        ConsoleWriteInfo("Compiling directory...");

                        string[] files = Directory.GetFiles(o.CompilePath, "*.gsc", SearchOption.AllDirectories);
                        StringBuilder directoryScript = new StringBuilder();
                        ParseTree scriptTree;

                        foreach (string file in files) { // Iterate over every script in the directory 
                            string scriptName = Path.GetFileName(file);
                            string scriptText = File.ReadAllText(file);
                            scriptTree = parser.Parse(scriptText);

                            if (scriptTree.ParserMessages.Count > 0) { // If the file had parsing errors 
                                // Print errors and exit the loop 
                                LogMessage parserMsg = scriptTree.ParserMessages[0];
                                string msg = string.Format("Bad syntax at line {0} in {1}", parserMsg.Location.Line, o.SyntaxCheckPath);
                                ConsoleWriteError(msg);

                                return;
                            }

                            if(scriptName == "main.gsc") { // Add the contents of main.gsc to the top of the string 
                                directoryScript.Insert(0, scriptText + '\n');
                                continue;
                            }
                            directoryScript.Append(scriptText + '\n'); // Append any non-main.gsc text to the bottom like normal 
                        }

                        // Compile script buffer 
                        scriptTree = parser.Parse(directoryScript.ToString());
                        byte[] scriptBuffer = CompileScript(selectedGametype, config, scriptTree);
                        string compiledPath = Path.Combine(o.CompilePath, "compiled.gsc");
                        // Write script buffer to file 
                        File.WriteAllBytes(compiledPath, scriptBuffer);
                        ConsoleWriteSuccess("Compiled directory to " + compiledPath);

                        return;
                    }
                    else if (File.Exists(o.CompilePath)) { // Path is a file 
                        ConsoleWriteInfo("Compiling file...");

                        string scriptText = File.ReadAllText(o.CompilePath);
                        ParseTree scriptTree = parser.Parse(scriptText);

                        if(scriptTree.ParserMessages.Count > 0) {
                            LogMessage parserMsg = scriptTree.ParserMessages[0];
                            string msg = string.Format("Bad syntax at line {0} in {1}", parserMsg.Location.Line, o.SyntaxCheckPath);
                            ConsoleWriteError(msg);

                            return;
                        }

                        string compiledPath = Path.Combine(Path.GetDirectoryName(o.CompilePath), "compiled.gsc");
                        byte[] scriptBuffer = CompileScript(selectedGametype, config, scriptTree);
                        // Write script buffer to file 
                        File.WriteAllBytes(compiledPath, scriptBuffer);
                        ConsoleWriteSuccess("Compiled file to " + compiledPath);

                        return;
                    }
                    else { // Path is unrecognized 
                        ConsoleWriteError("Path to file or directory not recognized");

                        return;
                    }
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

                    if(Directory.Exists(o.InjectPath)) { // Path is directory 
                        string[] files = Directory.GetFiles(o.SyntaxCheckPath, "*.gsc", SearchOption.AllDirectories);
                        StringBuilder sb = new StringBuilder();
                        ParseTree scriptTree;

                        foreach (string file in files) { // Iterate over every script in the directory 
                            string scriptName = Path.GetFileName(file);
                            string scriptText = File.ReadAllText(file);
                            ParseTree _scriptTree = parser.Parse(scriptText);

                            if (_scriptTree.ParserMessages.Count > 0) { // If the script had parsing errors 
                                LogMessage parserMsg = _scriptTree.ParserMessages[0];
                                string _msg = string.Format("Bad syntax at line {0} in {1}", parserMsg.Location.Line, file);
                                ConsoleWriteError(_msg);

                                return;
                            }

                            if(scriptName == "main.gsc") {
                                sb.Insert(0, scriptText);
                                continue;
                            }
                            sb.Append(scriptText);
                        }

                        scriptTree = parser.Parse(sb.ToString());
                        byte[] scriptBuffer = CompileScript(selectedGametype, config, scriptTree);
                        InjectScript(PS3, selectedGametype, config, scriptBuffer);

                        string msg = string.Format("Directory injected ({0} bytes)", scriptBuffer.Length);
                        ConsoleWriteSuccess(msg);

                        return;
                    }
                    else if(File.Exists(o.InjectPath)) { // Path is file 
                        if (o.InjectCompiledScript) { // File is already compiled 
                            byte[] scriptBuffer = File.ReadAllBytes(o.InjectPath);
                            InjectScript(PS3, selectedGametype, config, scriptBuffer);

                            string msg = string.Format("File injected ({0} bytes)", scriptBuffer.Length);
                            ConsoleWriteSuccess(msg);

                            return;
                        }
                        else { // File is not compiled 
                            string scriptText = File.ReadAllText(o.InjectPath);
                            ParseTree scriptTree = parser.Parse(scriptText);
                            byte[] scriptBuffer = CompileScript(selectedGametype, config, scriptTree);

                            InjectScript(PS3, selectedGametype, config, scriptBuffer);
                            string msg = string.Format("File injected ({0} bytes)", scriptBuffer.Length);
                            ConsoleWriteSuccess(msg);

                            return;
                        }
                    }
                    else {
                        ConsoleWriteError("Path to file or directory not recognized");

                        return;
                    }

                    return;
                }
            });
        }

        static void InjectScript(PS3API PS3, Gametype gametype, Configuration config, byte[] script) {
            switch(gametype) {
                default:
                case Gametype.MP:
                    PS3.Extension.WriteUInt32(config.MP.Defaults.PointerAddress, config.MP.Customs.BufferAddress); // Write script pointer 
                    PS3.Extension.WriteBytes(config.MP.Customs.BufferAddress, script); // Write script buffer 

                    return;
                case Gametype.ZM:
                    PS3.Extension.WriteUInt32(config.ZM.Defaults.PointerAddress, config.ZM.Customs.BufferAddress); // Write script pointer 
                    PS3.Extension.WriteBytes(config.ZM.Customs.BufferAddress, script); // Write script buffer 

                    return;
            }
        }

        static byte[] CompileScript(Gametype gametype, Configuration config, ParseTree tree) {
            Compiler compiler;
            switch(gametype) {
                default:
                case Gametype.MP:
                    compiler = new Compiler(tree, config.MP.ScriptPath);
                     
                    return compiler.CompileScript();
                case Gametype.ZM:
                    compiler = new Compiler(tree, config.ZM.ScriptPath);

                    return compiler.CompileScript();
            }
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
    }
}
