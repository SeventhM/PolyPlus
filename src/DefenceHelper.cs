using Polytopia.Data;

namespace PolyPlus;
public static class DefenceHelper
{
    internal static int ComputeDefenceBonus(UnitState unit, GameState gameState)
    {
        int bonus = ComputeTerrainAndCityBonus(unit, gameState);

        if (unit.HasEffect(UnitEffect.Poisoned))
            bonus /= 2;

        return bonus;
    }

    private static int ComputeTerrainAndCityBonus(UnitState unit, GameState gameState)
    {
        TileData tile = gameState.Map.GetTile(unit.coordinates);

        if (tile == null || !unit.HasAbility(UnitAbility.Type.Fortify))
            return DefenceBonus.Base;

        if (tile.HasImprovement(ImprovementData.Type.City))
        {
            int bonus = DefenceBonus.Fortified;
            if (tile.improvement.HasReward(CityReward.CityWall))
                bonus = DefenceBonus.CityWall;
            return bonus;
        }


        if (unit.owner != 0 && gameState.TryGetPlayer(unit.owner, out var playerState))
        {
            int bonus = playerState.GetDefenceBonus(tile.terrain, gameState);
            if (bonus == 1) bonus = DefenceBonus.Base;
            return bonus;
        }

        return DefenceBonus.Base;
    }

    private static class DefenceBonus
    {
        public const int Base = 10;
        public const int Fortified = 15;
        public const int CityWall = 40;
    }
}