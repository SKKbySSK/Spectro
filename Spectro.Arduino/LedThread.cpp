
#ifndef __THREAD__
#define __THREAD__
#include "Thread.cpp"
#endif


class LedThread: public Thread {
private:
  bool playAnimation;

public:
  LedThread(): Thread("LED_THREAD") {
  }

  void Stop() {
    playAnimation = false;
  }

  void Toggle() {
    playAnimation = !playAnimation;
    if (playAnimation) {
      Start();
    } else {
      Stop();
    }
  }

protected:
  void Run() override {
    println("Started");
    playAnimation = true;
    while(playAnimation) {
      println("LED");
      Delay(1000);
    }

    println("Finished");
  }
};
