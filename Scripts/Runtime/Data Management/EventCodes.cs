/*
 * All codes for photon raise events
 */
public static class EventCodes
{
    // GameManager
    public static readonly byte StartGame = 0;
    public static readonly byte AddReadyPlayer = 1;
    public static readonly byte RemoveReadyPlayer = 2;
    public static readonly byte LoadScene = 3;

    // General
    public static readonly byte Destroy = 4;
    public static readonly byte ResetData = 5;
    public static readonly byte UpdateTeams = 6;

    // Waiting room
    public static readonly byte UpdateUI = 7;
    public static readonly byte LeaveWaitingRoom = 8;
    public static readonly byte LevelObjectUpload = 9;
    public static readonly byte ReadyToLeave = 10;

    // Boosts
    public static readonly byte SpawnNewBoost = 11;
}
