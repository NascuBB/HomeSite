// Auto-generated helper for MinecraftVersion
namespace HomeSite.Generated
{
    public static class VersionHelperGenerated
    {
        public static string GetVersion(MinecraftVersion version)
        {
            return version switch
            {
                MinecraftVersion._1_12_2 => "_1_12_2",
                MinecraftVersion._1_16_5 => "_1_16_5",
                MinecraftVersion._1_19_2 => "_1_19_2",
                MinecraftVersion._1_20_1 => "_1_20_1",
                MinecraftVersion._1_21_4 => "_1_21_4",
                _ => ""
            };
        }

        public static string GetVersionDBO(MinecraftVersion version)
        {
            return version switch
            {
                MinecraftVersion._1_12_2 => "1.12.2",
                MinecraftVersion._1_16_5 => "1.16.5",
                MinecraftVersion._1_19_2 => "1.19.2",
                MinecraftVersion._1_20_1 => "1.20.1",
                MinecraftVersion._1_21_4 => "1.21.4",
                _ => ""
            };
        }
    }
}
