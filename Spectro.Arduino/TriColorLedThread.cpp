
#ifndef __THREAD__
#define __THREAD__
#include "Thread.cpp"
#endif
#include "Arduino.h"

#ifndef __COLOR__
#define __COLOR__
#include "Color.cpp"
#endif

#ifndef __COLOR_ANIM__
#define __COLOR_ANIM__
#include "ColorAnimator.cpp"
#endif

class TriColorLedThread: public Thread {
private:
  int r, g, b;

public:
  Color color = Color(0, 0, 0);

  TriColorLedThread(int redPin, int greenPin, int bluePin): Thread("TRI_LED_THREAD") {
    r = redPin;
    g = greenPin;
    b = bluePin;
    pinMode(r, OUTPUT);
    pinMode(g, OUTPUT);
    pinMode(b, OUTPUT);
    analogWrite(r, color.r);
    analogWrite(g, color.g);
    analogWrite(b, color.b);
  }

  void SetColor(Color to) {
    color = to;
  }

protected:
  void Run() override {
    while(true) {
      analogWrite(r, color.r);
      analogWrite(g, color.g);
      analogWrite(b, color.b);
      Delay(100);
    }
  }
};
