![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/WujekFoliarz/DualSenseY/total)

## Notes
- Bluetooth connection is recommended only for controller emulation and UDP protocol, use USB if possible.
- Please report bugs
- If your DualSense controller is invisible after unexpected shutdown, launch the application, connect to controller and close the app.

### If you need any help, add me on discord - wujek_foliarz

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

![image](https://github.com/user-attachments/assets/081590c9-53a0-4659-bd74-8c0a5834866d)

![image](https://github.com/user-attachments/assets/d8eec1f7-7af8-4702-9e8e-8f8a987bd115)

![image](https://github.com/user-attachments/assets/c070c340-2dc6-4d67-b1f6-290065c5a6bb)

![image](https://github.com/user-attachments/assets/8d711908-c4a4-421b-8d4a-7129e99ca10a)

![image](https://github.com/user-attachments/assets/cf1edb04-69f7-4b44-b0fc-a9382b8d778f)

