
# Example Empty project for DPB4 (.NET 8 Core)

**NOTE:** DPB will load all types that inherit from `IBase` from an assembly, so `IBot`, `IContent`, `IPlugin`, `IRoutine`, `IPlayerMover`

### Detail
- `public interface IBot : IConfigurable, IMessageHandler, IStartStopEvents, ITickEvents, IAuthored, ILogicProvider, IBase`
- `public interface IContent : IConfigurable, IMessageHandler, IAuthored, ILogicProvider, IBase` 
- `public interface IPlayerMover : IConfigurable, IStartStopEvents, ITickEvents, IAuthored, ILogicProvider, IMessageHandler, IBase`
- `public interface IPlugin : IConfigurable, IMessageHandler, IAuthored, IBase, IEnableable, ILogicProvider, IBase`
- `public interface IRoutine : IConfigurable, IMessageHandler, IStartStopEvents, ITickEvents, IAuthored, IBase, ILogicProvider, IBase`

This means that you **CAN** and most probably **SHOULD** put those types into one **assembly** to make references easier for yourself.

## Example Assembly Structure
```
MyAssembly.dll
│~~~~
├── MyBot.cs : IBot
├── MyPlugin1.cs : IPlugin
├── MyPlugin2.cs : IPlugin
└── MyContent.cs : IContent
```
This allows you to make cross-references between your classes easier, without relying on reflection or message system (although you can still use them)

# .NET8 changes for DPB Content

## Type of project
New name of project template is called **WPF Class Library**. In Visual Studio 2022, you can find it under C#->Windows->Desktop
## WPF and Windows Forms
This may sound counter-intuitive, but you **have to** reference both WPF and Windows Forms.
Currently, it is done via directly editing .csproj file, adding 
```xml
<UseWPF>true</UseWPF>
<UseWindowsForms>true</UseWindowsForms>
```
 to projects' property group (done in this project, see `EmptyProject.csproj`). Do not forget to unload/reload the project in your IDE if it does not seem to work.
 
## Loading content from source

This feature is not planned due to the cumbersome amount of work involved.

## Content load paths

Currently, DPB looks for `*.dll` files under **subfolders** of `Plugins` directory. You are expected to name your Dll file similar to the folder's name, to not mess up the dependency loading.

```
Plugins\
│~~~~
├── MyPlugin1\MyPlugin1.dll <--- correct
└── SuperBot2\irrelevant_assembly_name.dll <--- incorrect
```