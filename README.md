# MyLittleCompany-reconstructed

Portfolio repository reconstructed from a Unity game project, focused on **tile system and render ordering design**.

## Overview

This repository is a reconstructed portfolio project based on a Unity game project *My Little Company*.

Only the parts I personally designed and implemented have been extracted and reorganized.
The goal of this repository is not to provide a complete game, but to present
the **system design and problem-solving process** behind key features.

This project specifically focuses on spatial logic and rendering challenges
encountered during development, and how they were addressed through custom systems.

## Key Systems

My Little Company is built on an isometric tile-based environment,  
requiring a spatial system for object placement and rendering.

During development, the following challenges had to be addressed:

- Multi-tile objects causing incorrect rendering in partial occlusion cases  
- Limitations of simple sorting for consistent render order  
- Precision issues in world ↔ tile coordinate conversion  

To resolve these, custom systems were implemented:

- **Tile System**  
  Spatial structure supporting multi-tile object placement  

- **Coordinate Conversion**  
  Accurate world ↔ tile transformation with precision correction  

- **Render Ordering (OrderTree)**  
  Dependency-based ordering for stable rendering  

These systems ensure correct spatial behavior and rendering consistency  
within an isometric environment.

Detailed design and implementation are documented here:

- [Tile System & Render Ordering](docs/tile-system-and-render-ordering.md)

## Sample Scope

This repository includes a curated subset of scripts focused on the systems listed above.

- Only system-related code has been extracted  
- Gameplay features and unrelated components are intentionally excluded  
- Code has been reorganized for clarity, with original logic preserved    

The goal is to provide a clear view of system design rather than full project completeness.

## Code Overview

This repository is structured into three core components:

- **TileSystem** — defines spatial rules and coordinate conversion  
- **TileObject** — represents tile-based objects and placement  
- **TileOrder** — resolves render ordering via OrderTree  

### TileSystem (`TileSystem.cs`)
Handles world ↔ tile conversion, isometric grid calculation, and global tile configuration.  
→ Defines the **spatial rules** of the environment

### TileObject (`TileObject.cs`)
Manages tile position, size, pivot-based placement, and multi-tile occupancy.  
→ Acts as the **tile-aware entity layer**

### TileOrder (`TileOrder.cs`)
Registers objects to OrderTree and applies sorting order dynamically.  
→ Connects spatial data to the **rendering pipeline**

### Design Principle

- `TileSystem` → spatial rules  
- `TileObject` → object representation  
- `TileOrder` → rendering integration  

Each system is isolated by responsibility, ensuring modularity and scalability.

## System Demo

This repository includes a build that demonstrates the behavior of the implemented systems.

- Platform: Android  
- Purpose: System demonstration (not a full gameplay experience)  
- Focus: Tile placement and render ordering behavior

[Download Demo APK (v1.0)](https://github.com/javaCof/MyLittleCompany-reconstructed/releases/tag/mlc-v1.0)

## External Links

- **Technical Documentation**  
  [Tile System & Render Ordering](docs/tile-system-and-render-ordering.md)

## Notes for Reviewers

- This is a **system-focused portfolio repository**, not a complete game  
- The emphasis is on design decisions, problem-solving, and architecture  
- Some elements are simplified to highlight core systems  

## Contact

- Email: javacoffee0930@gmail.com