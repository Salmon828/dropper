using UnityEngine;

// The colours picked in Options, saved between sessions.
// Player 1 defaults to white, player 2 (Local Play only) defaults to red.
public static class PlayerColor
{
    // Keys are "p1R"/"p1G"/"p1B" and "p2R"/..., so P1's existing saved colour carries over
    private static string Key(int player, string channel) => "p" + player + channel;

    public static Color32 Load(int player = 1)
    {
        int defaultG = player == 1 ? 255 : 0;
        int defaultB = player == 1 ? 255 : 0;
        return new Color32(
            (byte)PlayerPrefs.GetInt(Key(player, "R"), 255),
            (byte)PlayerPrefs.GetInt(Key(player, "G"), defaultG),
            (byte)PlayerPrefs.GetInt(Key(player, "B"), defaultB),
            255);
    }

    public static void Save(int player, int r, int g, int b)
    {
        PlayerPrefs.SetInt(Key(player, "R"), r);
        PlayerPrefs.SetInt(Key(player, "G"), g);
        PlayerPrefs.SetInt(Key(player, "B"), b);
        PlayerPrefs.Save(); // Write to disk now, not just on clean exit
    }
}
