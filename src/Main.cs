using HarmonyLib;
using Polytopia.Data;

namespace PolyPlus;
public static class Main
{

    public static void Load()
    {
        PolyMod.Loader.AddPatchDataType("tileEffect", typeof(TileData.EffectType));
        Harmony.CreateAndPatchAll(typeof(Main));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TileData), nameof(TileData.GetMovementCost))]
    private static bool TileData_GetMovementCost(
        ref int __result,
        TileData __instance,
        MapData map,
        TileData fromTile,
        PathFinderSettings settings)
    {
        __result = MovementHelper.ComputeMovementCost(__instance, map, fromTile, settings);
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.GenerateInternal))]
    private static void MapGenerator_GenerateInternal(int seed, GameState gameState, MapGeneratorSettings settings)
    {
        WorldCoordinates[] corners = ExploreLightHouseTask.GetCorners(gameState);
        for (int i = 0; i < corners.Length; i++)
        {
            TileData tile = gameState.Map.GetTile(corners[i]);
            if(tile.HasImprovement(ImprovementData.Type.LightHouse))
            {
                tile.improvement = null; // Yes, I know about AddLightHouseImprovements. It is simply not longer called. idk why
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.SetTileAsCapital))]
    private static void MapGenerator_SetTileAsCapital(GameState gameState, PlayerState playerState, TileData tile)
    {
        if (tile == null || !gameState.GameLogicData.TryGetData(playerState.tribe, out TribeData tribeData)
            || !tribeData.HasAbility(EnumCache<TribeAbility.Type>.GetType("citypark")))
        {
            return;
        }

        tile.improvement.production = 2;
        tile.improvement.baseScore += 250;
        tile.improvement.AddReward(CityReward.Park);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ActionUtils), nameof(ActionUtils.OnRevealLighthouseTile))]
    public static bool OnRevealLighthouseTile(GameState gameState, PlayerState playerState, WorldCoordinates tile)
    {
        gameState.CheckTask(playerState, TaskData.Type.ExploreLighthouses);
        ActionUtils.EnableTask(gameState, playerState, TaskData.Type.ExploreLighthouses);
        return false;
    }
}
