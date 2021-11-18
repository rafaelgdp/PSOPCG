using System;
using System.Collections.Generic;
using Godot;

public class MapParticle
{

    public ParticleColumn Head;
    public ParticleColumn Tail;
    public ParticleColumn GenerationHead;
    public ParticleColumn GenerationTail;
    public bool IsLeftGeneration;
    public int Width;
    bool isFitnessUpdated = false;
    int cachedFitness = 0;
    public int Fitness
    {
        get
        {
            if (!isFitnessUpdated)
            {
                cachedFitness = GetFitness();
            }
            return cachedFitness;
        }
    }

    public bool IsPlayable
    {
        get
        {
            // TODO: implement algorithm that determines playability
            return true;
        }
    }

    public MapParticle(ParticleColumn head)
    {
        this.Head = head;
        var iterator = head;
        if (head.IsMutable)
        {
            this.GenerationHead = head;
            this.IsLeftGeneration = true;
            while (iterator.Next != null && iterator.Next.IsMutable) { iterator = iterator.Next; }
            this.GenerationTail = iterator;
        }
        else
        {
            this.IsLeftGeneration = false;
            while (!iterator.IsMutable) { iterator = iterator.Next; }
            this.GenerationHead = iterator;
            while (iterator.Next != null) { iterator = iterator.Next; }
            this.GenerationTail = iterator;
        }
        while (iterator.Next != null) { iterator = iterator.Next; }
        this.Tail = iterator;
        this.Width = this.Tail.GlobalX - this.Head.GlobalX + 1;
        this.maxSpikes = this.Width / 10F;
    }

    public MapParticle(ParticleColumn head, ParticleColumn generationHead, ParticleColumn generationTail, bool isLeftGeneration)
    {

        if (!arePlayerMoveConstsCalculated)
        {
            calculatePlayerMoveConstants();
        }

        this.Head = head;
        this.GenerationHead = generationHead;
        this.GenerationTail = generationTail;
        this.IsLeftGeneration = isLeftGeneration;

        ParticleColumn iterator;
        if (generationTail != null)
        {
            iterator = GenerationTail;
        }
        else
        {
            iterator = Head;
        }
        while (iterator.Next != null) { iterator = iterator.Next; }
        this.Tail = iterator;
        this.Width = this.Tail.GlobalX - this.Head.GlobalX + 1;
        this.maxSpikes = this.Width / 10F;

    }

    public int GetFitness()
    {

        int fitness = 200 * this.Width;

        fitness += getGroundFitness();
        fitness += getSpikeFitness();
        fitness += getClockFitness();
        fitness += getSafeJumpFitness();

        cachedFitness = fitness;
        isFitnessUpdated = true;

        return fitness;
    }

    static int maxJumpStraightXDistance = 4;
    static int spikePenalty = 20;
    float maxSpikes = 1;
    private int getSpikeFitness()
    {
        int spikeFitness = 0;
        int spikeLength = 0;
        int totalSpikes = 0;
        for (ParticleColumn x = this.Head; x != null; x = x.Next)
        {
            if (x.HasSpike)
            {
                totalSpikes++;
                x = x.Next;
                spikeLength = 1;
                while (x != null && x.HasSpike)
                {
                    x = x.Next;
                    spikeLength++;
                    totalSpikes++;
                }
                // Compute fitness penalty
                if (spikeLength > maxJumpStraightXDistance)
                {
                    spikeFitness -= spikeLength * spikePenalty;
                }
            }
            if (x == null) break;
        }

        // Also, if there are too many spikes in chunk, penalize as well.
        if (totalSpikes > maxSpikes)
        {
            spikeFitness -= (totalSpikes - (int)maxSpikes) * spikePenalty;
        }

        return spikeFitness;
    }

