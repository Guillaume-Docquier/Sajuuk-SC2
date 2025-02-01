# Philosophies

## Cells and Positions
CP1:   A Vector2 is to be used when the height does not matter.  

CP2:   A Position is any coordinate in the world.

CP3:   A Cell is a 2D Position represented by its corner.  
CP3.1: A Cell center is to be used only when required (like when drawing).  

## Trackers and GameState
TGS1:   The GameState is only to be used by Trackers and acts as an abstraction layer between SC2 and the program to facilitate testing.  
TGS1.1: The Bot should only read "state" from Trackers to reduce coupling with SC2 and facilitate testing.  
