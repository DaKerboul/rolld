// 
// THIS FILE HAS BEEN GENERATED AUTOMATICALLY
// DO NOT CHANGE IT MANUALLY UNLESS YOU KNOW WHAT YOU'RE DOING
// 
// GENERATED USING @colyseus/schema 4.0.15
// 

using Colyseus.Schema;
#if UNITY_5_3_OR_NEWER
using UnityEngine.Scripting;
#endif

namespace RolldSchema {
	public partial class GameState : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public GameState() { }
		[Type(0, "map", typeof(MapSchema<Player>))]
		public MapSchema<Player> players = null;

		[Type(1, "string")]
		public string phase = default(string);

		[Type(2, "float32")]
		public float countdown = default(float);

		[Type(3, "int8")]
		public sbyte roundNumber = default(sbyte);

		[Type(4, "int8")]
		public sbyte totalRounds = default(sbyte);

		[Type(5, "int8")]
		public sbyte playersAlive = default(sbyte);

		[Type(6, "string")]
		public string gameMode = default(string);

		[Type(7, "string")]
		public string winnerName = default(string);
	}
}
