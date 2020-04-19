//
//  ViewController.m
//  Stride iOS Relay
//
// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//

#import "ViewController.h"

@implementation ViewController

@synthesize Address;
@synthesize StartStopButton;
@synthesize LogView;

- (void)viewDidLoad {
    [super viewDidLoad];

    // Do any additional setup after loading the view.
    mBundleFolder = [[NSBundle mainBundle] resourcePath];
    
    NSString* defAddress = [[NSUserDefaults standardUserDefaults] stringForKey:@"Address"];
    if(defAddress)
    {
        Address.stringValue = defAddress;
    }
}

- (void)setRepresentedObject:(id)representedObject {
    [super setRepresentedObject:representedObject];

    // Update the view, if already loaded.
}

-(void)viewWillDisappear {
    if(mRunning)
    {
        [mTask terminate];
        
        mRunning = NO;
        
        StartStopButton.title = @"Start";
    }
    
    _exit(0);
}

- (IBAction)StartStop:(id)sender {
    if(mRunning)
    {
        [mTask terminate];
        
        mRunning = NO;
        
        StartStopButton.title = @"Start";
    }
    else
    {
        mTask = [[NSTask alloc] init];
        mTask.launchPath = @"/usr/bin/python";
        NSString* cmd = [NSString stringWithFormat:@"%@/stride-ios-relay.py", mBundleFolder];
        mTask.arguments = @[@"-u", cmd, Address.stringValue];
        
        mPipe = [[NSPipe alloc] init];
        mTask.standardOutput = mPipe;
        
        [[mPipe fileHandleForReading] waitForDataInBackgroundAndNotify];
        
        //3
        [[NSNotificationCenter defaultCenter] addObserverForName:NSFileHandleDataAvailableNotification object:[mPipe fileHandleForReading] queue:nil usingBlock:^(NSNotification *notification){

            NSData *output = [[mPipe fileHandleForReading] availableData];
            if(output.length <= 0)
            {
                [[mPipe fileHandleForReading] waitForDataInBackgroundAndNotify];
                return;
            }
            
            NSString *outStr = [[NSString alloc] initWithData:output encoding:NSUTF8StringEncoding];
            //5
            self.LogView.string = [self.LogView.string stringByAppendingString:[NSString stringWithFormat:@"\n%@", outStr]];
            
            // Scroll to end of outputText field
            NSRange range;
            range = NSMakeRange([self.LogView.string length], 0);
            [self.LogView scrollRangeToVisible:range];

            //this has happend before and will happen again...
            [[mPipe fileHandleForReading] waitForDataInBackgroundAndNotify];
        }];
        
        [mTask launch];
        
        mRunning = YES;
        
        [[NSUserDefaults standardUserDefaults] setValue:Address.stringValue forKey:@"Address"];
        
        StartStopButton.title = @"Stop";
    }
}

@end
