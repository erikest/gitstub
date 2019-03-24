﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;


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
        /// -gsr repository/project name to create
        /// -gss solution/repository name [OPTIONAL]
        /// -gsd description of new project [OPTIONAL]
        /// -gaa access, pub for public, private otherwise [OPTIONAL]
        /// </param>
        static void Main(string[] args)
        {
            //1: Parse Args
            string user, pass, project, solution, desc, accessPrivate;
            List<string> dotnetNewArgs;
            ParseArguments(args, out user, out pass, out project, out solution, out desc, out accessPrivate, out dotnetNewArgs);

            //2:  Make API Call to create the github repository
            string auth, repoName;
            CreateGitHubRepository(user, pass, project, solution, desc, accessPrivate, out auth, out repoName);

            //3:  Run Git Commands - Let's get ready
            InitializeLocalRepository(user, auth, repoName);

            //4:  Create new project - To Rumble!
            CreateProjectStructure(project, solution, dotnetNewArgs);

            //5:  Add, Commit, Push - ohhh, feel the power!
            AddCommitPush();
        }

        private static void AddCommitPush()
        {
            RunGit("add -A"); //Add all the files in the directory
            RunGit("commit -m \"gitstub => Project Initialization\"");
            RunGit("push --set-upstream origin master");
        }

        private static void CreateProjectStructure(string project, string solution, List<string> dotnetNewArgs)
        {
            if (!string.IsNullOrWhiteSpace(solution)) //if solution was specified, make a simple structure
            {
                //Create the folder structure =>src/Proj/
                var projSubDir = $"src\\{project}";
                Directory.CreateDirectory(projSubDir);
                RunDotnetNew($"sln -n {solution}"); //Create the solution file

                //Create the project in src/project
                RunDotnetNew(string.Join(" ", dotnetNewArgs), $"\\{projSubDir}");
                RunDotnetSln($"add {projSubDir}"); //Add the new project to the solution

                //TODO add tests/testProject and any other structure scaffolding
            }
            else
                RunDotnetNew(string.Join(" ", dotnetNewArgs));

            //Add .gitignore
            //Thank you: https://github.com/dotnet/core/blob/master/.gitignore
            File.WriteAllText(".gitignore", gitIgnores(), System.Text.Encoding.ASCII);
        }

        private static void InitializeLocalRepository(string user, string auth, string repoName)
        {

            //Initialize the local git repo
            var gitArgs = "init";
            RunGit(gitArgs);

            //Point the repository to github location
            RunGit($"remote add origin https://{auth}@github.com/{user}/{repoName}.git");
        }

        private static void CreateGitHubRepository(string user, string pass, string project, string solution, string desc, string accessPrivate, out string auth, out string repoName)
        {
            ////    This can be extended to provide more parameters to creation, in addition to name and description
            ////    This can be extended to parse the response in case of error, for example, if the repo already exists.

            //Thank you: https://stackoverflow.com/questions/2423777/is-it-possible-to-create-a-remote-repo-on-github-from-the-cli-without-opening-br
            var gitHubApiUrl = "https://api.github.com/user/repos";
            auth = $"{user}:{pass}";
            repoName = solution ?? project;
            //Thank you: https://stackoverflow.com/questions/18971510/how-do-i-set-up-httpcontent-for-my-httpclient-postasync-second-parameter
            var apiValues = $"{{\"name\":\"{repoName}\", \"description\":\"{desc}\", \"private\":\"{accessPrivate}\"}}";
            var apiContent = new StringContent(apiValues, System.Text.Encoding.UTF8, "application/json");
            using (var client = new HttpClient())
            {
                try
                {

                    //Thank you: https://stackoverflow.com/questions/50732772/best-curl-u-equivalence-in-c-sharp
                    client.DefaultRequestHeaders.Add("Authorization", "Basic " + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(auth)));
                    //Thank you: https://stackoverflow.com/questions/39907742/github-api-is-responding-with-a-403-when-using-requests-request-function
                    client.DefaultRequestHeaders.Add("User-Agent", $"{user}/{project}");

                    var response = client.PostAsync(gitHubApiUrl, apiContent).Result;
                    if (response.StatusCode == System.Net.HttpStatusCode.Created) //SUCCESS!! If only it was so easy in the rest of your life...
                        Console.WriteLine($"GitHub Repository Created: https://github.com/{user}/{repoName}");

                    //TODO check the response for problems
                    //Left as an exercise to the forker...
                }
                catch (HttpRequestException reqEx)
                {
                    //TODO: be better at everything, including exception handling, but do that part last, because hubris.
                }
            }
        }

        private static void ParseArguments(string[] args, out string user, out string pass, out string project, out string solution, out string desc, out string accessPrivate, out List<string> dotnetNewArgs)
        {
            ////Help me Hanselman: Better to have options not to need username and pass, like supporting SSH, or caching credentials in a secrets manager or some such, right?
            user = null;
            pass = null;
            project = null;
            solution = null;
            desc = "";
            accessPrivate = "true";
            dotnetNewArgs = new List<string>();
            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i].ToLower())
                    {
                        case "-gsu":
                            user = args[++i]; break;
                        case "-gsp":
                            pass = args[++i]; break;
                        case "-gsr":
                            project = args[++i]; break;
                        case "-gss":
                            solution = args[++i]; break;
                        case "-gsd":
                            desc = args[++i]; break;
                        case "-gsa":
                            accessPrivate = args[++i].ToLower() == "pub" ? "false" : "true"; break;
                        default:
                            dotnetNewArgs.Add(args[i]); break;
                    }
                }
            }
            catch (IndexOutOfRangeException outofRange)
            {
                Console.WriteLine("bork: Invalid Parameters.  It's not you baby, it's just the params.");
            }

            //TODO throw nasty exceptions if the args aren't filled in.  Make 'em pay.
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
            using (var gitProc = new Process() { StartInfo = new ProcessStartInfo("dotnet", "new " + args) { WorkingDirectory =Directory.GetCurrentDirectory() +  workDirectory } })
            {
                gitProc.Start();
                gitProc.WaitForExit();
            }
        }

        private static void RunDotnetSln(string args)
        {
            using (var gitProc = new Process() { StartInfo = new ProcessStartInfo("dotnet", "sln " + args) })
            {
                gitProc.Start();
                gitProc.WaitForExit();
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
}