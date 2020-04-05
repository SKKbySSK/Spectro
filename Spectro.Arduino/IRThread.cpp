
#ifndef __THREAD__
#define __THREAD__
#include "Thread.cpp"
#endif

#include "IRDecoder.cpp"

class IRThread: public Thread {
private:
  int pin;
  IRDecoder* decoder;
  void (*callback)(IRControlKey);

  static void handle_ir_event(void* param, IRControlKey key, bool repeat) {
    IRThread* thread = (IRThread*)param;
    thread->handle_event(key, repeat);
  }

  void handle_event(IRControlKey key, bool repeat) {
    if (repeat) {
      callback(key);
    } else {
      callback(key);
      Delay(250);
    }
  }

public:
  IRThread(int pin, void (*callback)(IRControlKey)): Thread("IR_THREAD") {
    this->pin = pin;
    this->callback = callback;
    decoder = new IRDecoder(pin, this, handle_ir_event);
  }

protected:
  void Run() override {
    while(true) {
      decoder->decodeIr();
      Delay(50);
    }
  }
};
