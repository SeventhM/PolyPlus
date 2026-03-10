using HarmonyLib;

namespace PolyPlus
{
    public static class Main
    {

        public static void Load()
        {
            PolyMod.Loader.AddPatchDataType("tileEffect", typeof(TileData.EffectType));
            Harmony.CreateAndPatchAll(typeof(Main));
        }
    }
}
