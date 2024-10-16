# Forked to upgrade base library to newer version

https://developer.android.com/identity/sign-in/legacy-gsi-migration
https://developers.google.com/identity/sign-in/ios/quick-migration-guide

Thank for ios fix which was cherrypicked from this fork : https://github.com/pillsgood/google-signin-unity which came from @DulgiKim https://github.com/googlesamples/google-signin-unity/pull/205#issuecomment-1724733615

Android was migrated to use `CredentialManager` and `AuthorizationClient` since [GoogleSignInAccount was deprecated](https://developers.google.com/android/reference/com/google/android/gms/auth/api/signin/GoogleSignInAccount)

However, `GoogleIdTokenCredential` actually not provide numeric unique ID anymore and set email as userId instead, so I have to extract jwt `sub` value from idToken (which seem like the same id as userId from GoogleSignIn of other platform)

Also, this new system seem like it did not support email hint. And now require WebClientId in addition to Android Client ID. Which need to provided at configuration initialization

```C#
        GoogleSignIn.Configuration = new GoogleSignInConfiguration() {
            RequestEmail = true,
            RequestProfile = true,
            RequestIdToken = true,
            RequestAuthCode = true,
            // must be web client ID, not android client ID
            WebClientId = "XXXXXXXXX-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx.apps.googleusercontent.com",
#if UNITY_EDITOR || UNITY_STANDALONE
            ClientSecret = "XXXXXX-xxxXXXxxxXXXxxx-xxxxXXXXX" // optional for windows/macos and test in editor
#endif
        };
```

Tested in unity 2021.3.21 and unity 6000.0.5

Add UPM dependency with branch tag `https://github.com/Thaina/google-signin-unity.git#newmigration`

```json
{
  "dependencies": {
    "com.google.external-dependency-manager": "https://github.com/googlesamples/unity-jar-resolver.git?path=upm",
    "com.google.signin": "https://github.com/Thaina/google-signin-unity.git#newmigration",
    ...
  }
}
```

Also, [New version of iOS recommend](https://developers.google.com/identity/sign-in/ios/quick-migration-guide#google_sign-in_sdk_v700) that we should set `GIDClientID` and `GIDServerClientID` into Info.plist

So I have add an editor tool `PListProcessor` that look for plist files in the project, extract `CLIENT_ID` and `WEB_CLIENT_ID` property of the plist which contain the `BUNDLE_ID` with the same name as bundle identifier of the project

The plist file in the project should be downloaded from Google Cloud Console credential page

Select iOS credential and download at ⬇ button

```xml
<!-- This plist was the default format downloaded from your google cloud console -->
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	<key>CLIENT_ID</key> 
	<string>{YourCloudProjectID}-yyyyyYYYYyyyyYYYYYYYYYYYYYYyyyyyy.apps.googleusercontent.com</string>
	<key>REVERSED_CLIENT_ID</key>
	<string>com.googleusercontent.apps.{YourCloudProjectID}-yyyyyYYYYyyyyYYYYYYYYYYYYYYyyyyyy</string>
	<key>PLIST_VERSION</key>
	<string>1</string>
	<key>BUNDLE_ID</key>
	<string>com.{YourCompany}.{YourProductName}</string>
<!-- Optional, These 2 lines below should be added manually if you need ServerAuthCode -->
  <key>WEB_CLIENT_ID</key>
  <string>{YourCloudProjectID}-zzzZZZZZZZZZZZZZZzzzzzzzzzzZZZzzz.apps.googleusercontent.com</string>
</dict>
</plist>
```
