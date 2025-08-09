using System;

namespace minecraft_windows_service_wrapper.Strategies.Minecraft
{
    public interface IMinecraftVersionStrategyFactory
    {
        IMinecraftVersionStrategy GetStrategy(Version minecraftVersion);
        bool IsVersionSupported(Version minecraftVersion);
    }
}