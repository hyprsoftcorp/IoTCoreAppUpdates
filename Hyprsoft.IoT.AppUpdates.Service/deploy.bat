dotnet publish -r win-arm -c Release
xcopy .\bin\Release\netcoreapp2.2\win-arm\publish \\enpmaster\c$\hyprsoft\appupdates /i /d /y /e