dotnet publish -r win-arm -c Release
xcopy .\bin\Release\netcoreapp2.1\win-arm\publish \\enpmaster\c$\appupdates /i /d /y /e