(*** hide ***)
#load "../../BRMLDrivers.fsx"

(**

BioTac SP driver
================

This driver reads samples of tactile data from a [Syntouch BioTac SP sensor](http://www.syntouchllc.com/Products/BioTac-SP/) conntected to the PC using a [Cheetah SPI/USB adapter](http://www.totalphase.com/products/cheetah-spi/).

Currently only reading out a single Biotac sensor is supported.

Installation
------------
Make sure that the [system driver](http://www.totalphase.com/products/usb-drivers-windows/) for the Cheetah SPI/USB adapter is properly installed.

Driver configuration
--------------------
Instantiating the driver requires providing a configuration record of type `BRML.Drivers.BioTacCfgT`.
*)
open BRML.Drivers

let cfg = {
    Cheetah        = uint32 1364033083   // adjust
    Index          = 0                   // adjust
}
(**
`Cheetah` is the unique id (UID) of the Cheetah SPI/USB adapter the BioTac sensor is connected to.
`Index` is the port the BioTac sensor is connected to on the multiplexer board. It can be 0, 1 or 2.


Driver instance
---------------
We can now instantiate the driver.
*)
let biotac = new BiotacT (cfg)
(**

After power on and initialization the driver starts to stream samples from the BioTac sensor at the highest possible sampling rate.

Reading the latest sensor sample
--------------------------------
The latest acquired sample is available in the `biotac.CurrentSample` property.
*)
let cs = biotac.CurrentSample
(**
If you intend to do machine learning with the tactile sensor data, you are probably interested in the flat sensor data.
It is available in the `cs.Flat` field.
*)
printfn "Current sensor data:\n%A" cs.Flat

(**

Event-based sample acquisition
------------------------------
The driver also provides an event-based interface to obtain samples.
Subscribe to the `biotac.SampleAcquired` event and you will be notified for each newly acquired sample.
This is the recommended method of processing the sample stream.
*)
biotac.SampleAcquired.Add (fun smpl ->
    printfn "Acquired sensor sample:\n%A" smpl.Flat)

(*** hide ***)
System.Threading.Thread.Sleep 2000

(**
This code will automatically print each new sample as it is acquired.

Disposing
---------
Dispose the driver instance after usage.
*)
biotac.Dispose ()
(**
This will turn off power to the sensor and release the Cheetah SPI/USB adapter.
*)



