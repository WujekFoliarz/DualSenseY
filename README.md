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


![{480F85A0-B126-4AEC-B286-9477683F0636}](https://github.com/user-attachments/assets/5260e8d3-9a26-41d6-9608-c52d3b3af12e)

![{1D17BF59-2790-4FB1-86CF-C45E38CD6E7C}](https://github.com/user-attachments/assets/2d0fc599-26e7-4499-bade-0b62a5ad24e4)

![{8C85D261-C275-4D2D-8D8B-DC1C21A7B84C}](https://github.com/user-attachments/assets/ecc1f0da-e325-4aef-ac7b-ff466b97ff34)

![{F06116C3-9AFD-49F2-957A-E4B1DA391DA5}](https://github.com/user-attachments/assets/22b5a0e7-f7ec-4cc9-bac7-bbc06834eeed)

![{EE532344-B7B7-4E07-8117-A3DD7B56C395}](https://github.com/user-attachments/assets/4cb1c032-3fed-482d-837b-5599bc71562c)

![{ADE2B376-CFA7-470B-9E77-8E7B6BAF325A}](https://github.com/user-attachments/assets/6a3532f3-2ec1-4bd6-9d8f-2abe76a7ae72)

![{324736FB-13BC-400F-98D3-FDDA350EF502}](https://github.com/user-attachments/assets/39ced5a2-f9f2-4d91-b9cd-7e3f57d98fa7)

![{998D3CF1-7CA4-493E-9F77-2EE192FF58BF}](https://github.com/user-attachments/assets/b2912a6b-9c5f-4693-a1e6-85090ba70c1c)

