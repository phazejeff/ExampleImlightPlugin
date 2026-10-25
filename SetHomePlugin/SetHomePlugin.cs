using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using Imlight.PluginApi;
using Imlight.PluginApi.Commands;
using Imlight.PluginApi.Events;

namespace SetHomePlugin;

/// <summary>
/// Lets players save a spot with .sethome, and sends them there when they press the Go Home button.
/// </summary>
/// <remarks>
/// Players without a saved home get the normal Go Home behaviour. Homes are kept in homes.json in the plugin's
/// data folder, so they survive restarts.
/// </remarks>
public sealed class SetHomePlugin : ImlightPlugin {

    // Vector3 stores its coordinates as fields, which System.Text.Json skips, so a home is saved as plain numbers.
    private sealed record Home(string Zone, float X, float Y, float Z, float Orientation) {
        public Vector3 Location => new(X, Y, Z);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // Keyed by character id. Event handlers run on each player's session, alongside the command dispatcher, so every
    // access is locked.
    private Dictionary<ulong, Home> homes = [];
    private readonly object homesLock = new();

    private string HomesPath => Path.Combine(DataFolder, "homes.json");

    public override string Name => "SetHomePlugin";

    public override string Version => "1.0.0";

    public override void OnEnable() {
        if (File.Exists(HomesPath)) {
            homes = JsonSerializer.Deserialize<Dictionary<ulong, Home>>(File.ReadAllText(HomesPath)) ?? [];
        }

        Commands.Register("sethome", c => SetHome(c.Player));
        Commands.Register("home", c => {
            if (!GoHome(c.Player)) {
                c.Reply("You haven't set a home yet. Use .sethome to set one.");
            }
        });
        Commands.Register("delhome", c => DeleteHome(c.Player));

        // Go Home raises a zone change when the world's hub is another zone,
        // and a teleport when the player is already in the hub zone.
        Events.Subscribe<OnPlayerZoneChange>(e => {
            if (e.Cause == ZoneChangeCause.Dorm && GoHome(e.Player)) {
                e.IsCancelled = true;
            }
        });
        Events.Subscribe<OnPlayerTeleport>(e => {
            if (e.Cause == ZoneChangeCause.Dorm && GoHome(e.Player)) {
                e.IsCancelled = true;
            }
        });
    }

    private void SetHome(IPlayer player) {
        IZone? zone = player.Zone;
        // Instances (dungeons, houses) are created per group and can't be travelled back to by name.
        if (zone is null || zone.IsInstance) {
            player.SendMessage("You can't set your home here.");
            return;
        }

        lock (homesLock) {
            Vector3 location = player.Location;
            homes[player.CharacterId] = new Home(zone.Name, location.X, location.Y, location.Z, player.Orientation);
            Save();
        }
        player.SendMessage($"Home set in {zone.Name}. Press Go Home or use .home to return here.");
    }

    /// <summary>
    /// Sends the player to their saved home. Returns false if they have none, or it can't be reached.
    /// </summary>
    private bool GoHome(IPlayer player) {
        Home? home;
        lock (homesLock) {
            homes.TryGetValue(player.CharacterId, out home);
        }
        if (home is null) {
            return false;
        }

        try {
            player.TransferZone(home.Zone, home.Location, home.Orientation);
            return true;
        } catch (ArgumentException) {
            // The zone was removed or renamed since the home was saved, so fall back to the normal Go Home.
            lock (homesLock) {
                homes.Remove(player.CharacterId);
                Save();
            }
            player.SendMessage($"Your home in {home.Zone} can't be reached anymore, so it was removed.");
            return false;
        }
    }

    private void DeleteHome(IPlayer player) {
        lock (homesLock) {
            if (!homes.Remove(player.CharacterId)) {
                player.SendMessage("You don't have a home set.");
                return;
            }
            Save();
        }
        player.SendMessage("Home removed. Go Home will take you to the world hub again.");
    }

    // Callers hold homesLock.
    private void Save() => File.WriteAllText(HomesPath, JsonSerializer.Serialize(homes, JsonOptions));

}
