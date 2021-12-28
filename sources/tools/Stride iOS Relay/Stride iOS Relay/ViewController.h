//
//  ViewController.h
//  Stride iOS Relay
//
// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//

#import <Cocoa/Cocoa.h>

@interface ViewController : NSViewController
{
    BOOL mRunning;
    pid_t mPid;
    NSString* mBundleFolder;
    NSTask* mTask;
    NSPipe* mPipe;
}

@property (weak) IBOutlet NSTextField *Address;
@property (weak) IBOutlet NSButton *StartStopButton;
@property (unsafe_unretained) IBOutlet NSTextView *LogView;

//http://www.raywenderlich.com/36537/nstask-tutorial

@end

