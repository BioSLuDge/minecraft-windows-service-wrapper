# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Windows service wrapper for Minecraft Java Edition servers. It allows running Minecraft servers as Windows services with proper lifecycle management, logging, and graceful shutdown handling.

## Build and Development Commands

```bash
# Build the project
dotnet build

# Run the application (for testing)
dotnet run -- --help

# Publish for deployment
dotnet publish -c Release -o publish

# Install as Windows service (run as administrator)
sc create MinecraftService binPath="C:\path\to\minecraft-windows-service-wrapper.exe --server-directory C:\path\to\minecraft --jar-filename server.jar"

# Start/stop service
sc start MinecraftService
sc stop MinecraftService
```

## Architecture

### Core Components

- **Program.cs**: Entry point, sets up dependency injection, logging, and Windows service hosting
- **CommandLineOptions.cs**: Command-line argument parsing and validation using CommandLineParser library
- **MinecraftServer.cs**: Main service logic implementing `BackgroundService` for the Minecraft server process

### Service Lifecycle

1. **Startup**: Validates directories, detects Java version, configures process arguments
2. **Runtime**: Manages Minecraft server process, pipes stdout/stderr to Windows Event Log
3. **Shutdown**: Issues `save-all` and `stop` commands, waits for graceful process termination

### Java Version Support

The service supports multiple Java LTS versions with optimized JVM arguments:
- **Java 8**: Pixelmon-optimized flags (legacy modded servers)
- **Java 11**: G1GC with experimental optimizations
- **Java 17 & 21**: Aikar's flags for modern Minecraft servers

### Minecraft Version Support

- **1.12**: Uses `nogui` argument format
- **1.16+**: Uses `--nogui` argument format
- Automatic port configuration (defaults to 25565)

## Key Configuration

### Required Command Line Arguments
- `--server-directory`: Path to Minecraft server directory
- `--jar-filename`: Name of the server JAR file

### Optional Arguments
- `--java-home`: Override JAVA_HOME environment variable
- `--port`: Server port (defaults to -1, uses Minecraft default 25565)
- `--minecraft-version`: Specify Minecraft version for argument compatibility

## Dependencies

- **Microsoft.Extensions.Hosting**: Windows service hosting framework
- **Microsoft.Extensions.Logging**: Structured logging with Event Log provider
- **CommandLineParser**: Command-line argument parsing and validation

## Logging Strategy

- All Minecraft server output (stdout) logged as Information level
- All Minecraft server errors (stderr) logged as Error level
- Service lifecycle events logged with context
- Integrated with Windows Event Log for production monitoring

## Development Notes

### Error Handling
- Validates server directory and JAR file existence on startup
- Throws meaningful exceptions for configuration issues
- Handles Java version detection failures gracefully

### Process Management
- Redirects all standard streams for proper logging
- Uses asynchronous stream readers to prevent blocking
- Implements proper disposal pattern for process cleanup

### Java Version Detection
- Parses `java -version` output using regex
- Handles both legacy (1.x) and modern (x.x) version formats
- Falls back to major version number for modern Java releases

## Performance Considerations

- Stream readers run asynchronously to prevent I/O blocking
- G1GC tuning for different Java versions optimizes server performance
- Memory allocation settings (Xmx/Xms) configured per Java version
- Process startup validation prevents resource waste on misconfiguration