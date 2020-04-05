//
// Created by Kaisei Sunaga on 2020/04/05.
//

struct Color {
public:
  Color(int r, int g, int b) {
    this->r = r;
    this->g = g;
    this->b = b;
  }

  Color(long hexValue) {
    this->r = (hexValue >> 16) & 0xFF;
    this->g = (hexValue >> 8) & 0xFF;
    this->b = hexValue & 0xFF;
  }

  int r;
  int g;
  int b;
};
