nuget.exe push -Source "HyprsoftNugetFeed" -ApiKey VSTS ..\Hyprsoft.Iot.AppUpdates\bin\Release\Hyprsoft.IoT.AppUpdates.1.0.15.nupkg
nuget.exe push -Source "HyprsoftNugetFeed" -ApiKey VSTS ..\Hyprsoft.Iot.AppUpdates.Web\bin\Release\Hyprsoft.IoT.AppUpdates.Web.1.0.31.nupkg
pause
