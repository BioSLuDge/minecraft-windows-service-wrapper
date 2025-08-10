# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Windows service wrapper for Minecraft Java Edition servers. It allows running Minecraft servers as Windows services with proper lifecycle management, logging, and graceful shutdown handling.

## Build and Development Commands

```bash
# Build the project
dotnet build

# Run the application (opens GUI)
dotnet run

# Publish for deployment
dotnet publish -c Release -o publish

# Install as Windows service (run as administrator)
sc create MinecraftService binPath="C:\path\to\minecraft-windows-service-wrapper.exe"

# Start/stop service
sc start MinecraftService
sc stop MinecraftService
```

## Architecture

### Core Components

- **Program.cs**: Entry point, sets up dependency injection, logging, GUI or Windows service hosting
- **Gui/MainForm.cs**: Windows Forms interface for managing configuration and service installation
- **Services/SettingsService.cs**: Handles reading and writing persistent configuration
- **MinecraftServer.cs**: Main service logic implementing `BackgroundService` for the Minecraft server process

### Service Lifecycle

1. **Startup**: Validates directories, detects Java version, configures process arguments
2. **Runtime**: Manages Minecraft server process, pipes stdout/stderr to Windows Event Log
3. **Shutdown**: Issues `save-all` and `stop` commands, waits for graceful process termination

### Java Version Support

The service supports multiple Java versions with optimized JVM arguments:
- **Java 8**: Pixelmon-optimized flags (legacy modded servers)
- **Java 11**: G1GC with experimental optimizations  
- **Java 17+**: Aikar's flags for modern Minecraft servers (supports 17, 18, 19, 20, 21, 22, 23, and future versions)

### Minecraft Version Support

- **1.12**: Uses `nogui` argument format
- **1.16+**: Uses `--nogui` argument format
- Automatic port configuration (defaults to 25565)

## Key Configuration

Settings are managed through the GUI and persisted between launches. Configuration is stored in `%APPDATA%/MinecraftServiceWrapper/settings.json`.

## Dependencies

- **Microsoft.Extensions.Hosting**: Windows service hosting framework
- **Microsoft.Extensions.Logging**: Structured logging with Event Log provider

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