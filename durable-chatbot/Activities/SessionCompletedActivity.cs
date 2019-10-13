using durable_chatbot.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace durable_chatbot.Activities
{
    public static class SessionCompletedActivity
    {
        [FunctionName("Telegram_SessionCompleted")]
        public static async Task InitialMenu(
            [ActivityTrigger] ChatMessage chat, 
            [Table("RunningInstances")] CloudTable table, 
            ILogger log)
        {

            InstanceTableEntity session = new InstanceTableEntity
            {
                PartitionKey = chat.ChatId,
                RowKey = chat.InstanceId,
                ETag = "*"
            };

            if (chat.MessageId.HasValue)
            {
                await DurableChatBot.botClient.DeleteMessageAsync(chat.ChatId, chat.MessageId.Value);
            }
            await table.ExecuteAsync(TableOperation.Delete(session));

            await DurableChatBot.botClient.SendTextMessageAsync(
                chatId: chat.ChatId,
                text: "Your session timed out. You can create a new session for a new menu");
        }
    }
}
