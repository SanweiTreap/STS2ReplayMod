namespace ReplayMod;

public sealed record UnitRngState(int Counter,ulong State0,ulong State1,ulong State2,ulong State3);
public sealed record UnitPowerState(string Model,decimal Amount);
public sealed record CombatUnitEntry(uint Identity,string Model,int? Owner,bool Alive,int Hp,int MaxHp,int Block,string? Move,UnitRngState? Rng,UnitPowerState[] Powers);
public sealed record CombatUnitState(uint NextIdentity,CombatUnitEntry[] Allies,uint[] Pets);
