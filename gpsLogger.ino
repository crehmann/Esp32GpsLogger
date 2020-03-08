#include <sstream>
#include <string>
#include <TinyGPS++.h>
#include <HardwareSerial.h>
#include "DFRobot_IL3895_SPI.h"
#include "FS.h"
#include "SD.h"
#include "SPI.h"

HardwareSerial SerialGPS(1);
DFRobot_IL3895_SPI epaper;
TinyGPSPlus gps;

uint32_t nextSerialTaskTs = 0;
bool sdCardInitialized = false;

#define SERIAL_BAUD 115200

#define SD_CARD_CS 25

#define GPS_BAUD 9600
#define GPS_RX 17
#define GPS_TX 16

#define EPAPER_CS 26
#define Font_CS 10
#define EPAPER_DC 5
#define EPAPER_BUSY 13

#define TASK_SERIAL_RATE 10 * 1000

bool initializeSdCard()
{
  if (!SD.begin(SD_CARD_CS))
  {
    Serial.println("Card Mount Failed");
    return false;
  }
  else
  {
    uint8_t cardType = SD.cardType();

    Serial.print("SD Card Type: ");
    if (cardType == CARD_MMC)
    {
      Serial.println("MMC");
    }
    else if (cardType == CARD_SD)
    {
      Serial.println("SDSC");
    }
    else if (cardType == CARD_SDHC)
    {
      Serial.println("SDHC");
    }
    else
    {
      Serial.println("UNKNOWN");
    }
    return true;
  }
}

void appendFile(fs::FS &fs, const char *path, const char *message)
{
  Serial.printf("Appending to file: %s\n", path);

  File file = fs.open(path, FILE_APPEND);
  if (!file)
  {
    Serial.println("Failed to open file for appending");
    return;
  }
  if (file.print(message))
  {
    Serial.println("Message appended");
  }
  else
  {
    Serial.println("Append failed");
  }
  file.close();
}

void logGps()
{
  char timestamp[17];
  sprintf(timestamp, "%04d-%02d-%02dT%02d:%02d:%02dZ",
          gps.date.year(),
          gps.date.month(),
          gps.date.day(),
          gps.time.hour(),
          gps.time.minute(),
          gps.time.second());
  const char *lat = String(gps.location.lat(), 8).c_str();
  const char *lng = String(gps.location.lng(), 8).c_str();

  std::stringstream logLine;
  logLine << "\"" << timestamp << "\";"
          << "\"" << lat << "\";"
          << "\"" << lng << "\";"
          << "\"" << gps.altitude.value() << "\";"
          << "\"" << String(gps.hdop.value(), 2).c_str() << "\";"
          << "\"" << gps.satellites.value() << "\"\r\n";

  appendFile(SD, "/gps.log", logLine.str().c_str());
  Serial.println(logLine.str().c_str());
}

void setup()
{
  Serial.begin(SERIAL_BAUD);
  sdCardInitialized = initializeSdCard();
  SerialGPS.begin(GPS_BAUD, SERIAL_8N1, GPS_RX, GPS_TX);
  Serial.println("Started");

  epaper.begin(EPAPER_CS, Font_CS, EPAPER_DC, EPAPER_BUSY);
  epaper.fillScreen(WHITE);
  epaper.flush(FULL);
}

void loop()
{
  while (SerialGPS.available() > 0)
  {
    gps.encode(SerialGPS.read());
  }

  if (nextSerialTaskTs < millis() && gps.satellites.value() > 3)
  {
    nextSerialTaskTs = millis() + TASK_SERIAL_RATE;
    logGps();
  }
}