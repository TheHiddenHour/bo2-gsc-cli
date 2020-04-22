using CommandLine;

namespace bo2_gsc_cli {
    class ParameterOptions {
        [Option('a', "api", HelpText = "PS3 API to use for reseting the ScriptParseTree and injection")]
        public string SelectedPS3API { get; set; }

        [Option('g', "gametype", HelpText = "Gametype to reset the ScriptParseTree or inject for")]
        public string SelectedGametype { get; set; }

        [Option('r', "reset", SetName = "reset", HelpText = "Reset the ScriptParseTree")]
        public bool ResetScriptParseTree { get; set; }

        [Option('s', "syntax", SetName = "syntax", HelpText = "Syntax check an uncompiled string, uncompiled .gsc file, or directory containing main.gsc in it's root")]
        public string SyntaxCheckPath { get; set; }

        [Option('c', "compile", SetName = "compile", HelpText = "Compile a string, file, or directory")]
        public string CompilePath { get; set; }

        [Option('i', "inject", SetName = "inject", HelpText = "Inject an uncompiled string, uncompiled or compiled .gsc file, or a directory containing main.gsc in it's root")]
        public string InjectPath { get; set; }
        
        [Option("compiled", SetName = "inject", HelpText = "Whether the .gsc file to be injected is compiled")]
        public bool InjectCompiledScript { get; set; }
    }
}
