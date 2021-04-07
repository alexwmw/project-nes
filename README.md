# ProjectNES: Emulating the Nintendo Entertainment System 
### MSc Computer Science Project Report Highlights

## Abstract
Computer emulation has been used increasing in video game-based entertainment over the past decades, with a number of obsolete gaming systems re-emerging in the marketplace as emulators. One of the most popular consoles of the 1980s was the 8-bit Nintendo Entertainment System (NES). This paper describes the development of ProjectNES, an emulator for the NES written in the C# programming language. ProjectNES is a low-level emulator that re-implements the hardware architecture of the NES, including a MOS Technology 6502 CPU and an implementation of the sophisticated, proprietary Ricoh 2C02 Picture Processing Unit (PPU). The result is a largely successful emulator with a fully functioning CPU and a PPU at an advanced stage of development. 

## Introduction
The Nintendo Entertainment System (NES), released in 1985, is one of the most famous pieces of computer hardware of all time. At its height, the market for NES software exceeded the market for all other home computer software [1]. It spawned many of the present day’s most famous game franchises, and to this day is one of the top ten highest selling home video games consoles. 
Many factors contributed to the popularity of the NES, including its well thought-out design. The machine features a MOS Technology 6502 processor; a mainstay of the age, making the device familiar for programmers. Its graphics processor had an intelligent design, maximising efficiency of memory and allowing programmers to take full advantage of the hardware.
This project report details the development of ProjectNES: an emulator for the NES. It aims to model the system in software and produce a desktop application capable of running NES programs. The result is a project with great attention paid to the NES hardware architecture, leading to some fascinating insights about the machine, and some difficult challenges faced in implementing what is in places a deceptively complex design.

## Specification & Design
### Objectives
The overall objective of this project is quite straightforward. Emulation software, as defined above, is software that imitates or reproduces the effects of another system. In this case the target system is the NES as described above. However, the simplicity of this objective does not reveal much of the challenge involved, nor does it give much indication of what might be considered a success or a partial success.
With this in mind, four statements or ‘wishes’ are given below which provide a clearer indication of the expectations for this software: 

_“The emulator should have a fully functional CPU.”_

At the heart of the emulator there should be an emulation of the 6502 processor, with a fully implemented instruction set. This CPU should produce the same output as a real 6502 for any given input.

_“The emulator should be able to read and run original NES software.”_

Obviously, a software emulator cannot read from proprietary ‘hard’ storage such as a physical games cartridge without some form of hardware interface. However, the emulator should be able to execute, in binary form, programs written for the NES. In this case, program files with be in the iNES format describe in Section 3.3.

_“The emulator should be visual and interactive.”_

Conceivably, a NES emulator could be fully functional and able to run software without any means of displaying its output – much like a real computer console not attached to any television or monitor. Indeed, some emulators are designed this way such as those that target the Libretro API [19]. However, for this project the emulator should have a self-contained means of displaying graphical output and taking user input.

_“The emulator should be able to render graphical output that visually identical to a real NES.”_

The ultimate and most challenging objective of this project is to produce output such that a typical user would not be able to distinguish it from that of a real NES console, or a known good emulator. The picture should be identical in its layout and timing, and not introduce any additional bugs or glitches. A caveat of this is that it was not expected that there would be sufficient time to implement both the background and foreground layers of the image. However, this objective should still hold for whatever is displayed.

### Software Framework and Language
ProjectNES was developed in the .NET Core software framework. It was written in the C# programming language.
While most emulators of this kind are written in C++, C# had the advantage of being familiar to the programmer and was deemed to have all of the relevant features for a project of this sort; that it is object oriented, suitable for desktop applications and offers sufficient performance. Furthermore, its support for first-class functions (not supported in Java, for example) offered additional flexibility when developing the instruction set and execution cycle.

### Third Party Libraries
A third party library was used for graphics processing, allowing ProjectNES’s graphical output to be visualised. Two such libraries were used during development. These were Simple DirectMedia Layer (SDL) [23], and Simple and Fast Multimedia Library (SFML) [24]. Both are developed in C or C++ but have C# bindings. Both were experimented with, but ultimately SFML was found to be easier to use. 


## Implementation


![image](https://user-images.githubusercontent.com/44982187/113860251-b78c0a80-979d-11eb-8fa7-969b1096733f.png)

