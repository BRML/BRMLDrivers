(*** hide ***)
#load "../../BRMLDrivers.fsx"

(**

Generic data recorder
=====================

The purpose of the generic data recorder is to record sample-based data of small to moderate sample size from multiple sources (usually sensors) with moderate sampling rates (about 1 kHz).
It is assumed that sensor samples are obtained independently, i.e. without hardware synchronization during acquisition, and with possibly different sampling rates.
The library further assumes that the involved sensors do not produce time stamps; hence the high-precision timer of the host is used for time stamping.
After recording a common timeline with all sensors synchronized onto a common sampling rate using linear interpolation can be computed.


Sensor interface
----------------

The data recorder can obtain samples from any object that implements the `ISensor<'T>` [interface](https://fsharpforfunandprofit.com/posts/interfaces/) where `'T` is the data type of the sample and can be an arbitrary type (for example `float []`).
Note that your sensor does not have to correspond to a hardware sensor.
In the context of this document a sensor can be anything, that periodically produces data samples.

This sensor interface is defined as follows

    type SampleRecorder.ISensor<'T> = 
        abstract DataType : Type
        abstract SampleAcquired : IEvent<'T>
        abstract Interpolate : fac: float -> a: 'T -> b: 'T -> 'T

To make your sensor compatible with the data recorder, implement this interface.
The `DataType` property must return `typeof<'T>`.
`SampleAcquired` must be an event that your sensor raises each time a *new* sample is acquired.
The argument of the event must be the sample itself.
The `Interpolate` method is used for computing a common timeline of multiple sensors after data acquisition is finished.
Your implementation of this method must perform the equivalent of linear interpolation for your sample data type between the two samples `a` and `b` using the interpolation factor `fac` according to the formula: `(1 - fac) * a + fac * b`.

### Example

In this section we will write a simple sensor that measures CPU utilization and available memory at a fixed sampling interval.
We first define a record type for our sample of type `LoadSample`.
*)

type LoadSample = {
    CPU:    float   // CPU load in percent
    Memory: int     // available memory in MB
}

(**
Then we write code for the sensor type and implement the ISensor<'T> interface.
*)

open System.Timers
open System.Diagnostics

type LoadSensor (samplingInterval: float) =
    // performance counters to obtain load data
    let cpuPerfCounter = new PerformanceCounter ("Processor", "% Processor Time", "_Total")
    let memPerfCounter = new PerformanceCounter ("Memory", "Available MBytes")

    // event that will be triggered for every sample
    let sampleAcquiredEvent = new Event<LoadSample> ()

    let acquireSample _ =
        // acquire a sample
        let smpl = {
            CPU    = float (cpuPerfCounter.NextValue ())
            Memory = int (memPerfCounter.NextValue ())
        }
        // and trigger the event with the sample as argument
        sampleAcquiredEvent.Trigger smpl

    // create and start sampling timer
    let samplingTimer = new Timer (samplingInterval)
    do 
        samplingTimer.AutoReset <- true
        samplingTimer.Elapsed.Add acquireSample            
        samplingTimer.Start ()

    // sensor interface
    interface SampleRecorder.ISensor<LoadSample> with
        member this.DataType = typeof<LoadSample>
        member this.SampleAcquired = sampleAcquiredEvent.Publish
        member this.Interpolate fac a b = {
            CPU    = (1. - fac) * a.CPU + b.CPU
            Memory = int ((1. - fac) * float a.Memory + float b.Memory)
        }

    interface System.IDisposable with
        member this.Dispose () = 
            samplingTimer.Dispose ()
            cpuPerfCounter.Dispose ()
            memPerfCounter.Dispose ()

(**
Let us test our sensor by instantiating it with a sampling interval of 50 ms.
*)
let ls = new LoadSensor (50.)


