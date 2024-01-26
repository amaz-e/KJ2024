using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace MemeBE.hubs
{
    public class TestHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            var trololol = 0;
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
        
}