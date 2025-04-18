# GPIBServer [Work in progress]
There's a decent OSS USB-GPIB controller [AR488 by Twilight Logic](https://github.com/Twilight-Logic/AR488). However currently there's no OSS available to acomplish some basic automated data acquisition through GPIB. That's why I opted to write my own 'General purpose GPIB data collection server'.
It's much more limited than, for example, EZGPIB, however it's open-source and (should be) cross-platform thanks to .NET Core 3.1 and [SerialPortStream](https://github.com/jcurl/SerialPortStream) IO library. It also doesn't have the annoying quirks of EZGPIB that made it unusable for me, namely: unavoidable "probing" of all serial ports during startup that not only disturbes some of my hardware that can't be disturbed, but also fails to detect AR488 due to Arduino's DTR reset behavior. The last one could be fixed by either modification of the Arduino (that results in inability to hardware reset the device and complicates it's reflashing process) or editing EZGPIB executable, but all of this makes learning how to use EZGPIB unfeasible altogether.

# Features
 - Simple command script execution;
 - Parallel execution of multiple scripts (note to self: only useful when multiple controllers are present, otherwise a deadlock is pretty much guaranteed);
 - Looping through a subset if scripted commands (`LoopIndex` parameter determines where the execution jumps after the script reaches its end);
 - Multiple controller support (with multiple addressable instruments);
 - Configurable data output (as formatted text files and/or through a named pipe);
 - JSON-serialized controller and instrument databases;
 - Simple controller/instrument simulator project available to assist debugging;
 - **New in v0.2:** Support variables and math expression evaluation, thanks to [MatheVAL.NET](https://github.com/surfsky/MathEval.NET).
 
# Limitations
 - No conditional branching or jumps in the script are supported (intentional choiсe to keep things simple, conditional branch and jump implementation would require a complete rewrite of script execution logic and probably a storage format reevaluation);
 - Currently, all controllers are expected to be identified as serial ports (though this is not that hard to generalize, I just don't happen to have any non-serial-port controllers to work with);
 - This project is a work in progress. No extensive testing was done under any OS but Windows, though all the dependencies are cross-platform.
 
# Quick Start
 - Run the application with option "-g". This will generate example configuration files (inside the working directory) from which you can infer their structure.
 - To assign values to variables or compute math expressions, use variable context (prefixed with `var:` by default), like this: `"var:v=IF((f%2)>0, v-0.01, v+0.01)",`. Variables can be used as GPIB command arguments utilizing shell-like syntax, for example: `AR488.Instrument.Command(${v}, ${f})`, here `v` abd `f` are variables and `v` is incremented or decremented depending on the value of `f` (bi-directional sweep). Everything after `=` is evaluated by [MatheVAL.NET](https://github.com/surfsky/MathEval.NET), see their readme for details.
 - Variables have to be defined before they are read. Considering the example above, the script must contain the following definition (usually in the beginning): `var:f=0`.
 - To use command arguments, insert .NET string-format specifiers into the command string definition, for example: `:SENS:VOLT {0}` will substitute `{0}` with first argument, passed to the command from the script.
 - Edit the configuration files to match your setup and task. You can verify that a correct sequence of commands is produced with the instrument(s) OFF and all script commands (temporarily) set to "AwaitResponse = false". The commands sent will be visible in the console.
 - You can perform a more extensive behavior validation using InstrumentSimulator project. It contains an almost barebones implementation of a JSON-serialized GPIB controller/instrument simulator that can be customized to fit your particular needs. Out-of-the-box it offers only a simple string reply dictionary and address matching.
 - Try it with a real connected instrument.
