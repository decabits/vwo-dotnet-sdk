# VWO .NET SDK

[![NuGet](https://img.shields.io/nuget/v/VWO.Sdk.svg?style=plastic)](https://www.nuget.org/packages/VWO.Sdk/)
[![Build Status](http://img.shields.io/travis/wingify/vwo-dotnet-sdk/master.svg?style=flat)](http://travis-ci.org/wingify/vwo-dotnet-sdk)
[![Coverage Status](https://img.shields.io/coveralls/wingify/vwo-dotnet-sdk.svg)](https://coveralls.io/r/wingify/vwo-dotnet-sdk)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](http://www.apache.org/licenses/LICENSE-2.0)

This open source library allows you to A/B Test your Website at server-side.

## Requirements

* Works with NetStandard: 2.0 onwards.

## Installation

```bash
PM> Install-Package VWO.Sdk
```

## Basic usage

**Using and Instantiation**

```c#
using VWOSdk;

Settings settingsFile = VWO.GetSettings(accountId, sdkKey);     //  Fetch settingsFile from VWO.
IVWOClient vwoClient = VWO.Instantiate(settingsFile);           //  Create VWO Client to user APIs.
```

**API usage**

```c#
// Activate API
string variationName = vwoClient.Activate(campaignKey, userId);

// GetVariation API
string variationName = vwoClient.GetVariation(campaignKey, userId);

// Track API
// For CUSTOM CONVERSION Goal
bool isSuccessful = vwoClient.Track(campaignKey, userId, goalIdentifier);

// For Revenue Goal
bool isSuccessful = vwoClient.Track(campaignKey, userId, goalIdentifier, revenueValue);
```

**Configure Log Level**

```c#
VWO.Configure(LogLevel.DEBUG);
```

**Implement and Configure Custom Logger** - implement your own logger class

```c#
using VWOSdk;

public class CustomLogWriter : ILogWriter
{
    public void WriteLog(LogLevel logLevel, string message)
    {
        // ...write to file or database or integrate with any third-party service
    }
}

//  Configure Custom Logger with SDK.
VWO.Configure(new CustomLogWriter());
```

**User Profile Service**

```c#
using VWOSdk;

public class UserProfileService : IUserProfileService
{
    public UserProfileMap Lookup(string userId)
    {
        // ...code here for getting data
        // return data
    }

    public void Save(UserProfileMap userProfileMap)
    {
        // ...code to persist data
    }
}


var settingsFile = VWO.GetSettings(VWOConfig.AccountId, VWOConfig.SdkKey);

//  Provide UserProfileService instance while vwoClient Instantiation.
var vwoClient = VWO.Instantiate(settingsFile, userProfileService: new UserProfileService());
```

## Documentation

Refer [Official VWO Documentation](https://developers.vwo.com/reference#server-side-introduction)

## Demo NetStandard application

[vwo-dotnet-sdk-example](https://github.com/wingify/vwo-dotnet-sdk-example)

## Setting Up development environment

```bash
chmod +x start-dev.sh; ./start-dev;
```

It will install the git-hooks necessary for commiting and pushing the code. Commit-messages follow a [guideline](https://github.com/angular/angular/blob/master/CONTRIBUTING.md#-commit-message-guidelines). All test cases must pass before pushing the code.

## Running Unit Tests

```bash
dotnet test
```

## Third-party Resources and Credits

Refer [third-party-attributions.txt](https://github.com/wingify/vwo-dotnet-sdk/blob/master/third-party-attributions.txt)

## Authors

* Main Contributor - [Sidhant Gakhar](https://github.com/sidhantgakhar)
* Repo health maintainer - [Varun Malhotra](https://github.com/softvar)([@s0ftvar](https://twitter.com/s0ftvar))

## Changelog

Refer [CHANGELOG.md](https://github.com/wingify/vwo-dotnet-sdk/blob/master/CHANGELOG.md)

## Contributing

Please go through our [contributing guidelines](https://github.com/wingify/vwo-dotnet-sdk/CONTRIBUTING.md)

## Code of Conduct

[Code of Conduct](https://github.com/wingify/vwo-dotnet-sdk/blob/master/CODE_OF_CONDUCT.md)

## License

[Apache License, Version 2.0](https://github.com/wingify/vwo-dotnet-sdk/blob/master/LICENSE)

Copyright 2019-2020 Wingify Software Pvt. Ltd.
