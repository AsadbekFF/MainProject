using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MainProject.Controllers;
using Microsoft.AspNetCore.SignalR;

namespace MainProject.Hub
{
    public class ChatHub : Microsoft.AspNetCore.SignalR.Hub
    {
        public async Task Send(string userId, string collectionId,
            string itemId, string message, string senderId, string senderName)
        {
            AccountController.dbContext.Users.First(x => x.Id == userId)
                .Collections.First(x => x.Id == collectionId)
                .Items.First(x => x.Id == itemId)
                .Chat.Add(new Models.Message
                {
                    Sender = senderId,
                    Text = message
                });

            string when = DateTime.Now.ToString("dd MMMM yyyy HH:mm");
            await Clients.All.SendAsync("Send", message, senderName, when);
        }
    }
}
