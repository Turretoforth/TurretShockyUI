# TurretShocky (or TurretShockyUI)
## A better app for the Smart Shock Collar asset

### Introduction
TurretShocky is my own program to replace the one included with the Smart Shock Collar asset by godsai ([Booth asset link](https://booth.pm/en/items/4790152)).\
It has an UI powered by AvaloniaUI and a number of additional functionalities:
- Support for **more than one shocker**
- Session **stats** (Times touched, times triggered...)
- **Text file triggers** (To trigger it when you die in a ToN instance for example)
- _More to come..._

Preview:\
![image](https://github.com/user-attachments/assets/ac55e687-3caa-47a5-a800-3322d949fc00)

### First use
#### Get your api key and the shocker(s) code
In order for the program to be able to send commands to your PiShock, you'll need to get an api key and create a code for each shocker you want to use.

To get the api key, connect to your PiShock account and go to the [account screen](https://pishock.com/#/account). You'll find a "Generate Api Key" button. Click on it and save the key generated somewhere safe, <ins>*as you won't be able to see it again*</ins>.\

For the shocker(s), go to the [control page](https://pishock.com/#/control) and click the share button on the shocker you want to add. Create a new code, then copy and save the code displayed, <ins>*as you won't be able to see it again*</ins>. (Note that it will be replaced by your username once a command has been sent through) \

![image](https://github.com/user-attachments/assets/7d5e4706-09d0-4b0c-8bba-c942fcfdb123)


#### Configure the Api
Open the Api Configuration window on the program with the "Configure API" button and enter your PiShock api key and username.\

![image](https://github.com/user-attachments/assets/cae821d0-fb15-4f9f-9216-fea53a20bf6b)

#### Add your shockers
Click the "Add a shocker" and enter the code you got previously. Enter whatever you want for the name as it is just for you to differentiate between several shockers.\
*Fun fact! This works with any PiShock code, so you can shock a friend through this* \
Don't forget to enable it afterwards.

![image](https://github.com/user-attachments/assets/31f51181-456b-4e42-b872-697f5ec52f91)

#### Start OSC and have fun
Everything is now configured, you only have to get into your avatar and click the "Start listening to OSC" to start.

### How to contribute
(soon)

### FAQ
#### Why didn't the PiShock trigger even though there isn't any error?
First, check if you're able to trigger it manually through the [PiShock website](https://pishock.com/#/control). If it doesn't work, your problem is there, not this program! (It's usually a problem of power for the hub)\
If it does work, verify:
- That the asset is correctly installed on your avatar
- That you're not on the Idle mode, the maximum is not 0 and that "Power" is enabled on your avatar
- That OSC is enabled both on TurretShocky and VRChat ( [VRChat doc](https://docs.vrchat.com/docs/osc-overview#enabling-it) )
- That the Api configuration is correct
- That the shockers are added and enabled (And the codes not deleted on the PiShock website)


If there's still an issue after verifying all of this, feel free to [open an issue](https://github.com/Turretoforth/TurretShockyUI/issues) with details.

### Credits
- The smart Shock collar asset by godsai : [Booth asset link](https://booth.pm/en/items/4790152)
- VRChatOSC library by ChrisFeline : [Github](https://github.com/ChrisFeline/VRChatOSCLib)
- AvaloniaUI, used by this program: [Website](https://avaloniaui.net/) and [Github](https://github.com/avaloniaui/avalonia)
