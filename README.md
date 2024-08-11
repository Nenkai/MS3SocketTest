# MS3SocketTest

Quick tool for interacting with motorstorm 3's Resource Viewer binary

## Usage (RPCS3)

1. Install Debug Firmware in RPCS3 (https://darthsternie.net/ps3-dev-firmwares/) - 4.81 is fine
2. Make sure Debug Console Mode in Advanced is disabled
3. Install PKG
4. Extract PSARC
5. Move contents to `RPCS3/app_home/published` (create folders)
6. Edit RPCS3/config/vfs.yml:
	/app_home/: $(EmulatorDir)app_home/
7. Create file in `RPCS3\app_home\overrides\scripts`, content should be IPAddress("0.0.0.0")
8. Boot through self
9. Check commands documented in Program.cs

## Compilation 

Requires VS 2022 & .NET 8

Make sure to edit the path to the game dir in `Program.cs`

## Known Issues

Needs to be restarted everytime (i'm lazy).
