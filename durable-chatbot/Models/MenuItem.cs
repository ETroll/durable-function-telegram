using System;
using System.Collections.Generic;
using System.Text;

namespace durable_chatbot.Models
{
    public class MenuItem
    {
        public string Text { get; set; }
        public string NavigateTo { get; set; }
        public string ActivityId { get; set; }
    }
}
