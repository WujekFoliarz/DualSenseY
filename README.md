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

![{B1075178-268A-411B-AF7D-9B171F2DBB14}](https://github.com/user-attachments/assets/6c5dfd63-b37e-4945-b49a-9d37b61eed72)

![{9241C119-B853-46AF-B3AA-7A31EA9846D5}](https://github.com/user-attachments/assets/9e64c723-3ee2-4119-897b-ec7e3c023d64)

![{225C6CFC-05B3-4905-9AD9-72B08E5B0060}](https://github.com/user-attachments/assets/744e32e8-06da-46d9-b137-b879d04e12f7)

![{61D5185A-1516-40D3-A75C-02E3E338E71C}](https://github.com/user-attachments/assets/537efb7c-0f65-4e45-b99a-ad4231ada12e)

![{CC5EF9C3-E156-41EB-AB8B-39D9A0ACF33A}](https://github.com/user-attachments/assets/5d86e224-b553-40be-b43b-17b923263c86)

![{CB903D0C-EA16-4D3D-B9EF-718504E4DDF7}](https://github.com/user-attachments/assets/d99298bc-3368-463b-86c2-e18c08b02e6b)

![{FFB936B1-3829-411C-91DE-AB930DB09F18}](https://github.com/user-attachments/assets/09cddb9b-3b8e-409e-bfb1-092b34677398)
