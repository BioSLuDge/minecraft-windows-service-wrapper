using System;
using CommandLine;

namespace minecraft_windows_service_wrapper
{
    public class CommandLineOptions
    {
        [Value(index: 0, Required = true, HelpText = "The path to the Minecraft server directory that contains the world directory, the server JAR, server.properties, etc.")]
        public string ServerDirectory { get; set; }

        [Option(shortName: 'v', longName: "minecraft-version", Required = true, HelpText = "The version of Minecraft, e.g. 1.12 or 1.16.5")]
        public Version MinecraftVersion { get; set; }

        [Option(shortName: 'p', longName: "port", Required = false, HelpText = "Optional: The port to run the Minecraft server on. If not specified, the default Minecraft server port is used.", Default = -1)]
        public int Port { get; set; }

        [Option(shortName: 'h', longName: "java-home", Required = false, HelpText = "Optional: Specify a Java home directory to use. By default, the JAVA_HOME environment variable is used.")]
        public string JavaHome { get; set; }

        [Option(shortName: 'j', longName: "jar-file", Required = false, HelpText = "Optional: The name of the JAR file to use in the server directory. By default, the JAR file name is assumed to be server.jar.", Default = "server.jar")]
        public string JarFileName { get; set; }
    }
}
