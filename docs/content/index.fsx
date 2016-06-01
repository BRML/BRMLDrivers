(**
BRML Drivers and Data Recorder
==============================

This F# library contains drivers for lab equipment and self-built robots at BRML labs.
It also contains a generic sample recorder class for obtaining time-synchronized samples from arbitrary sensors.

Currently the following hardware is supported:

  * [Linmot](http://www.linmot.com/products/linear-motors/) using the [LinRS serial protocol](http://www.linmot.com/fileadmin/user_upload/Downloads/software-firmware/servo-drives/linmot-talk-6/Usermanual_LinRS_e_recent.pdf)
  * [Movtec Wacht XY-table](http://www.movtec.de/positioniersysteme/2-achsen-system.html) using [Nanotec stepper motor controller SMCI33](http://en.nanotec.com/products/1034-smci33-stepper-motor-controller-with-closed-loop-controller/) or compatible serial interface
  * [Syntouch BioTac SP sensor](http://www.syntouchllc.com/Products/BioTac-SP/) using a [Cheetah SPI USB adapter](http://www.totalphase.com/products/cheetah-spi/)



Contributing and copyright 
--------------------------

The project is hosted on [GitHub][gh] where you can [report issues][issues], fork the project and submit pull requests. 
The library is available under the [Apache License 2.0][license], which allows modification and redistribution for both commercial and non-commercial purposes.  

  [gh]: https://github.com/BRML/BRMLDrivers
  [issues]: https://github.com/BRML/BRMLDrivers/issues
  [license]: https://github.com//BRML/BRMLDrivers/blob/master/LICENSE
*)
