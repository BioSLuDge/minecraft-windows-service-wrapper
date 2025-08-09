using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace minecraft_windows_service_wrapper.Strategies.Java
{
    public class JavaVersionStrategyFactory : IJavaVersionStrategyFactory
    {
        private readonly ILogger<JavaVersionStrategyFactory> _logger;
        private readonly Dictionary<int, IJavaVersionStrategy> _strategies;

        public JavaVersionStrategyFactory(ILogger<JavaVersionStrategyFactory> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _strategies = new Dictionary<int, IJavaVersionStrategy>
            {
                { 8, new Java8Strategy() },
                { 11, new Java11Strategy() },
                { 17, new ModernJavaStrategy(17) },
                { 21, new ModernJavaStrategy(21) }
            };
        }

        public IJavaVersionStrategy GetStrategy(int javaVersion)
        {
            if (_strategies.TryGetValue(javaVersion, out var strategy))
            {
                _logger.LogDebug("Using strategy for Java version {Version}", javaVersion);
                return strategy;
            }

            _logger.LogError("No strategy found for Java version {Version}", javaVersion);
            throw new NotSupportedException($"Java version {javaVersion} is not supported");
        }

        public bool IsVersionSupported(int javaVersion)
        {
            return _strategies.ContainsKey(javaVersion);
        }
    }
}