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

### How to use
(soon)

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
