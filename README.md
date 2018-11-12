This is a template to use for a Kontur.LogPacker task solution.  
  
Consists of the following projects:

### Kontur.LogPacker

An empty project for you to fill in.

### Kontur.LogPacker.SelfCheck

A set of simple tests to validate the correctness of your solution. Run with `dotnet run -c Release <project-directory>`, for example `dotnet run -c Release ../Kontur.LogPacker`.

### Kontur.LogPacker.SubmitHelper

An utility that helps you properly pack your solution into a zip archive for submission. The resulting zip file will be placed into the root directory of the solution (the folder where `Kontur.LogPacker.sln` resides).
Run with `dotnet run -c Release` of simply from the IDE.

### Kontur.LogPackerGZip

A simple log archiver that uses GZip. Is used internally by `Kontur.LogPacker.SelfCheck`.
