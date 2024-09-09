![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/WujekFoliarz/DualSenseY/total)

##
### Download → https://github.com/WujekFoliarz/DualSenseY/releases
### Discord → https://discord.gg/AFYvxf282U

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

![{4AE485EF-3F18-4A4D-B98E-58BF3CB9FEC4}](https://github.com/user-attachments/assets/5462e403-e94b-4d4f-9375-0b738cf5fac1)

![{AB9D2CBE-9FBA-473B-B66A-C91864615902}](https://github.com/user-attachments/assets/6649733d-2d12-42fa-892b-9b714393b97f)

![{4E7906E7-22A0-4F30-9773-4E3C6B5D7229}](https://github.com/user-attachments/assets/8a94ad0f-3575-466d-b5c5-ea4cda4e4026)

![{4740CC97-06A2-414E-9AF7-3E03E52070E1}](https://github.com/user-attachments/assets/17a2bdd9-5539-4e4d-9f8e-75c292d2738c)

![{945AD6F0-5FC8-47C6-911E-1AF9E4C2CE49}](https://github.com/user-attachments/assets/527c745b-7c64-4423-b0c0-b1a3c14bba4a)

![{8D36D252-9A44-42DA-9E30-46CF02694DFB}](https://github.com/user-attachments/assets/5d72a84d-6585-404f-8cf6-deb3b8b935ec)

![{C5267861-37A5-49F4-8619-86942B0323DA}](https://github.com/user-attachments/assets/8b783328-f6dc-4e23-8b11-9dc404a24764)
