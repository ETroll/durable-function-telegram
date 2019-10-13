using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace durable_chatbot.Extensions
{
    public static class UpdateExtensions
    {
        public static string GetChatId(this Update update)
        {
            return ((update.Message?.Chat?.Id ?? update.CallbackQuery?.Message?.Chat?.Id) ?? -1).ToString();
        }
    }
}
