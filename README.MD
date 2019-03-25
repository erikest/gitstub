This program automates the creation of a github repo and connecting that to a new dotnet project/solution.
It is intended to be invoked as a dotnet cli extension.

Installation: build the project, the resulting exe, dotnet-gitstub, can be moved anywhere, but that location needs to be added to PATH environment variable to be seen by dotnet as an extension.

Usage:  dotnet gitstub &lt;new args exa: console, or exa: webapp&gt; -gsu &lt;github user&gt; -gsp &lt;github password&gt; -gsr &lt;repo/project name&gt; -gss &lt;solution/project name [OPTIONAL] -gsd &lt;project description [OPTIONAL]&gt; -gsa &lt;'pub' for public otherwise private [OPTIONAL]&gt;

Note:  If specifying -gss, the program uses that as the github project name and creates a solution and adds -gsp as a project under src/&lt;project&gt;.


