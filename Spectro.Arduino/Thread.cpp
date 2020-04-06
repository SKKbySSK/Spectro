#include "Arduino.h"
#include "Arduino_FreeRTOS.h"
#include <string.h>

class Thread {
private:
  char* name;
  TaskHandle_t handle;
  bool running = false;

  static void RunStaticInternal(void* pvParameters) {
    Thread* t = (Thread*)pvParameters;
    t->RunInternal();
  }

  void RunInternal() {
    Run();
    running = false;
    vTaskDelete(handle);
  }

public:
  bool enableOutput = true;

  Thread(char* name) {
    this->name = name;
  }

  void Start() {
    if (running) {
      return;
    }
    running = true;
    xTaskCreate(RunStaticInternal, name, 128, this, 2, &handle);
  }

  void Delay(int millis) {
    vTaskDelay(millis / portTICK_PERIOD_MS );
  }

protected:
  virtual void Run() {}

  void println(char* value) {
    if (!enableOutput) {
      return;
    }
    char* buffer = malloc(strlen(value) + 4);
    sprintf(buffer, "[%s] ", name);
    Serial.write(buffer);
    Serial.println(value);
    Serial.flush();
    free(buffer);
  }

  void print(char* value) {
    if (!enableOutput) {
      return;
    }
    char* buffer = malloc(strlen(value) + 4);
    sprintf(buffer, "[%s] ", name);
    Serial.write(buffer);
    Serial.write(value);
    Serial.flush();
    free(buffer);
  }
};
