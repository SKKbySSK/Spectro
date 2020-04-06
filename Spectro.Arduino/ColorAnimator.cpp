#include "Arduino.h"

#ifndef __COLOR__
#define __COLOR__
#include "Color.cpp"
#endif

extern  unsigned long timer0_millis;

class ColorAnimator {
private:
  int time;
  float durationMs;
  unsigned long offset, position;
  int dr, dg, db, r, g, b;
  Color color, from, to;

public:
  float Progress;

  ColorAnimator(Color from, Color to, float durationMs) {
    this->from = from;
    this->to = to;
    this->durationMs = durationMs;
  }

  void Start() {
    offset = millis();
    position = 0;
    Progress = 0;
    color = from;
  }

  Color Update() {
    position = millis() - offset;
    Progress = position / durationMs;
    Progress = min(1, Progress);

    dr = to.r - from.r;
    dg = to.g - from.g;
    db = to.b - from.b;
    r = from.r + (int)(dr * Progress);
    g = from.g + (int)(dg * Progress);
    b = from.b + (int)(db * Progress);
    color = Color(r, g, b);
    return color;
  }
};
