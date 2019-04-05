using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Security;


//Thank you: https://blog.maartenballiauw.be/post/2017/04/10/extending-dotnet-cli-with-custom-tools.html, for clarity on CLI extensions

namespace gitstub
{
    class Program
    {
        /// <summary>
        /// Creates a GitHub project, a new dotnet project, the local git repository for the project and then commits the 'stub'.
        /// </summary>
        /// <param name="args">
        /// -gsu GitHub username
        /// -gsp GitHub password
        /// -gsr repository/project name to create, default to current directory name [OPTIONAL]
        /// -gss solution/repository name, creates a solution and folder stucture for the project [OPTIONAL]
        /// -gsd description of new project [OPTIONAL]
        /// -gsa access, pub for public, private otherwise [OPTIONAL]
        /// -gsc commit message [OPTIONAL]
        /// --existing, use the existing solution/project in the current working directory, instead of creating a new project/solution.  Note: this will still add .gitignore [OPTIONAL]
        /// --sln, create a solution as well as a project.  Use this when you want to use the current directory name for a solution and project name. [OPTIONAL]
        /// </param>
        static void Main(string[] args)
        {
            //1: Parse Args            
            var project = ParseArguments(args);

            //2:  Make API Call to create the github repository            
            CreateGitHubRepository(project);

            //3:  Run Git Commands - Let's get ready
            InitializeLocalRepository(project);

            //4:  Create new project - To Rumble!
            CreateProjectStructure(project);

            //5:  Add, Commit, Push - ohhh, feel the power!
            AddCommitPush(project);
        }

        private static void AddCommitPush(GitStubProject project)
        {
            RunGit("add -A"); //Add all the files in the directory
            RunGit($"commit -m \"{project.CommitMessage}\"");
            RunGit("push --set-upstream origin master");
        }

        private static void CreateProjectStructure(GitStubProject project)
        {
            if (!project.UseExisting)
            {
                if (project.UseSolution) //if solution was specified, make a simple structure
                {
                    //Create the folder structure =>src/Proj/
                    var projSubDir = $"src\\{project.Project}";
                    Directory.CreateDirectory(projSubDir);
                    RunDotnetNew($"sln -n {project.Solution}"); //Create the solution file

                    //Create the project in src/project
                    RunDotnetNew(string.Join(" ", project.NewArgs), $"\\{projSubDir}");
                    RunDotnetSln($"add {projSubDir}"); //Add the new project to the solution

                    //TODO add tests/testProject and any other structure scaffolding
                }
                else
                    RunDotnetNew(string.Join(" ", project.NewArgs));
            }

            //Add .gitignore
            //Thank you: https://github.com/dotnet/core/blob/master/.gitignore
            File.WriteAllText(".gitignore", gitIgnores(), System.Text.Encoding.ASCII);
        }

        private static void InitializeLocalRepository(GitStubProject project)
        {
            //Initialize the local git repo
            var gitArgs = "init";
            RunGit(gitArgs);

            //Point the repository to github location
            RunGit($"remote add origin https://{project.Auth}@github.com/{project.User}/{project.RepoName}.git");
        }

        private static void CreateGitHubRepository(GitStubProject project)
        {
            ////    This can be extended to provide more parameters to creation, in addition to name and description
            //Thank you: https://stackoverflow.com/questions/18971510/how-do-i-set-up-httpcontent-for-my-httpclient-postasync-second-parameter
            var apiValues = $"{{\"name\":\"{project.RepoName}\", \"description\":\"{project.Desc}\", \"private\":\"{project.AccessPrivate}\"}}";
            var apiContent = new StringContent(apiValues, System.Text.Encoding.UTF8, "application/json");
            using (var client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = CreateGitHubRepository(project, apiContent, client);

                    ProcessGitHubAPIResponse(project, response);
                }
                catch (HttpRequestException reqEx)
                {
                    Console.WriteLine($"HttpRequestException while trying to create the repository on GitHub.  The exception says: {reqEx.Message}");
                }
            }
        }

        private static HttpResponseMessage CreateGitHubRepository(GitStubProject project, StringContent apiContent, HttpClient client)
        {
            //Thank you: https://stackoverflow.com/questions/2423777/is-it-possible-to-create-a-remote-repo-on-github-from-the-cli-without-opening-br
            var gitHubApiUrl = "https://api.github.com/user/repos";
            //Thank you: https://stackoverflow.com/questions/50732772/best-curl-u-equivalence-in-c-sharp
            client.DefaultRequestHeaders.Add("Authorization", "Basic " + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(project.Auth)));
            //Thank you: https://stackoverflow.com/questions/39907742/github-api-is-responding-with-a-403-when-using-requests-request-function
            client.DefaultRequestHeaders.Add("User-Agent", $"{project.User}/{project.RepoName}");

