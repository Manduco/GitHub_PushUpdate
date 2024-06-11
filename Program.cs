using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GitHub_PushUpdate
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start");
           
            //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

            List<string> repoPaths = new List<string>
            {
                @"C:\Users\arman\source\repos\DRF-TPS\Praxedo_Testing", // Add more paths as needed
                @"C:\Users\arman\source\repos\Praxedo_Pull_PDF",
                @"C:\Users\arman\source\repos\Praxedo_GetAssignedTech",
                @"C:\Users\arman\source\repos\Praxedo_BackGround_Updates",
                @"C:\Users\arman\source\repos\InforConfig\InforConfig"
            };

            string remoteName = "origin";
            string username = "Manduco"; // Set this in your environment
            string token = "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";       // Set this in your environment
            string branchName = "main"; // Change this to your branch name if it's different

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(token))
            {
                Console.WriteLine("GitHub username or token environment variables are not set.");
                return;
            }

            foreach (var repoPath in repoPaths)
            {
                if (!Directory.Exists(repoPath) || !Directory.Exists(Path.Combine(repoPath, ".git")))
                {
                    Console.WriteLine($"The specified path '{repoPath}' does not contain a Git repository.");
                    continue;
                }

                try
                {
                    using (var repo = new Repository(repoPath))
                    {
                        // Fetch latest changes from the remote
                        var remote = repo.Network.Remotes[remoteName];
                        if (remote == null)
                        {
                            Console.WriteLine($"Remote '{remoteName}' not found for repository at '{repoPath}'.");
                            continue;
                        }

                        var refSpecs = remote.FetchRefSpecs.Select(x => x.Specification).ToArray();
                        var fetchOptions = new FetchOptions
                        {
                            CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials
                            {
                                Username = username,
                                Password = token
                            }
                        };

                        try
                        {
                            Commands.Fetch(repo, remote.Name, refSpecs, fetchOptions, logMessage: null);
                        }
                        catch (Exception fetchEx)
                        {
                           // Console.WriteLine($"Fetching error for repository at '{repoPath}': {fetchEx.Message}");
                            Logout_red($"Fetching error for repository at '{repoPath}': {fetchEx.Message}");
                            Console.WriteLine(fetchEx.StackTrace);
                            continue;
                        }

                        // Check if HEAD is detached
                        bool isHeadDetached = repo.Head.Tip != null && !repo.Head.IsCurrentRepositoryHead;

                        // Loop through all branches
                        foreach (var branch in repo.Branches)
                        {
                            if (branch.IsRemote || (isHeadDetached && branch.FriendlyName == repo.Head.FriendlyName))
                            {
                                continue; // Skip remote branches and detached HEAD branch
                            }

                            //Logout_green($"Local branch: '{branch.FriendlyName}' in repository '{repoPath}'");
                            Console.WriteLine($"Local branch: '{branch.FriendlyName}' in repository '{repoPath}'");

                            var remoteBranch = branch.TrackedBranch;
                            if (remoteBranch != null)
                            {
                                Console.WriteLine($"  Tracking remote branch: '{remoteBranch.FriendlyName}'");
                                PushBranchToRemote(repo, branch, remoteName, username, token);
                            }
                            else
                            {
                               // Console.WriteLine("  Not tracking any remote branch.");
                                Logout_red("  -Not tracking any remote branch");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred with repository at '{repoPath}': {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }

            Console.WriteLine();
        }

        static void PushBranchToRemote(Repository repo, Branch localBranch, string remoteName, string username, string token)
        {
            // Compare local and remote branch commits
            var divergence = repo.ObjectDatabase.CalculateHistoryDivergence(localBranch.Tip, localBranch.TrackedBranch.Tip);
            var aheadBy = divergence.AheadBy ?? 0;

            if (aheadBy > 0)
            {
                // Push local changes to the remote repository
                Console.WriteLine($"  Local branch '{localBranch.FriendlyName}' is ahead by {aheadBy} commit(s). Pushing changes...");
                var options = new PushOptions
                {
                    CredentialsProvider = (url, user, cred) => new UsernamePasswordCredentials
                    {
                        Username = username,
                        Password = token
                    }
                };
                repo.Network.Push(localBranch, options);
                Console.WriteLine($"  Push completed for branch '{localBranch.FriendlyName}'.");
            }
            else
            {
                //Console.WriteLine($"  Local branch '{localBranch.FriendlyName}' is up-to-date with the remote branch.");
                Logout_green($"  Local branch '{localBranch.FriendlyName}' is up-to-date with the remote branch.");
            }
        }
        static void Logout_green(string text){

            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(" -" + text);

            // Reset the console text color to the original color
            Console.ForegroundColor = originalColor;

        }
        static void Logout_red(string text)
        {

            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" -" + text);

            // Reset the console text color to the original color
            Console.ForegroundColor = originalColor;

        }
    }
}
