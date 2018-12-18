# PiGpio
Minimal C# (Mono) GPIO library for the Raspberry Pi
---


Minimal but complete support for GPIO functionality on the Raspberry Pi

Tested with the Raspberry Pi 2 and Zero W, should supports all models

**Usage:** create an instance of the 'RaspberryPiGpio' class and:

ConfigureInput - set a pin as input and set the pullup/pulldown mode

ConfigureOuput - set a pin as output

ReadGpio - read the value of an input pin

WriteGpio - write the value of an output pin


Ported from [raspi-gpio](https://github.com/RPi-Distro/raspi-gpio/blob/master/raspi-gpio.c)

Includes code from [Raspberry#](www.raspberry-sharp.org) and Raspberry.IO
