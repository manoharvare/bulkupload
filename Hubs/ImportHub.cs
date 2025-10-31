using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace OilAndGasImport.Hubs
{
    public class ImportHub : Hub
    {
        /// <summary>
        /// Called by clients before or during file upload to join a shared progress group
        /// identified by a unique fileKey (file name + size or hash).
        /// </summary>
        public async Task JoinFileGroup(string fileKey)
        {
            if (string.IsNullOrWhiteSpace(fileKey))
                return;

            await Groups.AddToGroupAsync(Context.ConnectionId, fileKey);

            // Notify only the joining client
            await Clients.Caller.SendAsync("JoinedFileGroup", new
            {
                fileKey,
                message = $"✅ Joined file group: {fileKey}"
            });
        }

        /// <summary>
        /// Optional cleanup or notification when a client disconnects.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Optionally, notify others or log disconnections
            await base.OnDisconnectedAsync(exception);
        }
    }
}
