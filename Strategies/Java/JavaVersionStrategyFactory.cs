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
                { 18, new ModernJavaStrategy(18) },
                { 19, new ModernJavaStrategy(19) },
                { 20, new ModernJavaStrategy(20) },
                { 21, new ModernJavaStrategy(21) },
                { 22, new ModernJavaStrategy(22) },
                { 23, new ModernJavaStrategy(23) }
            };
        }

        public IJavaVersionStrategy GetStrategy(int javaVersion)
        {
            if (_strategies.TryGetValue(javaVersion, out var strategy))
            {
                _logger.LogDebug("Using strategy for Java version {Version}", javaVersion);
                return strategy;
            }

            // For Java versions 17+, use ModernJavaStrategy as fallback
            if (javaVersion >= 17)
            {
                _logger.LogDebug("Using ModernJavaStrategy fallback for Java version {Version}", javaVersion);
                return new ModernJavaStrategy(javaVersion);
            }

            _logger.LogError("No strategy found for Java version {Version}", javaVersion);
            throw new NotSupportedException($"Java version {javaVersion} is not supported. Supported versions: 8, 11, 17+");
        }

        public bool IsVersionSupported(int javaVersion)
        {
            // Explicitly supported versions or any modern version (17+)
            return _strategies.ContainsKey(javaVersion) || javaVersion >= 17;
        }
    }
}