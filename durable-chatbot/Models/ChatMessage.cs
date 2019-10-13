using System;
using System.Collections.Generic;
using System.Text;
using Telegram.Bot.Types;

namespace durable_chatbot.Models
{
    public class ChatMessage
    {
        public int? MessageId { get; set; }
        public string ChatId { get; set; }
        public string InstanceId { get; set; }
    }
}
