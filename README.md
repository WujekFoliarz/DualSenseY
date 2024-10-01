![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/WujekFoliarz/DualSenseY/total)

##
### Download → https://github.com/WujekFoliarz/DualSenseY/releases
### Discord → https://discord.gg/AFYvxf282U

## Notes
- Bluetooth connection is recommended only for controller emulation and UDP protocol, use USB if possible.
- Please report bugs
- If your DualSense controller is invisible after unexpected shutdown, launch the application, connect to controller and close the app.

### If you need any help, add me on discord - wujek_foliarz

## My mod says DSX is not running!
This can happen because there is no DSX.exe running in the background, to fix this download [this](https://raw.githubusercontent.com/WujekFoliarz/DualSenseY/refs/heads/master/DSX/DSX.exe) and place it right next to DualSenseY.exe

I had to separate this file from main release ZIP because windows defender is being pissy about it.

## Using Haptic Feedback via UDP (for mods)
Simply send this json (this can be easily done in existing DSX mods)
```
{
  "instructions":[
    {
      "type":8,
      "parameters":[
        0, // INT - Controller index
        "C:/game/bin/win64/haptics/effect.wav", // STRING - Path to WAV file (Must be a Stereo 48KHz IEEE Float PCM)
        1, // FLOAT 0-1 - Speaker volume
        1, // FLOAT 0-1 - Left actuator volume
        1, // FLOAT 0-1 - Right actuator volume
        1 // BOOL - Clear buffer (this will stop the currently playing effect, if set to 0 the effect will play after the previous one has finished)
    ]
  }
]
```

## Features

- Works with UDP protocol of another application with similar name
- Emulate Dualshock 4 or Xbox 360 controller via ViGEMBus Driver
- Control adaptive triggers
- Control LED
- Test vibrations
- Test the speaker and haptic feedback
- Test touchpad
- Use touchpad as mouse
- Use system audio as haptic feedback
- Display audio volume on your controller (LED)
- Bind keyboard keys to the mic button
- Bind screenshot to the mic button


![{33F06A2B-3E37-40DF-A64B-A86A57CB2D3E}](https://github.com/user-attachments/assets/ec74dcef-403d-4769-99ae-f1b15bd52c64)

![{8A11242C-5635-4C79-8961-372BAB742B9E}](https://github.com/user-attachments/assets/93aaea5b-1dfd-4276-91c7-ab14df3f2455)

![{BC304887-EAB2-4AE7-8316-BA6F01E49AD4}](https://github.com/user-attachments/assets/e1f56fdd-71ae-4c9d-b9ee-3689cfab5123)

![{6FBF441D-8D02-4DE9-9BEE-23F6DCE74A85}](https://github.com/user-attachments/assets/0de75993-b78d-462c-bf5c-fcd5cb697df6)

![{6DA1B3BD-5888-4098-8F5C-EC7FABF05EA0}](https://github.com/user-attachments/assets/8f745832-6d37-45fc-8efc-17c00d795a75)

![{D70AEB80-B88A-43F2-94E4-A9EC373B5F79}](https://github.com/user-attachments/assets/38831a6d-6933-4abe-bf51-321103e1806e)

![{A00ACE89-CD49-45B9-B8DE-D789CCE44A9F}](https://github.com/user-attachments/assets/01c129e3-d6ff-4718-b424-45d4a96c45fb)

![{2AFBA212-02F3-4040-A2D7-D97D6F39F063}](https://github.com/user-attachments/assets/dba35baa-264e-4df6-a102-b5c052bfadf4)



