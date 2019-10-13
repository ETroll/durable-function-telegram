using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System.IO;
using Telegram.Bot.Types;
using Newtonsoft.Json;
using System;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using System.Linq;
using System.Threading;
using durable_chatbot.Models;
using durable_chatbot.Extensions;

namespace durable_chatbot
{
    public static class DurableChatBot
    {
        //staic botClient to be shared for all intances of the functons.
        public static readonly TelegramBotClient botClient = new TelegramBotClient(Environment.GetEnvironmentVariable("TELEGRAM_TOKEN_REBECCATHEBOT"));

        [FunctionName("BotBrainFunction")]
        public static async Task RunOrchestrator(
            [Table("RunningInstances")] CloudTable table,
            [OrchestrationTrigger] DurableOrchestrationContext context, ILogger log)
        {
            log.LogInformation($"************** BotBrain starting up ********************");

            Update initialUpdate = context.GetInput<Update>();

            //TODO: Decide if a menu is to be shown based on initial input
            Message currentMessage;
            if (initialUpdate.Message != null)
            {
                currentMessage = await context.CallActivityAsync<Message>("Telegram_InitialMenu", new ChatMessage
                {
                    ChatId = initialUpdate.GetChatId(),
                    InstanceId = context.InstanceId
                });
            }
            else
            {
                currentMessage = initialUpdate.CallbackQuery?.Message; //TODO: Check if there can be other responces that needs to be accounted for
            }

            bool didTimeOut = false;

            while (!didTimeOut)
            {
                using (CancellationTokenSource timeout = new CancellationTokenSource())
                {
                    Task timeoutTask = context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(10), timeout.Token);

                    Task<Update> callbackEvent = context.WaitForExternalEvent<Update>("Callback");

                    if (callbackEvent == await Task.WhenAny(callbackEvent, timeoutTask))
                    {
                        timeout.Cancel();

                        Update updateMessage = callbackEvent.Result;

                        try
                        {
                            string nextKey = updateMessage.CallbackQuery.Data.Split(';')[1];
                            if (nextKey.StartsWith("A-"))
                            {

                                currentMessage = await context.CallActivityAsync<Message>($"Telegram_{nextKey.Split("-")[1]}", new UpdateMessage
                                {
                                    Chat = new ChatMessage
                                    {
                                        ChatId = initialUpdate.GetChatId(),
                                        MessageId = currentMessage?.MessageId,
                                        InstanceId = context.InstanceId
                                    },
                                    Update = updateMessage
                                });
                            }
                            else
                            {
                                currentMessage = await context.CallActivityAsync<Message>("Telegram_ShowMenu", new UpdateMessage
                                {
                                    Chat = new ChatMessage
                                    {
                                        ChatId = initialUpdate.GetChatId(),
                                        MessageId = currentMessage?.MessageId,
                                        InstanceId = context.InstanceId
                                    },
                                    Update = updateMessage
                                });
                            }
                            log.LogInformation("Got callback message");
                        }
                        catch(Exception ex)
                        {
                            log.LogWarning(ex, "Error handling message");
                            //Swallow for now for test
                        }
                    }
                    else
                    {
                        didTimeOut = true;
                        log.LogWarning($" - Session timed out - User did not reply within given time - Removing session with ID = '{context.InstanceId}'");
                    }
                }
            }

            await context.CallActivityAsync<Message>("Telegram_SessionCompleted", new ChatMessage
            {
                ChatId = currentMessage?.Chat?.Id.ToString(),
                MessageId = currentMessage?.MessageId,
                InstanceId = context.InstanceId
            });

            log.LogInformation($"************** BotBrain finished ********************");
        }

        [FunctionName("router")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequestMessage req,
            [Table("RunningInstances")] CloudTable tableInput,
            [Table("RunningInstances")] IAsyncCollector<InstanceTableEntity> tableOutput,
            [OrchestrationClient]DurableOrchestrationClient starter,
            ILogger log)
        {

            string requestBody = await req.Content.ReadAsStringAsync();

            try
            {
                Update update = JsonConvert.DeserializeObject<Update>(requestBody);

                string chatId = update.GetChatId();

                TableQuery<InstanceTableEntity> sessionQuery = new TableQuery<InstanceTableEntity>().Where(
                        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, chatId));

                TableQuerySegment<InstanceTableEntity> sessions = await tableInput.ExecuteQuerySegmentedAsync(sessionQuery, null);

                if ((update.Message?.Entities?.Length ?? 0) != 0 && 
                    (update.Message?.Entities?.First().Type ?? MessageEntityType.Unknown) == MessageEntityType.BotCommand &&
                    (update.Message?.EntityValues?.First()?.ToLowerInvariant().Equals("/cancel") ?? false))
                {
                    //Cancel current user session and reset all state!
                    foreach (var entity in sessions)
                    {
                        await tableInput.ExecuteAsync(TableOperation.Delete(entity));
                    }

                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"All running ({sessions.Count()}) sessions have now been purged");
                }
                else
                {
                    bool didContinueASession = false;
                    if (sessions.Count() > 0)
                    {
                        foreach (InstanceTableEntity session in sessions)
                        {
                            OrchestrationRuntimeStatus status = (await starter.GetStatusAsync(session.RowKey)).RuntimeStatus;
                            if (status == OrchestrationRuntimeStatus.Failed || 
                                status == OrchestrationRuntimeStatus.Canceled || 
                                status == OrchestrationRuntimeStatus.Completed || 
                                status == OrchestrationRuntimeStatus.Terminated ||  
                                status == OrchestrationRuntimeStatus.Unknown)
                            {
                                log.LogInformation($"Removing session with ID = '{session.RowKey}' because it was terminated");
                                await tableInput.ExecuteAsync(TableOperation.Delete(session));

                                await botClient.SendTextMessageAsync(
                                    chatId: chatId,
                                    text: $"FYI: I purged a old session with ID {session.RowKey} that had status: {status.ToString()} (Was started at UTC: {session.Timestamp.UtcDateTime.ToShortDateString()})");
                            }
                            else
                            {
                                if(update.CallbackQuery != null)
                                {
                                    //One should not have more than 1 running session, but I will not crash because of it. So just continue all the session until they get cleaned up.
                                    await starter.RaiseEventAsync(session.RowKey, "Callback", update);
                                    log.LogInformation($"Continuing on session with ID = '{session.RowKey}'");
                                    didContinueASession = true;
                                }
                            }
                        }
                    }

                    if(!didContinueASession)
                    {
                        var newInstance = new InstanceTableEntity
                        {
                            PartitionKey = chatId,
                            RowKey = await starter.StartNewAsync("BotBrainFunction", update)
                        };

                        await tableOutput.AddAsync(newInstance);

                        log.LogInformation($"Started orchestration with ID = '{newInstance.RowKey}'.");
                    }
                }

            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Could not parse update data. {ex.Message}");
                log.LogInformation(requestBody);
            }


            // Function input comes from the request content.
            //string instanceId = await starter.StartNewAsync("Function2", null);

            //log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            //return starter.CreateCheckStatusResponse(req, instanceId);


            HttpResponseMessage response = req.CreateResponse(HttpStatusCode.OK);
            //await Task.Delay(1);
            return response;
        }
    }
}