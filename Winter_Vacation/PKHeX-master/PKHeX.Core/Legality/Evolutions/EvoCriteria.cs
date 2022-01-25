﻿namespace PKHeX.Core;

public sealed record EvoCriteria(int Species, int Form) : DexLevel(Species, Form)
{
    public int MinLevel { get; set; }
    public bool RequiresLvlUp { get; set; }
    public int Method { get; init; } = -1;

    public bool IsTradeRequired => ((EvolutionType) Method).IsTrade();

    public override string ToString() => $"{(Species) Species}{(Form != 0 ? $"-{Form}" : "")}}} [{MinLevel},{Level}] via {(EvolutionType) Method}";
}
