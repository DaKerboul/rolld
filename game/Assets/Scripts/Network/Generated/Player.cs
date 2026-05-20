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
	public partial class Player : Schema {
#if UNITY_5_3_OR_NEWER
[Preserve]
#endif
public Player() { }
		[Type(0, "float32")]
		public float x = default(float);

		[Type(1, "float32")]
		public float y = default(float);

		[Type(2, "float32")]
		public float z = default(float);

		[Type(3, "float32")]
		public float vx = default(float);

		[Type(4, "float32")]
		public float vy = default(float);

		[Type(5, "float32")]
		public float vz = default(float);

		[Type(6, "float32")]
		public float rx = default(float);

		[Type(7, "float32")]
		public float ry = default(float);

		[Type(8, "float32")]
		public float rz = default(float);

		[Type(9, "float32")]
		public float rw = default(float);

		[Type(10, "float64")]
		public double t = default(double);

		[Type(11, "string")]
		public string name = default(string);

		[Type(12, "float32")]
		public float colorR = default(float);

		[Type(13, "float32")]
		public float colorG = default(float);

		[Type(14, "float32")]
		public float colorB = default(float);

		[Type(15, "float32")]
		public float avx = default(float);

		[Type(16, "float32")]
		public float avy = default(float);

		[Type(17, "float32")]
		public float avz = default(float);

		[Type(18, "boolean")]
		public bool isEliminated = default(bool);

		[Type(19, "boolean")]
		public bool isQualified = default(bool);

		[Type(20, "boolean")]
		public bool isReady = default(bool);

		[Type(21, "int8")]
		public sbyte checkpointIndex = default(sbyte);
	}
}
