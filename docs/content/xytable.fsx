(*** hide ***)
#load "../../BRMLDrivers.fsx"

(**

Movtec XY table driver
======================

This driver controls a [Movtec Wacht XY-table](http://www.movtec.de/positioniersysteme/2-achsen-system.html) using a [Nanotec stepper motor controller SMCI33](http://en.nanotec.com/products/1034-smci33-stepper-motor-controller-with-closed-loop-controller/) or compatible serial interface.

Driver configuration
--------------------
Instantiating the driver requires providing a configuration record of type `BRML.Drivers.BioTacCfgT`.
*)
open BRML.Drivers

let cfg : XYTableCfgT = {
    PortName       = "COM5"   
    PortBaud       = 115200
    X              = { StepperConfig = {Id=1; AnglePerStep=1.8; StepMode=8; StartVel=1000.}
                       DegPerMM      = 360. / 1.25
                       Home          = Stepper.Right
                       MaxPos        = 147. }
    Y              = { StepperConfig = {Id=2; AnglePerStep=1.8; StepMode=8; StartVel=1000.}
                       DegPerMM      = 360. / 1.25
                       Home          = Stepper.Left
                       MaxPos        = 140. }
    DefaultVel     = 30.
    DefaultAccel   = 30.
    HomeVel        = 10.    
}
(**
`PortName` is the serial port the drive is connected to, and `PortBaud` is the corresponding baud rate.
Then the parameters of the X and Y axes follow.
They are described as follows.
`Id` is the motor id of the axis.
`AnglePerStep` is the angle in degree per motor step.
`StepMode` is the stepping mode of the motor.
`StartVel` is the initial velocity when starting the motor in steps per second.
`DegPerMM` is how many degrees the motor must turn to move the table by $1\mathrm{mm}$.
`Home` is the direction of the reference switch and can be `Stepper.Left` or `Stepper.Right`.
`MaxPos` is the maximum reachable position in $\mathrm{mm}$.
`DefaultVel` is the default movement velocity in $\mathrm{mm}/\mathrm{s}$.
`DefaultAccel` is the default acceleration and deceleration in $\mathrm{mm} / \mathrm{s}^2$.
`HomeVel` is the homing velocity in $\mathrm{mm}/\mathrm{s}$.

Driver instance
---------------
We can now instantiate the driver.
*)
let tbl = new XYTableT (cfg)
(**

Initialization and checking of communication is performed automatically.
After initialization the driver starts to stream position samples from the XY table.

All driver instance methods return an `Async<_>` instance.
You can perform [asynchronous control](https://fsharpforfunandprofit.com/posts/concurrency-async-and-parallel/) or pipe all results into the `Async.RunSynchronously` to perform immediate command execution and wait for the operation to finish before your program continues.

Homing
------
The XY table must be homed (finding the zero reference positions) before it can be moved.
Use the `tbl.Home` method for that.
Homing is only performed, if the XY table is currently not homed.
*)
tbl.Home () |> Async.RunSynchronously
(**

Reading the current position
----------------------------
The current position is available in the `tbl.CurrentPos` property.
It is a tuple of X and Y positions in $\mathrm{mm}$
*)
let x, y = tbl.CurrentPos
printfn "The table is currently at X=%.3f mm and Y=%.3f mm." x y

(**

Event-based position acquisition
--------------------------------
The driver also provides an event-based interface to obtain position samples.
Subscribe to the `tbl.PosAcquired` event and you will be notified for each newly acquired sample.
This is the recommended method if you want to record the position of the XY table or perform closed-loop control (using external sensors).
*)
tbl.PosAcquired.Add (fun (x, y) ->
    printfn "Acquired position sample: X=%.3f mm   Y=%.3f mm" x y)

(*** hide ***)
System.Threading.Thread.Sleep 2000

(**
This code will automatically print each new sample as it is acquired.

You can adjust the position sampling interval by setting the `tbl.PosReportInterval` property to the desired sampling interval in $\mathrm{ms}$, but changing this property is usually not necessary.


Moving to a target position
---------------------------
To move to a target position with a fixed velocity and linear acceleration and deceleration ramps use the `tbl.DriveTo` method.
*)
tbl.DriveTo ((50. (* mm *), 80. (* mm *)))
(**
The first argument is the target XY position in $\mathrm{mm}$ as a tuple.

You can specify additional arguments for the movement velocity, acceleration and deceleration.
If they are omitted, the default values from the configuration are used.
*)
tbl.DriveTo ((10. (* mm *), 10. (* mm *)), (5. (* mm/s *), 5. (* mm/s *)), (10. (* mm/s^2 *), 10. (* mm/s^2 *)))

(**

Moving with a set velocity
--------------------------
It is also possible to directly control the movement velocity of the XY table.



*)



(**
Disposing
---------
Dispose the driver instance after usage.
*)
//tbl.Dispose ()
(**
This will stop the table and release the serial port.
*)



