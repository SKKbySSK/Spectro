#include "Arduino.h"
#include "Arduino_FreeRTOS.h"
#include "IRThread.cpp"
#include "LedThread.cpp"
#include "TriColorLedThread.cpp"
#include <stdio.h>
#include "MemoryFree.h"
#include "semphr.h"

#ifndef __COLOR__
#define __COLOR__
#include "Color.cpp"
#endif

const int IR_RECV_PIN = 12;
bool _logOut = true;
int color = 0;
LedThread* _ledThread;
IRThread* _irThread;
TriColorLedThread* _triLedThread;
ColorAnimator* _animator = nullptr;
SemaphoreHandle_t xMutex = NULL;

void handle_key(IRControlKey key) {
  xSemaphoreTake(xMutex, portMAX_DELAY);
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
    _triLedThread->enableOutput = _logOut;
    break;

  case Ch:
    ColorAnimator* anim = _animator;
    _animator = nullptr;
    if (anim != nullptr) {
      free(anim);
    }

    Color from = _triLedThread->color;
    switch (color++ % 3) {
    case 0:
      Serial.println("Red!");
      _animator = new ColorAnimator(from, Color(255, 0, 0), 500);
      break;
    case 1:
      Serial.println("Green!");
      _animator = new ColorAnimator(from, Color(0, 255, 0), 500);
      break;
    case 2:
      Serial.println("Blue!");
      _animator = new ColorAnimator(from, Color(0, 0, 255), 500);
      break;
    }
    _animator->Start();
    break;

  default:
    break;
  }
  xSemaphoreGive(xMutex);
}

void setup() {
  xMutex = xSemaphoreCreateMutex();
  xSemaphoreTake(xMutex, portMAX_DELAY);
  Serial.begin(9600);
  _ledThread = new LedThread();
  _irThread = new IRThread(IR_RECV_PIN, handle_key);
  _triLedThread = new TriColorLedThread(9, 10, 11);
  _triLedThread->Start();
  _irThread->Start();
  xSemaphoreGive(xMutex);
}

void freeRam () {
  Serial.print("Free ram: ");
  Serial.println(freeMemory());

}

void loop() {
  xSemaphoreTake(xMutex, portMAX_DELAY);
  if (_animator != nullptr) {
    _triLedThread->SetColor(_animator->Update());
    if (_animator->Progress >= 1) {
      free(_animator);
      _animator = nullptr;
    }
  }

  delay(50);
  xSemaphoreGive(xMutex);
}
