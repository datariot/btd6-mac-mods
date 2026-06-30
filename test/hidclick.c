// HID-level mouse click + key press via CoreGraphics CGEvent — Unity honors these, unlike
// AppKit System Events "click at". Usage: hidclick <x> <y>   (click) ; posts click + space + return.
#include <ApplicationServices/ApplicationServices.h>
#include <unistd.h>

static void click(double x, double y) {
    CGPoint p = CGPointMake(x, y);
    CGEventRef move = CGEventCreateMouseEvent(NULL, kCGEventMouseMoved, p, kCGMouseButtonLeft);
    CGEventPost(kCGHIDEventTap, move); CFRelease(move); usleep(40000);
    CGEventRef down = CGEventCreateMouseEvent(NULL, kCGEventLeftMouseDown, p, kCGMouseButtonLeft);
    CGEventPost(kCGHIDEventTap, down); CFRelease(down); usleep(40000);
    CGEventRef up = CGEventCreateMouseEvent(NULL, kCGEventLeftMouseUp, p, kCGMouseButtonLeft);
    CGEventPost(kCGHIDEventTap, up); CFRelease(up); usleep(40000);
}

static void key(CGKeyCode k) {
    CGEventRef d = CGEventCreateKeyboardEvent(NULL, k, true);
    CGEventPost(kCGHIDEventTap, d); CFRelease(d); usleep(30000);
    CGEventRef u = CGEventCreateKeyboardEvent(NULL, k, false);
    CGEventPost(kCGHIDEventTap, u); CFRelease(u); usleep(30000);
}

int main(int argc, char** argv) {
    double x = argc > 1 ? atof(argv[1]) : 704;
    double y = argc > 2 ? atof(argv[2]) : 440;
    click(x, y);
    key(49); // space
    key(36); // return
    return 0;
}
