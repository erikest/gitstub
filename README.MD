This program automates the creation of a github repo and connecting that to a new dotnet project/solution.
It is intended to be invoked as a dotnet cli extension.

Installation: build the project, the resulting exe, dotnet-gitstub, can be moved anywhere, but that location needs to be added to PATH environment variable to be seen by dotnet as an extension.

Usage:  dotnet gitstub <new args exa: console, or exa: webapp> -gsu <github user> -gsp <github password> -gsr <repo/project name> -gss <solution/project name [OPTIONAL] -gsd <project description [OPTIONAL]> -gsa <'pub' for public otherwise private [OPTIONAL]>

Note:  If specifying -gss, the program uses that as the github project name and creates a solution and adds -gsp as a project under src/<project>.


