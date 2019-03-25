# gitstub

This dotnet cli extension automates the creation of a github repository, a new dotnet project/solution and a local git repository remoted to github.

#### Installation 
1. publish for your platform (exa: dotnet publish -c Release -r Win10-x64)
2. place the resulting executable (on *nix make sure to chmod +x to make it executable), which should be named dotnet-gitstub, in whichever folder you choose
3. modify your PATH environment variable to include the location of dotnet-gitstub, because the dotnet CLI searches the PATH for executables with the naming convention dotnet-subcommand 

#### Parameters
The gitstub command takes several parameters:
- **-gsu** - your github username
- **-gsp** - your github password 
- **-gsr** - the name of the github repo to create, which is also used as the name for the dotnet project that is created
- **-gsd** - the description for the github repository
- **-gss** - the name of the solution.  This is **optional**.  If it is included, the github repository will be named after the solution.  See below for more info
- **-gsa** - repository access, defaults to private, use 'pub' for public repos. **optional**
- all other parameters are passed on to **dotnet new**, for clarity, it is best to put these arguments immediately after **dotnet gitstub**, exa: **dotnet gitstub webapp &lt;other parameters&gt;**

#### Solutions and Projects  
If you specify just a *project name* with **-gsr**, the tool will create the project in the current directory:
- &lt;**project name**&gt;.csproj
- &lt;**project files**&gt;

If you specify a *solution name* with **-gss**, the tool will create a simple solution/project structure:  
- &lt;**solution name**&gt;.sln
- **src/**
    - &lt;**project name**&gt;/
        - &lt;**project name**&gt;.csproj 
        - &lt;**project files**&gt;...
 
#### Examples
- *dotnet gitstub* console **-gsu** gunnarbfamoz **-gsp** herddatsheep **-gsr** ConsoleAppFTW 
- *dotnet gitstub* webapp **-gsu** gnottareelname **-gsp** cantguessme **-gsr** DemoWebAppOfAwesomeness **-gss** MotherOfAllDemoSolutions **-gsd** "*This demonstrates the awesome power of gitstub! no? okay.. the modest power of gitstub! .. .. hmm, then perhaps the marginal utility of gitstub?*"

