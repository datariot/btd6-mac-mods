#include <ApplicationServices/ApplicationServices.h>
#include <unistd.h>
static void post(CGEventType t, CGPoint p){ CGEventRef e=CGEventCreateMouseEvent(NULL,t,p,kCGMouseButtonLeft); CGEventPost(kCGHIDEventTap,e); CFRelease(e); }
int main(int argc,char**argv){
  double x1=atof(argv[1]),y1=atof(argv[2]),x2=atof(argv[3]),y2=atof(argv[4]);
  post(kCGEventMouseMoved, CGPointMake(x1,y1)); usleep(400000);
  post(kCGEventLeftMouseDown, CGPointMake(x1,y1)); usleep(400000);   // long hold so pickup registers
  int N=40; for(int i=1;i<=N;i++){ double t=(double)i/N; double x=x1+(x2-x1)*t, y=y1+(y2-y1)*t; post(kCGEventLeftMouseDragged, CGPointMake(x,y)); usleep(25000);} 
  usleep(400000);                                                    // settle at target so Input.mousePosition updates
  post(kCGEventLeftMouseUp, CGPointMake(x2,y2)); usleep(120000);
  return 0;
}
