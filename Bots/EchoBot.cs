using Azure;
using Azure.AI.OpenAI;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace EchoStreaming.Bots
{
    public class EchoBot : ActivityHandler
    {
        private readonly IConfiguration Configuration;

        public EchoBot(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var replyText = $"Echo: {turnContext.Activity.Text}";
            await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);

            var openAIUri = Configuration["OpenAI:ApiUrl"];
            var openAIKey = Configuration["OpenAI:ApiKey"];

            var client = new OpenAIClient(new Uri(openAIUri), new AzureKeyCredential(openAIKey));

            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = "gpt-35-turbo", 
                Messages =
                {
                    new ChatRequestSystemMessage("You are a helpful assistant. You will talk like a pirate."),
                    new ChatRequestUserMessage(turnContext.Activity.Text),
                }
            };

            var activityId = string.Empty;

            await foreach (StreamingChatCompletionsUpdate chatUpdate in client.GetChatCompletionsStreaming(chatCompletionsOptions))
            {
                if (chatUpdate.Role.HasValue)
                {
                    Console.Write($"{chatUpdate.Role.Value.ToString().ToUpperInvariant()}: ");
                }
                if (!string.IsNullOrEmpty(chatUpdate.ContentUpdate))
                {
                    if (string.IsNullOrEmpty(activityId))
                    {
                        var activity = await turnContext.SendActivityAsync(MessageFactory.Text(chatUpdate.ContentUpdate, chatUpdate.ContentUpdate), cancellationToken);
                        activityId = activity.Id;
                    }
                    else 
                    {
                        var activity = MessageFactory.Text(chatUpdate.ContentUpdate, chatUpdate.ContentUpdate);
                        activity.Id = activityId;
                        await turnContext.UpdateActivityAsync(activity, cancellationToken);
                    }
                    Console.Write(chatUpdate.ContentUpdate);
                }
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello and welcome!";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }
    }
}