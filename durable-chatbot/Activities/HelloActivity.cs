using durable_chatbot.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace durable_chatbot.Activities
{
    public static class HelloActivity
    {
        [FunctionName("Telegram_Hello")]
        public static async Task<Message> InitialMenu([ActivityTrigger] UpdateMessage chat, ILogger log)
        {
            return await DurableChatBot.botClient.SendTextMessageAsync(
                chatId: chat.Chat.ChatId,
                text: "Hello!"
            );
        }
    }
}
