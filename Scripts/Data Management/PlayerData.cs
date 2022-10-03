[System.Serializable]
public class PlayerData
{
    public int lives;
    public int kills;
    public int shots;
    public int deaths;
    public float time;
    public float bestTime;
    public int sceneIndex;
    public int previousSceneIndex;

    public PlayerData Copy(PlayerData into)
    {
        into.lives = lives;
        into.kills = kills;
        into.shots = shots;
        into.deaths = deaths;
        into.time = time;
        into.bestTime = bestTime;
        into.sceneIndex = sceneIndex;
        into.previousSceneIndex = previousSceneIndex;

        return into;
    }
}
