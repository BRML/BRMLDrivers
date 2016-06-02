#if RELEASE
// WORKAROUND: I do not know why, but if the following is not included in the search path,
//             IntelliSense in Visual Studio stops working.
#I "../DeepNet/DeepNet/bin/Release"
#I "BRMLDrivers/bin/Release"
#else
// WORKAROUND: I do not know why, but if the following is not included in the search path,
//             IntelliSense in Visual Studio stops working.
#I "../DeepNet/DeepNet/bin/Debug"
#I "BRMLDrivers/bin/Debug"
#endif

#r "SampleRecorder.dll"
#r "BRMLDrivers.dll"

