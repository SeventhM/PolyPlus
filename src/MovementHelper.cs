using Polytopia.Data;

namespace PolyPlus;
public static class MovementHelper
{
    internal static int ComputeMovementCost(
        TileData tile,
        MapData map,
        TileData fromTile,
        PathFinderSettings settings)
    {
        if (settings.unit == null)
            return GetNoUnitCost(tile, settings);

        if (WillEmbarkOrDisembark(tile, settings))
            return MovementCost.Max;

        if (IsBlockedByZoneOfControl(tile, map, settings))
            return MovementCost.Max;

        int cost;
        if(settings.unitData.HasAbility(UnitAbility.Type.Fly))
        {
            cost = GetFlyingCost(tile);
        }
        else if(settings.unitData.HasAbility(UnitAbility.Type.Skate))
        {
            cost = GetSkatingCost(tile);
        }
        else if(settings.unitData.HasAbility(UnitAbility.Type.Creep))
        {
            cost = GetCreepCost(tile);
        }
        else if(settings.unitData.HasAbility(UnitAbility.Type.Amphibious) // All my homies hate UnitAbility.Type.Swim
            || settings.unitData.HasAbility(UnitAbility.Type.Water))
        {
            cost = GetAmphibiousCost(tile, fromTile, settings);
        }
        else
        {
            cost = GetLandCost(tile, fromTile, settings);
        }
        return cost;
    }

    private static int GetNoUnitCost(TileData tile, PathFinderSettings settings)
    {
        if (settings.shouldFollowTransportPaths && (tile.HasRoad || tile.hasRoute))
            return MovementCost.Road;

        return MovementCost.Normal;
    }

    private static bool WillEmbarkOrDisembark(TileData tile, PathFinderSettings settings)
    {
        return ActionUtils.WillUnitEmbark(settings.unit, tile, settings.gameState) != ActionUtils.EmbarkStatus.None;
    }

    private static bool IsBlockedByZoneOfControl(TileData tile, MapData map, PathFinderSettings settings)
    {
        if (settings.shouldAllowOccupiedTiles)                         return false;
        if (settings.unitData.HasAbility(UnitAbility.Type.Sneak))      return false;
        if (settings.unitData.HasAbility(UnitAbility.Type.Hide))       return false;
        if (settings.unit.HasEffect(UnitEffect.Invisible))             return false;

        foreach (TileData neighbor in map.GetTileNeighbors(tile.coordinates))
        {
            if(neighbor.unit == null)
                continue;

            bool isEnemy = neighbor.unit.owner != settings.playerState.Id
                && !settings.playerState.HasPeaceWith(neighbor.unit.owner);

            bool isVisible = !neighbor.unit.HasEffect(UnitEffect.Invisible);
            bool isNotLeader = neighbor.unit.leader == 0;

            if (isEnemy && isVisible && isNotLeader)
                return true;
        }

        return false;
    }

    private static int GetFlyingCost(TileData tile)
    {
        return MovementCost.Normal;
    }

    private static int GetSkatingCost(TileData tile)
    {
        return tile.terrain == TerrainData.Type.Ice ? MovementCost.Normal : MovementCost.Max;
    }

    private static int GetCreepCost(TileData tile)
    {
        return tile.terrain == TerrainData.Type.Mountain ? MovementCost.Max : MovementCost.Normal;
    }

    private static int GetAmphibiousCost(TileData tile, TileData fromTile, PathFinderSettings settings)
    {
        bool isEffectivelyWater = tile.IsWater || tile.HasEffect(TileData.EffectType.Flooded);

        if (!isEffectivelyWater)
            return MovementCost.Max;

        if (tile.HasEffect(TileData.EffectType.Algae))
            return MovementCost.Max;

        return GetSharedWaterAndLandCost(tile, fromTile, settings);
    }

    private static int GetLandCost(TileData tile, TileData fromTile, PathFinderSettings settings)
    {
        if (tile.IsWater && !tile.HasEffect(TileData.EffectType.Flooded))
            return MovementCost.Max;

        if (tile.HasEffect(TileData.EffectType.Flooded) && settings.unit.UnitData.IsWaterBound())
            return MovementCost.Max;

        if (tile.HasEffect(TileData.EffectType.Algae) && settings.unit.UnitData.IsLandBound())
            return MovementCost.Max;

        if (tile.terrain == TerrainData.Type.Ice
            && settings.playerState.HasAbility(PlayerAbility.Type.Glide, settings.gameState))
            return MovementCost.GlideIce;

        return GetSharedWaterAndLandCost(tile, fromTile, settings);
    }

    private static int GetSharedWaterAndLandCost(TileData tile, TileData fromTile, PathFinderSettings settings)
    {
        if (tile.HasRoadTo(fromTile, settings.gameState, settings.unit.owner)
            && tile.terrain != TerrainData.Type.Ice)
            return MovementCost.Road;

        if (HasSlowImprovement(tile, settings))
            return MovementCost.Max;

        if(tile.terrain == TerrainData.Type.Forest)
            return MovementCost.Forest;

        if (tile.terrain == TerrainData.Type.Mountain)
            return MovementCost.Max;

        return MovementCost.Normal;
    }

    private static bool HasSlowImprovement(TileData tile, PathFinderSettings settings)
    {
        if (tile.improvement == null) return false;
        return settings.gameState.GameLogicData.TryGetData(tile.improvement.type, out ImprovementData? data)
            && data != null
            && data.HasAbility(ImprovementAbility.Type.Slow);
    }

    private static class MovementCost
    {
        public const int Road = 5;
        public const int GlideIce = 9;
        public const int Normal = 10;
        public const int Forest = 20;
        public const int Max = 1000;
    }
}