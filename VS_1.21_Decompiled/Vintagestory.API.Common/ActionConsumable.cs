namespace Vintagestory.API.Common;

public delegate bool ActionConsumable<T>(T t1);
public delegate bool ActionConsumable();
public delegate bool ActionConsumable<T1, T2>(T1 t1, T2 t2);
