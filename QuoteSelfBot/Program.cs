using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace QuoteSelfBot
{
    class Program
    {
        public static readonly string AppPath = Directory.GetParent(new Uri(Assembly.GetEntryAssembly().CodeBase).LocalPath).FullName;
        public static DiscordSocketClient Client;
        public CommandService Commands;
        private IServiceProvider serviceProvider;
        private string commandHeader;

        private async Task MainAsync()
        {
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                MessageCacheSize = 1000,
                AlwaysDownloadUsers = true,
                LargeThreshold = 200000
            });

            this.Commands = new CommandService(new CommandServiceConfig
            {
                DefaultRunMode = RunMode.Async,
                CaseSensitiveCommands = false
            });

            this.serviceProvider = new ServiceCollection().BuildServiceProvider();

            Client.Log += Log;
            this.Commands.Log += Log;

            await this.InstallCommands();

            XmlDocument doc = new XmlDocument();
            doc.Load(Path.Combine(AppPath, "config.xml"));
            XmlNodeList xmlNodeList = doc.SelectNodes("/Settings/Token");
            if (xmlNodeList != null)
            {
                string token = xmlNodeList[0].InnerText;

                try
                {
                    await Client.LoginAsync(TokenType.User, token);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Token invalid!\n\nException: " + exception.ToString());
                    throw;
                }
            }
            else
            {
                Console.WriteLine("Invalid config file!");
                Environment.Exit(404);
            }

            xmlNodeList = doc.SelectNodes("/Settings/Seperator");
            if (xmlNodeList != null)
            {
                this.commandHeader = xmlNodeList[0].InnerText;
            }
            else
            {
                Console.WriteLine("Invalid config file!");
                Environment.Exit(404);
            }

            await Client.StartAsync();

            await Task.Delay(-1);
        }

        public async Task InstallCommands()
        {
            // Hook the MessageReceived Event into our Command Handler
            Client.MessageReceived += this.HandleCommand;

            // Discover all of the commands in this assembly and load them.
            await this.Commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public async Task HandleCommand(SocketMessage msg)
        {
            SocketUserMessage message = msg as SocketUserMessage;

            if (message == null)
            {
                return;
            }

            if (message.Content.StartsWith(this.commandHeader) && message.Author.Id == Client.CurrentUser.Id)
            {
                int argPos = -1;
                
                if (message.HasStringPrefix(this.commandHeader, ref argPos))
                {
                    // Create a Command Context
                    CommandContext context = new CommandContext(Client, message);

                    // Execute the command. (result does not indicate a return value,
                    // rather an object stating if the command executed succesfully)
                    IResult result = await this.Commands.ExecuteAsync(context, argPos);
                    if (!result.IsSuccess)
                    {
                        Console.Out.WriteLine(result.ErrorReason + $"\nCommand:\n{message.Content.Substring(this.commandHeader.Length)}");
                    }
                }
            }
        }

        private Task Log(LogMessage arg)
        {
            Console.WriteLine(arg.Message);
            return Task.CompletedTask;
        }

        private static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();
    }
}
