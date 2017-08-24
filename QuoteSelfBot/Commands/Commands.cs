using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace QuoteSelfBot.Commands
{
    public class Commands : ModuleBase
    {
        private int quoteRetries;

        [Command("quote")]
        public async Task Quote(ulong messageId, int cache = 100)
        {
            this.quoteRetries++;

            try
            {
                List<IReadOnlyCollection<IMessage>> messageCache = await this.Context.Channel.GetMessagesAsync(cache).ToList();

                IMessage message = null;

                foreach (IReadOnlyCollection<IMessage> readOnlyCollection in messageCache)
                {
                    if (readOnlyCollection.FirstOrDefault(e => e.Id == messageId) != null)
                    {
                        message = readOnlyCollection.FirstOrDefault(e => e.Id == messageId);
                    }
                }

                if (message == null)
                {
                    if (this.quoteRetries == 10)
                    {
                        Console.WriteLine("Unable to get message");
                    }

                    await this.Quote(messageId, cache + 100);
                    return;
                }

                string authorName;

                if (message.Author is IGuildUser user)
                {
                    authorName = string.IsNullOrWhiteSpace(user.Nickname) ? user.Nickname : user.Username;
                }
                else
                {
                    authorName = message.Author.Username;
                }

                EmbedBuilder builder = new EmbedBuilder
                {
                    Description = message.Content,
                    Footer = new EmbedFooterBuilder
                    {
                        Text = message.Timestamp.ToLocalTime().ToString()
                    }
                };

                Console.WriteLine(authorName);

                builder.WithAuthor(new EmbedAuthorBuilder
                {
                    IconUrl = message.Author.GetAvatarUrl(),
                    Name = authorName
                });

                await this.Context.Message.DeleteAsync();
                await this.ReplyAsync(string.Empty, false, builder.Build());

                this.quoteRetries = 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
