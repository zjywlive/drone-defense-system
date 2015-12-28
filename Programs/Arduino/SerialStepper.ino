#include <AccelStepper.h>

AccelStepper pan(1, 2, 3);
AccelStepper tilt(1, 5, 6);

void setup(){

  pan.setMaxSpeed(125);
  tilt.setMaxSpeed(250);
  pan.setAcceleration(5000);
  tilt.setAcceleration(5000);
  pinMode(2, OUTPUT);
  pinMode(3, OUTPUT);
  pinMode(5, OUTPUT);
  pinMode(6, OUTPUT);
  Serial.begin(9600);
}

void loop(){
  if(Serial.available() > 0)
  {
    char c = Serial.read();
    if(c == 'D')
    {
      tilt.move(25);
      tilt.runToPosition();
    }
    else if(c == 'U')
    {
      tilt.move(-25);
      tilt.runToPosition();
    }
    else if(c == 'L')
    {
      pan.move(8);
      pan.runToPosition();
    }
    else if(c == 'R')
    {
      pan.move(-8);
      pan.runToPosition();
    }
    else if(c == 'P')
    {
      tilt.moveTo(250);
      tilt.runToPosition();
    }
    Serial.println("K"); //all done
  }

}
