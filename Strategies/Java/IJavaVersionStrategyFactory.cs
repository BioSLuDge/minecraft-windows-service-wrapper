namespace minecraft_windows_service_wrapper.Strategies.Java
{
    public interface IJavaVersionStrategyFactory
    {
        IJavaVersionStrategy GetStrategy(int javaVersion);
        bool IsVersionSupported(int javaVersion);
    }
}