# LexemeExtractor Releases

This document describes how to create and manage releases for LexemeExtractor.

## Automated Releases

The project uses GitHub Actions to automatically build cross-platform AOT binaries for:

- **Windows** (x64, ARM64)
- **Linux** (x64, ARM64) 
- **macOS** (x64, ARM64)

### Creating a Release

#### Option 1: Tag-based Release (Recommended)
1. Create and push a version tag:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```
2. The GitHub Action will automatically trigger and create a release with binaries

#### Option 2: Manual Release
1. Go to the GitHub repository
2. Navigate to Actions → "Build and Release"
3. Click "Run workflow"
4. Enter the desired version (e.g., v1.0.0)
5. Click "Run workflow"

### Release Artifacts

Each release includes:
- `LexemeExtractor-windows-x64.zip` - Windows 64-bit executable
- `LexemeExtractor-windows-arm64.zip` - Windows ARM64 executable  
- `LexemeExtractor-linux-x64.tar.gz` - Linux 64-bit executable
- `LexemeExtractor-linux-arm64.tar.gz` - Linux ARM64 executable
- `LexemeExtractor-macos-x64.tar.gz` - macOS Intel executable
- `LexemeExtractor-macos-arm64.tar.gz` - macOS Apple Silicon executable

### Installation Instructions for Users

1. Download the appropriate archive for your platform from the [Releases page](https://github.com/Applied-General-Intelligence-Inc/LexemeExtractor/releases)
2. Extract the archive
3. Run the executable directly - no .NET runtime installation required

#### Windows
```cmd
# Extract the zip file
# Run from command prompt or PowerShell
LexemeExtractor.exe [arguments]
```

#### Linux/macOS
```bash
# Extract the tar.gz file
tar -xzf LexemeExtractor-linux-x64.tar.gz
cd LexemeExtractor-linux-x64

# Make executable (if needed)
chmod +x LexemeExtractor

# Run
./LexemeExtractor [arguments]
```

## Development Notes

- The project uses .NET 9.0 with Native AOT compilation
- Cross-compilation is handled by GitHub Actions using platform-specific runners
- Each platform requires its native toolchain (automatically installed by the workflow)
- The `RuntimeIdentifier` is set dynamically during the build process
