using durable_chatbot.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace durable_chatbot.Activities
{
    public static class InitialMenuActivity
    {
        [FunctionName("Telegram_InitialMenu")]
        public static async Task<Message> InitialMenu([ActivityTrigger] ChatMessage chat, ILogger log)
        {
            List<InlineKeyboardButton> menuButtons = new List<InlineKeyboardButton>();

            foreach(var menuitem in MenuMap.Commands["main"])
            {
                menuButtons.Add(InlineKeyboardButton.WithCallbackData(menuitem.Text, $"{chat.InstanceId};{menuitem.NavigateTo}"));
            }

            return await DurableChatBot.botClient.SendTextMessageAsync(
                chatId: chat.ChatId,
                text: "Hi there! What can I do for you?",
                replyMarkup: new InlineKeyboardMarkup(menuButtons)
            );
        }
    }
}
