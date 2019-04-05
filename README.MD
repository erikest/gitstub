# gitstub

This dotnet cli extension and/or global tool automates the creation of a github repository, using a new (or existing) dotnet project/solution and a local git repository remoted to github.

#### Installation 

##### Manually Publish and Deploy

1. publish for your platform (exa: dotnet publish -c Release -r Win10-x64)
2. place the resulting executable (on *nix make sure to chmod +x to make it executable), which should be named dotnet-gitstub, in whichever folder you choose
3. modify your PATH environment variable to include the location of dotnet-gitstub, because the dotnet CLI searches the PATH for executables with the naming convention dotnet-subcommand 

The tool is then available as a cli extension by calling *dotnet gitstub*

##### As a Global Tool (easier)

1. dotnet install --global gitstub

The tool is then available by directly calling *gitstub*, you don't need to use *dotnet gitstub*

#### Parameters
The gitstub command takes several parameters:
- **-gsu** - your github username
- **-gsp** - your github password 
- **-gsr** - the name of the github repo to create, which is also used as the name for the dotnet project that is created
- **-gsd** - the description for the github repository
- **-gss** - the name of the solution.  This is **optional**.  If it is included, the github repository will be named after the solution.  See below for more info
- **-gsa** - repository access, defaults to private, use 'pub' for public repos. **optional**
- **-gsc** - commit message **optional**
- **--existing** - use the existing solution/project in the current working directory, instead of creating a new project/solution.  Note: this will still add .gitignore.  Also note, this overrides -gsr, -gss and --sln, so don't use them together **optional**
- **--sln** - create a solution as well as a project.  Use this when you want to use the current directory name for a solution and project name. **optional**
- all other parameters are passed on to **dotnet new**. For clarity, it is best to put these arguments immediately after **dotnet gitstub**, exa: **dotnet gitstub webapp &lt;other parameters&gt;**

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
		
If you don't specify a *project name*, the current directory will be used
If you don't specify a *soluton name*, but add the **--sln** flag, a solution will made from the *current directory*
 
#### Examples

##### CLI Extension
- *dotnet gitstub* console **-gsu** gunnarbfamoz **-gsp** herddatsheep **-gsr** ConsoleAppFTW 
- *dotnet gitstub* webapp **-gsu** gnottareelname **-gsp** cantguessme **-gsr** DemoWebAppOfAwesomeness **-gss** MotherOfAllDemoSolutions **-gsd** "*This demonstrates the awesome power of gitstub! no? okay.. the modest power of gitstub! .. .. hmm, then perhaps the marginal utility of gitstub?*"

##### Global Tool
- *gitstub* webapp **--sln** **-gsu** quickstubmcgee **-gsp** fasteststubinthewest
- *gitstub* grpc **-gsu** mrclientserver **-gsp** fasterthanjson **-gsr** CoolNewClientServerProject **-gss** CoolNewClientServerSolution **-gsd** "Testing the new gRPC awesomeness" **-gsc** "Nothing as pure as the first commit"
- *gitstub* **--existing** **-gsu** diffthedude **-gsp** tiedtheroomtogether

