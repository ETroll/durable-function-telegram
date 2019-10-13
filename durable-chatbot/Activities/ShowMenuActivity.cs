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
    public static class ShowMenuActivity
    {
        [FunctionName("Telegram_ShowMenu")]
        public static async Task<Message> InitialMenu([ActivityTrigger] UpdateMessage msg, ILogger log)
        {
            List<InlineKeyboardButton> menuButtons = new List<InlineKeyboardButton>();

            string nextKey = msg.Update.CallbackQuery.Data.Split(';')[1];
            if(MenuMap.Commands.ContainsKey(nextKey))
            {
                foreach (var menuitem in MenuMap.Commands[nextKey])
                {
                    if(string.IsNullOrWhiteSpace(menuitem.NavigateTo))
                    {
                        menuButtons.Add(InlineKeyboardButton.WithCallbackData(menuitem.Text, $"{msg.Chat.InstanceId};A-{menuitem.ActivityId}"));
                    }
                    else
                    {
                        menuButtons.Add(InlineKeyboardButton.WithCallbackData(menuitem.Text, $"{msg.Chat.InstanceId};{menuitem.NavigateTo}"));
                    }
                }
            }

            return await DurableChatBot.botClient.EditMessageReplyMarkupAsync(
                chatId: msg.Chat.ChatId,
                messageId:msg.Chat.MessageId.Value,
                replyMarkup: new InlineKeyboardMarkup(menuButtons)
            );
        }
    }
}
