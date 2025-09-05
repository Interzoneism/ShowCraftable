namespace Vintagestory.API.Common;

public class FastParticlePool
{
	public delegate ParticleBase CreateParticleDelegate();

	public ParticleBase FirstAlive;

	public ParticleBase FirstDead;

	public int PoolSize;

	public int AliveCount;

	public FastParticlePool(int poolSize, CreateParticleDelegate createParticle)
	{
		if (poolSize != 0)
		{
			PoolSize = poolSize;
			ParticleBase particleBase = (FirstDead = createParticle());
			poolSize--;
			while (poolSize-- > 0)
			{
				particleBase.Next = createParticle();
				particleBase.Next.Prev = particleBase;
				particleBase = particleBase.Next;
			}
		}
	}

	public void Kill(ParticleBase elem)
	{
		if (elem == FirstAlive)
		{
			FirstAlive = elem.Next;
			if (FirstAlive != null)
			{
				FirstAlive.Prev = null;
			}
		}
		else
		{
			elem.Prev.Next = elem.Next;
			if (elem.Next != null)
			{
				elem.Next.Prev = elem.Prev;
			}
		}
		if (FirstDead == null)
		{
			elem.Prev = null;
			elem.Next = null;
		}
		else
		{
			FirstDead.Prev = elem;
			elem.Next = FirstDead;
			elem.Prev = null;
		}
		FirstDead = elem;
		AliveCount--;
	}

	public ParticleBase ReviveOne()
	{
		if (FirstDead == null)
		{
			return null;
		}
		ParticleBase firstDead = FirstDead;
		FirstDead = firstDead.Next;
		if (FirstAlive == null)
		{
			firstDead.Prev = null;
			firstDead.Next = null;
		}
		else
		{
			FirstAlive.Prev = firstDead;
			firstDead.Next = FirstAlive;
			firstDead.Prev = null;
		}
		FirstAlive = firstDead;
		AliveCount++;
		return firstDead;
	}
}