    static int maxBlockJumpHeight = 2;
    static float maxJumpHeight = 0F;
    static float timeToWalkTile = 999F;
    static bool arePlayerMoveConstsCalculated = false;
    static void calculatePlayerMoveConstants()
    {
        // Max Jump Height in a perfectly vertical jump
        // v = v0 + a * t
        float airTimeToMaxHeight = -Global.PlayerJumpForce / Global.Gravity;
        // s = s0 + v0*t + (a * (t**2) / 2)
        maxJumpHeight = Global.PlayerJumpForce * airTimeToMaxHeight +
                              (Global.Gravity * (airTimeToMaxHeight * airTimeToMaxHeight) / 2F);
        maxBlockJumpHeight = (int)maxJumpHeight / Global.TilePixelWidth;

        // Calculate array of reacheable blocks over jump parabola
        timeToWalkTile = Global.TilePixelWidth / Global.PlayerMaxXSpeed;
        arePlayerMoveConstsCalculated = true;
    }
    private int getSafeJumpFitness()
    {
        /*
			For each safe tile, perform a jump to the neighbor safe spots
			For each safe jump, add a small fitness bonus.
			If there are no possible safe jumps for a given safe block,
			apply a great penalty and remove all the points added so far
			for this block.
		*/
        int safeJumpFitness = 0;
        ParticleColumn currentGC = this.Head; // Start from left to right.
        int contiguousHazards = 0;
        int contiguousHazardsPenalty = 50;

        // From the first safe spot, try to perform a jump to the next
        // safe block.
        while (currentGC != null)
        {
            ParticleColumn nextSafe = currentGC.NextSafe();
            if (!isJumpPossible(currentGC, nextSafe))
            {
                if (nextSafe == null)
                {
                    contiguousHazards = this.Tail.GlobalX - currentGC.GlobalX;
                }
                else
                {
                    contiguousHazards = nextSafe.Previous.GlobalX - currentGC.GlobalX;
                }
                // If there were too many contiguous hazards, apply penalty.
                if (contiguousHazards > maxJumpStraightXDistance)
                {
                    safeJumpFitness += -contiguousHazardsPenalty * contiguousHazards;
                }
            }
            currentGC = nextSafe;
        }

        currentGC = this.Tail; // Now from right to left.
        contiguousHazards = 0;

        while (currentGC != null)
        {
            ParticleColumn previousSafe = currentGC.PreviousSafe();
            if (!isJumpPossible(currentGC, previousSafe))
            {
                if (previousSafe == null)
                {
                    contiguousHazards = currentGC.GlobalX - this.Head.GlobalX;
                }
                else
                {
                    contiguousHazards = currentGC.GlobalX - previousSafe.Next.GlobalX;
                }
                // If there were too many contiguous hazards, apply penalty.
                if (contiguousHazards > maxJumpStraightXDistance)
                {
                    safeJumpFitness += -contiguousHazardsPenalty * contiguousHazards;
                }
            }
            currentGC = previousSafe;
        }

        return safeJumpFitness;
    }

    static bool isJumpPossible(ParticleColumn start, ParticleColumn end)
    {
        if (start == null || end == null) return false;
        int xBlockDistance = Mathf.Abs(start.GlobalX - end.GlobalX);
        int direction = start.GlobalX < end.GlobalX ? 1 : -1;
        ParticleColumn iterator = start.SeekNthColumn(direction);
        float xPixelDistance = xBlockDistance * Global.TilePixelWidth;
        float d = xPixelDistance;
        float H = maxJumpHeight;
        float K = -4 * H / (d * d);
        for (int i = 1; i <= xBlockDistance; i++)
        {
            float xi = Global.TilePixelWidth * i;
            float yi = K * xi * (xi - d);
            int maxObstacleHeight = (int)(yi / Global.TilePixelWidth) + start.ObstacleHeight;
            if (maxObstacleHeight < iterator.ObstacleHeight) return false;
            iterator = iterator.SeekNthColumn(direction);
            if (iterator == null) return false;
        }
        return true;
    }

    private int getGroundFitness()
    {
        int groundFitness = 0;
        int holeLength = 0;
        for (ParticleColumn x = this.Head.Next; x.Next != null; x = x.Next)
        {
            if (x.Previous.ObstacleHeight == 0)
            {
                holeLength++;
                if (holeLength > maxJumpStraightXDistance)
                {
                    groundFitness -= 100;
                }
            }
            else
            {
                holeLength = 0;
            }
            var ghDiff = x.Previous.ObstacleHeight - x.ObstacleHeight;
            if (ghDiff == int.MinValue)
                ghDiff = int.MaxValue;
            else
                ghDiff = Mathf.Abs(x.Previous.ObstacleHeight - x.ObstacleHeight);
            if (ghDiff > maxBlockJumpHeight)
            {
                groundFitness -= 100 * ghDiff;
            }
            else
            {
                // The closer they are, the better
                groundFitness += 10 - (2 * ghDiff);
            }
        }
        return groundFitness;
    }

    private int getClockFitness()
    {
        float idealExtraTime = ((Width) * Global.TilePixelWidth / Global.PlayerMaxXSpeed) * 15.0F;
        float extraTime = 0F;

        for (ParticleColumn x = this.Head; x != null; x = x.Next)
        {
            if (x.HasClock)
            {
                extraTime += x.ClockExtraTime;
            }
        }

        int clockFitness = (int)((5 * idealExtraTime) - Mathf.Abs(idealExtraTime - extraTime));
        return clockFitness;
    }

