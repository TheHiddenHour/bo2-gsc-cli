# bo2-gsc-cli 
Call of Duty: Black Ops II GSC syntax checker, compiler, and injector for CFW-enabled PlayStation 3s.

## Parameters 
```
-a, --api :			PS3 API to use for resetting the ScriptParseTree or injection
-g, --gametype :	Gametype to reset the ScriptParseTree or inject for
-r, --reset :		Reset the ScriptParseTree
-s, --syntax :		Syntax check an uncompiled .gsc file or directory
-c, --compile :		Compile a .gsc file or directory containing main.gsc in it's root
-i, --inject :		Inject an uncompiled .gsc file or directory containing main.gsc in it's root
--compiled :		Whether the .gsc file to be injected is compiled
--help :			Display the help screen
--version :			Display version information
```

## Syntax Checking
* Check the syntax of a .gsc file or the contents of a directory.

### Usage Example 
`bo2-gsc-cli.exe -s /dir/to/script.gsc` :	Syntax check script.gsc 

`bo2-gsc-cli.exe -s /dir/to/script-dir/` :	Syntax check the contents of /script-dir/ 

## Compiling 
* Compile a .gsc file or the contents of a directory containing main.gsc in it's root.

* Scripts are compiled by default for multiplayer unless `-g, --gametype` is set to zombies.

### Usage Example 
`bo2-gsc-cli.exe -c /dir/to/script.gsc` :		Compile script.gsc to compiled.gsc for multiplayer 

`bo2-gsc-cli.exe -g MP -c /dir/to/script.gsc` :	Compile script.gsc to compiled.gsc for multiplayer 

`bo2-gsc-cli.exe -g ZM -c /dir/to/script.gsc` :	Compile script.gsc to compiled.gsc for zombies  

## Injection
* Inject a pre-compiled .gsc file, uncompiled .gsc file, or directory containing main.gsc in it's root.

* Scripts are injected by default using Target Manager unless `-a, --api` is set to Control Console.

* Script are compiled by default for multiplayer unless `-g, --gametype` is set to zombies.

* `--compiled` can be used to inject a pre-compiled .gsc file. 

### Usage Example 
`bo2-gsc-cli.exe -i /dir/to/script.gsc` :				Compile script.gsc for multiplayer and inject using Target Manager 

`bo2-gsc-cli.exe -a CC -g ZM -i /dir/to/script.gsc` :	Compile script.gsc for zombies and inject using Control Console 

`bo2-gsc-cli.exe -a CC -i /dir/to/script-dir/` :		Compile the contents of /script-dir/ for multiplayer and inject using Control Console

`bo2-gsc-cli.exe --compiled -i /dir/to/script.gsc` :	Inject pre-compiled script.gsc for multiplayer using Target Manager

`bo2-gsc-cli.exe --compiled -g ZM -i /dir/to/script.gsc` :	Inject pre-compiled script.gsc for zombies using Target Manager

# Credits
* dtx12 - original BO2 GSC compiler

* CraigChrist8239 - original console compiler port

* SeriousHD - updated console compiler