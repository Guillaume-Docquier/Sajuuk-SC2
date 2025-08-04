# Gemini Code Assistant Context

## Project Overview

This project contains Sajuuk, a StarCraft 2 bot written in C#. The bot is designed to play as the Zerg race and features a modular architecture that separates concerns like game interaction, map analysis, and high-level strategy.

The core components of the project are:

*   **Sajuuk:** The main bot logic, including strategies, build orders, and unit micro-management.
*   **SC2Client:** A client for interacting with the StarCraft II game engine.
*   **S2ClientProtocol:** The implementation of the StarCraft II protocol buffers.
*   **MapAnalysis:** A component for analyzing game maps to identify strategic locations like expansions, choke points, and ramps.
*   **Algorithms:** A collection of algorithms used by the bot, such as pathfinding and clustering.

## Building and Running

The project is built and tested using the .NET CLI.

### Build

To build the project, run the following command from the root directory:

```sh
dotnet build --configuration Release
```

### Test

To run the unit tests, use the following command:

```sh
dotnet test --configuration Release
```

## Development Conventions

The project follows standard C# coding conventions and uses a modular architecture to separate concerns. Unit tests are written using MSTest and are located in the `Sajuuk.Tests`, `MapAnalysis.Tests`, and `Algorithms.Tests` projects.

The project uses a git-flow branching model, with the `master` branch containing the latest stable release and feature branches used for new development.
