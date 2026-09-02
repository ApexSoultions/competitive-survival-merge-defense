using System;



[Serializable]

public sealed class MatchLoadout

{

    public UnitData[] units = Array.Empty<UnitData>();

    public ActiveAbilityDefinition[] actives = Array.Empty<ActiveAbilityDefinition>();

    public RelicDefinition relic;

    public SpecialTileDefinition specialTile;



    public MatchLoadout Clone()

    {

        return new MatchLoadout

        {

            units = units != null ? (UnitData[])units.Clone() : Array.Empty<UnitData>(),

            actives = actives != null ? (ActiveAbilityDefinition[])actives.Clone() : Array.Empty<ActiveAbilityDefinition>(),

            relic = relic,

            specialTile = specialTile

        };

    }



    public int CountUnits()

    {

        if (units == null)

            return 0;

        int count = 0;

        for (int i = 0; i < units.Length; i++)

        {

            if (units[i] != null)

                count++;

        }

        return count;

    }



    public int CountActives()

    {

        if (actives == null)

            return 0;

        int count = 0;

        for (int i = 0; i < actives.Length; i++)

        {

            if (actives[i] != null)

                count++;

        }

        return count;

    }



    public bool IsComplete(int requiredUnits, int requiredActives)

    {

        return CountUnits() >= requiredUnits && CountActives() >= requiredActives;

    }

}

