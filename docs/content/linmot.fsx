(*** hide ***)
#load "../../BRMLDrivers.fsx"

(**

Linmot Driver
=============

This driver support controlling a [Linmot](http://www.linmot.com/products/linear-motors/) using the [LinRS serial protocol](http://www.linmot.com/fileadmin/user_upload/Downloads/software-firmware/servo-drives/linmot-talk-6/Usermanual_LinRS_e_recent.pdf).
It has been tested using a [Linmot E1250-EC-UC drive](http://shop.linmot.com/D/ag7000.e12/drives-f%C3%BCr-motoren-p01-&-pr01/serie-e1200/e1250-ec-uc.htm), but should be compatible with more drives that support the LinRS protocol.
In our setup a standard RS232-to-USB adapter is used to connect the serial port of the drive to a PC.

The driver is provided by the `BRML.Drivers.LinmotT` type.
An instance of this type is fully thread-safe.
Movement requests will be queued and executed in arrival order.


Drive parameter configuration
-----------------------------

Using the LinMot-Talk software the LinRS drive parameters must be configured as follows:

  * LinRS -> Dis-/Enable: Enable
  * LinRS -> RS Config -> Stop Bit: 1
  * LinRS -> RS Config -> Parity: None
  * LinRS -> Protocol Config -> MACID: as desired
  * LinRS -> Protocol Config -> Checksum: None
  * LinRS -> Protocol Config -> MC Response Configuration: Enable the following: Communication State, Status Word, State Var, Monitoring Channel 1. Disable all others.
  * LinRS -> Protocol Config -> MC Response Configuration -> Channel 1 UPID: 1B8Dh (Actual Position) 

Note the value of the variable LinRS -> MACID as you will need to specify it in the driver configuration.


Driver configuration
--------------------
Instantiating the driver requires providing a configuration record of type `BRML.Drivers.LinmotCfgT`.
*)
open BRML.Drivers

let cfg = {
    PortName       = "COM6"    // adjust
    PortBaud       = 57600     // adjust
    Id             = 0x11      // adjust
    DefaultVel     = 50.0
    DefaultAccel   = 200.0    
}
(**
`PortName` is the serial port name.
`PortBaud` must match the baud rate configured in the drive, using the LinRS -> RS Config -> Baud Rate parameters.
`Id` must match the drive variable LinRS -> MACID.
`DefaultVel` specifies the default movement velocity in $\mathrm{mm}/\mathrm{s}$.
`DefaultAccel` specifies the default acceleration and deceleration in $\mathrm{mm}/\mathrm{s^2}$.

Driver instance
---------------
We can now instantiate the driver.
*)
let linmot = new LinmotT (cfg)
(**
Initialization and checking of communication is performed automatically.

All driver instance methods return an `Async<_>` instance.
You can perform [asynchronous control](https://fsharpforfunandprofit.com/posts/concurrency-async-and-parallel/) or pipe all results into the `Async.RunSynchronously` to perform immediate command execution and wait for the operation to finish before your program continues.

Homing
------
The drive must be homed (finding the zero reference position) before movement commands can be send.
Use the `linmot.Home` method for that.
By default, homing is only performed if the drive is currently not homed.
You can override this behavior and force homing, by specifying an additional parameter with the value true.
*)
linmot.Home () |> Async.RunSynchronously
(**

Getting the current position
----------------------------
The current position in $\mathrm{mm}$ is available in the `linmot.Pos` property.
*)
printfn "The Linmot is currently at %.3f mm." linmot.Pos
(**

Moving
------
Use the `linmot.DriveTo` method to move the Linmot to the specified position in $\mathrm{mm}$.
*)
linmot.DriveTo -10. (* mm *) |> Async.RunSynchronously
(**
You can specify additional parameters corresponding to the velocity and acceleration. 
*)
linmot.DriveTo (-20. (* mm *), 10. (* mm/s *), 100. (* mm/s^2 *)) |> Async.RunSynchronously
(**

Power
-----
You can turn motor power off, by calling the `linmot.Power` method with a `false` argument.
*)
linmot.Power false |> Async.RunSynchronously
(**
The slider can now move freely.
You can still use the `linmot.Pos` property to obtain the actual position.

Specify a `true` argument to `linmot.Power` to re-enable the motor.
*)
linmot.Power true |> Async.RunSynchronously
(**
It is *not* necessary to home the motor again after toggling power off and then on.


Close the driver
----------------
You should dispose the driver instance when not using it anymore.
*)
linmot.Dispose()
(**
This will close the serial port.
*)


