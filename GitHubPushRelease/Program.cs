using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Octokit;

namespace GitHubPushRelease
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Usage();
                return;
            }

            Assembly assembly = Assembly.GetExecutingAssembly();

            KeysManager keysManager = new(Path.Combine(Path.GetDirectoryName(assembly.Location), "keymanager.xml"));
            string GitUser = null;
            string GitToken = null;
            string GitRepository = null;
            string LocalFolder = null;
            try
            {
                foreach (string order in args)
                {
                    Console.WriteLine(order);

                    var myorder = order.Split("=");
                    string command = myorder[0].ToLower();
                    string value = myorder[1];

                    switch (command)
                    {
                        case "--gituser":
                        case "-u":
                            if (string.IsNullOrWhiteSpace(value))
                            {
                                throw new AppException("GitUser cannot be empty");
                            }
                            GitUser = value;
                            break;

                        case "--settoken":
                        case "-t":
                            if (string.IsNullOrWhiteSpace(value))
                            {
                                throw new AppException("Token cannot be empty");
                            }
                            if (string.IsNullOrWhiteSpace(GitUser))
                            {
                                throw new AppException("GitUser must be specified");
                            }
                            GitToken = value;
                            break;
                        case "--gitrepository":
                        case "-r":
                            if (string.IsNullOrWhiteSpace(value))
                            {
                                throw new AppException("Repository cannot be empty");
                            }
                            GitRepository = value;
                            break;
                        case "--localfolder":
                        case "-f":
                            if (string.IsNullOrWhiteSpace(value))
                            {
                                throw new AppException("Local folder cannot be empty");
                            }
                            LocalFolder = value;
                            break;
                    }
                }

                if (!string.IsNullOrWhiteSpace(GitUser))
                {
                    if (!string.IsNullOrWhiteSpace(GitToken))
                    {
                        UpdateKeys(keysManager, GitUser, GitToken);
                    }
                    if (!string.IsNullOrWhiteSpace(LocalFolder)
                        && !string.IsNullOrWhiteSpace(GitRepository))
                    {
                        if (string.IsNullOrWhiteSpace(GitToken))
                        {
                            GitToken = keysManager.GetToken(GitUser)?.GitToken;
                            if (string.IsNullOrWhiteSpace(GitToken))
                            {
                                Console.WriteLine("GitToken was not stored nor specified");
                                Console.Write("Input your gittoken here:");
                                GitToken = Console.ReadLine();
                                if (string.IsNullOrWhiteSpace(GitToken))
                                {
                                    UpdateKeys(keysManager, GitUser, GitToken);
                                }
                            }
                        }

                        if (!Directory.Exists(LocalFolder))
                        {
                            throw new AppException("Local Folder not found");
                        }

                        string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                        GitHubClient github = new(new ProductHeaderValue("GitHubPushRelease", version))
                        {
                            Credentials = new Credentials(GitToken)
                        };
                        Release latest = await github.Repository.Release.GetLatest(GitUser, GitRepository);

                        if (latest is null)
                        {
                            throw new AppException("No releases published");
                        }
                        foreach (string file in Directory.GetFiles(LocalFolder))
                        {
                            string filename = Path.GetFileName(file);
                            ReleaseAsset exists = latest.Assets.Where(x => string.Equals(x.Name, filename, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                            if (exists != null)
                            {
                                Console.WriteLine($"  Deleting remote file {filename}");
                                await github.Repository.Release.DeleteAsset(GitUser, GitRepository, exists.Id);
                            }

                            using (FileStream rawData = File.OpenRead(file))
                            {
                                ReleaseAssetUpload data = new()
                                {
                                    FileName = filename,
                                    ContentType = "application/octet-stream",
                                    RawData = rawData
                                };
                                Console.WriteLine($"  Uploading local file {filename}");
                                await github.Repository.Release.UploadAsset(latest, data);
                            }
                        }
                        Console.WriteLine("done.");
                    }
                }
            }
            catch (AppException ex)
            {
                Usage($"Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Usage($"Error in parameters. {ex.Message}");
            }

            return;
        }

        private static void UpdateKeys(KeysManager keysManager, string GitUser, string GitToken)
        {
            Keys newkey = new()
            {
                GitUser = GitUser,
                GitToken = GitToken
            };
            keysManager.AddorUpdateKey(newkey);
            keysManager.SaveKeys();
        }

        private static void Usage(string error = null)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.WriteLine(error);
                Console.WriteLine("");
            }
            Console.WriteLine("Usage: ");
            Console.WriteLine("     GitHubPushRelease -u=cool -t=token123");
            Console.WriteLine("     GitHubPushRelease -u=cool -r=mynewrepo -f='./Releases'");
            Console.WriteLine("");
            Console.WriteLine("Options:");
            Console.WriteLine("     -u, --GitUser=VALUE             Set gituser");
            Console.WriteLine("     -t, --SetToken=VALUE            Save gittoken into settings for future usage");
            Console.WriteLine("     -r, --GitRepository=VALUE       Set Repository");
            Console.WriteLine("     -f, --localfolder=VALUE         Set local folder");
        }
    }
}
