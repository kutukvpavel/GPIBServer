# GPIBServer [Work in progress]
There's a decent OSS USB-GPIB controller [AR488 by Twilight Logic](https://github.com/Twilight-Logic/AR488). However currently there's no OSS available to acomplish some basic automated data acquisition through GPIB. That's why I opted to write my own 'General purpose GPIB data collection server'.
It's much more limited than, for example, EZGPIB, however it's open-source and (should be) cross-platform thanks to .NET Core 3.1 and SerialPortStream IO library.

# Features
 - Simple command script execution;
 - Parallel execution of multiple scripts;
 - Multiple controller (with multiple addressable instruments) support;
 - Configurable data output (as formatted text files and/or through a named pipe);
 - JSON-serialized controller and instrument databases.
 
# Limitations
 - No branching or jumps in the script are supported (intentional choise to keep things simple, conditional branch and jump implementstion would require a complete rewrite of script execution logic and probably a storage format reevaluation);
 - Currently, all controllers are expected to be identified as serial ports (though this is not that hard to generalize, I just don't happed to have any non-serial-port controllers to work with);
 - This project is a work in progress. No extensive testing was done under any OS but Windows, though all the dependencies are cross-platform.
 
# Quick Start
 - Run the application with option "-g". This will generate example configuration files (inside thw working directory) from which you can infer their structure.
 - Edit the configuration files to match your setup and task. You can verify that a correct sequence of commands is produced with the instrument(s) OFF and all script commands (temporarily) set to "AwaitResponse = false". The commands sent will be visible in the console.
 - Try it with a real connected instrument.
