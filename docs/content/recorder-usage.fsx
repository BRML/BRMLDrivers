(*** hide ***)
#load "../../BRMLDrivers.fsx"

(**

Generic data recorder
=====================

The purpose of the generic data recorder is to record sample-based data of small to moderate sample size from multiple sources (usually sensors) with moderate sampling rates (about 1 kHz).
It is assumed that sensor samples are obtained independently, i.e. without hardware synchronization during acquisition, and with possibly different sampling rates.
The library further assumes that the involved sensors do not produce time stamps; hence the high-precision timer of the host is used for time stamping.
After recording a common timeline with all sensors synchronized onto a common sampling rate using linear interpolation is computed.

You can run the example presented in this document by cloning the Git repository and executing `FsiAnyCPU.exe docs/content/recorder-usage.fsx`.

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
`SampleAcquired` must be an [event](https://en.wikibooks.org/wiki/F_Sharp_Programming/Events) that your sensor raises each time a *new* sample is acquired.
The argument of the event must be the sample itself.
The `Interpolate` method is used for computing a common timeline of multiple sensors after data acquisition is finished.
Your implementation of this method must perform the equivalent of linear interpolation for your sample data type between the two samples `a` and `b` using the interpolation factor `fac` according to the formula: `(1 - fac) * a + fac * b`.

### Example sensor measuring performance counters

In this section we will write a simple sensor that queries a [Windows performance counter](https://msdn.microsoft.com/en-us/library/windows/desktop/aa373083(v=vs.85).aspx) (for example CPU utilization or available memory) at a fixed sampling interval.
Performance counter measurements have the data type `float32`.
For the sake of decency we define a type alias. 
*)
type PerfSample = float32
(**
Then we write code for the sensor type and implement the ISensor<'T> interface.
*)

open System.Timers
open System.Diagnostics

type PerfSensor (categoryName:      string, 
                 counterName:       string,
                 instanceName:      string,
                 samplingInterval:  float) =

    // performance counter
    let perfCounter = 
        new PerformanceCounter (categoryName, counterName, instanceName)

    // event that will be triggered for every sample
    let sampleAcquiredEvent = new Event<PerfSample> ()

    let acquireSample _ =
        // acquire a sample
        let smpl = perfCounter.NextValue ()
        // and trigger the event with the sample as argument
        sampleAcquiredEvent.Trigger smpl

    // create and start sampling timer
    let samplingTimer = new Timer (samplingInterval)
    do 
        samplingTimer.AutoReset <- true
        samplingTimer.Elapsed.Add acquireSample            
        samplingTimer.Start ()

    // sensor interface
    interface SampleRecorder.ISensor<PerfSample> with
        member this.DataType = typeof<PerfSample>
        member this.SampleAcquired = sampleAcquiredEvent.Publish
        member this.Interpolate fac a b = 
            float32 (1. - fac) * a + float32 fac * b

    interface System.IDisposable with
        member this.Dispose () = 
            samplingTimer.Dispose ()
            perfCounter.Dispose ()

(**
The `samplingTimer` triggers the execution of the `acquireSample` function every time the `samplingInterval` has passed.
This function obtains a sample from the performance counter using `NextValue` method and triggers the `SampleAcquired` event of the `SampleRecorder.ISensor<_>` interface.
Interpolation is simple in this case, the only thing we need to care about is the conversion of the interpolation factor `fac` to our sample data type `float32`.

Before using the recorder, let us test our sensor by instantiating it with a sampling interval of 100 ms and the performance counter for CPU load.
Then we attach a function that prints the current measurement every time the `ISensor<_>.SampleAcquired` event is triggered.
*)
let cpuSensor = new PerfSensor ("Processor", "% Processor Time", "_Total", 100.)
let evtHandle = (cpuSensor :> SampleRecorder.ISensor<_>).SampleAcquired.Subscribe (fun smpl ->
    printfn "The current CPU load is %6.2f %%." smpl
)
Async.Sleep 2000 |> Async.RunSynchronously  // wait for a few seconds
evtHandle.Dispose()                         // detach event handler
(**
After waiting for two seconds we detach our event handler to stop the continuous output.

This will print something similar to

    The current CPU load is   0.00 %.
    The current CPU load is   7.67 %.
    The current CPU load is   0.00 %.
    The current CPU load is   0.00 %.
    The current CPU load is   0.00 %.
    The current CPU load is   2.96 %.
    The current CPU load is   2.96 %.
    The current CPU load is   7.11 %.
    The current CPU load is   0.00 %.
    The current CPU load is   0.00 %.
    The current CPU load is   5.33 %.
    The current CPU load is   4.99 %.
    The current CPU load is   4.63 %.
    The current CPU load is   1.81 %.
    The current CPU load is   0.71 %.
    The current CPU load is   2.84 %.
    The current CPU load is   0.00 %.
    The current CPU load is   0.00 %.

Let us instantiate a second sensor measuring available memory at an interval of 70 ms.
*)
let memSensor = new PerfSensor ("Memory", "Available MBytes", "", 70.)
(**

Using the recorder
------------------

The sample recorder is provided by the `SampleRecorder.Recorder<'T>` type where `'T` must be a (user-defined) record type that stores the data of *all* recorded sensors and has an additional field for the sample time of type `float`.
The sample time is stored in seconds since the start of the recording.
Continuing with our example we define the sample record `LoadSample` that captures CPU load and available memory.
*)
type LoadSample = {
    Time:   float       // time in seconds since start of recording
    Cpu:    PerfSample
    Mem:    PerfSample
}
(**
Also note, that no additional fields may be present in the sample type.
The constructor of the recorder takes a list of sensors to record from as its only argument.
The sensors must be listed in the same order as they appear in the sample type `'T`.
We can now instantiate the recorder.
*)
let recorder = 
    SampleRecorder.Recorder<LoadSample> [cpuSensor; memSensor]
(**
During instantiation the recorder automatically subscribes to the sample events of your sensors.

We can control recording by calling the  `recorder.Start` and `recorder.Stop` methods.
Let us record for two seconds.
*)
recorder.Start ()
Async.Sleep 2000 |> Async.RunSynchronously
recorder.Stop ()
(**
Recording statistics can be outputted by calling the `recorder.PrintStatistics` methods.
*)
recorder.PrintStatistics ()
(**
This will print individual sensor statistics.
For example, here we obtain:

    Channel FSI_0003+PerfSensor:
             first sample time: 0.017 s  last sample time: 1.950 s
             average interval: 0.102 s  number of samples: 19  sampling rate: 9.8 Hz
    Channel FSI_0003+PerfSensor:
             first sample time: 0.017 s  last sample time: 2.022 s
             average interval: 0.080 s  number of samples: 25  sampling rate: 12.5 Hz

### Getting the recorded samples
Use the `recorder.GetSamples` method to obtain the recorded data samples on a common timeline.
The function takes an argument that can either be 

  * `Some interval`. In this case the specified interval is used on the common timeline.
  * `None`. In this case the average sampling interval of the fastest sensor is used.

Linear interpolation is performed to calculate the sensor data on the common timeline.
*)
let smpls = recorder.GetSamples None |> Array.ofSeq
(**
The function returns a sequence of the sample type; that is `LoadSample` in our case.
We convert the sequence to an array for efficient storage.

We can now print the recorded data.
*)
for smpl in smpls do
    printfn "At %6.3f s the CPU load was %6.2f %% and %7.0f MB of memory were available."
        smpl.Time smpl.Cpu smpl.Mem
(**
The output will be similar to

    At  0.017 s the CPU load was   0.00 % and   19729 MB of memory were available.
    At  0.097 s the CPU load was   5.09 % and   19729 MB of memory were available.
    At  0.177 s the CPU load was   2.45 % and   19729 MB of memory were available.
    At  0.257 s the CPU load was   0.34 % and   19729 MB of memory were available.
    At  0.338 s the CPU load was   1.41 % and   19729 MB of memory were available.
    At  0.418 s the CPU load was   7.68 % and   19729 MB of memory were available.
    At  0.498 s the CPU load was   3.00 % and   19729 MB of memory were available.
    At  0.578 s the CPU load was   2.91 % and   19729 MB of memory were available.
    At  0.658 s the CPU load was   6.69 % and   19729 MB of memory were available.
    At  0.739 s the CPU load was   1.88 % and   19729 MB of memory were available.
    At  0.819 s the CPU load was   3.83 % and   19729 MB of memory were available.
    At  0.899 s the CPU load was   8.06 % and   19729 MB of memory were available.
    At  0.979 s the CPU load was   1.73 % and   19729 MB of memory were available.
    At  1.060 s the CPU load was   3.50 % and   19729 MB of memory were available.
    At  1.140 s the CPU load was   3.85 % and   19729 MB of memory were available.
    At  1.220 s the CPU load was   0.69 % and   19729 MB of memory were available.
    At  1.300 s the CPU load was   3.67 % and   19729 MB of memory were available.
    At  1.380 s the CPU load was   4.93 % and   19729 MB of memory were available.
    At  1.461 s the CPU load was   4.15 % and   19729 MB of memory were available.
    At  1.541 s the CPU load was   0.36 % and   19729 MB of memory were available.
    At  1.621 s the CPU load was   5.03 % and   19729 MB of memory were available.
    At  1.701 s the CPU load was   3.36 % and   19729 MB of memory were available.
    At  1.781 s the CPU load was   0.00 % and   19729 MB of memory were available.
    At  1.862 s the CPU load was   0.00 % and   19729 MB of memory were available.


### Clearing the recorder's memory
The sample recorder can be reused without having to create a new instance by calling the `recorder.Clear` method.
*)
recorder.Clear ()

(**

Usage with BRML drivers
-----------------------
The [BioTac](biotac.html) and [XY table](xytable.html) drivers in this library already implement the sensor interface.
Thus they can be directly used with the sample recorder.
It is also recommended that you implement the recorder interface for your custom-built drivers.


Conclusion
----------
The generic data recorder provides a simple, efficient way of obtaining data from multiple sensors and synchronizing them on a common time line.
After recording, samples are returned in a user-defined sample record that combines the data from all sensors.

*)




