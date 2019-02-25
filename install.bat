net stop HyprsoftIoTAppUpdatesService
sc query HyprsoftIoTAppUpdatesService 
sc delete HyprsoftIoTAppUpdatesService 

sc create HyprsoftIoTAppUpdatesService binPath= "D:\Source\Hyprsoft.IoT.AppUpdates.Solution\Hyprsoft.IoT.AppUpdates.Service\bin\Release\netcoreapp2.2\win-x64\publish\Hyprsoft.IoT.AppUpdates.Service.exe" DisplayName= "Hyprsoft IoT App Updates Service" start= auto
sc description HyprsoftIoTAppUpdatesService "Windows 10 IoT Core App Updates Service" 
net start HyprsoftIoTAppUpdatesService