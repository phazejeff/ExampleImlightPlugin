using System;
using Imlight.PluginApi;
using Imlight.PluginApi.Events;

namespace ExamplePlugin;

/// <summary>
/// Greets players when they enter a zone, and drops zone chat containing a blocked word.
/// </summary>
public sealed class ExamplePlugin : ImlightPlugin {

    public override string Name => "ExamplePlugin";

    public override string Version => "1.0.0";

    public override void OnEnable() {
        Events.Subscribe<PlayerZoneEnterEvent>(e => e.Player.SendMessage($"Welcome to {e.Zone}, {e.Player.Name}!"));

        Events.Subscribe<PlayerChatEvent>(e => {
            if (e.Message.Contains("badword", StringComparison.OrdinalIgnoreCase)) {
                e.IsCancelled = true;
                e.Player.SendMessage("Your message was not sent because it contains a blocked word.");
            }

            if (e.Message.ToLower().Equals("ping")) {
                e.IsCancelled = true;
                e.Player.SendMessage("Pong!");
            }
        });
    }

}
