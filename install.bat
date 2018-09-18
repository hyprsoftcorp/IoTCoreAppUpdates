sc delete HyprsoftIoTAppUpdateService 
sc create HyprsoftIoTAppUpdateService binPath= "D:\Source\Hyprsoft.IoT.AppUpdates.Solution\Hyprsoft.IoT.AppUpdates\bin\Release\netcoreapp2.1\win7-x64\publish\Hyprsoft.IoT.AppUpdates.Service.exe" DisplayName= "Hyprsoft IoT App Update Service" start= auto
sc description HyprsoftIoTAppUpdateService "Windows 10 IoT Core App Update Service" 
net start HyprsoftIoTAppUpdateService