# TurretShocky (or TurretShockyUI)
## A better app for the Smart Shock Collar asset

### Introduction
TurretShocky is my own program to replace the one included with the Smart Shock Collar asset by godsai ([Booth asset link](https://booth.pm/en/items/4790152)).\
It has an UI powered by AvaloniaUI and a number of additional functionalities:
- Support for **more than one shocker** and for PiShock and OpenShock at the same time
- Session **stats** (Times touched, times triggered...)
- **Text file triggers** (To trigger it when you die in a ToN instance for example)
- _More to come..._

Preview:\
![image](https://github.com/user-attachments/assets/e6031290-8f2b-4b5e-bfe5-d8eaa076ec1f)

### Installation
This program has only been tested on Windows (Although a Linux build should be possible, if someone else wants to try)

There is no installation needed, you just need to grab the [latest "TurretShocky.zip"](https://github.com/Turretoforth/TurretShockyUI/releases/latest), extract it and launch "TurretShocky.exe". The settings will be saved in a "prefs.json", so be sure to save it if necessary.

### First use
#### Get your api key and the shocker(s) code
##### For PiShock:
In order for the program to be able to send commands to your PiShock, you'll need to get an api key and create a code for each shocker you want to use.

To get the api key, connect to your PiShock account and go to the [account screen](https://pishock.com/#/account). You'll find a "Generate Api Key" button. Click on it and save the key generated somewhere safe, <ins>*as you won't be able to see it again*</ins>.

For the shocker(s), go to the [control page](https://pishock.com/#/control) and click the share button on the shocker you want to add. Create a new code, then copy and save the code displayed, <ins>*as you won't be able to see it again*</ins>. (Note that it will be replaced by your username once a command has been sent through)

![image](https://github.com/user-attachments/assets/7d5e4706-09d0-4b0c-8bba-c942fcfdb123)

##### For OpenShock:
In order for the program to be able to send commands to your OpenShock shockers, you'll need to get an api token and add the shockers to your OpenShock account.

To get the api token, go to the [API Tokens](https://openshock.app/#/dashboard/tokens) tab and click on the plus icon at the bottom right. Copy the code shown to you and save it somewhere, <ins>*as you won't be able to see it again*</ins>.

#### Configure the Api
Open the Api Configuration window on the program with the "Configure API" button and enter your PiShock api key and username and/or OpenShock api token and url (if needed).

![image](https://github.com/user-attachments/assets/afc4f5b5-b21d-41fe-b3b8-392c64b5757d)

#### Add your shockers
Click the "Add a shocker" button. For PiShock, enter the code you got previously and for OpenShock, you can select from the dropdown (Including shared shockers!). You can enter whatever you want for the name as it is just for you to differentiate between several shockers.

*Fun fact! This works with any PiShock code and OpenShock shared shocker, so you can shock a friend through this*

Don't forget to enable it afterwards.

![image](https://github.com/user-attachments/assets/debf0cf2-3467-4a54-8198-ced977c6ea00)
![image](https://github.com/user-attachments/assets/e6315ffb-fc98-4f3c-9726-00c6fcd32946)

#### Start OSC and have fun
Everything is now configured, you only have to get into your avatar and click the "Start listening to OSC" to start.

### How to contribute
Just make a fork, do your changes and make a Pull Request! However try to detail the changes you're introducing and make sure to test what you did.

### FAQ
#### Why didn't the shocker trigger even though there isn't any error?
First, check if you're able to trigger it manually through the [PiShock website](https://pishock.com/#/control) or the [OpenShock website](https://openshock.app/#/dashboard/shockers/own). If it doesn't work, your problem is there, not this program! (It's usually a problem of power for the hub) \
If it does work, verify:
- That the asset is correctly installed on your avatar
- That you're not on the Idle mode, the maximum is not 0 and that "Power" is enabled on your avatar
- That OSC is enabled both on TurretShocky and VRChat ( [VRChat doc](https://docs.vrchat.com/docs/osc-overview#enabling-it) )
- That the Api configuration is correct
- That the shockers are added and enabled (And the codes not deleted/paused on the PiShock/OpenShock website)


If there's still an issue after verifying all of this, feel free to [open an issue](https://github.com/Turretoforth/TurretShockyUI/issues) with details.
#### Can I suggest a feature / report a bug?
Yeah, just go to the [Issues](https://github.com/Turretoforth/TurretShockyUI/issues), look if it hasn't been suggested or reported and open a new issue if needed.

### Credits and thanks
- The smart Shock collar asset by godsai : [Booth asset link](https://booth.pm/en/items/4790152)
- VRChatOSC library by ChrisFeline : [Github](https://github.com/ChrisFeline/VRChatOSCLib)
- AvaloniaUI, used by this program: [Website](https://avaloniaui.net/) and [Github](https://github.com/avaloniaui/avalonia)
- Thanks to Sloppu and Liliac for helping me test and suggesting some changes!
