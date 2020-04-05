//
// Created by Kaisei Sunaga on 2020/04/05.
//

#include "IRremote.h"

enum IRControlKey {
    Unknown,
    ChDown, Ch, ChUp,
    Prev, Next, PlayPause,
    Down, Up, EQ,
    Zero, Hundred, TwoHundred,
    One, Two, Three,
    Four, Five, Six,
    Seven, Eight, Nine,
    Repeat,
};

class IRDecoder {
private:
    IRrecv* recv;
    decode_results results = {};
    IRControlKey lastKey = Unknown;
    void* parameter;
    void (*callback)(void*, IRControlKey, bool);
    
    void keyPressed(IRControlKey key) {
        if (key == Repeat) {
            callback(parameter, lastKey, true);
        } else {
            lastKey = key;
            callback(parameter, key, false);
        }
    }

public:
    IRDecoder(const int pin, void* parameter, void (*callback)(void*, IRControlKey, bool)) {
        recv = new IRrecv(pin);
        recv->enableIRIn();
        this->parameter = parameter;
        this->callback = callback;
    }

    void decodeIr() {
        if (recv->decode(&results)) {
            // https://gist.github.com/steakknife/e419241095f1272ee60f5174f7759867
            switch (results.value) {
                case 0xFFA25D:
                    keyPressed(ChDown);
                    break;
                case 0xFF629D:
                    keyPressed(Ch);
                    break;
                case 0xFFE21D:
                    keyPressed(ChUp);
                    break;
                case 0xFF22DD:
                    keyPressed(Prev);
                    break;
                case 0xFF02FD:
                    keyPressed(Next);
                    break;
                case 0xFFC23D:
                    keyPressed(PlayPause);
                    break;
                case 0xFFE01F:
                    keyPressed(Down);
                    break;
                case 0xFFA857:
                    keyPressed(Up);
                    break;
                case 0xFF906F:
                    keyPressed(EQ);
                    break;
                case 0xFF6897:
                    keyPressed(Zero);
                    break;
                case 0xFF9867:
                    keyPressed(Hundred);
                    break;
                case 0xFFB04F:
                    keyPressed(TwoHundred);
                    break;
                case 0xFF30CF:
                    keyPressed(One);
                    break;
                case 0xFF18E7:
                    keyPressed(Two);
                    break;
                case 0xFF7A85:
                    keyPressed(Three);
                    break;
                case 0xFF10EF:
                    keyPressed(Four);
                    break;
                case 0xFF38C7:
                    keyPressed(Five);
                    break;
                case 0xFF5AA5:
                    keyPressed(Six);
                    break;
                case 0xFF42BD:
                    keyPressed(Seven);
                    break;
                case 0xFF4AB5:
                    keyPressed(Eight);
                    break;
                case 0xFF52AD:
                    keyPressed(Nine);
                    break;
                case 0xFFFFFFFF:
                    keyPressed(Repeat);
                    break;
                default:
                    keyPressed(Unknown);
                    break;
            }

            recv->resume();
        }
    }
};
