using Godot;
using System;

public class DebugLabel : Label
{
    Player player = null;

    public override void _Process(float delta)
    {
        if (player != null) {
            Text = player.Speed.ToString();
        }
    }

    private void OnPlayerReady(Player p) {
        this.player = p;
    }
}
