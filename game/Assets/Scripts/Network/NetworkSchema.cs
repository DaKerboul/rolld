using Colyseus.Schema;

// Must match server-side defineTypes field order exactly
public partial class NetworkPlayer : Schema
{
    [Type(0,  "float32")] public float  x = 0;
    [Type(1,  "float32")] public float  y = 5;
    [Type(2,  "float32")] public float  z = 0;
    [Type(3,  "float32")] public float  vx = 0;
    [Type(4,  "float32")] public float  vy = 0;
    [Type(5,  "float32")] public float  vz = 0;
    [Type(6,  "float32")] public float  rx = 0;
    [Type(7,  "float32")] public float  ry = 0;
    [Type(8,  "float32")] public float  rz = 0;
    [Type(9,  "float32")] public float  rw = 1;
    [Type(10, "float64")] public double t = 0;
    [Type(11, "string")]  public string name = "";
    [Type(12, "float32")] public float  colorR = 1;
    [Type(13, "float32")] public float  colorG = 1;
    [Type(14, "float32")] public float  colorB = 1;
    [Type(15, "float32")] public float  avx = 0;
    [Type(16, "float32")] public float  avy = 0;
    [Type(17, "float32")] public float  avz = 0;
    // Game state — order must match server defineTypes exactly
    [Type(18, "boolean")] public bool   isEliminated = false;
    [Type(19, "boolean")] public bool   isQualified = false;
    [Type(20, "boolean")] public bool   isReady = false;
    [Type(21, "int8")]    public int    checkpointIndex = 0;
}

public partial class NetworkState : Schema
{
    [Type(0, "map", typeof(MapSchema<NetworkPlayer>))]
    public MapSchema<NetworkPlayer> players;

    [Type(1, "string")]  public string phase = "lobby";
    [Type(2, "float32")] public float  countdown = 0;
    [Type(3, "int8")]    public int    roundNumber = 1;
    [Type(4, "int8")]    public int    totalRounds = 4;
    [Type(5, "int8")]    public int    playersAlive = 0;
    [Type(6, "string")]  public string gameMode = "race";
    [Type(7, "string")]  public string winnerName = "";
}