    internal MapParticle CrossoverWith(MapParticle parent2)
    {
        int mutableXRange = this.GenerationTail.GlobalX - this.GenerationHead.GlobalX + 1;
        int leftCrossLimit = (mutableXRange / 3);
        int rightCrossLimit = mutableXRange - (mutableXRange / 3);

        MapParticle leftParent = Global.random.Next(1) == 0 ? this : parent2;
        MapParticle rightParent = leftParent == this ? parent2 : this;

        int maxTries = rightCrossLimit - leftCrossLimit + 1;
        int tries = 0;
        int originalCrossoverXPoint = Global.random.Next(leftCrossLimit, rightCrossLimit);
        int crossoverXPoint = originalCrossoverXPoint;

        ParticleColumn leftParentCrossPoint = leftParent.GenerationHead.SeekNthColumn(crossoverXPoint);
        ParticleColumn rightParentCrossPoint = rightParent.GenerationHead.SeekNthColumn(crossoverXPoint);

        int lowestDiff = Mathf.Abs(leftParentCrossPoint.GroundHeight - rightParentCrossPoint.GroundHeight);
        // Make sure gene combination creates a valid individual
        int crossoverJump = 1;
        while (tries < maxTries && !IsCrossOverXValid(leftParentCrossPoint, rightParentCrossPoint))
        {
            leftParentCrossPoint = leftParentCrossPoint.SeekNthColumn(crossoverJump);
            rightParentCrossPoint = rightParentCrossPoint.SeekNthColumn(crossoverJump);
            if (leftParentCrossPoint.GlobalX - leftParent.GenerationHead.GlobalX > rightCrossLimit)
            {
                leftParentCrossPoint = leftParent.GenerationHead.SeekNthColumn(leftCrossLimit);
                rightParentCrossPoint = rightParent.GenerationHead.SeekNthColumn(leftCrossLimit);
            }
            tries++;
        }

        // Here we start cloning the child genes from the left parent
        ParticleColumn parentIterator = leftParent.Head; // Used to iterate over left parent
        ParticleColumn childHead = new ParticleColumn(parentIterator); // This head will be returned in the end
        ParticleColumn childIterator = new ParticleColumn(parentIterator.Next, childHead, null); // To actually iterate, this is used
        childHead.Next = childIterator;
        parentIterator = parentIterator.Next; // Pointing now to the second column
                                              // Now, starting from the 3rd column now, iterate until the crossover point is reached
        for (parentIterator = parentIterator.Next; parentIterator != leftParentCrossPoint; parentIterator = parentIterator.Next)
        {
            // We need to clone the parent's genes, but link to the child neighbor columns.
            ParticleColumn nextChildColumn = new ParticleColumn(parentIterator, childIterator, null);
            childIterator.Next = nextChildColumn;
            childIterator = nextChildColumn;
        }

        // Copy right parent genetic code to child, starting from crossover point.
        for (parentIterator = rightParentCrossPoint; parentIterator != null; parentIterator = parentIterator.Next)
        {
            ParticleColumn nextChildColumn = new ParticleColumn(parentIterator, childIterator, null);
            childIterator.Next = nextChildColumn;
            childIterator = nextChildColumn;
        }
        // Finally, create an individual from the child genetic code!
        MapParticle childIndividual = new MapParticle(childHead);
        return childIndividual;
    }

    private bool IsCrossOverXValid(ParticleColumn leftParent, ParticleColumn rightParent)
    {
        return Mathf.Abs(leftParent.GroundHeight - rightParent.GroundHeight) < maxBlockJumpHeight;
    }

    private void mutateClockExtraTimeAtX(ParticleColumn x)
    {
        x.ClockExtraTime += (float)Global.random.NextDouble() - 0.5F;
    }

    private void mutateClockAtX(ParticleColumn x)
    {
        x.HasClock = !x.HasClock;
    }

    private void mutateSpikeAtX(ParticleColumn x)
    {
        x.HasSpike = !x.HasSpike;
    }

    private void mutateGroundHeightAtX(ParticleColumn x)
    {
        var changeDirection = Global.random.Next(2) == 1 ? 1 : -1;
        var shift = changeDirection;
        x.GroundHeight += shift;
    }

