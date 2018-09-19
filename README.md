# Windows 10 IoT Core App Update Service
We needed a way to remotely update .NET Core 2.x apps (not UWP apps) installed on devices running Windows 10 IoT Core.  We created a standard Windows service that periodically reads an app update manifest from a secure remote URI and updates locally installed apps on the device as needed.

### Current Features
1. The app update manifest and associated app update packages can be hosted on the local file system for testing, a website, Azure storage, Dropbox, One Drive, Google Drive, etc.
2. Configurable daily check time (defaults to 3am). 
3. The service can update multiple apps.
4. Package integrity is validated using a MD5 hash.
5. The update manager will automatically "kill" processes being updated and restart them after they've been updated.
6. The service can install new apps that aren't already on the device.  However, this feature is disabled by default for security reasons.

### Process to Update an App
1. Add a new package definition to the manifest with an incremented file version, new release date, new source URI, and new checksum.
2. Upload the app update package (i.e. zip file) containing the updated application files (EXEs, DLLs, etc.) to your desired "host" (i.e  file sharing service).
3. The next time the service checks for updates the new package definition is detected and the package is automatically downloaded to the device and installed.

### Sample Service Configuration

```json
{
  "AllowInstalls": false,
  "CheckTime": "03:00:00",
  "NextCheckDate": "2018-09-18T03:00:00",
  "ManifestUri": "https://hyprsoft.blob.core.windows.net/appupdates/app-update-manifest.json?sp=r&st=2018-09-18T16:16:41Z&se=2099-09-19T00:16:41Z&spr=https&sv=2017-11-09&sig=0UkUjglKo0NDoJs9ZPLvnGiwK38vj6G2l4NibOSorWQ%3D&sr=b",
  "InstalledApps": [
    {
      "ApplicationId": "04fc007e-db18-430f-b4fa-f5b54de1e142",
      "InstallUri": "c:\\testapp"
    }
  ]
}
```
### Sample Manifest

```json
[
  {
    "Id": "04fc007e-db18-430f-b4fa-f5b54de1e142",
    "Name": "Test App 01",
    "Description": "Test App 01",
    "ExeFilename": "hyprsoft.iot.appupdates.testapp.exe",
    "VersionFilename": "hyprsoft.iot.appupdates.testapp.dll",
    "CommandLine": "param1",
    "Packages": [
      {
        "Id": "02902554-4d3d-4159-b106-41e0ac158733",
        "Version": {
          "Major": 1,
          "Minor": 0,
          "Build": 0,
          "Revision": 0,
          "MajorRevision": 0,
          "MinorRevision": 0
        },
        "ReleaseDateUtc": "2018-09-01T00:00:00",
        "SourceUri": "https://hyprsoft.blob.core.windows.net/appupdates/testapp01_1000.zip?sp=r&st=2018-09-18T16:18:09Z&se=2099-09-19T00:18:09Z&spr=https&sv=2017-11-09&sig=aCWyOnZ0TnPzrwWbUIgfFjutiLBERaX0t7HwNOR1%2BG8%3D&sr=b",
        "Checksum": "bddf0cd85b9b4985fb10a1555e10ab6d",
        "Changes": []
      },
      {
        "Id": "02902554-4d3d-4159-b106-41e0ac158733",
        "Version": {
          "Major": 1,
          "Minor": 0,
          "Build": 1,
          "Revision": 0,
          "MajorRevision": 0,
          "MinorRevision": 0
        },
        "ReleaseDateUtc": "2018-09-16T00:00:00",
        "SourceUri": "https://hyprsoft.blob.core.windows.net/appupdates/testapp01_1010.zip?sp=r&st=2018-09-18T16:18:46Z&se=2099-09-19T00:18:46Z&spr=https&sv=2017-11-09&sig=7LTMdPFGlmBl0FmjjXcJ%2BSiwPLH3VSx1XGtHPMJ2aL8%3D&sr=b",
        "Checksum": "e6276631b2c67664810132f1ee565d59",
        "Changes": [
          {
            "Title": "Bug Fix",
            "Notes": "Fixed a bug."
          },
          {
            "Title": "Enhancement",
            "Notes": "Implemented the enhancement."
          }
        ]
      }
    ]
  }
]
```

### Security Concerns
By default the app update service runs on the device under the 'NT AUTHORITY\SYSTEM' user context and has full rights/access to the operating and file system.  This means that the processes the service invokes after an update also run under the same unrestricted user context. **This can be a security risk!**