            var response = client.PostAsync(gitHubApiUrl, apiContent).Result; 
            return response;
        }

        private static void ProcessGitHubAPIResponse(GitStubProject project, HttpResponseMessage response)
        {
            (string code, string msg) err = (null, null);
            switch (response.StatusCode)
            {
                case HttpStatusCode.Created: //SUCCESS!! If only it was so easy in the rest of your life...
                    Console.WriteLine($"GitHub Repository Created: https://github.com/{project.User}/{project.RepoName}"); break;
                case HttpStatusCode.NotFound:
                    err = ("404", "Not Found.  In this context, it probably means a problem with your username or password"); break;
                case HttpStatusCode.Forbidden:
                    err = ("403", "Forbidden.  Too many failed login attempts"); break;
                case HttpStatusCode.Unauthorized:
                    err = ("401", "Unauthorized. Check username and password"); break;
                default:
                    err = (response.StatusCode.ToString(), "Not sure what happened here..."); break;
            }

            if (err.code != null)
            {
                Console.WriteLine($"GitHub API Error, returned ({err.code}) while trying to create the repository. {err.msg}");
                Environment.Exit(2);
            }
        }

        private static GitStubProject ParseArguments(string[] args)
        {
            ////Help me Hanselman: Better to have options not to need username and pass, like supporting SSH, or caching credentials in a secrets manager or some such, right?
            var project = ProcessArgsArray(args);

            ValidateParameters(project);

            return project;
        }

        private static void ValidateParameters(GitStubProject project)
        {
            List<(string field, string param)> errs = new List<(string, string)>();
            if (string.IsNullOrWhiteSpace(project.User))
                errs.Add(("your GitHub username", "-gsa <username>"));
            if (string.IsNullOrWhiteSpace(project.Pass))
                errs.Add(("your GitHub password", "-gsp <password>"));            

            if (errs.Count > 0)
            {
                errs.ForEach(err => Console.WriteLine($"Error: missing parameter, need {err.field}, please specify it with {err.param}"));
                Environment.Exit(1);
            }
        }

        private static GitStubProject ProcessArgsArray(string[] args)
        {
            GitStubProject project = new GitStubProject();
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i].ToLower())
                    {
                        case "-gsu":
                            project.User = args[++i]; break;
                        case "-gsp":
                            project.Pass = args[++i]; break;
                        case "-gsr":
                            project.Project = args[++i]; break;
                        case "-gss":
                            project.Solution = args[++i];
                            project.UseSolution = true;
                            break;
                        case "-gsd":
                            project.Desc = args[++i]; break;
                        case "-gsc":
                            project.CommitMessage = args[++i]; break;
                        case "-gsa":
                            project.AccessPrivate = args[++i].ToLower() == "pub" ? "false" : "true"; break;
                        case "--existing":
                            project.UseExisting = true; break;
                        case "--sln":
                            project.UseSolution = true;break;
                        default:
                            project.NewArgs.Add(args[i]); break;
                    }
                }
            }
            catch (IndexOutOfRangeException outofRange)
            {
                Console.WriteLine("Error: Invalid Parameters.  It's not you baby, it's just the params.");
                Environment.Exit(2);
            }

            return project;
        }

        //These should probably monitor the std or err out and do nice, gentle things when git gets rough.
        private static void RunGit(string gitArgs)
        {
            using (var gitProc = new Process() { StartInfo = new ProcessStartInfo("git", gitArgs) })
            {
                gitProc.Start();
                gitProc.WaitForExit();
            }
        }

        private static void RunDotnetNew(string args, string workDirectory=null)
        {
            using (var dotnetProc = new Process() { StartInfo = new ProcessStartInfo("dotnet", "new " + args) { WorkingDirectory =Directory.GetCurrentDirectory() +  workDirectory } })
            {
                dotnetProc.Start();
                dotnetProc.WaitForExit();
            }
        }

        private static void RunDotnetSln(string args)
        {
            using (var dotnetProc = new Process() { StartInfo = new ProcessStartInfo("dotnet", "sln " + args) })
            {
                dotnetProc.Start();
                dotnetProc.WaitForExit();
            }
        }

        //Help me Hanselman: You found a sweet 5k .gitignore file on the 3/5/19 standup... where's that again?  Explain it to me like I'm a 5 year old.
        static string gitIgnores()
        {
            return 
@"*.swp
*.* ~
project.lock.json
.DS_Store
*.pyc
nupkg/

# Visual Studio Code
.vscode

# Rider
.idea

# User-specific files
*.suo
*.user
*.userosscache
*.sln.docstates

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
build/
bld/
[Bb]in/
[Oo]bj/
[Oo]ut/
msbuild.log
msbuild.err
msbuild.wrn

# Visual Studio 2015
.vs / ";
        }
    }

    internal class GitStubProject
    {
        public string User { get; set; }
        public string Pass { get; set; }
        public string Project { get; set; } = Path.GetFileName(Directory.GetCurrentDirectory());
        public string Solution { get; set; } = Path.GetFileName(Directory.GetCurrentDirectory());
        public string Desc { get; set; }
        public string CommitMessage { get; set; } = "GitStub => Project Initialization!";
        public string AccessPrivate { get; set; }
        public bool UseExisting { get; set; }
        public bool UseSolution { get; set; }
        public List<string> NewArgs { get; set; } = new List<string>();

        public string Auth => $"{User}:{Pass}";

        public string RepoName => Solution ?? Project;
    }
}
