using Microsoft.AspNetCore.SignalR;

namespace CodeRoyale.Hubs;

/// <summary>
/// Real-time channel for a game lobby. Clients are placed into a SignalR group named
/// after the game's join code, so a broadcast reaches exactly the players in that game.
/// </summary>
/// <remarks>
/// This hub is transport only. The authoritative phase, clock and scores live in
/// <c>GameService</c>, which broadcasts through <c>IHubContext&lt;GameHub&gt;</c>.
///
/// The Blazor Server UI does not consume this hub: its components subscribe to
/// <c>GameService.GameChanged</c> directly and re-render, which reaches the browser
/// over the Blazor circuit's own WebSocket. This hub exists so that a browser client,
/// a WebAssembly build, or a mobile app can follow the same game without Blazor --
/// and it is the seam the project's SignalR work is built on.
/// </remarks>
public class GameHub : Hub
{
    /// <summary>
    /// SignalR group carrying every message for a single game.
    /// </summary>
    public static string GroupName(string joinCode) => $"game:{joinCode.ToUpperInvariant()}";

    /// <summary>
    /// Subscribes the caller to a game's broadcasts. Joining the SignalR group is
    /// separate from taking a seat in the game, which goes through GameService.
    /// </summary>
    public async Task Subscribe(string joinCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(joinCode));
    }

    /// <summary>Unsubscribes the caller from a game's broadcasts.</summary>
    public async Task Unsubscribe(string joinCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(joinCode));
    }
}
