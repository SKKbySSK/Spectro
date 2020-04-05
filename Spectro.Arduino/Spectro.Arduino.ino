#include "Arduino.h"
#include "Arduino_FreeRTOS.h"
#include "IRThread.cpp"
#include "LedThread.cpp"
#include "Color.cpp"
#include <stdio.h>

const int IR_RECV_PIN = 12;
bool _logOut = true;
LedThread* _ledThread;
IRThread* _irThread;

void handle_key(IRControlKey key) {
  switch (key)
  {
  case PlayPause:
     _ledThread->Toggle();
    break;

  case EQ:
    _logOut = !_logOut;
    if (_logOut) {
      Serial.println("[LOG] Enabled");
    } else {
      Serial.println("[LOG] Disabled");
    }
    _ledThread->enableOutput = _logOut;
    _irThread->enableOutput = _logOut;
    break;

  default:
    break;
  }
}

void setup() {
  Serial.begin(9600);
  _ledThread = new LedThread();
  _irThread = new IRThread(IR_RECV_PIN, handle_key);
  _irThread->Start();
}

void loop() {
}
