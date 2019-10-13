using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace durable_chatbot.Models
{
    public class UpdateMessage
    {
        public Update Update { get; set; }
        public ChatMessage Chat { get; set; }
    }
}
