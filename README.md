# Esp32GpsLogger
The aim of this spare time project was to build a low power GPS logger for multi-day hikes using an ESP32. It logs the following attributes into a CSV file:
* Timestamp
* Latitude/Longitude
* Altitude
* HDOP value
* Number of available satellites

The Deep Sleep functionality of the ESP32 is used to improve the battery life. Every time a GPS position with a good HDOP value was found, the ESP32 will be going into Deep Sleep for 30s.

## Hardware
* [FireBeetle ESP32](https://wiki.dfrobot.com/FireBeetle_ESP32_IOT_Microcontroller(V3.0)__Supports_Wi-Fi_&_Bluetooth__SKU__DFR0478#target_7)
* [GPS Modul L76X Multi-GNSS Waveshare](https://www.waveshare.com/wiki/L76X_GPS_Module)
* Micro SD Card Module 3.3V/5V
* 18650 Li-Ion Battery (3000mA)

## Breadboard Connections Pinout
![screenshot](https://raw.githubusercontent.com/crehmann/Esp32GpsLogger/master/assets/breadboard.png)  

## Battery Life
First test have shown a battery life of around 32 hours with the 3000mAh battery if logging the position every 10s (without Deep Sleep).

## GPX Conversion
A simple CSV file is used for logging the GPS coordinates. To use the logs with other programs or to visualize it better, it can be converted into the GPX format. The F# console application from the tools directory of this repository can be used for this.

## Todo
* Measure battery life when using the Deep Sleep mode
* Improve battery power by using the low power mode of the L76X GPS module
* Implement a more soffisticated Deep Sleep algorithm do improve battery life even further.
* ...