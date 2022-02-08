# greenshot-erpnext-plugin

A [Greenshot](https://github.com/greenshot/greenshot) v1.2.10 plugin for uploading tagged images to [ERPNext](https://erpnext.com)

## Installation
1. Create a folder called `GreenshotPlugins.ERPNext` in Greenshot's plugins folder (e.g. `C:\Program Files\Greenshot\Plugins\`)
2. Go to [Releases](https://github.com/nicholas-ramsey/greenshot-erpnext-plugin/releases)
3. Download the latest `GreenshotPlugins.ERPNext.zip` file
4. Unzip the file's contents into the `GreenshotPlugins.ERPNext` folder you created
5. Restart Greenshot

## Configuration

On your ERPNext instance,

1. Create a new OAuth client
   1. `App Name` - `greenshot_erpnext_plugin`
   2. `Scopes` - `all openid`
   3. `Redirect URIs` - Redirect URI for app (`http://localhost:5057`)
   4. `Default Redirect URI` - Redirect URI for app (`http://localhost:5057`)

In the plugin's config on Greenshot,

2. Configure the plugin
	1. Under `Instance URL`, add the URL of your ERPNext instance.
	2. Under `Client ID`, add the client ID of the OAuth client you created in step #1.
	3. Finally, click the `Login` button to login.

### If Unable to Login

If you aren't able to login under the config form for whatever reason, find Greenshot's config file (typically `%APPDATA%\Roaming\Greenshot\Greenshot.ini`) and manually add your refresh token under the ERPNext section.

```ini
[ERPNext]
; ERPNext refresh Token
RefreshToken=<MY_REFRESH_TOKEN>
```