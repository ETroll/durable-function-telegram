using durable_chatbot.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace durable_chatbot
{
    public static class MenuMap
    {
        public static Dictionary<string, List<MenuItem>> Commands = new Dictionary<string, List<MenuItem>>() {
            {"main", new List<MenuItem> {
                new MenuItem
                {
                    NavigateTo = "test1",
                    Text = "Test 1"
                },
                new MenuItem
                {
                    NavigateTo = "test2",
                    Text = "Test 2"
                },
                new MenuItem
                {
                    NavigateTo = "test3",
                    Text = "Test 3"
                }}
            },
            {"test1", new List<MenuItem> {
                new MenuItem
                {
                    NavigateTo = "test1-1",
                    Text = "Test 1-1"
                },
                new MenuItem
                {
                    NavigateTo = "test1-2",
                    Text = "Test 1-2"
                }}
            },
            {"test1-2", new List<MenuItem> {
                new MenuItem
                {
                    ActivityId = "Hello",
                    Text = "Hello"
                }}
            }
        };
    }
}
