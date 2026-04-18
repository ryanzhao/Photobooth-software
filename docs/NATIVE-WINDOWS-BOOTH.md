# Native Windows Booth

This app is the direct-launch Windows booth runtime.

## Build

```powershell
$env:DOTNET_CLI_HOME='D:\rocket\photobooth\.dotnet-cli'
$env:NUGET_PACKAGES='D:\rocket\photobooth\.nuget\packages'
$env:NUGET_CONFIG_FILE='D:\rocket\photobooth\NuGet.Config'
$env:APPDATA='D:\rocket\photobooth\.appdata'
$env:USERPROFILE='D:\rocket\photobooth\.userprofile'
dotnet restore apps\booth-windows-native\booth-windows-native.csproj --configfile D:\rocket\photobooth\NuGet.Config
dotnet build apps\booth-windows-native\booth-windows-native.csproj -c Release --no-restore
```

## Run

```powershell
D:\rocket\photobooth\apps\booth-windows-native\bin\Release\net9.0-windows\Photobooth.BoothNative.exe
```

## Current Native Features

- direct `.exe` launch
- local booth session folder creation
- recent session history persisted under `booth-data/native-booth`
- native template pack list
- digiCamControl localhost device probe
- Windows-first operator shell without Node/pnpm

## Next Native Migration Steps

- move tethered capture trigger into this native app
- add real live preview surface
- move edit/template/render flow from TypeScript implementation into native Windows services or a local embedded renderer
- add printer spooler integration
