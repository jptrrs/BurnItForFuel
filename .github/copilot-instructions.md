# Copilot Instruction for RimWorld Modding Project - "BurnItForFuel"

## Mod Overview and Purpose

The "BurnItForFuel" mod aims to enhance the gameplay experience of RimWorld by introducing a comprehensive system that allows players to manage and utilize various materials as fuel. This mod is designed to extend the vanilla game's mechanics by allowing the selection and mixing of different fuels to optimize efficiency and output. The primary purpose is to add depth to resource management without disrupting the game's balance.

## Key Features and Systems

1. **Fuel Selection and Management**  
   - Implemented through the `CompSelectFuel` class that facilitates the setup and modification of fuel settings.
   - Offers options for selecting base fuels and mixing them to achieve desired properties.

2. **User Interface Enhancements**  
   - Custom tab (`ITab_Fuel`) for fuel management within the storage system.
   - Provides an interactive UI for configuring and previewing fuel settings.

3. **Flexible Mod Settings**  
   - `ModBaseBurnItForFuel` and `SettingsUI` classes ensure seamless integration of customizable settings.
   - Settings can be saved and adjusted through a user-friendly interface.

4. **Compatibility with Other Mods**  
   - Designed to work alongside commonly used mods like Dubs Hygiene, incorporating logic to exclude incompatible elements (e.g., specific burning pits).

## Coding Patterns and Conventions

- **Class and Method Naming**: Follow PascalCase convention for class names (e.g., `CompSelectFuel`, `ITab_Fuel`) and methods (e.g., `Notify_SettingsChanged`, `SetupFuelSettings`).
- **Code Organization**: Group related functionalities in classes, such as `CompSelectFuel` for fuel management logic. Use partial classes where necessary to separate core game logic from mod-specific alterations.
- **Error Handling**: Ensure robust error handling, especially when dealing with mod compatibility and user-defined settings.
  
## XML Integration

- Despite the error in parsing XML, the mod should include Defs for integrating new fuel types and storage components.
- Define new XML files for specifying allowable fuels and storage structures. Keep XML well-structured and maintainable for easy updates and bug fixes.

## Harmony Patching

- Use `HarmonyPatches` class for modifying base game functionality.
- Ensure patches are non-invasive and mod-safe, focusing on extending rather than altering existing behaviors.
- Example: Patch methods that handle fuel consumption in core game classes to include logic for new fuel types introduced by the mod.

## Suggestions for Copilot

- **Code Generation**: Leverage Copilot to automatically generate boilerplate code for new classes or methods, focusing on adherence to established patterns.
- **XML Assistance**: Use Copilot to assist in writing XML def files, especially when defining complex relationships between different game components.
- **Debugging and Testing**: Propose unit tests where applicable, particularly for methods with significant logic (e.g., fuel validation and mixing functions).
- **Documentation**: Automate inline comments and code documentation to maintain readability and clarity for other developers or contributors.

By following these instructions and leveraging GitHub Copilot, developers can efficiently expand the "BurnItForFuel" mod while ensuring compatibility with the ever-evolving RimWorld modding ecosystem.
