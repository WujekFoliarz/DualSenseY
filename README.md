![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/WujekFoliarz/DualSenseY/total)

## Notes
- Bluetooth connection is recommended only for controller emulation and UDP protocol, use USB if possible.
- Please report bugs
- If your DualSense controller is invisible after unexpected shutdown, launch the application, connect to controller and close the app.

### If you need any help, add me on discord - wujek_foliarz

## Using Haptic Feedback via UDP (for mods)
Simply send this json
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
- Audio volume display on your controller (LED)

![image](https://github.com/user-attachments/assets/e5ee0fe9-211d-444b-8bbc-bd78cb886cb8)

![image](https://github.com/user-attachments/assets/d559cc36-1e82-4195-9882-8490631b0904)

![image](https://github.com/user-attachments/assets/eab4f200-7c25-477d-96f7-9b63753d6a0f)

![image](https://github.com/user-attachments/assets/ac230c1a-577e-4597-95a4-7a38d605c3cc)

![image](https://github.com/user-attachments/assets/61eb7a45-8961-4a1c-b41d-571477494305)
