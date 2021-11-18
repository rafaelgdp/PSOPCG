using System;
using Godot;

public class ParticleColumn {
	private static long uid = 0;
	private long myuid = uid++;

	public double _floatGroundHeight = 0;

	public int GroundHeight {
		get => (int)Math.Floor(_floatGroundHeight);
		set => _floatGroundHeight = (int)value;
	}

	public int ObstacleHeight => (GroundHeight == 0) ? 0 : (GroundHeight + (HasSpike ? 1 : 0));
	
	public bool IsSafe => !(HasSpike || GroundHeight == 0);

	public double _floatHasSpike = -1;
	public bool HasSpike {
		get => GroundHeight != 0 && _floatHasSpike > 0;
		set {
			if (GroundHeight == 0 && value == !HasSpike) {
				// Possibly trying to mutate with masked HasSpike
				// So I force an internal toggle
				_floatHasSpike *= -1;
			} else {
				_floatHasSpike *= (value ? 1 : -1);
			}
		}
	}

	public double _floatHasClock = -1;
	public bool HasClock {
		get => GroundHeight != 0 &&  _floatHasClock > 0;
		set{
			if (GroundHeight == 0 && value == !HasClock) {
				// Possibly trying to mutate with masked HasSpike
				// So I force an internal toggle
				_floatHasClock *= -1;
			} else {
				_floatHasClock *= (value ? 1 : -1);
			}
		}
	}

	public float _floatClockExtraTime = 5F;
	public float ClockExtraTime {
		set { _floatClockExtraTime = Mathf.Clamp(value, 2f, 10f); }
		get { 
				if (HasClock) {
					return _floatClockExtraTime;
				} else {
					return 0F;
				}
			}
	}

	private int idealClockPosition { 
		get {
			if (Previous == null || Next == null || (Previous.ObstacleHeight < ObstacleHeight && Next.ObstacleHeight < ObstacleHeight))
				return ObstacleHeight;
			return Mathf.Max(Previous.ObstacleHeight, Next.ObstacleHeight);
		}
	}

	public bool IsMutable = true;

	public int GlobalX = 0;

	public ParticleColumn Next = null;
	public ParticleColumn Previous = null;

	public ParticleColumn(ParticleColumn toBeCopied) {
		this.Clone(toBeCopied);
	}

	public ParticleColumn(ParticleColumn toBeCopied, ParticleColumn previous, ParticleColumn next) : this(toBeCopied) {
		this.Previous = previous;
		this.Next = next;
	}

	public ParticleColumn(int globalX, ParticleColumn previous, ParticleColumn next) : this(
		previous, next, globalX,
		Global.random.NextDouble() * 4, // GroundHeight
		Global.random.NextDouble() * 2 - 1, // HasSpike
		Global.random.NextDouble() * 2 - 1, // HasClock
		(float)(Global.random.NextDouble() * 3 + 2), // clock extra time
		true // is mutable
	) {}

	public ParticleColumn(ParticleColumn left, ParticleColumn right, int globalX, double groundHeight, double hasSpike, double hasClock, float clockExtraTime, bool isMutable) {
		this.Previous = left;
		this.Next = right;
		this.GlobalX = globalX;
		this._floatGroundHeight = groundHeight;
		this._floatHasSpike = hasSpike;
		this._floatHasClock = hasClock;
		this._floatClockExtraTime = clockExtraTime;
		this.IsMutable = isMutable;
	}

	public bool ClockPlaced = false;

	internal char CellAtY(int y) {
		y = Mathf.Abs(y);
		if (y < GroundHeight) return 'G';
		if (HasSpike && y == GroundHeight) return 'S';
		if (HasClock && y == idealClockPosition)
			return ClockPlaced ? 'c' : 'C';
		return 'B';
	}

	public void Clone(ParticleColumn gc) {
		this.GroundHeight = gc.GroundHeight;
		this.HasSpike = gc.HasSpike;
		this.HasClock = gc.HasClock;
		this.ClockPlaced = gc.ClockPlaced;
		this.GlobalX = gc.GlobalX;
		this.ClockExtraTime = gc.ClockExtraTime;
		this.IsMutable = gc.IsMutable;
	}

	public ParticleColumn SeekNthColumn(int n) {
		if (n == 0) return this;
		int totalSteps = Mathf.Abs(n);
		ParticleColumn iterator = this;
		if (n < 0) {
			for (int i = 0; i < totalSteps; i++) {
				iterator = iterator.Previous;
				if (iterator == null) {
					return null;
				}
			}
			return iterator;
		} else {
			for (int i = 0; i < totalSteps; i++) {
				iterator = iterator.Next;
				if (iterator == null) {
					return null;
				}
			}
			return iterator;
		}
	}

	public override string ToString() {
		String r = $"gX: {GlobalX}, uid: {myuid}, GroundHeight: {GroundHeight}, HasSpike: {HasSpike}, HasClock: {HasClock}, ClockExtraTime: {ClockExtraTime}.";
		return r;
	}

	public ParticleColumn PreviousSafe() {
		ParticleColumn iterator = Previous;
		while (iterator != null) {
			if (iterator.IsSafe) return iterator;
			iterator = iterator.Previous;
		}
		return null;
	}

	public ParticleColumn NextSafe() {
		ParticleColumn iterator = Next;
		while (iterator != null) {
			if (iterator.IsSafe) return iterator;
			iterator = iterator.Next;
		}
		return null;
	}
}