    internal List<ParticleColumn> Move(double w, List<ParticleColumn> velocities, double r1, double c1, List<ParticleColumn> personalBest, double r2, double c2, MapParticle groupBest, bool isLeft = false)
    {
        this.w = w;
        this.c1 = c1;
        this.c2 = c2;
        this.r1 = r1;
        this.r2 = r2;

        ParticleColumn gco = GenerationHead.Next;
        int i = 0;
        ParticleColumn gcv = velocities[i];
        ParticleColumn gcp = personalBest[i];
        ParticleColumn gcg = groupBest.GenerationHead;
        List<ParticleColumn> resultVelocities = new List<ParticleColumn>(velocities.Count);

        while (gco != null && gco.GlobalX <= GenerationTail.GlobalX)
        {
            double newGroundHeight =
                gco._floatGroundHeight +
                w * gcv._floatGroundHeight +
                r1 * c1 * (gcp._floatGroundHeight - gco._floatGroundHeight) +
                r2 * c2 * (gcg._floatGroundHeight - gco._floatGroundHeight);

            double newHasSpike =
                gco._floatHasSpike +
                w * gcv._floatHasSpike +
                r1 * c1 * (gcp._floatHasSpike - gco._floatHasSpike) +
                r2 * c2 * (gcg._floatHasSpike - gco._floatHasSpike);

            double newHasClock =
                gco._floatHasClock +
                w * gcv._floatHasClock +
                r1 * c1 * (gcp._floatHasClock - gco._floatHasClock) +
                r2 * c2 * (gcg._floatHasClock - gco._floatHasClock);

            double newClockExtraTime =
                gco._floatClockExtraTime +
                w * gcv._floatClockExtraTime +
                r1 * c1 * (gcp._floatClockExtraTime - gco._floatClockExtraTime) +
                r2 * c2 * (gcg._floatClockExtraTime - gco._floatClockExtraTime);

            resultVelocities.Add(new ParticleColumn(null, null, 0,
                newGroundHeight - gco._floatGroundHeight,
                newHasSpike - gco._floatHasSpike,
                newHasClock - gco._floatHasClock,
                (float)(newClockExtraTime - gco._floatClockExtraTime),
                false));

            gco._floatGroundHeight = newGroundHeight;
            gco._floatHasSpike = newHasSpike;
            gco._floatHasClock = newHasClock;
            gco._floatClockExtraTime = (float)newClockExtraTime;

            gco = gco.Next;
            i++;
            if (i < velocities.Count)
            {
                gcv = velocities[i];
                gcp = personalBest[i];
                gcg = gcg.Next;
            }
        }

        isFitnessUpdated = false;

        return resultVelocities;
    }

    public double w { get; private set; }
    public double c1 { get; private set; }
    public double c2 { get; private set; }
    public double r1 { get; private set; }
    public double r2 { get; private set; }

    internal List<ParticleColumn> GetShallowCopy()
    {
        List<ParticleColumn> result = new List<ParticleColumn>();
        for (ParticleColumn gco = GenerationHead; gco != null && gco.GlobalX <= GenerationTail.GlobalX; gco = gco.Next)
            result.Add(new ParticleColumn(gco));
        return result;
    }

    /*
		This method guarantees that the genetic code renders a playable map.
		The idea is to inspect each consecutive x coordinate starting from 
		the x in the mutable area that touches the immutable area towards 
		the other end of the mutable area. Verifications being made:
			- are consecutive x heights jumpable?
			- are consecutive spikes jumpable?
			- are holes jumpable?
	*/
    public void forcePlayability()
    {

        int spikeLength = 0;
        int currentObstacleHeight = 0;

        for (var x = GenerationHead.Next; x != GenerationTail.Next; x = x.Next)
        {

            // Check for unreacheable height diffs
            var obstacleDiff = x.ObstacleHeight - x.Previous.ObstacleHeight;
            if (Mathf.Abs(obstacleDiff) > maxBlockJumpHeight)
            {
                x.GroundHeight += -obstacleDiff + Mathf.Sign(obstacleDiff);
                currentObstacleHeight = x.ObstacleHeight; // Update height
            }

            // TODO: Check this
            // if (x.Previous.GroundHeight != 0) {
            //     var previousGroundDistance = Mathf.Abs(previousGroundIndex - x); // Horizontal dist.
            //     previousGroundDistance += Mathf.Abs(currentObstacleHeight - previousGroundHeight); // Approx. vertical dist.

            //     if (previousGroundDistance > jumpLimit) {
            //         x.GroundHeight += -(currentObstacleHeight - x.Previous.GroundHeight);
            //         currentObstacleHeight = x.GroundHeight; // Update height
            //     }
            // }

            // Check for lengthy spikes
            if (x.HasSpike)
            {
                var obsDiff = x.ObstacleHeight - x.Previous.ObstacleHeight;
                spikeLength += 1 + obsDiff;
                if (spikeLength > maxJumpStraightXDistance)
                {
                    mutateSpikeAtX(x); // This toggles spike here
                }
            }
            else
            {
                if (spikeLength > maxJumpStraightXDistance || x.GroundHeight == 0)
                {
                    if (x.Previous.HasSpike)
                    {
                        mutateSpikeAtX(x.Previous); // This toggles spike here
                    }
                }
                spikeLength = 0;
            }
        }
    }
}
