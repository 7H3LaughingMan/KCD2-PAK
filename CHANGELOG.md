# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/)
and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [2.0.0] - 2025-03-26

### Added

- Created right-click context menu option for when you right-click inside a folder
- You can now process multiple folders by processing the parent folder
- If you would like to ignore a specific mod, add an empty `kcd-pak.ignore` file into the mod's folder
- If you would like to save the log to a file for a specific mod, create a `kcd-pack.log` file into the mod's folder
  - Note: This file is overwritten each time it is run
- If you would like to output the generate pak files to another folder, create a new shortcut to the other folder with the name `kcd-pack` in the mod's folder
  - Note: Once created you can delete the folder it's pointing to and it will create it if it doesn't exist
  - FYI you can create a shortcut by right-clicking in a folder and selecting the option to create a new Shortcut
- If you would like to not have the prompt to press any key to continue you can create an empty `.nopause` file in `%LocalAppData%\KCD2-PAK`
  - If you are using the portable version you need to put it in the main folder.

### Changed

- Switched to Velopack

## [1.3.0] - 2025-03-24

### Added

- If the folder you are trying to process ends with `_dev` it will output the generated PAK files into a folder with the same name but without the `_dev` suffix.

## [1.2.0] - 2025-03-23

### Changed

- Switch back to a trimmed self-contained application
- Remove prerequisites from the installer

## [1.1.0] - 2025-03-23

### Added

- Windows Installer Package
- Update Check
- Context Menu Integration

### Changed

- Improved Logging

## [1.0.0] - 2025-03-22

Initial Release

[Unreleased]: https://github.com/7H3LaughingMan/KCD2-PAK/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/7H3LaughingMan/KCD2-PAK/compare/v1.3.0...v2.0.0
[1.3.0]: https://github.com/7H3LaughingMan/KCD2-PAK/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/7H3LaughingMan/KCD2-PAK/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/7H3LaughingMan/KCD2-PAK/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/7H3LaughingMan/KCD2-PAK/releases/tag/v1.0.0
