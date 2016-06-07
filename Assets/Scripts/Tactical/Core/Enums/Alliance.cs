using UnityEngine;

namespace Tactical.Core.Enums {

	public enum Alliance {
		None = 0,
		Neutral = 1 << 0,
		Hero = 1 << 1,
		Enemy = 1 << 2
	}

}
