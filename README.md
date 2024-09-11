
# Command Alias Plugin

## Description

This plugins allows you to setup custom command aliases for ASF commands and even groups of commands.
It's not very straightforward in configuration, but very flexible and versatile, so if you'll manage to configure it - your efforts will be well rewarded.
Read carefully this readme, and don't worry if you don't understand it from the first time - there are examples below, hopefully they will help you a bit!

## Installation

Work of this plugin is only guaranteed with [generic ASF variant](https://github.com/JustArchiNET/ArchiSteamFarm/wiki/Setting-up#generic-setup), so if you use any other variant you are encouraged to switch to generic. Please don't report any issues if you are running some other variant, like `win-x64`, etc.
After that, download `CommandAliasPlugin.zip` from [latest release page](https://github.com/Rudokhvist/CommandAliasPlugin/releases/latest), create new folder (called, for example, `CommandAliasPlugin`) inside of ASF's `plugins` folder, and unpack downloaded archive there.
Now you have to configure aliases you want in `ASF.json` file in `config` folder of your ASF installation, as described below. If your ASF is configured to not restart after global config edits - restart it manually, and if you did everything right - your aliases should work now.


## Configuration
This plugin adds one parameter of complex structure to global config parameters (file ASF.json). This parameter should contain array of objects to define your aliases.
General structure of this parameter looks like this:


```
 "Aliases" : [
	{
           "Alias":"string",
           "ParamNumber": byte,
           "Access": "string",
           "Commands": [
              "string", "string", "string", ...
           ],
           "AllResponses": bool
        },
        ...
 ]
```

Let's walk through all fields here. `Aliases` is an additional parameter you add, it's an array of objects, where each object describes one alias.
You may need to set two or more objects for one alias command, if you want it to be flexible as regular ASF commands in regard to number of parameters, but in most simple case of alias without parameters only one object will be enough.

`Alias` is required parameter of type `string`, representing your new command. You don't need to add standart ASF prefix here ("!" by default), just the command itself.

`ParamNumber` is a required parameter of type `byte` that sets what number of parameters you want your new command to have. Please be aware, that for example "list of bots" is still one parameter. Read [ASF wiki](https://github.com/JustArchiNET/ArchiSteamFarm/wiki/Commands) to get better idea what is considered one parameter in terms of ASF. Also, please be aware that if you will use your command with more parameters than specified here, all extra parameters will be concatenated and considered part of last parameter (same as with list parameters of regular asf commands).

`Access` is optional parameter of type `string` that sets access rights the user must have to use your alias. It should be one of following values, [defined in ASF](https://github.com/JustArchiNET/ArchiSteamFarm/wiki/Configuration#steamuserpermissions):
```
	None
	FamilySharing
	Operator
	Master
	Owner
```
If this parameter is not specified, or the specified string does not match any of access rights above, `Master` will be used by default. Please note, that access rights of commands that your alias will actually execute does not matter, and only the access rights specified here will be required. That allows, in particular, to create alias for some ASF command that can be run only by `Owner` with rights `None`, allowing **EVERYONE** to execute this command. Be extremely careful with this parameter, and unless you know what you're doing - never specify it explicitly and let plugin use the default value!
   
`Commands` is a required parameter of type `array of strings` that will define, what command(s) will be actually run instead of your alias. Each string should contain one asf command and optionally - up to `ParamNumber` placeholders for parameters, in format `command {0} {1} {2}`. Each placeholder will be replaced with actual parameter specified with your new alias command, according to number (starting from 0, so first parameter is {0}). So, if you have two commands, one of which needs 3 parameters, and another only second one, your commands field will look like `["command1 {0} {1} {2}", "command2 {1}"]`. If, for example, you need to run two commands, with different parameters, you can specify `"ParamNumber":2` and `"Commands":["command1 {0}","command2 {1}]` and you're done! Please note, that if you will use placeholder with number bigger than `ParamNumber-1`, it won't work.
You can use regular ASF commands here, commands from other plugins (even from multiple different plugins in one alias), and even other aliases.

Apart from that, you can use special pseudo-command `wait` with parameter of type `int` to set delay in milliseconds between commands, if needed. You can also use placeholders for this pseudo-command to provide delay time as parameter for your alias.

`AllResponses` is optional parameter of type `bool`, that will define whether you want to get responses of all your commands, or no. In the latter case you will only get message "Done" when all your commans executed, disregard of their success or failure. Default value is `false`, so if you want to see response of your commands, be sure to explicitly specify it as `"AllResponses":true`


## Example 1

Let's define an alias for command `!loot asf` as `!la`. It's the simplest possible thing: we don't need parameters, and we need to run only one command. Configuration for this case will look like this:
``` json
 "Aliases" : [
	{
           "Alias":"la",
           "ParamNumber": 0,
           "Commands": ["loot asf"],
           "AllResponses": true
        }
 ]
```
`Alias` is set to `la` - our alias command, `ParamNumber` is set to 0, since we have no parameters, `Commands` is set to array of one element, a string `loot asf` - that's the command that will be executed instead of our alias, and `AllResponses` is set to `true` so that we could see actual response. And we don't set `Access` because we're fine with default `Master` one. Easy, huh?

## Example 2

Let's define an alias for command `!2faok` that can be run with or without parameters, as `!ok`. Our config will look like this:

``` json
 "Aliases" : [
	{
           "Alias":"ok",
           "ParamNumber": 0,
           "Commands": ["2faok"],
           "AllResponses": true
        },
	{
           "Alias":"ok",
           "ParamNumber": 1,
           "Commands": ["2faok {0}"],
           "AllResponses": true
        }

 ]
```

Ok, that's a bit more complex. We want command to take a list of bots, but want it to be optional (so that if you message it directly to some bot - it will work for this bot without explicitly specifying it's name, same as original `!2faok` works).
So, we need two objects with same `Alias` of `"ok"` (alias that we want to use), but with different `ParamNumber` - one with 0 and one with 1 (because list of bots is still one parameter).
In both cases we want to see actual response of command, so we set `AllResponses` to true, and we are fine with `Master` access rights, so we don't specify `Access` in both cases.
In object with `"ParamNumber": 0` we set `Commands` to just `["2faok"]`, since we don't need any parameters here.
But in object with `"ParamNumber": 1` we need to use a placeholder, so value for `Commands` will be `["2faok {0}"]`. This way, whatever we put after our alias `ok` will be set after actual command `2faok`.

## Example 3

Let's define an alias named `rc` (short for "reconnect"), that will work with a list of bots or without a parameter, that will `stop` bot(s), and then `start` bot(s) again. Let's look at config:
``` json
 "Aliases" : [
	{
           "Alias":"rc",
           "ParamNumber": 0,
           "Commands": [
             "stop",
             "wait 500",
             "start",
             "wait 10000"
           ],
        },
	{
           "Alias":"rc",
           "ParamNumber": 1,
           "Commands": [
             "stop {0}",
             "wait 500",
             "start {0}",
             "wait 10000"
           ],
        }

 ]
```

Same as abowe, we need two objects. We're okay with default access of `Master`, so we don't specify `Access`, and we don't care about bot response, so we don't specify `AllResponses` too.
Same as with [example 2](#example-2) above, for alias with parameters we specify a placeholder in commands, and for alias without parameters - we don't.
We need to run two consequtive commands, so we should put to `Commands` array of at least two strings, with "stop" and "start" commands accordingly. But apart from that, we add a pseudo-command `wait 500` between stop and start, to make sure that bot has time to properly stop before we attemt to start it again, and commad `wait 10000` to wait 10 seconds after start before reporting "Done!", since bot start can take a lot of time.

# GOOD LUCK!
